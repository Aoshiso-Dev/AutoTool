using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Panels.List.Class;
using AutoTool.Core.Diagnostics;
using AutoTool.Panels.Model.MacroFactory;
using System.Text.RegularExpressions;

namespace AutoTool.ViewModel;

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
            return;
        }

        _logPanel.WriteLog(string.Empty, "システム", $"エラー: {NormalizeLine(ExceptionDetailsFormatter.GetMostRelevantMessage(ex))}");
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
}
