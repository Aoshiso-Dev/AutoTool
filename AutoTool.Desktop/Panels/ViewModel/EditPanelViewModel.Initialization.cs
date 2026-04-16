using System.Collections.ObjectModel;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.ViewModel.Shared;
using AutoTool.Commands.Services;
using AutoTool.Panels.Services;
using AutoTool.Core.Ports;


namespace AutoTool.Panels.ViewModel;

public partial class EditPanelViewModel
{
    public EditPanelViewModel(
        ICommandRegistry commandRegistry,
        IWindowService windowService,
        IPathResolver pathResolver,
        INotifier notifier,
        IPanelDialogService panelDialogService,
        ICapturePathProvider capturePathProvider)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _panelDialogService = panelDialogService ?? throw new ArgumentNullException(nameof(panelDialogService));
        _capturePathProvider = capturePathProvider ?? throw new ArgumentNullException(nameof(capturePathProvider));

        _commandRegistry.Initialize();

        InitializeItemTypes();
        InitializeMouseButtons();
        InitializeOperators();
        InitializeAIDetectModes();
    }

    private void InitializeItemTypes()
    {
        var displayItems = _commandRegistry.GetOrderedTypeNames()
            .Select(typeName => new CommandDisplayItem
            {
                TypeName = typeName,
                DisplayName = _commandRegistry.GetDisplayName(typeName),
                Category = _commandRegistry.GetCategoryName(typeName)
            })
            .ToList();

        ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
    }

    private void InitializeMouseButtons()
    {
        foreach (var button in Enum.GetValues<System.Windows.Input.MouseButton>())
        {
            MouseButtons.Add(button);
        }
    }

    private void InitializeOperators()
    {
        foreach (var op in new[] { "==", "!=", ">", "<", ">=", "<=", "Contains", "NotContains", "StartsWith", "EndsWith", "IsEmpty", "IsNotEmpty" })
        {
            Operators.Add(op);
        }
    }

    private void InitializeAIDetectModes()
    {
        foreach (var mode in new[] { "Class", "Count" })
        {
            AIDetectModes.Add(mode);
        }
    }

    private void OnSelectedItemTypeChanged(string typeName)
    {
        if (string.IsNullOrEmpty(typeName) || Item is null)
            return;

        var lineNumber = Item.LineNumber;
        var isSelected = Item.IsSelected;

        try
        {
            var newItem = _commandRegistry.CreateCommandItem(typeName);
            if (newItem is not null)
            {
                newItem.LineNumber = lineNumber;
                newItem.IsSelected = isSelected;
                newItem.ItemType = typeName;

                _isUpdating = true;
                try
                {
                    _item = newItem;
                    OnPropertyChanged(nameof(Item));
                }
                finally
                {
                    _isUpdating = false;
                }

                UpdateProperties();
                UpdateIsProperties();
                ItemEdited?.Invoke(newItem);
            }
            else
            {
                throw new ArgumentException($"Unknown ItemType: {typeName}");
            }
        }
        catch (Exception ex)
        {
            _notifier.ShowError($"コマンドアイテムの作成に失敗しました: {ex.Message}", "エラー");
        }
    }
}
