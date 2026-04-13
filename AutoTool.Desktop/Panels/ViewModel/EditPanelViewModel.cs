using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.ViewModel.Helpers;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.ViewModel.Shared;
using AutoTool.Commands.Services;
using AutoTool.Panels.Services;
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

    public event Action<ICommandListItem?>? ItemEdited;

    [ObservableProperty]
    private bool _isRunning;

    private bool _isUpdating;

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

    private void UpdateIsProperties()
    {
        foreach (var name in IsPropertyNames)
        {
            OnPropertyChanged(name);
        }
    }

    private void UpdateProperties()
    {
        if (_isUpdating) return;

        try
        {
            _isUpdating = true;

            foreach (var name in AllPropertyNames)
            {
                OnPropertyChanged(name);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    public ICommandListItem? GetItem() => Item;
    public void SetItem(ICommandListItem? item) => Item = item;
    public void SetListCount(int listCount) => ListCount = listCount;
    public void SetRunningState(bool isRunning) => IsRunning = isRunning;
    public void Prepare() { }
}
