using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using AutoTool.Commands.Model.Input;
using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Desktop.Panels.ViewModel.Shared;

namespace AutoTool.Desktop.Panels.ViewModel;

public partial class EditPanelViewModel
{
    [ObservableProperty]
    private ObservableCollection<PropertyGroup> _propertyGroups = [];

    [ObservableProperty]
    private bool _hasValidationErrors;

    [ObservableProperty]
    private string _validationSummary = string.Empty;

    [ObservableProperty]
    private ICommandListItem? _item;

    [ObservableProperty]
    private int _listCount;

    public bool IsListNotEmpty => ListCount > 0;
    public bool IsListEmpty => ListCount == 0;
    public bool IsListNotEmptyButNoSelection => ListCount > 0 && Item is null;
    public bool IsNotNullItem => Item is not null;
    public bool IsWaitImageItem => Item is WaitImageItem or WaitImageDisappearItem;
    public bool IsClickImageItem => Item is ClickImageItem;
    public bool IsClickImageAIItem => Item is ClickImageAIItem;
    public bool IsHotkeyItem => Item is HotkeyItem;
    public bool IsClickItem => Item is ClickItem;
    public bool IsWaitItem => Item is WaitItem;
    public bool IsLoopItem => Item is LoopItem or RetryItem;
    public bool IsEndLoopItem => Item is LoopEndItem or RetryEndItem;
    public bool IsBreakItem => Item is LoopBreakItem;
    public bool IsIfImageExistItem => Item is IfImageExistItem;
    public bool IsIfImageNotExistItem => Item is IfImageNotExistItem;
    public bool IsIfImageExistAIItem => Item is IfImageExistAIItem;
    public bool IsIfImageNotExistAIItem => Item is IfImageNotExistAIItem;
    public bool IsEndIfItem => Item is IfEndItem;
    public bool IsExecuteProgramItem => Item is ExecuteItem;
    public bool IsSetVariableItem => Item is SetVariableItem;
    public bool IsSetVariableAIItem => Item is SetVariableAIItem;
    public bool IsIfVariableItem => Item is IfVariableItem;
    public bool IsScreenshotItem => Item is ScreenshotItem;
    public bool IsOcrPreviewAvailable => Item is IFindTextItem or IIfTextExistItem or IIfTextNotExistItem or ISetVariableOCRItem;
    public bool IsImageSearchPreviewAvailable => Item is IWaitImageItem or IClickImageItem or IIfImageExistItem or IIfImageNotExistItem or IFindImageItem;
    public bool IsAiDetectionPreviewAvailable => Item is IIfImageExistAIItem or IIfImageNotExistAIItem or IClickImageAIItem or ISetVariableAIItem;
    public bool HasImagePreview => Item is WaitImageItem or WaitImageDisappearItem or ClickImageItem or IfImageExistItem or IfImageNotExistItem;

    [ObservableProperty]
    private bool _hasOcrPreviewResult;

    [ObservableProperty]
    private bool _isOcrPreviewRunning;

    [ObservableProperty]
    private string _ocrPreviewSummary = "OCRプレビューは未実行です。";

    [ObservableProperty]
    private string _ocrPreviewText = string.Empty;

    [ObservableProperty]
    private string _ocrPreviewConfidenceText = string.Empty;

    [ObservableProperty]
    private bool _isOcrAutoTuning;

    [ObservableProperty]
    private string _ocrAutoTuneSummary = string.Empty;

    [ObservableProperty]
    private bool _hasImageSearchPreviewResult;

    [ObservableProperty]
    private bool _isImageSearchPreviewRunning;

    [ObservableProperty]
    private string _imageSearchPreviewSummary = "画像検索テストは未実行です。";

    [ObservableProperty]
    private string _imageSearchPreviewDetail = string.Empty;

    [ObservableProperty]
    private string _imageSearchSearchArea = string.Empty;

    [ObservableProperty]
    private string _imageSearchRecoveryGuide = string.Empty;

    [ObservableProperty]
    private bool _isImageSearchAutoTuning;

    [ObservableProperty]
    private string _imageSearchAutoTuneSummary = string.Empty;

    [ObservableProperty]
    private bool _hasAiDetectionPreviewResult;

    [ObservableProperty]
    private bool _isAiDetectionPreviewRunning;

    [ObservableProperty]
    private string _aiDetectionPreviewSummary = "AI検出テストは未実行です。";

    [ObservableProperty]
    private string _aiDetectionPreviewDetail = string.Empty;

    [ObservableProperty]
    private string _aiDetectionSearchArea = string.Empty;

    [ObservableProperty]
    private string _aiDetectionRecoveryGuide = string.Empty;

    [ObservableProperty]
    private bool _isAiDetectionAutoTuning;

    [ObservableProperty]
    private string _aiDetectionAutoTuneSummary = string.Empty;

    public string? PreviewImagePath
    {
        get
        {
            var imageProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyInfo.Name == "ImagePath");
            var path = imageProp?.Value?.ToString();
            if (string.IsNullOrEmpty(path)) return null;
            return _pathResolver.ToAbsolutePath(path);
        }
    }

    public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
    public string WindowTitle { get => _propertyManager.WindowTitle.GetValue(Item); set => SetAndRefresh(() => _propertyManager.WindowTitle.SetValue(Item, value)); }
    public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
    public string WindowClassName { get => _propertyManager.WindowClassName.GetValue(Item); set => SetAndRefresh(() => _propertyManager.WindowClassName.SetValue(Item, value)); }

    public string ImagePath
    {
        get
        {
            var relativePath = _propertyManager.ImagePath.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        }
        set
        {
            SetPathAndRefresh(_propertyManager.ImagePath.SetValue, value);
        }
    }

    public double Threshold { get => _propertyManager.Threshold.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Threshold.SetValue(Item, value)); }
    public CommandColor? SearchColor
    {
        get => _propertyManager.SearchColor.GetValue(Item);
        set => SetAndRefresh(
            () => _propertyManager.SearchColor.SetValue(Item, value),
            nameof(SearchColorBrush),
            nameof(SearchColorText),
            nameof(SearchColorTextColor));
    }
    public int Timeout { get => _propertyManager.Timeout.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Timeout.SetValue(Item, value)); }
    public int Interval { get => _propertyManager.Interval.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Interval.SetValue(Item, value)); }
    public CommandMouseButton MouseButton { get => _propertyManager.MouseButton.GetValue(Item); set => SetAndRefresh(() => _propertyManager.MouseButton.SetValue(Item, value)); }
    public bool Ctrl { get => _propertyManager.Ctrl.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Ctrl.SetValue(Item, value)); }
    public bool Alt { get => _propertyManager.Alt.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Alt.SetValue(Item, value)); }
    public bool Shift { get => _propertyManager.Shift.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Shift.SetValue(Item, value)); }
    public CommandKey Key { get => _propertyManager.Key.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Key.SetValue(Item, value)); }
    public int X { get => _propertyManager.X.GetValue(Item); set => SetAndRefresh(() => _propertyManager.X.SetValue(Item, value)); }
    public int Y { get => _propertyManager.Y.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Y.SetValue(Item, value)); }
    public int Wait { get => _propertyManager.Wait.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Wait.SetValue(Item, value)); }
    public int LoopCount { get => _propertyManager.LoopCount.GetValue(Item); set => SetAndRefresh(() => _propertyManager.LoopCount.SetValue(Item, value)); }

    public string ModelPath
    {
        get
        {
            var relativePath = _propertyManager.ModelPath.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        }
        set
        {
            SetPathAndRefresh(_propertyManager.ModelPath.SetValue, value);
        }
    }

    public int ClassID { get => _propertyManager.ClassID.GetValue(Item); set => SetAndRefresh(() => _propertyManager.ClassID.SetValue(Item, value)); }
    public string AIDetectMode { get => _propertyManager.Mode.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Mode.SetValue(Item, value)); }

    public string ProgramPath
    {
        get
        {
            var relativePath = _propertyManager.ProgramPath.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        }
        set
        {
            SetPathAndRefresh(_propertyManager.ProgramPath.SetValue, value);
        }
    }

    public string Arguments { get => _propertyManager.Arguments.GetValue(Item); set => SetAndRefresh(() => _propertyManager.Arguments.SetValue(Item, value)); }

    public string WorkingDirectory
    {
        get
        {
            var relativePath = _propertyManager.WorkingDirectory.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        }
        set
        {
            SetPathAndRefresh(_propertyManager.WorkingDirectory.SetValue, value);
        }
    }

    public bool WaitForExit { get => _propertyManager.WaitForExit.GetValue(Item); set => SetAndRefresh(() => _propertyManager.WaitForExit.SetValue(Item, value)); }
    public string VariableName { get => _propertyManager.VariableName.GetValue(Item); set => SetAndRefresh(() => _propertyManager.VariableName.SetValue(Item, value)); }
    public string VariableValue { get => _propertyManager.VariableValue.GetValue(Item); set => SetAndRefresh(() => _propertyManager.VariableValue.SetValue(Item, value)); }
    public string CompareOperator { get => _propertyManager.CompareOperator.GetValue(Item); set => SetAndRefresh(() => _propertyManager.CompareOperator.SetValue(Item, value)); }
    public string CompareValue { get => _propertyManager.CompareValue.GetValue(Item); set => SetAndRefresh(() => _propertyManager.CompareValue.SetValue(Item, value)); }

    public string SaveDirectory
    {
        get
        {
            var relativePath = _propertyManager.SaveDirectory.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        }
        set
        {
            SetPathAndRefresh(_propertyManager.SaveDirectory.SetValue, value);
        }
    }

    public double ConfThreshold { get => _propertyManager.ConfThreshold.GetValue(Item); set => SetAndRefresh(() => _propertyManager.ConfThreshold.SetValue(Item, value)); }
    public double IoUThreshold { get => _propertyManager.IoUThreshold.GetValue(Item); set => SetAndRefresh(() => _propertyManager.IoUThreshold.SetValue(Item, value)); }

    public string Comment
    {
        get => Item?.Comment ?? string.Empty;
        set
        {
            if (Item is not null && Item.Comment != value)
            {
                Item.Comment = value;
                UpdateProperties();
            }
        }
    }

    public Brush SearchColorBrush => new SolidColorBrush(SearchColor is { } c ? Color.FromArgb(c.A, c.R, c.G, c.B) : Color.FromArgb(0, 0, 0, 0));
    public string SearchColorText => SearchColor is not null ? $"R:{SearchColor.Value.R:D3} G:{SearchColor.Value.G:D3} B:{SearchColor.Value.B:D3}" : "指定なし";
    public Brush SearchColorTextColor => SearchColor is not null ? new SolidColorBrush(Color.FromArgb(255, (byte)(255 - SearchColor.Value.R), (byte)(255 - SearchColor.Value.G), (byte)(255 - SearchColor.Value.B))) : new SolidColorBrush(Colors.Black);

    [ObservableProperty] private ObservableCollection<CommandDisplayItem> _itemTypes = [];

    [ObservableProperty]
    private CommandDisplayItem? _selectedItemTypeObj;

    public string SelectedItemType
    {
        get => Item?.ItemType ?? "None";
        set
        {
            if (Item is null) return;
            if (Item.ItemType == value) return;
            OnSelectedItemTypeChanged(value);
        }
    }

    [ObservableProperty] private ObservableCollection<CommandMouseButton> _mouseButtons = [];
    public CommandMouseButton SelectedMouseButton
    {
        get => MouseButton;
        set
        {
            if (MouseButton != value)
            {
                MouseButton = value;
            }
        }
    }

    [ObservableProperty] private ObservableCollection<string> _operators = [];
    public string SelectedOperator { get => CompareOperator; set { CompareOperator = value; } }

    [ObservableProperty] private ObservableCollection<string> _aIDetectModes = [];
    public string SelectedAIDetectMode { get => AIDetectMode; set { AIDetectMode = value; } }

    partial void OnItemChanged(ICommandListItem? value)
    {
        if (value is not null)
        {
            var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == value.ItemType);
            if (displayItem is not null && SelectedItemTypeObj != displayItem)
            {
                SelectedItemTypeObj = displayItem;
            }
        }
        else
        {
            SelectedItemTypeObj = null;
        }

        UpdateProperties();
        UpdateIsProperties();
        UpdatePropertyGroups();
        if (!IsOcrPreviewAvailable)
        {
            HasOcrPreviewResult = false;
            IsOcrPreviewRunning = false;
            IsOcrAutoTuning = false;
            OcrPreviewSummary = "OCRプレビューは未実行です。";
            OcrAutoTuneSummary = string.Empty;
            OcrPreviewText = string.Empty;
            OcrPreviewConfidenceText = string.Empty;
        }
        if (!IsImageSearchPreviewAvailable)
        {
            HasImageSearchPreviewResult = false;
            IsImageSearchPreviewRunning = false;
            IsImageSearchAutoTuning = false;
            ImageSearchPreviewSummary = "画像検索テストは未実行です。";
            ImageSearchAutoTuneSummary = string.Empty;
            ImageSearchPreviewDetail = string.Empty;
            ImageSearchSearchArea = string.Empty;
            ImageSearchRecoveryGuide = string.Empty;
        }
        if (!IsAiDetectionPreviewAvailable)
        {
            HasAiDetectionPreviewResult = false;
            IsAiDetectionPreviewRunning = false;
            IsAiDetectionAutoTuning = false;
            AiDetectionPreviewSummary = "AI検出テストは未実行です。";
            AiDetectionAutoTuneSummary = string.Empty;
            AiDetectionPreviewDetail = string.Empty;
            AiDetectionSearchArea = string.Empty;
            AiDetectionRecoveryGuide = string.Empty;
        }

        RunOcrPreviewCommand.NotifyCanExecuteChanged();
        RunOcrAutoTuneCommand.NotifyCanExecuteChanged();
        RunImageSearchPreviewCommand.NotifyCanExecuteChanged();
        RunImageSearchAutoTuneCommand.NotifyCanExecuteChanged();
        RunAiDetectionPreviewCommand.NotifyCanExecuteChanged();
        RunAiDetectionAutoTuneCommand.NotifyCanExecuteChanged();
    }

    partial void OnListCountChanged(int value)
    {
        UpdateIsProperties();
        UpdateProperties();
    }

    partial void OnSelectedItemTypeObjChanged(CommandDisplayItem? value)
    {
        if (value is not null)
        {
            OnSelectedItemTypeChanged(value.TypeName);
        }
    }

    private void SetPathAndRefresh(Action<ICommandListItem?, string> setter, string value)
    {
        var relativePath = string.IsNullOrEmpty(value) ? value : _pathResolver.ToRelativePath(value);
        SetAndRefresh(() => setter(Item, relativePath));
    }

    private void SetAndRefresh(Action setter, params string[] additionalPropertyNames)
    {
        setter();
        UpdateProperties();

        foreach (var propertyName in additionalPropertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
