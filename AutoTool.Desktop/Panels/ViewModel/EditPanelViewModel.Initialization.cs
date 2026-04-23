using System.Collections.ObjectModel;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Desktop.Panels.ViewModel.Shared;
using AutoTool.Commands.Services;
using AutoTool.Commands.Model.Input;
using AutoTool.Infrastructure.Panels;
using AutoTool.Application.Ports;
using AutoTool.Desktop.Panels.Services;


namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 画面状態とユーザー操作を管理する ViewModel です。
/// </summary>
public partial class EditPanelViewModel
{
    public EditPanelViewModel(
        ICommandRegistry commandRegistry,
        IWindowService windowService,
        IPathResolver pathResolver,
        IImageMatcher imageMatcher,
        IOcrEngine ocrEngine,
        IObjectDetector objectDetector,
        IDetectionHighlightService detectionHighlightService,
        INotifier notifier,
        IPanelDialogService panelDialogService,
        ICapturePathProvider capturePathProvider,
        PropertyMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(commandRegistry);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(imageMatcher);
        ArgumentNullException.ThrowIfNull(ocrEngine);
        ArgumentNullException.ThrowIfNull(objectDetector);
        ArgumentNullException.ThrowIfNull(detectionHighlightService);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(panelDialogService);
        ArgumentNullException.ThrowIfNull(capturePathProvider);
        ArgumentNullException.ThrowIfNull(metadataProvider);
        _commandRegistry = commandRegistry;
        _windowService = windowService;
        _pathResolver = pathResolver;
        _imageMatcher = imageMatcher;
        _ocrEngine = ocrEngine;
        _objectDetector = objectDetector;
        _detectionHighlightService = detectionHighlightService;
        _notifier = notifier;
        _panelDialogService = panelDialogService;
        _capturePathProvider = capturePathProvider;
        _metadataProvider = metadataProvider;

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
        foreach (var button in Enum.GetValues<CommandMouseButton>())
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
                    Item = newItem;
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
                throw new ArgumentException($"未対応の ItemType です: {typeName}");
            }
        }
        catch (Exception ex)
        {
            _notifier.ShowError($"コマンドアイテムの作成に失敗しました: {ex.Message}", "エラー");
        }
    }
}

