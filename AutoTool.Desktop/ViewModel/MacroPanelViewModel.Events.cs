using System.Collections.Generic;
using AutoTool.Application.Assistant;
using AutoTool.Application.History.Commands;
using AutoTool.Application.Ports;
using AutoTool.Commands.Services;
using AutoTool.Commands.Threading;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Lists;
using System.Globalization;
using System.Reflection;

namespace AutoTool.Desktop.ViewModel;

/// <summary>
/// マクロ編集パネルの状態と操作を管理する ViewModel です。
/// </summary>
public partial class MacroPanelViewModel
{
    private void SubscribeToChildViewModelEvents()
    {
        _buttonPanel.RunRequested += HandleRunRequestedAsync;
        _buttonPanel.StopRequested += HandleStopRequested;
        _buttonPanel.SaveRequested += HandleSaveRequested;
        _buttonPanel.LoadRequested += HandleLoadRequested;
        _buttonPanel.ClearRequested += HandleClear;
        _buttonPanel.AddRequested += HandleAdd;
        _buttonPanel.UpRequested += HandleUp;
        _buttonPanel.DownRequested += HandleDown;
        _buttonPanel.DeleteRequested += HandleDelete;
        _buttonPanel.CopyRequested += HandleCopy;
        _buttonPanel.PasteRequested += HandlePaste;

        _listPanel.SelectedItemChanged += HandleSelectedItemChanged;
        _listPanel.ItemDoubleClicked += HandleItemDoubleClick;
        _listPanel.InteractionRequested += HandleListInteractionRequested;
        _listPanel.MoveItemRequested += HandleMoveItemRequested;
        _listPanel.DeleteRequested += HandleDelete;

        _editPanel.ItemEdited += HandleEdit;
        _favoritePanel.AddRequested += HandleFavoriteAddRequested;
        _favoritePanel.DeleteRequested += HandleFavoriteDeleteRequested;
        _favoritePanel.LoadRequested += HandleFavoriteLoadRequested;
        _favoritePanel.InsertRequested += HandleFavoriteInsertRequested;
        _logPanel.StatusMessageRequested += HandleLogPanelStatusMessageRequested;
        _variablePanel.StatusMessageRequested += HandleVariablePanelStatusMessageRequested;
        _assistantPanel.StatusMessageRequested += HandleAssistantPanelStatusMessageRequested;
        _assistantPanel.MacroGenerated += HandleAssistantPanelMacroGenerated;
    }

    private void UnsubscribeFromChildViewModelEvents()
    {
        _buttonPanel.RunRequested -= HandleRunRequestedAsync;
        _buttonPanel.StopRequested -= HandleStopRequested;
        _buttonPanel.SaveRequested -= HandleSaveRequested;
        _buttonPanel.LoadRequested -= HandleLoadRequested;
        _buttonPanel.ClearRequested -= HandleClear;
        _buttonPanel.AddRequested -= HandleAdd;
        _buttonPanel.UpRequested -= HandleUp;
        _buttonPanel.DownRequested -= HandleDown;
        _buttonPanel.DeleteRequested -= HandleDelete;
        _buttonPanel.CopyRequested -= HandleCopy;
        _buttonPanel.PasteRequested -= HandlePaste;

        _listPanel.SelectedItemChanged -= HandleSelectedItemChanged;
        _listPanel.ItemDoubleClicked -= HandleItemDoubleClick;
        _listPanel.InteractionRequested -= HandleListInteractionRequested;
        _listPanel.MoveItemRequested -= HandleMoveItemRequested;
        _listPanel.DeleteRequested -= HandleDelete;
        _editPanel.ItemEdited -= HandleEdit;
        _favoritePanel.AddRequested -= HandleFavoriteAddRequested;
        _favoritePanel.DeleteRequested -= HandleFavoriteDeleteRequested;
        _favoritePanel.LoadRequested -= HandleFavoriteLoadRequested;
        _favoritePanel.InsertRequested -= HandleFavoriteInsertRequested;
        _logPanel.StatusMessageRequested -= HandleLogPanelStatusMessageRequested;
        _variablePanel.StatusMessageRequested -= HandleVariablePanelStatusMessageRequested;
        _assistantPanel.StatusMessageRequested -= HandleAssistantPanelStatusMessageRequested;
        _assistantPanel.MacroGenerated -= HandleAssistantPanelMacroGenerated;
    }

    private Task HandleRunRequestedAsync()
    {
        _ = TryStartExecution(out _);
        return Task.CompletedTask;
    }

    private void HandleStopRequested()
    {
        _ = TryStopExecution(out _);
    }

    private void HandleSaveRequested()
    {
        _listPanel.Save();
        PublishStatusMessage("保存処理を実行しました。");
    }

    private void HandleLoadRequested()
    {
        _listPanel.Load();
        _editPanel.SetListCount(_listPanel.GetCount());
        _commandHistory?.Clear();
        PublishStatusMessage("マクロを読み込みました。");
    }

    private void HandleSelectedItemChanged(ICommandListItem? item)
    {
        ClosePreflightPanelOnly();
        _editPanel.SetItem(item);
    }

    private void HandleListInteractionRequested()
    {
        ClosePreflightPanelOnly();
    }

    private void HandleLogPanelStatusMessageRequested(string message)
    {
        PublishStatusMessage(message);
    }

    private void HandleVariablePanelStatusMessageRequested(string message)
    {
        PublishStatusMessage(message);
    }

    private void HandleAssistantPanelStatusMessageRequested(string message)
    {
        PublishStatusMessage(message);
    }

    private void HandleAssistantPanelMacroGenerated(AssistantMacroGenerationResult result)
    {
        OnUiThread(() => PreviewAssistantGeneratedMacro(result));
    }

    private void PreviewAssistantGeneratedMacro(AssistantMacroGenerationResult result)
    {
        var plan = BuildAssistantGeneratedMacroPlan(result.Commands);
        if (plan.Items.Count == 0)
        {
            PublishStatusMessage("AI生成マクロを追加できませんでした。対応していないコマンド種別が含まれています。");
            _notifier.ShowWarning(BuildAssistantMacroPreviewText(result, plan), "AI生成マクロ");
            return;
        }

        var actions = new List<AppDialogAction>
        {
            new("add", "追加", IsDefault: true),
            new("retry", "再生成"),
            new("cancel", "キャンセル", IsCancel: true)
        };
        if (HasAssistantMacroReplaceTarget())
        {
            actions.Insert(1, new("replace", "選択置換"));
        }

        var action = _appDialogService.Show(
            "AI生成マクロの確認",
            BuildAssistantMacroPreviewText(result, plan),
            actions,
            plan.Warnings.Count > 0 ? AppDialogTone.Warning : AppDialogTone.Question);

        if (string.Equals(action, "retry", StringComparison.Ordinal))
        {
            _assistantPanel.RequestGenerateMacro(result.RequestText);
            PublishStatusMessage("AI生成マクロを再生成します。");
            return;
        }

        if (!string.Equals(action, "add", StringComparison.Ordinal))
        {
            PublishStatusMessage("AI生成マクロの追加をキャンセルしました。");
            return;
        }

        if (string.Equals(action, "replace", StringComparison.Ordinal))
        {
            ReplaceSelectionWithAssistantGeneratedMacro(plan);
            return;
        }

        InsertAssistantGeneratedMacro(plan);
    }

    private AssistantGeneratedMacroPlan BuildAssistantGeneratedMacroPlan(IReadOnlyList<AssistantGeneratedMacroCommand> commands)
    {
        if (commands.Count == 0)
        {
            return new AssistantGeneratedMacroPlan([], 0, ["AI生成マクロに追加できるコマンドがありませんでした。"]);
        }

        List<ICommandListItem> itemsToAdd = [];
        List<string> warnings = [];
        var skippedCount = 0;
        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command.ItemType)
                || _commandRegistry.IsEndCommand(command.ItemType))
            {
                skippedCount++;
                warnings.Add($"未対応のコマンド種別を除外しました: {command.ItemType}");
                continue;
            }

            var createdItems = CreateItemsForAdd(command.ItemType);
            if (createdItems.Count == 0)
            {
                skippedCount++;
                warnings.Add($"追加できないコマンド種別を除外しました: {command.ItemType}");
                continue;
            }

            for (var i = 0; i < createdItems.Count; i++)
            {
                var item = createdItems[i];
                item.IsEnable = command.IsEnabled;
                item.Comment = i == 0
                    ? $"AI生成: {command.Comment}"
                    : $"AI生成: {CommandListItem.GetDisplayNameForType(command.ItemType)} の終了";
                if (i == 0)
                {
                    ApplyAssistantGeneratedParameters(item, command, warnings);
                    AddAssistantSafetyWarnings(item, command, warnings);
                    if (command.Warnings is not null)
                    {
                        warnings.AddRange(command.Warnings.Select(x => $"{CommandListItem.GetDisplayNameForType(command.ItemType)}: {x}"));
                    }
                }

                itemsToAdd.Add(item);
            }
        }

        return new AssistantGeneratedMacroPlan(itemsToAdd, skippedCount, warnings.Distinct().ToList());
    }

    private void InsertAssistantGeneratedMacro(AssistantGeneratedMacroPlan plan)
    {
        ClosePreflightPanelForListInteraction();
        if (plan.Items.Count == 0)
        {
            PublishStatusMessage("AI生成マクロを追加できませんでした。対応していないコマンド種別が含まれています。");
            return;
        }

        var targetIndex = _listPanel.SelectedLineNumber >= 0
            ? _listPanel.SelectedLineNumber + 1
            : _listPanel.GetCount();

        if (_commandHistory is not null)
        {
            var addCommand = new AddItemsCommand(
                plan.Items,
                targetIndex,
                (item, index) => _listPanel.InsertAt(index, item),
                index => _listPanel.RemoveAt(index));
            _commandHistory.ExecuteCommand(addCommand);
        }
        else
        {
            for (var i = 0; i < plan.Items.Count; i++)
            {
                _listPanel.InsertAt(targetIndex + i, plan.Items[i]);
            }
        }

        _editPanel.SetListCount(_listPanel.GetCount());
        var skippedText = plan.SkippedCount > 0 ? $"（未対応 {plan.SkippedCount} 件を除外）" : string.Empty;
        PublishStatusMessage($"AI生成マクロを {plan.Items.Count} 行追加しました。{skippedText}");
    }

    private void ReplaceSelectionWithAssistantGeneratedMacro(AssistantGeneratedMacroPlan plan)
    {
        ClosePreflightPanelForListInteraction();
        if (plan.Items.Count == 0)
        {
            PublishStatusMessage("AI生成マクロを追加できませんでした。対応していないコマンド種別が含まれています。");
            return;
        }

        var selectedItems = _listPanel.GetSelectedItems().Distinct().ToList();
        if (selectedItems.Count == 0)
        {
            var selectedItem = _listPanel.SelectedItem;
            if (selectedItem is null && _listPanel.SelectedLineNumber >= 0 && _listPanel.SelectedLineNumber < _listPanel.GetCount())
            {
                selectedItem = _listPanel.GetItem(_listPanel.SelectedLineNumber + 1);
            }

            if (selectedItem is not null)
            {
                selectedItems.Add(selectedItem);
            }
        }

        var entries = ExpandDeleteEntriesWithPairs(selectedItems);
        if (entries.Count == 0)
        {
            InsertAssistantGeneratedMacro(plan);
            return;
        }

        var targetIndex = entries.Min(x => x.Index);
        if (_commandHistory is not null)
        {
            var removeCommand = new RemoveItemsCommand(
                entries,
                (item, index) => _listPanel.InsertAt(index, item),
                index => _listPanel.RemoveAt(index));
            _commandHistory.ExecuteCommand(removeCommand);

            var addCommand = new AddItemsCommand(
                plan.Items,
                targetIndex,
                (item, index) => _listPanel.InsertAt(index, item),
                index => _listPanel.RemoveAt(index));
            _commandHistory.ExecuteCommand(addCommand);
        }
        else
        {
            foreach (var (_, index) in entries.OrderByDescending(x => x.Index))
            {
                _listPanel.RemoveAt(index);
            }

            for (var i = 0; i < plan.Items.Count; i++)
            {
                _listPanel.InsertAt(targetIndex + i, plan.Items[i]);
            }
        }

        _editPanel.SetListCount(_listPanel.GetCount());
        var skippedText = plan.SkippedCount > 0 ? $"（未対応 {plan.SkippedCount} 件を除外）" : string.Empty;
        PublishStatusMessage($"選択範囲をAI生成マクロ {plan.Items.Count} 行に置き換えました。{skippedText}");
    }

    private bool HasAssistantMacroReplaceTarget()
    {
        return _listPanel.GetSelectedItems().Count > 0 || _listPanel.SelectedLineNumber >= 0;
    }

    private string BuildAssistantMacroPreviewText(
        AssistantMacroGenerationResult result,
        AssistantGeneratedMacroPlan plan)
    {
        var targetIndex = _listPanel.SelectedLineNumber >= 0
            ? _listPanel.SelectedLineNumber + 2
            : _listPanel.GetCount() + 1;
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("次のAI生成マクロをコマンド一覧へ追加します。");
        builder.AppendLine($"依頼: {result.RequestText}");
        builder.AppendLine($"追加位置: {targetIndex}行目");
        builder.AppendLine($"追加行数: {plan.Items.Count}");
        if (plan.SkippedCount > 0)
        {
            builder.AppendLine($"除外: {plan.SkippedCount}件");
        }

        builder.AppendLine();
        builder.AppendLine("追加予定:");
        foreach (var item in plan.Items.Take(30))
        {
            builder.Append("- ")
                .Append(CommandListItem.GetDisplayNameForType(item.ItemType))
                .Append(" / ")
                .AppendLine(item.Comment);
        }

        if (plan.Items.Count > 30)
        {
            builder.AppendLine($"... 残り {plan.Items.Count - 30} 行");
        }

        if (plan.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("確認が必要な点:");
            foreach (var warning in plan.Warnings.Take(20))
            {
                builder.Append("- ").AppendLine(warning);
            }
        }

        builder.AppendLine();
        builder.Append("AI生成コマンドは実行前に設定とコメントを確認してください。");
        return builder.ToString();
    }

    private static void ApplyAssistantGeneratedParameters(
        ICommandListItem item,
        AssistantGeneratedMacroCommand command,
        ICollection<string> warnings)
    {
        if (command.Parameters is null || command.Parameters.Count == 0)
        {
            return;
        }

        foreach (var (name, rawValue) in command.Parameters)
        {
            var property = item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => x.CanWrite
                    && x.GetIndexParameters().Length == 0
                    && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is null || IsProtectedGeneratedProperty(property.Name))
            {
                warnings.Add($"{CommandListItem.GetDisplayNameForType(item.ItemType)}: 未対応の設定 {name} は反映しませんでした。");
                continue;
            }

            if (!TryConvertGeneratedParameter(rawValue, property.PropertyType, out var converted))
            {
                warnings.Add($"{CommandListItem.GetDisplayNameForType(item.ItemType)}: 設定 {property.Name} の値を反映できませんでした。");
                continue;
            }

            try
            {
                property.SetValue(item, converted);
            }
            catch
            {
                warnings.Add($"{CommandListItem.GetDisplayNameForType(item.ItemType)}: 設定 {property.Name} の反映に失敗しました。");
            }
        }
    }

    private static bool TryConvertGeneratedParameter(string rawValue, Type targetType, out object? converted)
    {
        converted = null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try
        {
            if (underlyingType == typeof(string))
            {
                converted = rawValue;
                return true;
            }

            if (underlyingType == typeof(bool)
                && bool.TryParse(rawValue, out var boolValue))
            {
                converted = boolValue;
                return true;
            }

            if (underlyingType == typeof(int)
                && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                converted = intValue;
                return true;
            }

            if (underlyingType == typeof(double)
                && double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            {
                converted = doubleValue;
                return true;
            }

            if (underlyingType.IsEnum
                && Enum.TryParse(underlyingType, rawValue, ignoreCase: true, out var enumValue))
            {
                converted = enumValue;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsProtectedGeneratedProperty(string propertyName)
    {
        return propertyName is
            nameof(ICommandListItem.ItemType)
            or nameof(ICommandListItem.LineNumber)
            or nameof(ICommandListItem.IsRunning)
            or nameof(ICommandListItem.IsSelected)
            or nameof(ICommandListItem.NestLevel)
            or nameof(ICommandListItem.IsInLoop)
            or nameof(ICommandListItem.IsInIf)
            or nameof(ICommandListItem.Progress)
            or "Pair";
    }

    private static void AddAssistantSafetyWarnings(
        ICommandListItem item,
        AssistantGeneratedMacroCommand command,
        ICollection<string> warnings)
    {
        var displayName = CommandListItem.GetDisplayNameForType(item.ItemType);
        if (item.ItemType is "Execute" or "Hotkey" or "Click")
        {
            warnings.Add($"{displayName}: 実行前に対象アプリと操作内容を確認してください。");
        }

        if (item is ILoopItem loopItem && loopItem.LoopCount <= 0)
        {
            warnings.Add($"{displayName}: ループ回数が0以下です。意図した回数か確認してください。");
        }

        if (item is IRetryItem retryItem && retryItem.RetryCount >= 10)
        {
            warnings.Add($"{displayName}: リトライ回数が多めです。実行時間を確認してください。");
        }

        if (ContainsDangerKeyword(command.Comment))
        {
            warnings.Add($"{displayName}: コメントに削除・上書き・大量操作などの注意語が含まれています。");
        }
    }

    private static bool ContainsDangerKeyword(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] keywords = ["削除", "消去", "上書き", "大量", "連打", "無限", "delete", "remove", "overwrite", "format"];
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record AssistantGeneratedMacroPlan(
        IReadOnlyList<ICommandListItem> Items,
        int SkippedCount,
        IReadOnlyList<string> Warnings);

    private void RegisterCommandEventHandlers()
    {
        _commandEventSubscriptionCts?.Cancel();
        _commandEventSubscriptionCts?.Dispose();
        _lastObservedDroppedCommandEvents = _commandEventBus.DroppedEventCount;
        _commandEventSubscriptionCts = new();
        _commandEventSubscriptionTask = ConsumeCommandEventsAsync(_commandEventSubscriptionCts.Token);
    }

    private void UnsubscribeCommandEventHandlers()
    {
        _commandEventSubscriptionCts?.Cancel();
    }

    private async Task ConsumeCommandEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var ev in _commandEventBus.ReadEventsAsync().ConfigureAwaitFalse(cancellationToken))
            {
                NotifyDroppedCommandEventsIfNeeded();

                switch (ev.Kind)
                {
                    case CommandEventKind.Started:
                        HandleStartCommand(ev.Command);
                        break;
                    case CommandEventKind.Finished:
                        HandleFinishCommand(ev.Command);
                        break;
                    case CommandEventKind.Doing:
                        HandleDoingCommand(ev.Command, ev.Detail, ev.Payload);
                        break;
                    case CommandEventKind.ProgressUpdated:
                        HandleUpdateProgress(ev.Command, ev.Progress);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void NotifyDroppedCommandEventsIfNeeded()
    {
        var dropped = _commandEventBus.DroppedEventCount;
        if (dropped <= _lastObservedDroppedCommandEvents)
        {
            return;
        }

        var delta = dropped - _lastObservedDroppedCommandEvents;
        _lastObservedDroppedCommandEvents = dropped;

        var detail = $"警告: コマンドイベントの取りこぼしが発生しました (+{delta}, 累計 {dropped})";
        _logWriter.WriteStructured(
            "CommandEventBus",
            "DropDetected",
            new Dictionary<string, object?>
            {
                ["DroppedTotal"] = dropped,
                ["DroppedDelta"] = delta,
                ["SubscriberCount"] = _commandEventBus.SubscriberCount
            });

        OnUiThread(() => _logPanel.WriteLog(string.Empty, "システム", detail));
    }

    private void PrepareAllPanels()
    {
        _listPanel.Prepare();
        _editPanel.Prepare();
        _logPanel.Prepare();
        _variablePanel.Prepare();
        _assistantPanel.Prepare();
        _favoritePanel.Prepare();
        _buttonPanel.Prepare();
    }

}




