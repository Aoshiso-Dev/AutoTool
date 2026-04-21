using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Runtime.Diagnostics;
using AutoTool.Automation.Runtime.MacroFactory;
using System.Text.RegularExpressions;

namespace AutoTool.Desktop.ViewModel;

/// <summary>
/// マクロ編集パネルの状態と操作を管理する ViewModel です。
/// </summary>
public partial class MacroPanelViewModel
{
    private void HandleStartCommand(ICommand command)
    {
        if (IsSyntheticRootLoop(command))
        {
            return;
        }

        var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
        var commandName = command.GetType().Name.Replace("Command", string.Empty);
        var commandType = command.GetType().FullName ?? command.GetType().Name;

        var settingDict = command.Settings.GetType().GetProperties()
            .ToDictionary(x => x.Name, x => x.GetValue(command.Settings, null));
        var detailedSettings = string.Join(", ", settingDict.Select(s => $"{s.Key}={FormatLogValue(s.Value)}"));

        _logWriter.Write(
            "START",
            $"Line={lineNumber.Trim()}",
            $"Command={commandName}",
            $"CommandType={commandType}",
            $"Settings=[{detailedSettings}]");

        OnUiThread(() =>
        {
            var commandItem = _listPanel.GetItem(command.LineNumber);
            if (commandItem is not null)
            {
                commandItem.Progress = 0;
                commandItem.IsRunning = true;
            }
        });
    }

    private void HandleFinishCommand(ICommand command)
    {
        if (IsSyntheticRootLoop(command))
        {
            return;
        }

        var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
        var commandName = command.GetType().Name.Replace("Command", string.Empty);

        _logWriter.Write(
            "FINISH",
            $"Line={lineNumber.Trim()}",
            $"Command={commandName}",
            "Status=Completed");

        OnUiThread(() =>
        {
            var commandItem = _listPanel.GetItem(command.LineNumber);
            if (commandItem is not null)
            {
                commandItem.Progress = 0;
                commandItem.IsRunning = false;
            }
        });
    }

    private void HandleDoingCommand(ICommand command, string detail, CommandLogPayload? payload = null)
    {
        if (IsSyntheticRootLoop(command))
        {
            return;
        }

        var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
        var commandName = command.GetType().Name.Replace("Command", string.Empty);
        var uiLineLabel = FormatUiLineLabel(command.LineNumber);
        var uiCommandLabel = GetUiCommandLabel(command);
        var normalizedDetail = payload switch
        {
            ProcessOutputLogPayload processOutput => $"{(processOutput.IsError ? "[stderr]" : "[stdout]")} {processOutput.Text}",
            _ => NormalizeLine(detail)
        };

        _logWriter.Write(
            "DOING",
            $"Line={lineNumber.Trim()}",
            $"Command={commandName}",
            $"Detail={normalizedDetail}");

        var briefDetail = ToUiSummary(normalizedDetail);
        OnUiThread(() => _logPanel.WriteLog(uiLineLabel, uiCommandLabel, briefDetail));
    }

    private void HandleUpdateProgress(ICommand command, int progress)
    {
        OnUiThread(() =>
        {
            var commandItem = _listPanel.GetItem(command.LineNumber);
            if (commandItem is not null)
            {
                commandItem.Progress = progress;
            }
        });
    }

    public async Task Run()
    {
        var listItems = _listPanel.CommandList.Items;

        try
        {
            var macro = _macroFactory.CreateMacro(listItems) as LoopCommand;
            if (macro is null)
            {
                throw new InvalidOperationException("マクロの作成に失敗しました。");
            }

            _cts = new();
            await macro.Execute(_cts.Token);
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            var isCancellation = _cts is { Token.IsCancellationRequested: true };
            if (!isCancellation)
            {
                AppendRuntimeErrorLog(ex);
                var message = BuildUserFriendlyErrorMessage(ex);
                OnUiThread(() => _notifier.ShowError(message, "エラー"));
            }
        }
        finally
        {
            OnUiThread(() =>
            {
                foreach (var item in listItems.Where(x => x.IsRunning))
                {
                    item.IsRunning = false;
                }

                foreach (var item in _listPanel.CommandList.Items)
                {
                    item.Progress = 0;
                }

                _cts?.Dispose();
                _cts = null;
                SetRunningState(false);
            });
        }
    }

    private bool ValidateBeforeRun()
    {
        var report = BuildPreflightReport();
        ApplyPreflightReport(report);

        if (report.BlockingCount <= 0)
        {
            return true;
        }

        OnUiThread(() =>
        {
            IsPreflightPanelOpen = true;
            _logPanel.WriteLog(string.Empty, "システム", $"実行前チェックで {report.BlockingCount} 件の要修正項目が見つかったため、実行を中止しました。");
            _notifier.ShowWarning(
                $"実行前チェックで {report.BlockingCount} 件の要修正項目が見つかりました。\n一覧を確認して修正後に再実行してください。",
                "実行前チェック");
        });

        return false;
    }

    private PreflightValidationReport BuildPreflightReport()
    {
        var items = GetEffectivelyEnabledItems(_listPanel.CommandList.Items).ToList();
        List<PreflightIssueItem> issues = [];

        if (items.Count == 0)
        {
            issues.Add(new PreflightIssueItem(
                Level: "要修正",
                Line: "-",
                CommandName: "システム",
                PropertyName: "コマンド構成",
                Message: "有効なコマンドがありません。1つ以上追加してください。"));

            return new PreflightValidationReport(items.Count, issues);
        }

        foreach (var item in items)
        {
            if (item is not ICommandSettings settings)
            {
                continue;
            }

            var commandName = CommandListItem.GetDisplayNameForType(item.ItemType);
            var lineLabel = item.LineNumber > 0 ? FormatUiLineLabel(item.LineNumber) : "-";
            var validationIssues = CommandSettingsValidator.GetIssues(
                settings,
                _pathResolver,
                includeExistenceChecks: true);

            foreach (var issue in validationIssues)
            {
                issues.Add(new PreflightIssueItem(
                    Level: "要修正",
                    Line: lineLabel,
                    CommandName: commandName,
                    PropertyName: ToUserPropertyName(issue.PropertyName),
                    Message: $"[{issue.Code}] {NormalizeLine(issue.Message)}",
                    LineNumber: item.LineNumber,
                    CommandItem: item));
            }
        }

        return new PreflightValidationReport(items.Count, issues);
    }

    private void ApplyPreflightReport(PreflightValidationReport report)
    {
        OnUiThread(() =>
        {
            PreflightIssues.Clear();
            foreach (var issue in report.Issues)
            {
                PreflightIssues.Add(issue);
            }

            _listPanel.SetValidationErrorItems(
                report.Issues
                    .Where(static x => x.Level == "要修正" && x.CommandItem is not null)
                    .Select(static x => x.CommandItem!));

            var now = _timeProvider.GetLocalNow();
            PreflightSummary = report.BlockingCount > 0
                ? $"実行前チェック: 要修正 {report.BlockingCount} 件 / 対象 {report.TargetCount} コマンド ({now:yyyy/MM/dd HH:mm:ss})"
                : $"実行前チェック: 問題なし / 対象 {report.TargetCount} コマンド ({now:yyyy/MM/dd HH:mm:ss})";
        });
    }

    private static void OnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = dispatcher.BeginInvoke(action);
        }
    }

    private string BuildUserFriendlyErrorMessage(Exception ex)
    {
        var validationError = FindCommandValidationException(ex);
        if (validationError is not null)
        {
            var line = validationError.LineNumber > 0 ? $"行 {validationError.LineNumber}" : string.Empty;
            var propertyHint = string.IsNullOrWhiteSpace(validationError.PropertyName)
                ? string.Empty
                : $"\n項目: {ToUserPropertyName(validationError.PropertyName)}";
            var commandName = ResolveCommandName(validationError);

            return $"コマンド設定が正しくありません。\n{line}\nコマンド: {commandName}{propertyHint}\nコード: {validationError.ErrorCode}\n詳細: {validationError.Message}";
        }

        var creationError = FindCommandCreationException(ex);
        if (creationError is not null)
        {
            var line = creationError.LineNumber.HasValue ? $"行 {creationError.LineNumber}" : string.Empty;
            var commandName = ToUserCommandName(creationError.ItemType);
            var commandHint = string.IsNullOrWhiteSpace(commandName) ? string.Empty : $"\n対象コマンド: {commandName}";
            return creationError switch
            {
                PairMismatchException =>
                    $"対応する開始/終了コマンドの組み合わせが不正です。\n{line}{commandHint}",
                EmptyStructureException =>
                    $"複合コマンドの中身が空です。\n{line}{commandHint}",
                UnsupportedCommandTypeException =>
                    $"未対応のコマンドです。\n{line}{commandHint}",
                _ =>
                    $"コマンド生成時にエラーが発生しました。\n{line}{commandHint}\n詳細: {ReplaceItemTypeNames(creationError.Message)}"
            };
        }

        var message = ReplaceItemTypeNames(ex.Message);
        var rootCause = ReplaceItemTypeNames(ex.GetBaseException().Message);
        return message == rootCause
            ? message
            : $"{message}\n原因: {rootCause}";
    }

    private static CommandCreationException? FindCommandCreationException(Exception ex)
    {
        Exception? current = ex;
        while (current is not null)
        {
            if (current is CommandCreationException creationException)
            {
                return creationException;
            }
            current = current.InnerException;
        }

        return null;
    }

    private static CommandValidationException? FindCommandValidationException(Exception ex)
    {
        Exception? current = ex;
        while (current is not null)
        {
            if (current is CommandValidationException validationException)
            {
                return validationException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private void AppendRuntimeErrorLog(Exception ex)
    {
        var validationError = FindCommandValidationException(ex);
        if (validationError is not null)
        {
            var lineLabel = validationError.LineNumber > 0
                ? FormatUiLineLabel(validationError.LineNumber)
                : string.Empty;

            _logPanel.WriteLog(
                lineLabel,
                ResolveCommandName(validationError),
                $"エラー [{validationError.ErrorCode}] {NormalizeLine(validationError.Message)}");

            foreach (var guide in BuildRecoveryGuideMessages(validationError))
            {
                _logPanel.WriteLog(lineLabel, ResolveCommandName(validationError), guide);
            }
            return;
        }

        var normalizedMessage = NormalizeLine(ExceptionDetailsFormatter.GetMostRelevantMessage(ex));
        _logPanel.WriteLog(string.Empty, "システム", $"エラー: {normalizedMessage}");

        foreach (var guide in BuildRecoveryGuideMessages(ex))
        {
            _logPanel.WriteLog(string.Empty, "システム", guide);
        }
    }

    private static IReadOnlyList<string> BuildRecoveryGuideMessages(CommandValidationException validationError)
    {
        var propertyName = ToUserPropertyName(validationError.PropertyName);
        var lineHint = validationError.LineNumber > 0
            ? $"{FormatUiLineLabel(validationError.LineNumber)} を選択して設定を修正してください。"
            : "該当コマンドの設定を見直してください。";

        var recovery = validationError.ErrorCode switch
        {
            CommandValidationErrorCodes.ImagePathRequired => "対処: 画像ファイルを再選択してください。",
            CommandValidationErrorCodes.ImagePathNotFound => "対処: 画像ファイルの保存場所を確認し、必要なら取り直してください。",
            CommandValidationErrorCodes.ProgramPathRequired => "対処: 実行ファイルのパスを指定してください。",
            CommandValidationErrorCodes.ProgramPathNotFound => "対処: 実行ファイルの存在と実行権限を確認してください。",
            CommandValidationErrorCodes.ModelPathRequired => "対処: AIモデルファイルのパスを指定してください。",
            CommandValidationErrorCodes.ModelPathNotFound => "対処: AIモデルファイルの配置先を確認してください。",
            CommandValidationErrorCodes.TessdataPathNotFound => "対処: tessdata フォルダのパスを再指定してください。",
            CommandValidationErrorCodes.TessdataDataMissing => "対処: tessdata フォルダに *.traineddata を配置してください。",
            CommandValidationErrorCodes.TimeoutOutOfRange => "対処: タイムアウトを 0 以上に設定してください。",
            CommandValidationErrorCodes.IntervalOutOfRange => "対処: 検索間隔を 0 以上に設定してください。",
            CommandValidationErrorCodes.WaitOutOfRange => "対処: 待機時間を 0 以上に設定してください。",
            CommandValidationErrorCodes.RetryCountOutOfRange => "対処: リトライ回数を 1 以上に設定してください。",
            CommandValidationErrorCodes.RetryIntervalOutOfRange => "対処: リトライ間隔を 0 以上に設定してください。",
            _ => $"対処: {propertyName} の値を見直してください。"
        };

        return
        [
            $"復旧ガイド: 項目={propertyName}",
            $"原因候補: {NormalizeLine(validationError.Message)}",
            recovery,
            $"次アクション: {lineHint}"
        ];
    }

    private static IReadOnlyList<string> BuildRecoveryGuideMessages(Exception ex)
    {
        var cause = NormalizeLine(ExceptionDetailsFormatter.GetMostRelevantMessage(ex));

        return
        [
            $"復旧ガイド: 原因候補={cause}",
            "対処: 直前に編集したコマンド設定と対象ウィンドウ状態を確認してください。",
            "次アクション: 実行前チェックを再実行し、要修正がないことを確認してから再実行してください。"
        ];
    }

    private string ResolveCommandName(CommandValidationException validationError)
    {
        if (validationError.LineNumber > 0)
        {
            var commandItem = _listPanel.GetItem(validationError.LineNumber);
            if (commandItem is not null)
            {
                var displayName = CommandListItem.GetDisplayNameForType(commandItem.ItemType);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }
            }
        }

        return ToJapaneseCommandName(validationError.CommandName);
    }

    private static string ToUserPropertyName(string propertyName)
    {
        return propertyName switch
        {
            "ImagePath" => "画像パス",
            "ModelPath" => "モデルパス",
            "ProgramPath" => "プログラムパス",
            "Threshold" => "しきい値",
            "ConfThreshold" => "信頼度しきい値",
            "IoUThreshold" => "IoUしきい値",
            "Timeout" => "タイムアウト",
            "Interval" => "間隔",
            "Wait" => "待機時間",
            "RetryCount" => "リトライ回数",
            "RetryInterval" => "リトライ間隔",
            "Width" => "幅",
            "Height" => "高さ",
            "MinConfidence" => "最小信頼度",
            "MatchMode" => "一致モード",
            "Name" => "名前",
            "Operator" => "演算子",
            "TessdataPath" => "tessdataパス",
            _ => propertyName
        };
    }

    private static string ToJapaneseCommandName(string commandName)
    {
        return commandName switch
        {
            "WaitImage" => "画像待機",
            "WaitImageDisappear" => "画像消失待機",
            "FindImage" => "画像検索",
            "ClickImage" => "画像クリック",
            "IfImageExist" => "If 画像あり",
            "IfImageNotExist" => "If 画像なし",
            "FindText" => "テキスト検索",
            "IfTextExist" => "If テキストあり",
            "IfTextNotExist" => "If テキストなし",
            "IfVariable" => "If 変数",
            "SetVariable" => "変数設定",
            "SetVariableAI" => "変数設定(AI)",
            "SetVariableOCR" => "変数設定(OCR)",
            "Execute" => "実行",
            "ClickImageAI" => "画像クリック(AI)",
            "IfImageExistAI" => "If 画像あり(AI)",
            "IfImageNotExistAI" => "If 画像なし(AI)",
            "Click" => "クリック",
            "Hotkey" => "ホットキー",
            "Wait" => "待機",
            "Loop" => "ループ",
            "Retry" => "リトライ",
            "RetryEnd" => "リトライ終了",
            _ => commandName
        };
    }

    private static string FormatLogValue(object? value)
    {
        if (value is null)
        {
            return "（null）";
        }

        return NormalizeLine(value.ToString() ?? string.Empty);
    }

    private static string NormalizeLine(string text)
    {
        return text
            .Replace("\r\n", " | ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();
    }

    private static string ToUiSummary(string detail)
    {
        var normalized = NormalizeLine(detail);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "実行中";
        }

        const int maxLength = 48;
        return normalized.Length > maxLength
            ? $"{normalized[..maxLength]}..."
            : normalized;
    }

    private static string FormatUiLineLabel(int lineNumber) => $"#{lineNumber:00}";

    private static string ToUserCommandName(string? itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
        {
            return string.Empty;
        }

        var displayName = CommandListItem.GetDisplayNameForType(itemType);
        return string.Equals(displayName, itemType, StringComparison.Ordinal)
            ? itemType
            : displayName;
    }

    private static string ReplaceItemTypeNames(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return ItemTypeNameRegex().Replace(
            text,
            static match =>
            {
                var displayName = CommandListItem.GetDisplayNameForType(match.Value);
                return string.Equals(displayName, match.Value, StringComparison.Ordinal)
                    ? match.Value
                    : displayName;
            });
    }

    [GeneratedRegex(@"\b[A-Za-z]+(?:_[A-Za-z0-9]+)+\b", RegexOptions.CultureInvariant)]
    private static partial Regex ItemTypeNameRegex();

    private string GetUiCommandLabel(ICommand command)
    {
        var commandItem = _listPanel.GetItem(command.LineNumber);
        if (commandItem is not null)
        {
            var displayName = CommandListItem.GetDisplayNameForType(commandItem.ItemType);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }
        }

        return command.GetType().Name.Replace("Command", string.Empty);
    }

    private static bool IsSyntheticRootLoop(ICommand command)
    {
        return command is LoopCommand
            && command.LineNumber <= 0
            && command.Parent is RootCommand;
    }

    private bool HasBlockingValidationIssue(ICommandListItem item)
    {
        if (!item.IsEnable || item is not ICommandSettings settings)
        {
            return false;
        }

        var issues = CommandSettingsValidator.GetIssues(
            settings,
            _pathResolver,
            includeExistenceChecks: true);
        return issues.Count > 0;
    }

    private void UpdateValidationErrorForEditedItem(ICommandListItem? beforeEdit, ICommandListItem? afterEdit)
    {
        OnUiThread(() =>
        {
            var trackedItems = _listPanel.ValidationErrorItems.ToList();
            if (beforeEdit is not null)
            {
                trackedItems.RemoveAll(x => ReferenceEquals(x, beforeEdit));
            }

            if (afterEdit is not null && HasBlockingValidationIssue(afterEdit))
            {
                trackedItems.Add(afterEdit);
            }

            _listPanel.SetValidationErrorItems(trackedItems);
        });
    }

    /// <summary>
    /// 不変前提で扱うデータをまとめ、比較やコピーを安全に行えるようにします。
    /// </summary>

    private sealed record PreflightValidationReport(int TargetCount, IReadOnlyList<PreflightIssueItem> Issues)
    {
        public int BlockingCount => Issues.Count(x => x.Level == "要修正");
    }

    private static IEnumerable<ICommandListItem> GetEffectivelyEnabledItems(IEnumerable<ICommandListItem> items)
    {
        var sortedItems = items.OrderBy(static x => x.LineNumber);
        Stack<int> disabledBlockEndLines = [];

        foreach (var item in sortedItems)
        {
            while (disabledBlockEndLines.Count > 0 && item.LineNumber > disabledBlockEndLines.Peek())
            {
                _ = disabledBlockEndLines.Pop();
            }

            if (disabledBlockEndLines.Count > 0)
            {
                continue;
            }

            if (!item.IsEnable)
            {
                if (TryGetBlockEndLine(item, out var blockEndLine) && blockEndLine > item.LineNumber)
                {
                    disabledBlockEndLines.Push(blockEndLine);
                }

                continue;
            }

            yield return item;
        }
    }

    private static bool TryGetBlockEndLine(ICommandListItem item, out int endLine)
    {
        endLine = item switch
        {
            IIfItem { Pair.LineNumber: > 0 } x when x.Pair!.LineNumber > x.LineNumber => x.Pair.LineNumber,
            ILoopItem { Pair.LineNumber: > 0 } x when x.Pair!.LineNumber > x.LineNumber => x.Pair.LineNumber,
            IRetryItem { Pair.LineNumber: > 0 } x when x.Pair!.LineNumber > x.LineNumber => x.Pair.LineNumber,
            _ => -1
        };

        return endLine > 0;
    }
}
