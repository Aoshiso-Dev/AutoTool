using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Desktop.Panels.ViewModel.Helpers;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Desktop.Panels.ViewModel.Shared;
using AutoTool.Desktop.Panels.Services;
using AutoTool.Commands.Services;
using AutoTool.Infrastructure.Panels;
using AutoTool.Application.Ports;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 画面状態とユーザー操作を管理する ViewModel です。
/// </summary>
public partial class EditPanelViewModel : ObservableObject, IEditPanelViewModel
{
    private readonly EditPanelPropertyManager _propertyManager = new();
    private readonly ICommandRegistry _commandRegistry;
    private readonly IWindowService _windowService;
    private readonly IPathResolver _pathResolver;
    private readonly IImageMatcher _imageMatcher;
    private readonly IOcrEngine _ocrEngine;
    private readonly IObjectDetector _objectDetector;
    private readonly IDetectionHighlightService _detectionHighlightService;
    private readonly INotifier _notifier;
    private readonly IPanelDialogService _panelDialogService;
    private readonly ICapturePathProvider _capturePathProvider;
    private readonly PropertyMetadataProvider _metadataProvider;

    public event Action<ICommandListItem?>? ItemEdited;

    [ObservableProperty]
    private bool _isRunning;

    private bool _isUpdating;

    private static readonly string[] IsPropertyNames = [
        nameof(IsListNotEmpty), nameof(IsListEmpty), nameof(IsListNotEmptyButNoSelection),
        nameof(IsNotNullItem), nameof(IsWaitImageItem),
        nameof(IsClickImageItem), nameof(IsClickImageAIItem), nameof(IsHotkeyItem),
        nameof(IsClickItem), nameof(IsWaitItem), nameof(IsLoopItem),
        nameof(IsEndLoopItem), nameof(IsBreakItem), nameof(IsIfImageExistItem),
        nameof(IsIfImageNotExistItem), nameof(IsEndIfItem), nameof(IsIfImageExistAIItem),
        nameof(IsIfImageNotExistAIItem), nameof(IsExecuteProgramItem), nameof(IsSetVariableItem),
        nameof(IsSetVariableAIItem), nameof(IsIfVariableItem), nameof(IsScreenshotItem),
        nameof(IsOcrPreviewAvailable), nameof(IsImageSearchPreviewAvailable), nameof(IsAiDetectionPreviewAvailable)
    ];

    private static readonly string[] AllPropertyNames = [
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
        nameof(SaveDirectory), nameof(Comment), nameof(OcrPreviewSummary),
        nameof(OcrPreviewText), nameof(OcrPreviewConfidenceText),
        nameof(HasOcrPreviewResult), nameof(IsOcrPreviewRunning),
        nameof(ImageSearchPreviewSummary), nameof(ImageSearchPreviewDetail),
        nameof(ImageSearchSearchArea), nameof(ImageSearchRecoveryGuide),
        nameof(HasImageSearchPreviewResult), nameof(IsImageSearchPreviewRunning),
        nameof(IsOcrAutoTuning), nameof(OcrAutoTuneSummary),
        nameof(IsImageSearchAutoTuning), nameof(ImageSearchAutoTuneSummary),
        nameof(IsAiDetectionAutoTuning), nameof(AiDetectionAutoTuneSummary),
        nameof(AiDetectionPreviewSummary), nameof(AiDetectionPreviewDetail),
        nameof(AiDetectionSearchArea), nameof(AiDetectionRecoveryGuide),
        nameof(HasAiDetectionPreviewResult), nameof(IsAiDetectionPreviewRunning)
    ];

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

