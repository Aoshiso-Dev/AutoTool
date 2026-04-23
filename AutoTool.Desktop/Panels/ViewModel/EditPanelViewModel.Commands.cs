using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Automation.Runtime.Diagnostics;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Panels.View;
using CommunityToolkit.Mvvm.Input;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 画面状態とユーザー操作を管理する ViewModel です。
/// </summary>
public partial class EditPanelViewModel
{
    #region Commands
    [RelayCommand] private void Enter(KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) UpdateProperties(); }

    [RelayCommand]
    public void GetWindowInfo()
    {
        var prop = FindProperty(nameof(WindowTitle));
        if (prop is not null)
        {
            GetWindowInfoForProperty(prop);
            return;
        }

        var w = new GetWindowInfoWindow();
        if (w.ShowDialog() == true)
        {
            WindowTitle = w.WindowTitle;
            WindowClassName = w.WindowClassName;
        }
    }

    [RelayCommand] public void ClearWindowInfo() { WindowTitle = string.Empty; WindowClassName = string.Empty; }

    [RelayCommand]
    public void Browse()
    {
        var prop = FindProperty(nameof(WaitImageItem.ImagePath));
        if (prop is not null)
        {
            BrowseImageForProperty(prop);
            return;
        }

        var f = _panelDialogService.SelectImageFile();
        if (f is not null) ImagePath = f;
    }

    [RelayCommand]
    public void Capture()
    {
        var prop = FindProperty(nameof(WaitImageItem.ImagePath));
        if (prop is not null)
        {
            CaptureImageForProperty(prop);
            return;
        }

        var cw = new CaptureWindow { Mode = 0 };
        if (cw.ShowDialog() == true)
        {
            var path = _capturePathProvider.CreateCaptureFilePath();
            var mat = Win32ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion.X, cw.SelectedRegion.Y, cw.SelectedRegion.Width, cw.SelectedRegion.Height);
            Win32ScreenCaptureHelper.SaveCapture(mat, path);
            ImagePath = path;
        }
    }

    [RelayCommand]
    public void PickSearchColor()
    {
        var prop = FindProperty(nameof(WaitImageItem.SearchColor));
        if (prop is not null)
        {
            PickColorForProperty(prop);
            return;
        }

        var w = new ColorPickWindow();
        w.ShowDialog();
        SearchColor = w.Color;
    }

    [RelayCommand] public void ClearSearchColor() { SearchColor = null; }

    [RelayCommand]
    public void PickPoint()
    {
        var xProp = FindProperty(nameof(ClickItem.X));
        if (xProp is not null)
        {
            PickPointForProperty(xProp);
            return;
        }

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

    [RelayCommand] public void BrowseModel() { var f = _panelDialogService.SelectModelFile(); if (f is not null) ModelPath = f; }
    [RelayCommand] public void BrowseProgram() { var f = _panelDialogService.SelectExecutableFile(); if (f is not null) ProgramPath = f; }
    [RelayCommand] public void BrowseWorkingDirectory() { var d = _panelDialogService.SelectFolder(); if (d is not null) WorkingDirectory = d; }
    [RelayCommand] public void BrowseSaveDirectory() { var d = _panelDialogService.SelectFolder(); if (d is not null) SaveDirectory = d; }

    [RelayCommand(CanExecute = nameof(CanRunOcrPreview))]
    private async Task RunOcrPreviewAsync()
    {
        if (!TryBuildOcrRequest(out var request))
        {
            OcrPreviewSummary = "OCRプレビュー対象のコマンドを選択してください。";
            HasOcrPreviewResult = false;
            return;
        }

        IsOcrPreviewRunning = true;
        RunOcrPreviewCommand.NotifyCanExecuteChanged();

        try
        {
            var sw = Stopwatch.StartNew();
            var result = await _ocrEngine.ExtractTextAsync(request, CancellationToken.None);
            sw.Stop();

            var ocrHighlightBounds = BuildOcrHighlightBounds(request);
            if (ocrHighlightBounds is { } ocrBounds)
            {
                await _detectionHighlightService.BlinkAsync(ocrBounds, CancellationToken.None);
            }

            HasOcrPreviewResult = true;
            OcrPreviewText = string.IsNullOrWhiteSpace(result.Text) ? "（抽出結果なし）" : result.Text;
            OcrPreviewConfidenceText = $"信頼度: {result.Confidence:F1}% / 処理時間: {sw.ElapsedMilliseconds}ms";
            OcrPreviewSummary = "OCRプレビューを実行しました。";
        }
        catch (Exception ex)
        {
            HasOcrPreviewResult = true;
            OcrPreviewText = string.Empty;
            OcrPreviewConfidenceText = string.Empty;
            OcrPreviewSummary = $"OCRプレビューに失敗しました: {ExceptionDetailsFormatter.GetMostRelevantMessage(ex)}";
        }
        finally
        {
            IsOcrPreviewRunning = false;
            RunOcrPreviewCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunOcrAutoTune))]
    private async Task RunOcrAutoTuneAsync()
    {
        if (!TryBuildOcrRequest(out var request))
        {
            OcrAutoTuneSummary = "OCR自動調整の対象コマンドを選択してください。";
            return;
        }

        IsOcrAutoTuning = true;
        RunOcrAutoTuneCommand.NotifyCanExecuteChanged();

        try
        {
            string[] preprocessModes = ["Gray", "Binarize", "AdaptiveThreshold", "None"];
            string[] psmModes = ["6", "7", "11", "13"];
            var candidates = new List<OcrTuneCandidate>();
            string? firstFailureMessage = null;

            foreach (var preprocess in preprocessModes)
            {
                foreach (var psm in psmModes)
                {
                    var tuneRequest = new OcrRequest
                    {
                        X = request.X,
                        Y = request.Y,
                        Width = request.Width,
                        Height = request.Height,
                        WindowTitle = request.WindowTitle,
                        WindowClassName = request.WindowClassName,
                        Language = request.Language,
                        PageSegmentationMode = psm,
                        Whitelist = request.Whitelist,
                        PreprocessMode = preprocess,
                        TessdataPath = request.TessdataPath
                    };

                    try
                    {
                        var result = await _ocrEngine.ExtractTextAsync(tuneRequest, CancellationToken.None);
                        var trimmed = (result.Text ?? string.Empty).Trim();
                        var lengthScore = Math.Min(20, trimmed.Length);
                        var score = (string.IsNullOrWhiteSpace(trimmed) ? -100d : 0d) + result.Confidence + lengthScore;
                        candidates.Add(new OcrTuneCandidate(preprocess, psm, result.Confidence, trimmed, score));
                    }
                    catch (Exception ex)
                    {
                        firstFailureMessage ??= ExceptionDetailsFormatter.GetMostRelevantMessage(ex);
                        candidates.Add(new OcrTuneCandidate(preprocess, psm, 0d, string.Empty, -200d));
                    }
                }
            }

            var best = candidates
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Confidence)
                .FirstOrDefault();

            if (candidates.Count == 0 || best.Score <= -150d)
            {
                OcrAutoTuneSummary = string.IsNullOrWhiteSpace(firstFailureMessage)
                    ? "OCR自動調整: 推奨候補を作れませんでした。範囲・言語・tessdataを確認してください。"
                    : $"OCR自動調整: 推奨候補を作れませんでした（原因: {firstFailureMessage}）。";
                return;
            }

            var recommendedMinConfidence = Math.Clamp(Math.Floor(best.Confidence * 0.8), 30d, 95d);
            ApplyOcrTunedValues(best.PreprocessMode, best.PageSegmentationMode, recommendedMinConfidence);
            OcrAutoTuneSummary = $"OCR自動調整: 推奨 前処理={best.PreprocessMode} / PSM={best.PageSegmentationMode} / 最小信頼度={recommendedMinConfidence:F0}";
            OcrPreviewSummary = "OCR自動調整を適用しました。";
            OcrPreviewText = string.IsNullOrWhiteSpace(best.TextSample) ? "（抽出結果なし）" : best.TextSample;
            OcrPreviewConfidenceText = $"推奨候補の信頼度: {best.Confidence:F1}%";
            HasOcrPreviewResult = true;
        }
        finally
        {
            IsOcrAutoTuning = false;
            RunOcrAutoTuneCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunImageSearchPreview))]
    private async Task RunImageSearchPreviewAsync()
    {
        if (!TryBuildImageSearchOptions(out var options))
        {
            ImageSearchPreviewSummary = "画像検索テスト対象のコマンドを選択してください。";
            HasImageSearchPreviewResult = false;
            return;
        }

        IsImageSearchPreviewRunning = true;
        RunImageSearchPreviewCommand.NotifyCanExecuteChanged();

        try
        {
            ImageSearchSearchArea = BuildImageSearchAreaPreview(options.WindowTitle, options.WindowClassName);
            var sw = Stopwatch.StartNew();
            var hits = await _imageMatcher.SearchImagesAsync(
                options.ImagePath,
                CancellationToken.None,
                options.Threshold,
                options.SearchColor,
                options.WindowTitle,
                options.WindowClassName,
                20);
            sw.Stop();

            HasImageSearchPreviewResult = true;
            if (hits.Count > 0)
            {
                var highlightBounds = hits
                    .Select(point => BuildImageHighlightBounds(point, options))
                    .Where(static x => x is not null)
                    .Select(static x => x!.Value)
                    .ToArray();
                if (highlightBounds.Length > 0)
                {
                    await _detectionHighlightService.BlinkAsync(highlightBounds, CancellationToken.None);
                }

                var first = hits[0];
                ImageSearchPreviewSummary = $"画像検索テスト: 一致（しきい値 {options.Threshold:F2}）";
                ImageSearchPreviewDetail = $"ヒット数: {hits.Count} / 先頭座標: X={first.X}, Y={first.Y} / 所要時間: {sw.ElapsedMilliseconds}ms";
                ImageSearchRecoveryGuide = "次の操作: この設定で実行できます。誤検知が出る場合はしきい値を0.03〜0.05上げてください。";
            }
            else
            {
                ImageSearchPreviewSummary = $"画像検索テスト: 不一致（しきい値 {options.Threshold:F2}）";
                ImageSearchPreviewDetail = $"所要時間: {sw.ElapsedMilliseconds}ms / テンプレート: {System.IO.Path.GetFileName(options.ImagePath)}";
                ImageSearchRecoveryGuide = BuildImageSearchRecoveryGuide(options);
            }
        }
        catch (Exception ex)
        {
            HasImageSearchPreviewResult = true;
            ImageSearchPreviewSummary = $"画像検索テストに失敗しました: {ex.Message}";
            ImageSearchPreviewDetail = string.Empty;
            ImageSearchRecoveryGuide = "次の確認: テンプレート画像の存在、探索対象ウィンドウ名、しきい値(0.0〜1.0)を見直してください。";
        }
        finally
        {
            IsImageSearchPreviewRunning = false;
            RunImageSearchPreviewCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunImageSearchAutoTune))]
    private async Task RunImageSearchAutoTuneAsync()
    {
        if (!TryBuildImageSearchOptions(out var options))
        {
            ImageSearchAutoTuneSummary = "画像検索自動調整の対象コマンドを選択してください。";
            return;
        }

        IsImageSearchAutoTuning = true;
        RunImageSearchAutoTuneCommand.NotifyCanExecuteChanged();

        try
        {
            double[] thresholds = [0.95, 0.9, 0.85, 0.8, 0.75, 0.7, 0.65, 0.6];
            var results = new List<(double Threshold, bool Found, int Elapsed, MatchPoint? Point)>();

            foreach (var threshold in thresholds)
            {
                var probe = options with { Threshold = threshold };
                var result = await FindImageExecutor.ExecuteAsync(
                    probe,
                    (imagePath, th, searchColor, windowTitle, windowClassName, cancellationToken) =>
                        _imageMatcher.SearchImageAsync(imagePath, cancellationToken, th, searchColor, windowTitle, windowClassName),
                    _ => { },
                    CancellationToken.None);
                results.Add((threshold, result.Found, result.ElapsedMilliseconds, result.Point));
            }

            var best = results
                .Where(x => x.Found)
                .OrderByDescending(x => x.Threshold)
                .ThenBy(x => x.Elapsed)
                .FirstOrDefault();

            if (!best.Found)
            {
                ImageSearchAutoTuneSummary = "画像検索自動調整: 一致するしきい値候補が見つかりませんでした。";
                return;
            }

            ApplyImageSearchTunedThreshold(best.Threshold);
            ImageSearchAutoTuneSummary = $"画像検索自動調整: 推奨しきい値 {best.Threshold:F2}";
            ImageSearchPreviewSummary = $"画像検索テスト: 推奨しきい値を適用しました（{best.Threshold:F2}）";
            if (best.Point is { } point)
            {
                ImageSearchPreviewDetail = $"推奨候補ヒット座標: X={point.X}, Y={point.Y} / 所要時間: {best.Elapsed}ms";
            }
        }
        finally
        {
            IsImageSearchAutoTuning = false;
            RunImageSearchAutoTuneCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunAiDetectionPreview))]
    private async Task RunAiDetectionPreviewAsync()
    {
        if (!TryBuildAiDetectionPreviewRequest(out var request))
        {
            AiDetectionPreviewSummary = "AI検出テスト対象のコマンドを選択してください。";
            HasAiDetectionPreviewResult = false;
            return;
        }

        IsAiDetectionPreviewRunning = true;
        RunAiDetectionPreviewCommand.NotifyCanExecuteChanged();

        try
        {
            if (!System.IO.File.Exists(request.ModelPath))
            {
                throw new FileNotFoundException($"モデルファイルが見つかりません: {request.ModelPath}");
            }

            AiDetectionSearchArea = BuildImageSearchAreaPreview(request.WindowTitle, request.WindowClassName);
            _objectDetector.Initialize(request.ModelPath, 640, true);
            var detections = _objectDetector.Detect(request.WindowTitle, request.ConfThreshold, request.IoUThreshold);
            var targetClassId = ResolveAiTargetClassIdForPreview(request);
            var targetDetections = targetClassId is { } classId
                ? detections.Where(x => x.ClassId == classId).ToList()
                : detections.ToList();

            HasAiDetectionPreviewResult = true;

            if (targetDetections.Count == 0)
            {
                AiDetectionPreviewSummary = targetClassId is { } missingClassId
                    ? $"AI検出テスト: 不一致（クラスID {missingClassId}）"
                    : "AI検出テスト: 不一致";
                AiDetectionPreviewDetail = $"検出数: 全体={detections.Count} / 対象=0";
                AiDetectionRecoveryGuide = BuildAiDetectionRecoveryGuide(request, detections.Count);
                return;
            }

            var best = targetDetections.OrderByDescending(x => x.Score).First();
            var highlightBounds = BuildAiHighlightBounds(targetDetections, request);
            if (highlightBounds.Count > 0)
            {
                await _detectionHighlightService.BlinkAsync(highlightBounds, CancellationToken.None);
            }

            AiDetectionPreviewSummary = targetClassId is { } resolvedClassId
                ? $"AI検出テスト: 一致（クラスID {resolvedClassId}）"
                : "AI検出テスト: 一致";
            var targetClassText = targetClassId is { } resolvedId ? resolvedId.ToString() : "全クラス";
            AiDetectionPreviewDetail = $"検出数: 全体={detections.Count} / 対象={targetDetections.Count} / 対象クラス={targetClassText} / 先頭スコア={best.Score:F2} / Rect=({best.Rect.X},{best.Rect.Y},{best.Rect.Width},{best.Rect.Height})";
            AiDetectionRecoveryGuide = "次の操作: この設定で実行できます。誤検知が多い場合は信頼度しきい値を上げてください。";
        }
        catch (Exception ex)
        {
            HasAiDetectionPreviewResult = true;
            AiDetectionPreviewSummary = $"AI検出テストに失敗しました: {ex.Message}";
            AiDetectionPreviewDetail = string.Empty;
            AiDetectionRecoveryGuide = "次の確認: モデルパス、クラスID、信頼度しきい値、対象ウィンドウを見直してください。";
        }
        finally
        {
            IsAiDetectionPreviewRunning = false;
            RunAiDetectionPreviewCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunAiDetectionAutoTune))]
    private async Task RunAiDetectionAutoTuneAsync()
    {
        await Task.Yield();

        if (!TryBuildAiDetectionPreviewRequest(out var request))
        {
            AiDetectionAutoTuneSummary = "AI検出自動調整の対象コマンドを選択してください。";
            return;
        }

        IsAiDetectionAutoTuning = true;
        RunAiDetectionAutoTuneCommand.NotifyCanExecuteChanged();

        try
        {
            if (!File.Exists(request.ModelPath))
            {
                throw new FileNotFoundException($"モデルファイルが見つかりません: {request.ModelPath}");
            }

            _objectDetector.Initialize(request.ModelPath, 640, true);
            float[] confCandidates = [0.35f, 0.45f, 0.55f, 0.65f, 0.75f];
            float[] iouCandidates = [0.2f, 0.3f, 0.45f];
            var targetClassId = ResolveAiTargetClassIdForPreview(request);

            var candidates = new List<AiTuneCandidate>();
            foreach (var conf in confCandidates)
            {
                foreach (var iou in iouCandidates)
                {
                    var detections = _objectDetector.Detect(request.WindowTitle, conf, iou);
                    var targets = targetClassId is { } classId
                        ? detections.Where(x => x.ClassId == classId).ToList()
                        : detections.ToList();
                    var bestScore = targets.Count > 0 ? targets.Max(x => x.Score) : 0f;
                    candidates.Add(new AiTuneCandidate(conf, iou, detections.Count, targets.Count, bestScore));
                }
            }

            var best = candidates
                .OrderByDescending(x => x.TargetCount > 0)
                .ThenByDescending(x => x.TargetCount)
                .ThenBy(x => x.TotalCount)
                .ThenByDescending(x => x.BestScore)
                .ThenByDescending(x => x.ConfThreshold)
                .FirstOrDefault();

            if (candidates.Count == 0 || best.TargetCount <= 0)
            {
                AiDetectionAutoTuneSummary = "AI検出自動調整: 一致候補が見つかりませんでした。";
                return;
            }

            ApplyAiTunedValues(best.ConfThreshold, best.IoUThreshold);
            AiDetectionAutoTuneSummary = $"AI検出自動調整: 推奨 conf={best.ConfThreshold:F2} / iou={best.IoUThreshold:F2}";
            AiDetectionPreviewSummary = "AI検出テスト: 推奨しきい値を適用しました。";
            AiDetectionPreviewDetail = $"推奨候補: 対象検出={best.TargetCount} / 全検出={best.TotalCount} / 最高スコア={best.BestScore:F2}";
        }
        finally
        {
            IsAiDetectionAutoTuning = false;
            RunAiDetectionAutoTuneCommand.NotifyCanExecuteChanged();
        }
    }
    #endregion

    private bool CanRunOcrPreview() => !IsRunning && !IsOcrPreviewRunning && IsOcrPreviewAvailable;
    private bool CanRunOcrAutoTune() => !IsRunning && !IsOcrPreviewRunning && !IsOcrAutoTuning && IsOcrPreviewAvailable;
    private bool CanRunImageSearchPreview() => !IsRunning && !IsImageSearchPreviewRunning && IsImageSearchPreviewAvailable;
    private bool CanRunImageSearchAutoTune() => !IsRunning && !IsImageSearchPreviewRunning && !IsImageSearchAutoTuning && IsImageSearchPreviewAvailable;
    private bool CanRunAiDetectionPreview() => !IsRunning && !IsAiDetectionPreviewRunning && IsAiDetectionPreviewAvailable;
    private bool CanRunAiDetectionAutoTune() => !IsRunning && !IsAiDetectionPreviewRunning && !IsAiDetectionAutoTuning && IsAiDetectionPreviewAvailable;

    partial void OnIsRunningChanged(bool value)
    {
        RunOcrPreviewCommand.NotifyCanExecuteChanged();
        RunOcrAutoTuneCommand.NotifyCanExecuteChanged();
        RunImageSearchPreviewCommand.NotifyCanExecuteChanged();
        RunImageSearchAutoTuneCommand.NotifyCanExecuteChanged();
        RunAiDetectionPreviewCommand.NotifyCanExecuteChanged();
        RunAiDetectionAutoTuneCommand.NotifyCanExecuteChanged();
    }

    private bool TryBuildOcrRequest(out OcrRequest request)
    {
        request = new OcrRequest();

        switch (Item)
        {
            case IFindTextItem findText:
                request = new OcrRequest
                {
                    X = findText.X,
                    Y = findText.Y,
                    Width = findText.Width,
                    Height = findText.Height,
                    WindowTitle = findText.WindowTitle,
                    WindowClassName = findText.WindowClassName,
                    Language = findText.Language,
                    PageSegmentationMode = findText.PageSegmentationMode,
                    Whitelist = findText.Whitelist,
                    PreprocessMode = findText.PreprocessMode,
                    TessdataPath = ToAbsolutePathIfAny(findText.TessdataPath)
                };
                return true;
            case IIfTextExistItem ifTextExist:
                request = new OcrRequest
                {
                    X = ifTextExist.X,
                    Y = ifTextExist.Y,
                    Width = ifTextExist.Width,
                    Height = ifTextExist.Height,
                    WindowTitle = ifTextExist.WindowTitle,
                    WindowClassName = ifTextExist.WindowClassName,
                    Language = ifTextExist.Language,
                    PageSegmentationMode = ifTextExist.PageSegmentationMode,
                    Whitelist = ifTextExist.Whitelist,
                    PreprocessMode = ifTextExist.PreprocessMode,
                    TessdataPath = ToAbsolutePathIfAny(ifTextExist.TessdataPath)
                };
                return true;
            case IIfTextNotExistItem ifTextNotExist:
                request = new OcrRequest
                {
                    X = ifTextNotExist.X,
                    Y = ifTextNotExist.Y,
                    Width = ifTextNotExist.Width,
                    Height = ifTextNotExist.Height,
                    WindowTitle = ifTextNotExist.WindowTitle,
                    WindowClassName = ifTextNotExist.WindowClassName,
                    Language = ifTextNotExist.Language,
                    PageSegmentationMode = ifTextNotExist.PageSegmentationMode,
                    Whitelist = ifTextNotExist.Whitelist,
                    PreprocessMode = ifTextNotExist.PreprocessMode,
                    TessdataPath = ToAbsolutePathIfAny(ifTextNotExist.TessdataPath)
                };
                return true;
            case ISetVariableOCRItem setVariableOcr:
                request = new OcrRequest
                {
                    X = setVariableOcr.X,
                    Y = setVariableOcr.Y,
                    Width = setVariableOcr.Width,
                    Height = setVariableOcr.Height,
                    WindowTitle = setVariableOcr.WindowTitle,
                    WindowClassName = setVariableOcr.WindowClassName,
                    Language = setVariableOcr.Language,
                    PageSegmentationMode = setVariableOcr.PageSegmentationMode,
                    Whitelist = setVariableOcr.Whitelist,
                    PreprocessMode = setVariableOcr.PreprocessMode,
                    TessdataPath = ToAbsolutePathIfAny(setVariableOcr.TessdataPath)
                };
                return true;
            default:
                return false;
        }
    }

    private string ToAbsolutePathIfAny(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : _pathResolver.ToAbsolutePath(path);
    }

    private PropertyMetadata? FindProperty(string propertyName)
    {
        return PropertyGroups
            .SelectMany(group => group.Properties)
            .FirstOrDefault(prop => prop.PropertyName == propertyName);
    }

    private bool TryBuildImageSearchOptions(out FindImageOptions options)
    {
        options = new FindImageOptions
        {
            ImagePath = string.Empty
        };

        switch (Item)
        {
            case IWaitImageItem waitImage:
                options = CreateFindImageOptions(waitImage.ImagePath, waitImage.Threshold, waitImage.SearchColor, waitImage.WindowTitle, waitImage.WindowClassName);
                return true;
            case IClickImageItem clickImage:
                options = CreateFindImageOptions(clickImage.ImagePath, clickImage.Threshold, clickImage.SearchColor, clickImage.WindowTitle, clickImage.WindowClassName);
                return true;
            case IIfImageExistItem ifImageExist:
                options = CreateFindImageOptions(ifImageExist.ImagePath, ifImageExist.Threshold, ifImageExist.SearchColor, ifImageExist.WindowTitle, ifImageExist.WindowClassName);
                return true;
            case IIfImageNotExistItem ifImageNotExist:
                options = CreateFindImageOptions(ifImageNotExist.ImagePath, ifImageNotExist.Threshold, ifImageNotExist.SearchColor, ifImageNotExist.WindowTitle, ifImageNotExist.WindowClassName);
                return true;
            case IFindImageItem findImage:
                options = CreateFindImageOptions(findImage.ImagePath, findImage.Threshold, findImage.SearchColor, findImage.WindowTitle, findImage.WindowClassName);
                return true;
            default:
                return false;
        }
    }

    private FindImageOptions CreateFindImageOptions(
        string imagePath,
        double threshold,
        AutoTool.Commands.Model.Input.CommandColor? searchColor,
        string? windowTitle,
        string? windowClassName)
    {
        return new FindImageOptions
        {
            ImagePath = ToAbsolutePathIfAny(imagePath),
            Threshold = threshold,
            SearchColor = searchColor,
            Timeout = 0,
            Interval = 0,
            WindowTitle = windowTitle ?? string.Empty,
            WindowClassName = windowClassName ?? string.Empty
        };
    }

    private string BuildImageSearchAreaPreview(string? windowTitle, string? windowClassName)
    {
        if (string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(windowClassName))
        {
            return "探索範囲: 画面全体";
        }

        var handle = _windowService.GetWindowHandle(windowTitle, windowClassName);
        if (handle == IntPtr.Zero)
        {
            return $"探索範囲: 指定ウィンドウ（未検出） / Title=\"{windowTitle}\" Class=\"{windowClassName}\"";
        }

        var rect = _windowService.GetWindowRect(handle);
        if (rect is null)
        {
            return $"探索範囲: 指定ウィンドウ（矩形取得失敗） / Title=\"{windowTitle}\" Class=\"{windowClassName}\"";
        }

        return $"探索範囲: 指定ウィンドウ X={rect.Value.Left}, Y={rect.Value.Top}, W={rect.Value.Width}, H={rect.Value.Height}";
    }

    private string BuildImageSearchRecoveryGuide(FindImageOptions options)
    {
        var hints = new List<string>();

        if (options.Threshold >= 0.85)
        {
            hints.Add("しきい値が高めです。0.03〜0.08下げて再テストしてください。");
        }
        else if (options.Threshold <= 0.6)
        {
            hints.Add("しきい値が低めです。誤検知がある場合は0.03〜0.08上げてください。");
        }
        else
        {
            hints.Add("しきい値を0.03刻みで前後に調整してください。");
        }

        if (!string.IsNullOrWhiteSpace(options.WindowTitle) || !string.IsNullOrWhiteSpace(options.WindowClassName))
        {
            hints.Add("探索範囲がウィンドウ指定です。ウィンドウ名・クラス名が現在表示中の画面と一致するか確認してください。");
        }

        hints.Add("テンプレート画像を再キャプチャし、余白を減らしてPNG形式で保存すると一致率が安定しやすくなります。");
        return string.Join(Environment.NewLine, hints);
    }

    private Rectangle? BuildOcrHighlightBounds(OcrRequest request)
    {
        var left = request.X;
        var top = request.Y;

        if (!string.IsNullOrWhiteSpace(request.WindowTitle) || !string.IsNullOrWhiteSpace(request.WindowClassName))
        {
            var handle = _windowService.GetWindowHandle(request.WindowTitle, request.WindowClassName);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var rect = _windowService.GetWindowRect(handle);
            if (rect is null)
            {
                return null;
            }

            left += rect.Value.Left;
            top += rect.Value.Top;
        }

        return new Rectangle(left, top, Math.Max(1, request.Width), Math.Max(1, request.Height));
    }

    private Rectangle? BuildImageHighlightBounds(MatchPoint point, FindImageOptions options)
    {
        if (!TryGetTemplateEffectiveBounds(
            options.ImagePath,
            out var templateWidth,
            out var templateHeight,
            out var offsetX,
            out var offsetY,
            out var effectiveWidth,
            out var effectiveHeight))
        {
            return null;
        }

        var centerX = point.X;
        var centerY = point.Y;
        if (!string.IsNullOrWhiteSpace(options.WindowTitle) || !string.IsNullOrWhiteSpace(options.WindowClassName))
        {
            var handle = _windowService.GetWindowHandle(options.WindowTitle, options.WindowClassName);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var rect = _windowService.GetWindowRect(handle);
            if (rect is null)
            {
                return null;
            }

            centerX += rect.Value.Left;
            centerY += rect.Value.Top;
        }

        var fullLeft = centerX - templateWidth / 2;
        var fullTop = centerY - templateHeight / 2;
        return new Rectangle(
            fullLeft + offsetX,
            fullTop + offsetY,
            effectiveWidth,
            effectiveHeight);
    }

    private static bool TryGetTemplateEffectiveBounds(
        string imagePath,
        out int templateWidth,
        out int templateHeight,
        out int offsetX,
        out int offsetY,
        out int effectiveWidth,
        out int effectiveHeight)
    {
        templateWidth = 0;
        templateHeight = 0;
        offsetX = 0;
        offsetY = 0;
        effectiveWidth = 0;
        effectiveHeight = 0;

        if (string.IsNullOrWhiteSpace(imagePath) || !System.IO.File.Exists(imagePath))
        {
            return false;
        }

        try
        {
            using var bitmap = new Bitmap(imagePath);
            templateWidth = Math.Max(1, bitmap.Width);
            templateHeight = Math.Max(1, bitmap.Height);

            var left = 0;
            var top = 0;
            var right = templateWidth - 1;
            var bottom = templateHeight - 1;

            if (TryFindOpaqueBounds(bitmap, out var opaqueLeft, out var opaqueTop, out var opaqueRight, out var opaqueBottom))
            {
                left = opaqueLeft;
                top = opaqueTop;
                right = opaqueRight;
                bottom = opaqueBottom;
            }
            else
            {
                TrimUniformBorder(bitmap, ref left, ref top, ref right, ref bottom);
            }

            offsetX = Math.Clamp(left, 0, templateWidth - 1);
            offsetY = Math.Clamp(top, 0, templateHeight - 1);
            effectiveWidth = Math.Max(1, Math.Clamp(right - left + 1, 1, templateWidth));
            effectiveHeight = Math.Max(1, Math.Clamp(bottom - top + 1, 1, templateHeight));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryFindOpaqueBounds(
        Bitmap bitmap,
        out int left,
        out int top,
        out int right,
        out int bottom)
    {
        left = bitmap.Width;
        top = bitmap.Height;
        right = -1;
        bottom = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.A < 16)
                {
                    continue;
                }

                if (x < left) left = x;
                if (y < top) top = y;
                if (x > right) right = x;
                if (y > bottom) bottom = y;
            }
        }

        return right >= left && bottom >= top;
    }

    private static void TrimUniformBorder(
        Bitmap bitmap,
        ref int left,
        ref int top,
        ref int right,
        ref int bottom)
    {
        var border = bitmap.GetPixel(0, 0);
        const int tolerance = 8;

        while (left < right && IsUniformVertical(bitmap, left, top, bottom, border, tolerance))
        {
            left++;
        }

        while (right > left && IsUniformVertical(bitmap, right, top, bottom, border, tolerance))
        {
            right--;
        }

        while (top < bottom && IsUniformHorizontal(bitmap, top, left, right, border, tolerance))
        {
            top++;
        }

        while (bottom > top && IsUniformHorizontal(bitmap, bottom, left, right, border, tolerance))
        {
            bottom--;
        }
    }

    private static bool IsUniformVertical(Bitmap bitmap, int x, int top, int bottom, Color border, int tolerance)
    {
        for (var y = top; y <= bottom; y++)
        {
            if (!IsNear(bitmap.GetPixel(x, y), border, tolerance))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsUniformHorizontal(Bitmap bitmap, int y, int left, int right, Color border, int tolerance)
    {
        for (var x = left; x <= right; x++)
        {
            if (!IsNear(bitmap.GetPixel(x, y), border, tolerance))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNear(Color a, Color b, int tolerance)
    {
        return Math.Abs(a.R - b.R) <= tolerance
            && Math.Abs(a.G - b.G) <= tolerance
            && Math.Abs(a.B - b.B) <= tolerance;
    }

    private void ApplyAiTunedValues(float confThreshold, float iouThreshold)
    {
        _ = TryApplyToEditableClone(clone =>
        {
            switch (clone)
            {
                case IIfImageExistAIItem ifExistAi:
                    ifExistAi.ConfThreshold = confThreshold;
                    ifExistAi.IoUThreshold = iouThreshold;
                    break;
                case IIfImageNotExistAIItem ifNotExistAi:
                    ifNotExistAi.ConfThreshold = confThreshold;
                    ifNotExistAi.IoUThreshold = iouThreshold;
                    break;
                case IClickImageAIItem clickAi:
                    clickAi.ConfThreshold = confThreshold;
                    clickAi.IoUThreshold = iouThreshold;
                    break;
                case ISetVariableAIItem setVariableAi:
                    setVariableAi.ConfThreshold = confThreshold;
                    setVariableAi.IoUThreshold = iouThreshold;
                    break;
            }
        });
    }

    private void ApplyOcrTunedValues(string preprocessMode, string pageSegmentationMode, double minConfidence)
    {
        _ = TryApplyToEditableClone(clone =>
        {
            switch (clone)
            {
                case IFindTextItem findText:
                    findText.PreprocessMode = preprocessMode;
                    findText.PageSegmentationMode = pageSegmentationMode;
                    findText.MinConfidence = minConfidence;
                    break;
                case IIfTextExistItem ifTextExist:
                    ifTextExist.PreprocessMode = preprocessMode;
                    ifTextExist.PageSegmentationMode = pageSegmentationMode;
                    ifTextExist.MinConfidence = minConfidence;
                    break;
                case IIfTextNotExistItem ifTextNotExist:
                    ifTextNotExist.PreprocessMode = preprocessMode;
                    ifTextNotExist.PageSegmentationMode = pageSegmentationMode;
                    ifTextNotExist.MinConfidence = minConfidence;
                    break;
                case ISetVariableOCRItem setVariableOcr:
                    setVariableOcr.PreprocessMode = preprocessMode;
                    setVariableOcr.PageSegmentationMode = pageSegmentationMode;
                    setVariableOcr.MinConfidence = minConfidence;
                    break;
            }
        });
    }

    private void ApplyImageSearchTunedThreshold(double threshold)
    {
        _ = TryApplyToEditableClone(clone =>
        {
            switch (clone)
            {
                case IWaitImageItem waitImage:
                    waitImage.Threshold = threshold;
                    break;
                case IClickImageItem clickImage:
                    clickImage.Threshold = threshold;
                    break;
                case IIfImageExistItem ifExistImage:
                    ifExistImage.Threshold = threshold;
                    break;
                case IIfImageNotExistItem ifNotExistImage:
                    ifNotExistImage.Threshold = threshold;
                    break;
                case IFindImageItem findImage:
                    findImage.Threshold = threshold;
                    break;
            }
        });
    }

    private bool TryApplyToEditableClone(Action<ICommandListItem> apply)
    {
        if (Item is null)
        {
            return false;
        }

        var clone = Item.Clone();
        apply(clone);
        Item = clone;
        UpdatePropertyGroups();
        ItemEdited?.Invoke(clone);
        return true;
    }

    private bool TryBuildAiDetectionPreviewRequest(out AiDetectionPreviewRequest request)
    {
        request = new AiDetectionPreviewRequest(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, 0.5f, 0.25f);
        switch (Item)
        {
            case IIfImageExistAIItem ifExist:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(ifExist.ModelPath),
                    ToAbsolutePathIfAny(ifExist.LabelsPath),
                    ifExist.LabelName,
                    ifExist.WindowTitle,
                    ifExist.WindowClassName,
                    ifExist.ClassID,
                    (float)ifExist.ConfThreshold,
                    (float)ifExist.IoUThreshold);
                return true;
            case IIfImageNotExistAIItem ifNotExist:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(ifNotExist.ModelPath),
                    ToAbsolutePathIfAny(ifNotExist.LabelsPath),
                    ifNotExist.LabelName,
                    ifNotExist.WindowTitle,
                    ifNotExist.WindowClassName,
                    ifNotExist.ClassID,
                    (float)ifNotExist.ConfThreshold,
                    (float)ifNotExist.IoUThreshold);
                return true;
            case IClickImageAIItem clickAi:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(clickAi.ModelPath),
                    ToAbsolutePathIfAny(clickAi.LabelsPath),
                    clickAi.LabelName,
                    clickAi.WindowTitle,
                    clickAi.WindowClassName,
                    clickAi.ClassID,
                    (float)clickAi.ConfThreshold,
                    (float)clickAi.IoUThreshold);
                return true;
            case ISetVariableAIItem setVariableAi:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(setVariableAi.ModelPath),
                    ToAbsolutePathIfAny(setVariableAi.LabelsPath),
                    setVariableAi.LabelName,
                    setVariableAi.WindowTitle,
                    setVariableAi.WindowClassName,
                    null,
                    (float)setVariableAi.ConfThreshold,
                    (float)setVariableAi.IoUThreshold);
                return true;
            default:
                return false;
        }
    }

    private bool TryBuildAiHighlightBounds(DetectionResult detection, AiDetectionPreviewRequest request, out Rectangle bounds)
    {
        var left = detection.Rect.X;
        var top = detection.Rect.Y;

        if (!string.IsNullOrWhiteSpace(request.WindowTitle) || !string.IsNullOrWhiteSpace(request.WindowClassName))
        {
            var handle = _windowService.GetWindowHandle(request.WindowTitle, request.WindowClassName);
            if (handle == IntPtr.Zero)
            {
                bounds = default;
                return false;
            }

            var windowRect = _windowService.GetWindowRect(handle);
            if (windowRect is null)
            {
                bounds = default;
                return false;
            }

            left += windowRect.Value.Left;
            top += windowRect.Value.Top;
        }

        bounds = new Rectangle(left, top, Math.Max(1, detection.Rect.Width), Math.Max(1, detection.Rect.Height));
        return true;
    }

    private IReadOnlyList<Rectangle> BuildAiHighlightBounds(IReadOnlyList<DetectionResult> detections, AiDetectionPreviewRequest request)
    {
        var bounds = new List<Rectangle>(detections.Count);
        foreach (var detection in detections)
        {
            if (!TryBuildAiHighlightBounds(detection, request, out var highlightBounds))
            {
                continue;
            }

            bounds.Add(highlightBounds);
        }

        return bounds;
    }

    private int? ResolveAiTargetClassIdForPreview(AiDetectionPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LabelName))
        {
            return request.TargetClassId;
        }

        if (_objectDetector.TryResolveClassId(
                request.ModelPath,
                request.LabelName,
                string.IsNullOrWhiteSpace(request.LabelsPath) ? null : request.LabelsPath,
                out var classId))
        {
            return classId;
        }

        throw new InvalidOperationException($"ラベル '{request.LabelName}' をクラスIDへ解決できません。モデルのmetadataまたはラベルファイルを確認してください。");
    }

    private static string BuildAiDetectionRecoveryGuide(AiDetectionPreviewRequest request, int totalCount)
    {
        var hints = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.LabelName))
        {
            hints.Add($"ラベル '{request.LabelName}' が見つかりません。metadataまたはラベルファイルの対応を確認してください。");
        }
        else if (request.TargetClassId is { } classId)
        {
            hints.Add($"クラスID {classId} が見つかりません。モデルのラベル対応を確認してください。");
        }

        if (request.ConfThreshold >= 0.7f)
        {
            hints.Add("信頼度しきい値が高めです。0.05〜0.15下げて再テストしてください。");
        }
        else if (request.ConfThreshold <= 0.2f && totalCount > 0)
        {
            hints.Add("検出が多すぎる場合は信頼度しきい値を0.05〜0.15上げてください。");
        }
        else
        {
            hints.Add("信頼度しきい値を0.05刻みで調整してください。");
        }

        hints.Add("対象ウィンドウが指定されている場合は、タイトル/クラス名の一致を確認してください。");
        return string.Join(Environment.NewLine, hints);
    }

    /// <summary>
    /// 処理で利用する値を軽量に保持し、受け渡し時のオーバーヘッドを抑えます。
    /// </summary>

    private readonly record struct AiDetectionPreviewRequest(
        string ModelPath,
        string LabelsPath,
        string LabelName,
        string WindowTitle,
        string WindowClassName,
        int? TargetClassId,
        float ConfThreshold,
        float IoUThreshold);

    /// <summary>
    /// 処理で利用する値を軽量に保持し、受け渡し時のオーバーヘッドを抑えます。
    /// </summary>

    private readonly record struct OcrTuneCandidate(
        string PreprocessMode,
        string PageSegmentationMode,
        double Confidence,
        string TextSample,
        double Score);

    /// <summary>
    /// 処理で利用する値を軽量に保持し、受け渡し時のオーバーヘッドを抑えます。
    /// </summary>

    private readonly record struct AiTuneCandidate(
        float ConfThreshold,
        float IoUThreshold,
        int TotalCount,
        int TargetCount,
        float BestScore);
}


