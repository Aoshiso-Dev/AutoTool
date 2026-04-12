using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.View;
using System.Windows.Input;
using System.Windows.Threading;
using AutoTool.Panels.ViewModel.Helpers;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.ViewModel.Shared;
using AutoTool.Commands.Services;
using AutoTool.Commands.Infrastructure;
using AutoTool.Panels.Services;
using AutoTool.Panels.Attributes;
using AutoTool.Core.Ports;

namespace AutoTool.Panels.ViewModel;

public partial class EditPanelViewModel : ObservableObject, IEditPanelViewModel
{
    private readonly EditPanelPropertyManager _propertyManager = new();
    private readonly ICommandRegistry _commandRegistry;
    private readonly IWindowService _windowService;
    private readonly IPathResolver _pathResolver;
    private readonly INotifier _notifier;
    private readonly IPanelDialogService _panelDialogService;
    private readonly ICapturePathProvider _capturePathProvider;
    private readonly PropertyMetadataProvider _metadataProvider = new();

    // イベント
    public event Action<ICommandListItem?>? ItemEdited;
    public event Action? RefreshRequested;

    [ObservableProperty]
    private bool _isRunning;
    private bool _isUpdating;
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

    /// <summary>
    /// メタデータ駆動UIのプロパティグループ
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PropertyGroup> _propertyGroups = new();

    #region Item
    private ICommandListItem? _item;
    public ICommandListItem? Item
    {
        get => _item;
        set 
        { 
            if (SetProperty(ref _item, value))
            {
                if (value != null)
                {
                    var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == value.ItemType);
                    if (displayItem != null && _selectedItemTypeObj != displayItem)
                    {
                        _selectedItemTypeObj = displayItem;
                        OnPropertyChanged(nameof(SelectedItemTypeObj));
                    }
                }
                else
                {
                    _selectedItemTypeObj = null;
                    OnPropertyChanged(nameof(SelectedItemTypeObj));
                }
                
                UpdateProperties(); 
                UpdateIsProperties();
                UpdatePropertyGroups();
            }
        }
    }
    #endregion




    /// <summary>
    /// メタデータからプロパティグループを更新
    /// </summary>
    private void UpdatePropertyGroups()
    {
        PropertyGroups.Clear();
        if (Item == null) return;
        
        // まず全グループを追加
        foreach (var group in _metadataProvider.GetGroupedMetadata(Item))
        {
            PropertyGroups.Add(group);
        }
        
        // 次に各プロパティにコマンドを設定（全プロパティが揃った後）
        foreach (var group in PropertyGroups)
        {
            foreach (var prop in group.Properties)
            {
                SetupPropertyCommands(prop);
            }
        }
    }
    
    
    
    
    /// <summary>
    /// プロパティのエディタタイプに応じてコマンドを設定
    /// </summary>
    private void SetupPropertyCommands(PropertyMetadata prop)
    {
        switch (prop.EditorType)
        {
            case EditorType.ImagePicker:
                prop.BrowseCommand = new RelayCommand(() => BrowseImageForProperty(prop));
                prop.CaptureCommand = new RelayCommand(() => CaptureImageForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; OnPropertyChanged(nameof(prop.StringValue)); });
                break;
                
            case EditorType.ColorPicker:
                prop.PickColorCommand = new RelayCommand(() => PickColorForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => prop.Value = null);
                break;
                
            case EditorType.WindowInfo:
                prop.GetWindowInfoCommand = new RelayCommand(() => GetWindowInfoForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; OnPropertyChanged(nameof(prop.StringValue)); });
                break;
                
            case EditorType.FilePicker:
                prop.BrowseCommand = new RelayCommand(() => BrowseFileForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; OnPropertyChanged(nameof(prop.StringValue)); });
                break;
                
            case EditorType.DirectoryPicker:
                prop.BrowseCommand = new RelayCommand(() => BrowseDirectoryForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; OnPropertyChanged(nameof(prop.StringValue)); });
                break;
                
            case EditorType.KeyPicker:
                prop.PickKeyCommand = new RelayCommand(() => PickKeyForProperty(prop));
                break;
                
            case EditorType.PointPicker:
                prop.PickPointCommand = new RelayCommand(() => PickPointForProperty(prop));
                // 関連するY座標を設定
                var yProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyInfo.Name == "Y" && p.Target == prop.Target);
                prop.RelatedProperty = yProp;
                break;
        }
    }
    
    
    
    
    
    
    private void BrowseImageForProperty(PropertyMetadata prop)
    {
        var path = _panelDialogService.SelectImageFile();
        if (!string.IsNullOrEmpty(path))
        {
            prop.Value = _pathResolver.ToRelativePath(path);
            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(PreviewImagePath));
            OnPropertyChanged(nameof(HasImagePreview));
            UpdateProperties();
        }
    }
    
    private void CaptureImageForProperty(PropertyMetadata prop)
    {
        var cw = new CaptureWindow { Mode = 0 };
        if (cw.ShowDialog() == true)
        {
            var path = _capturePathProvider.CreateCaptureFilePath();
            var mat = Win32ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion);
            Win32ScreenCaptureHelper.SaveCapture(mat, path);
            prop.Value = _pathResolver.ToRelativePath(path);
            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(PreviewImagePath));
            OnPropertyChanged(nameof(HasImagePreview));
            UpdateProperties();
        }
    }
    
    private void PickColorForProperty(PropertyMetadata prop)
    {
        var w = new ColorPickWindow();
        w.ShowDialog();
        if (w.Color.HasValue)
        {
            prop.Value = w.Color.Value;
            UpdateProperties();
        }
    }
    
    private void GetWindowInfoForProperty(PropertyMetadata prop)
    {
        var w = new GetWindowInfoWindow();
        if (w.ShowDialog() == true)
        {
            // ウィンドウタイトルを設定
            prop.Value = w.WindowTitle;
            
            // 同じターゲットのWindowClassNameプロパティも探して設定
            var classNameProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyInfo.Name == "WindowClassName" && p.Target == prop.Target);
            
            if (classNameProp != null)
            {
                classNameProp.Value = w.WindowClassName;
            }
            
            UpdateProperties();
        }
    }
    
    private void BrowseFileForProperty(PropertyMetadata prop)
    {
        var path = _panelDialogService.SelectModelFile();
        if (!string.IsNullOrEmpty(path))
        {
            prop.Value = _pathResolver.ToRelativePath(path);
            UpdateProperties();
        }
    }
    
    private void BrowseDirectoryForProperty(PropertyMetadata prop)
    {
        var path = _panelDialogService.SelectFolder();
        if (!string.IsNullOrEmpty(path))
        {
            prop.Value = _pathResolver.ToRelativePath(path);
            UpdateProperties();
        }
    }
    
    private void PickKeyForProperty(PropertyMetadata prop)
    {
        var keyPickerWindow = new KeyPickerWindow();
        if (keyPickerWindow.ShowDialog() == true)
        {
            prop.Value = keyPickerWindow.SelectedKey;
            UpdateProperties();
        }
    }
    
    private void PickPointForProperty(PropertyMetadata prop)
    {
        var cw = new CaptureWindow { Mode = 1 };
        if (cw.ShowDialog() != true) return;
        
        var absoluteX = (int)cw.SelectedPoint.X;
        var absoluteY = (int)cw.SelectedPoint.Y;
        
        // ウィンドウタイトルを取得（同じターゲット内から）
        var windowTitleProp = PropertyGroups
            .SelectMany(g => g.Properties)
            .FirstOrDefault(p => p.PropertyInfo.Name == "WindowTitle" && p.Target == prop.Target);
        var windowClassNameProp = PropertyGroups
            .SelectMany(g => g.Properties)
            .FirstOrDefault(p => p.PropertyInfo.Name == "WindowClassName" && p.Target == prop.Target);
        
        var windowTitle = windowTitleProp?.Value?.ToString() ?? string.Empty;
        var windowClassName = windowClassNameProp?.Value?.ToString() ?? string.Empty;
        
        var (relativeX, relativeY, success, errorMessage) = _windowService.ConvertToRelativeCoordinates(
            absoluteX, absoluteY, windowTitle, windowClassName);
        
        
        // Xプロパティの場合
        if (prop.PropertyInfo.Name == "X")
        {
            prop.Value = relativeX;
            // Yプロパティも探して設定
            var yProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyInfo.Name == "Y" && p.Target == prop.Target);
            if (yProp != null)
            {
                yProp.Value = relativeY;
            }
            prop.NotifyRelatedValueChanged();
        }
        // Yプロパティの場合
        else if (prop.PropertyInfo.Name == "Y")
        {
            prop.Value = relativeY;
            // Xプロパティも探して設定
            var xProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyInfo.Name == "X" && p.Target == prop.Target);
            if (xProp != null)
            {
                xProp.Value = relativeX;
                xProp.NotifyRelatedValueChanged();
            }
        }
        
        UpdateProperties();
        
        if (!string.IsNullOrEmpty(windowTitle) || !string.IsNullOrEmpty(windowClassName))
        {
            if (success)
            {
                _notifier.ShowInfo(
                    $"Relative coordinates set: ({relativeX}, {relativeY})\nWindow: {windowTitle}[{windowClassName}]",
                    "Coordinates Set");
            }
            else
            {
                _notifier.ShowWarning(
                    $"{errorMessage}\nAbsolute coordinates ({relativeX}, {relativeY}) set.",
                    "Warning");
            }
        }
    }

    #region ListCount
    private int _listCount;
    public int ListCount
    {
        get => _listCount;
        set { SetProperty(ref _listCount, value); UpdateIsProperties(); UpdateProperties(); }
    }
    #endregion

    #region IsProperties
    public bool IsListNotEmpty => ListCount > 0;
    public bool IsListEmpty => ListCount == 0;
    public bool IsListNotEmptyButNoSelection => ListCount > 0 && Item == null;
    public bool IsNotNullItem => Item != null;
    public bool IsWaitImageItem => Item is WaitImageItem;
    public bool IsClickImageItem => Item is ClickImageItem;
    public bool IsClickImageAIItem => Item is ClickImageAIItem;
    public bool IsHotkeyItem => Item is HotkeyItem;
    public bool IsClickItem => Item is ClickItem;
    public bool IsWaitItem => Item is WaitItem;
    public bool IsLoopItem => Item is LoopItem;
    public bool IsEndLoopItem => Item is LoopEndItem;
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
    
    /// <summary>
    /// Has image preview (for image-based commands)
    /// </summary>
    public bool HasImagePreview => Item is WaitImageItem or ClickImageItem or IfImageExistItem or IfImageNotExistItem;
    
    /// <summary>
    /// Preview image path (from dynamic properties)
    /// </summary>
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
    #endregion

    #region Properties (via PropertyManager)
    public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
    public string WindowTitle { get => _propertyManager.WindowTitle.GetValue(Item); set { _propertyManager.WindowTitle.SetValue(Item, value); UpdateProperties(); } }
    public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
    public string WindowClassName { get => _propertyManager.WindowClassName.GetValue(Item); set { _propertyManager.WindowClassName.SetValue(Item, value); UpdateProperties(); } }
    
    public string ImagePath 
    { 
        get 
        {
            var relativePath = _propertyManager.ImagePath.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        } 
        set 
        { 
            var relativePath = string.IsNullOrEmpty(value) ? value : _pathResolver.ToRelativePath(value);
            _propertyManager.ImagePath.SetValue(Item, relativePath); 
            UpdateProperties(); 
        } 
    }
    
    public double Threshold { get => _propertyManager.Threshold.GetValue(Item); set { _propertyManager.Threshold.SetValue(Item, value); UpdateProperties(); } }
    public Color? SearchColor { get => _propertyManager.SearchColor.GetValue(Item); set { _propertyManager.SearchColor.SetValue(Item, value); UpdateProperties(); OnPropertyChanged(nameof(SearchColorBrush)); OnPropertyChanged(nameof(SearchColorText)); OnPropertyChanged(nameof(SearchColorTextColor)); } }
    public int Timeout { get => _propertyManager.Timeout.GetValue(Item); set { _propertyManager.Timeout.SetValue(Item, value); UpdateProperties(); } }
    public int Interval { get => _propertyManager.Interval.GetValue(Item); set { _propertyManager.Interval.SetValue(Item, value); UpdateProperties(); } }
    public System.Windows.Input.MouseButton MouseButton { get => _propertyManager.MouseButton.GetValue(Item); set { _propertyManager.MouseButton.SetValue(Item, value); UpdateProperties(); } }
    public bool Ctrl { get => _propertyManager.Ctrl.GetValue(Item); set { _propertyManager.Ctrl.SetValue(Item, value); UpdateProperties(); } }
    public bool Alt { get => _propertyManager.Alt.GetValue(Item); set { _propertyManager.Alt.SetValue(Item, value); UpdateProperties(); } }
    public bool Shift { get => _propertyManager.Shift.GetValue(Item); set { _propertyManager.Shift.SetValue(Item, value); UpdateProperties(); } }
    public Key Key { get => _propertyManager.Key.GetValue(Item); set { _propertyManager.Key.SetValue(Item, value); UpdateProperties(); } }
    public int X { get => _propertyManager.X.GetValue(Item); set { _propertyManager.X.SetValue(Item, value); UpdateProperties(); } }
    public int Y { get => _propertyManager.Y.GetValue(Item); set { _propertyManager.Y.SetValue(Item, value); UpdateProperties(); } }
    public int Wait { get => _propertyManager.Wait.GetValue(Item); set { _propertyManager.Wait.SetValue(Item, value); UpdateProperties(); } }
    public int LoopCount { get => _propertyManager.LoopCount.GetValue(Item); set { _propertyManager.LoopCount.SetValue(Item, value); UpdateProperties(); } }
    
    public string ModelPath 
    { 
        get 
        {
            var relativePath = _propertyManager.ModelPath.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        } 
        set 
        { 
            var relativePath = string.IsNullOrEmpty(value) ? value : _pathResolver.ToRelativePath(value);
            _propertyManager.ModelPath.SetValue(Item, relativePath); 
            UpdateProperties(); 
        } 
    }
    
    public int ClassID { get => _propertyManager.ClassID.GetValue(Item); set { _propertyManager.ClassID.SetValue(Item, value); UpdateProperties(); } }
    public string AIDetectMode { get => _propertyManager.Mode.GetValue(Item); set { _propertyManager.Mode.SetValue(Item, value); UpdateProperties(); } }
    
    public string ProgramPath 
    { 
        get 
        {
            var relativePath = _propertyManager.ProgramPath.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        } 
        set 
        { 
            var relativePath = string.IsNullOrEmpty(value) ? value : _pathResolver.ToRelativePath(value);
            _propertyManager.ProgramPath.SetValue(Item, relativePath); 
            UpdateProperties(); 
        } 
    }
    
    public string Arguments { get => _propertyManager.Arguments.GetValue(Item); set { _propertyManager.Arguments.SetValue(Item, value); UpdateProperties(); } }
    
    public string WorkingDirectory 
    { 
        get 
        {
            var relativePath = _propertyManager.WorkingDirectory.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        } 
        set 
        { 
            var relativePath = string.IsNullOrEmpty(value) ? value : _pathResolver.ToRelativePath(value);
            _propertyManager.WorkingDirectory.SetValue(Item, relativePath); 
            UpdateProperties(); 
        } 
    }
    
    public bool WaitForExit { get => _propertyManager.WaitForExit.GetValue(Item); set { _propertyManager.WaitForExit.SetValue(Item, value); UpdateProperties(); } }
    public string VariableName { get => _propertyManager.VariableName.GetValue(Item); set { _propertyManager.VariableName.SetValue(Item, value); UpdateProperties(); } }
    public string VariableValue { get => _propertyManager.VariableValue.GetValue(Item); set { _propertyManager.VariableValue.SetValue(Item, value); UpdateProperties(); } }
    public string CompareOperator { get => _propertyManager.CompareOperator.GetValue(Item); set { _propertyManager.CompareOperator.SetValue(Item, value); UpdateProperties(); } }
    public string CompareValue { get => _propertyManager.CompareValue.GetValue(Item); set { _propertyManager.CompareValue.SetValue(Item, value); UpdateProperties(); } }
    
    public string SaveDirectory 
    { 
        get 
        {
            var relativePath = _propertyManager.SaveDirectory.GetValue(Item);
            return string.IsNullOrEmpty(relativePath) ? relativePath : _pathResolver.ToAbsolutePath(relativePath);
        } 
        set 
        { 
            var relativePath = string.IsNullOrEmpty(value) ? value : _pathResolver.ToRelativePath(value);
            _propertyManager.SaveDirectory.SetValue(Item, relativePath); 
            UpdateProperties(); 
        } 
    }
    
    public double ConfThreshold { get => _propertyManager.ConfThreshold.GetValue(Item); set { _propertyManager.ConfThreshold.SetValue(Item, value); UpdateProperties(); } }
    public double IoUThreshold { get => _propertyManager.IoUThreshold.GetValue(Item); set { _propertyManager.IoUThreshold.SetValue(Item, value); UpdateProperties(); } }
    
    public string Comment 
    { 
        get => Item?.Comment ?? string.Empty; 
        set 
        { 
            if (Item != null && Item.Comment != value)
            {
                Item.Comment = value;
                UpdateProperties();
            }
        } 
    }
    #endregion

    #region ColorPicker
    public Brush SearchColorBrush => new SolidColorBrush(SearchColor ?? Color.FromArgb(0, 0, 0, 0));
    public string SearchColorText => SearchColor != null ? $"R:{SearchColor.Value.R:D3} G:{SearchColor.Value.G:D3} B:{SearchColor.Value.B:D3}" : "指定なし";
    public Brush SearchColorTextColor => SearchColor != null ? new SolidColorBrush(Color.FromArgb(255, (byte)(255 - SearchColor.Value.R), (byte)(255 - SearchColor.Value.G), (byte)(255 - SearchColor.Value.B))) : new SolidColorBrush(Colors.Black);
    #endregion

    #region ComboBox / Collections
    [ObservableProperty] private ObservableCollection<CommandDisplayItem> _itemTypes = new();
    
    private CommandDisplayItem? _selectedItemTypeObj;
    public CommandDisplayItem? SelectedItemTypeObj 
    { 
        get => _selectedItemTypeObj;
        set 
        {
            if (SetProperty(ref _selectedItemTypeObj, value) && value != null)
            {
                OnSelectedItemTypeChanged(value.TypeName);
            }
        }
    }
    
    public string SelectedItemType 
    { 
        get => Item?.ItemType ?? "None"; 
        set 
        { 
            if (Item == null) return;
            if (Item.ItemType == value) return; 
            OnSelectedItemTypeChanged(value);
        } 
    }
    [ObservableProperty] private ObservableCollection<System.Windows.Input.MouseButton> _mouseButtons = new();
    public System.Windows.Input.MouseButton SelectedMouseButton 
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
    [ObservableProperty] private ObservableCollection<string> _operators = new();
    public string SelectedOperator { get => CompareOperator; set { CompareOperator = value; UpdateProperties(); } }
    [ObservableProperty] private ObservableCollection<string> _aIDetectModes = new();
    public string SelectedAIDetectMode { get => AIDetectMode; set { AIDetectMode = value; UpdateProperties(); } }
    #endregion

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
        
        // RefreshTimerは使用しない（ToggleSwitchリセット問題を回避）
        // アイテムのプロパティ変更はINotifyPropertyChangedで自動通知される
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _refreshTimer.Tick += (_, _) => { _refreshTimer.Stop(); RefreshRequested?.Invoke(); };
        
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
            MouseButtons.Add(button);
    }

    private void InitializeOperators()
    {
        foreach (var op in new[] { "==", "!=", ">", "<", ">=", "<=", "Contains", "NotContains", "StartsWith", "EndsWith", "IsEmpty", "IsNotEmpty" })
            Operators.Add(op);
    }

    private void InitializeAIDetectModes()
    {
        foreach (var mode in new[] { "Class", "Count" }) 
            AIDetectModes.Add(mode);
    }

    #region OnChanged
    private void OnSelectedItemTypeChanged(string typeName)
    {
        if (string.IsNullOrEmpty(typeName) || Item == null)
            return;

        var lineNumber = Item.LineNumber;
        var isSelected = Item.IsSelected;
        
        try
        {
            var newItem = _commandRegistry.CreateCommandItem(typeName);
            if (newItem != null)
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
    #endregion

    #region Update
    private void UpdateIsProperties()
    {
        foreach (var name in IsPropertyNames)
            OnPropertyChanged(name);
    }

    private static readonly string[] IsPropertyNames = {
        nameof(IsListNotEmpty), nameof(IsListEmpty), nameof(IsListNotEmptyButNoSelection), 
        nameof(IsNotNullItem), nameof(IsWaitImageItem), 
        nameof(IsClickImageItem), nameof(IsClickImageAIItem), nameof(IsHotkeyItem), 
        nameof(IsClickItem), nameof(IsWaitItem), nameof(IsLoopItem), 
        nameof(IsEndLoopItem), nameof(IsBreakItem), nameof(IsIfImageExistItem), 
        nameof(IsIfImageNotExistItem), nameof(IsEndIfItem), nameof(IsIfImageExistAIItem), 
        nameof(IsIfImageNotExistAIItem), nameof(IsExecuteProgramItem), nameof(IsSetVariableItem), 
        nameof(IsSetVariableAIItem), nameof(IsIfVariableItem), nameof(IsScreenshotItem)
    };

    private static readonly string[] AllPropertyNames = {
        nameof(SelectedItemType), nameof(WindowTitle), nameof(WindowTitleText), 
        nameof(WindowClassName), nameof(WindowClassNameText), nameof(ImagePath), 
        nameof(Threshold), nameof(SearchColor), nameof(Timeout), nameof(Interval), 
        nameof(MouseButton), nameof(SelectedMouseButton), nameof(Ctrl), nameof(Alt), 
        nameof(Shift), nameof(Key), nameof(X), nameof(Y), nameof(Wait), nameof(LoopCount), 
        nameof(ConfThreshold), nameof(IoUThreshold), nameof(SearchColorBrush), 
        nameof(SearchColorText), nameof(SearchColorTextColor), nameof(ModelPath), 
        nameof(ClassID), nameof(AIDetectMode), nameof(ProgramPath), nameof(Arguments), 
        nameof(WorkingDirectory), nameof(WaitForExit), nameof(VariableName), 
        nameof(VariableValue), nameof(CompareOperator), nameof(CompareValue), 
        nameof(SaveDirectory), nameof(Comment)
    };

    private void UpdateProperties()
    {
        if (_isUpdating) return;
        
        try
        {
            _isUpdating = true;
            
            foreach (var name in AllPropertyNames)
                OnPropertyChanged(name);
            
            // _refreshTimerを使わない（ToggleSwitchリセット問題を回避）
            // アイテムのプロパティはINotifyPropertyChangedで自動的に通知される
        }
        finally
        {
            _isUpdating = false;
        }
    }
    #endregion

    #region Commands
    [RelayCommand] private void Enter(KeyEventArgs e) { if (e.Key == Key.Enter) UpdateProperties(); }
    [RelayCommand] public void GetWindowInfo() { var w = new GetWindowInfoWindow(); if (w.ShowDialog() == true) { WindowTitle = w.WindowTitle; WindowClassName = w.WindowClassName; } }
    [RelayCommand] public void ClearWindowInfo() { WindowTitle = string.Empty; WindowClassName = string.Empty; }
    
    [RelayCommand] 
    public void Browse() 
    { 
        var f = _panelDialogService.SelectImageFile(); 
        if (f != null) ImagePath = f;
    }
    
    [RelayCommand] 
    public void Capture() 
    { 
        var cw = new CaptureWindow { Mode = 0 }; 
        if (cw.ShowDialog() == true) 
        { 
            var path = _capturePathProvider.CreateCaptureFilePath(); 
            var mat = Win32ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion); 
            Win32ScreenCaptureHelper.SaveCapture(mat, path); 
            ImagePath = path;
        } 
    }
    
    [RelayCommand] public void PickSearchColor() { var w = new ColorPickWindow(); w.ShowDialog(); SearchColor = w.Color; }
    [RelayCommand] public void ClearSearchColor() { SearchColor = null; }
    
    [RelayCommand] 
    public void PickPoint() 
    { 
        var cw = new CaptureWindow { Mode = 1 }; 
        if (cw.ShowDialog() != true) return;
        
        var absoluteX = (int)cw.SelectedPoint.X;
        var absoluteY = (int)cw.SelectedPoint.Y;
        
        var (relativeX, relativeY, success, errorMessage) = _windowService.ConvertToRelativeCoordinates(
            absoluteX, absoluteY, WindowTitle, WindowClassName);
        
        X = relativeX;
        Y = relativeY;
        
        if (!string.IsNullOrEmpty(WindowTitle) || !string.IsNullOrEmpty(WindowClassName))
        {
            if (success)
            {
                _notifier.ShowInfo(
                    $"ウィンドウ相対座標を設定しました: ({X}, {Y})\nウィンドウ: {WindowTitle}[{WindowClassName}]\n絶対座標: ({absoluteX}, {absoluteY})", 
                    "座標設定完了");
            }
            else
            {
                _notifier.ShowWarning(
                    $"{errorMessage}\n絶対座標 ({X}, {Y}) を設定しました。", 
                    "警告");
            }
        }
        else
        {
            _notifier.ShowInfo($"絶対座標を設定しました: ({X}, {Y})", "座標設定完了");
        }
    }
    
    [RelayCommand] public void BrowseModel() { var f = _panelDialogService.SelectModelFile(); if (f != null) ModelPath = f; }
    [RelayCommand] public void BrowseProgram() { var f = _panelDialogService.SelectExecutableFile(); if (f != null) ProgramPath = f; }
    [RelayCommand] public void BrowseWorkingDirectory() { var d = _panelDialogService.SelectFolder(); if (d != null) WorkingDirectory = d; }
    [RelayCommand] public void BrowseSaveDirectory() { var d = _panelDialogService.SelectFolder(); if (d != null) SaveDirectory = d; }
    #endregion

    #region External API
    public ICommandListItem? GetItem() => Item;
    public void SetItem(ICommandListItem? item) => Item = item;
    public void SetListCount(int listCount) => ListCount = listCount;
    public void SetRunningState(bool isRunning) => IsRunning = isRunning;
    public void Prepare() { }
    #endregion
}


