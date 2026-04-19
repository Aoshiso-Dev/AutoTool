using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Panels.View;
using CommunityToolkit.Mvvm.Input;

namespace AutoTool.Desktop.Panels.ViewModel;

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
            var mat = Win32ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion);
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
            OcrPreviewSummary = $"OCRプレビューに失敗しました: {ex.Message}";
        }
        finally
        {
            IsOcrPreviewRunning = false;
            RunOcrPreviewCommand.NotifyCanExecuteChanged();
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
            var result = await FindImageExecutor.ExecuteAsync(
                options,
                (imagePath, threshold, searchColor, windowTitle, windowClassName, cancellationToken) =>
                    _imageMatcher.SearchImageAsync(imagePath, cancellationToken, threshold, searchColor, windowTitle, windowClassName),
                _ => { },
                CancellationToken.None);

            HasImageSearchPreviewResult = true;
            if (result.Found && result.Point is { } point)
            {
                var imageHighlightBounds = BuildImageHighlightBounds(point, options);
                if (imageHighlightBounds is { } imageBounds)
                {
                    await _detectionHighlightService.BlinkAsync(imageBounds, CancellationToken.None);
                }

                ImageSearchPreviewSummary = $"画像検索テスト: 一致（しきい値 {options.Threshold:F2}）";
                ImageSearchPreviewDetail = $"ヒット座標: X={point.X}, Y={point.Y} / 所要時間: {result.ElapsedMilliseconds}ms";
                ImageSearchRecoveryGuide = "次の操作: この設定で実行できます。誤検知が出る場合はしきい値を0.03〜0.05上げてください。";
            }
            else
            {
                ImageSearchPreviewSummary = $"画像検索テスト: 不一致（しきい値 {options.Threshold:F2}）";
                ImageSearchPreviewDetail = $"所要時間: {result.ElapsedMilliseconds}ms / テンプレート: {System.IO.Path.GetFileName(options.ImagePath)}";
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
            var targetDetections = request.TargetClassId is { } classId
                ? detections.Where(x => x.ClassId == classId).ToList()
                : detections.ToList();

            HasAiDetectionPreviewResult = true;

            if (targetDetections.Count == 0)
            {
                AiDetectionPreviewSummary = request.TargetClassId is { } missingClassId
                    ? $"AI検出テスト: 不一致（クラスID {missingClassId}）"
                    : "AI検出テスト: 不一致";
                AiDetectionPreviewDetail = $"検出数: 全体={detections.Count} / 対象=0";
                AiDetectionRecoveryGuide = BuildAiDetectionRecoveryGuide(request, detections.Count);
                return;
            }

            var best = targetDetections.OrderByDescending(x => x.Score).First();
            if (TryBuildAiHighlightBounds(best, request, out var highlightBounds))
            {
                await _detectionHighlightService.BlinkAsync(highlightBounds, CancellationToken.None);
            }

            AiDetectionPreviewSummary = request.TargetClassId is { } targetClassId
                ? $"AI検出テスト: 一致（クラスID {targetClassId}）"
                : "AI検出テスト: 一致";
            AiDetectionPreviewDetail = $"検出数: 全体={detections.Count} / 対象={targetDetections.Count} / 先頭スコア={best.Score:F2} / Rect=({best.Rect.X},{best.Rect.Y},{best.Rect.Width},{best.Rect.Height})";
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
    #endregion

    private bool CanRunOcrPreview() => !IsRunning && !IsOcrPreviewRunning && IsOcrPreviewAvailable;
    private bool CanRunImageSearchPreview() => !IsRunning && !IsImageSearchPreviewRunning && IsImageSearchPreviewAvailable;
    private bool CanRunAiDetectionPreview() => !IsRunning && !IsAiDetectionPreviewRunning && IsAiDetectionPreviewAvailable;

    partial void OnIsRunningChanged(bool value)
    {
        RunOcrPreviewCommand.NotifyCanExecuteChanged();
        RunImageSearchPreviewCommand.NotifyCanExecuteChanged();
        RunAiDetectionPreviewCommand.NotifyCanExecuteChanged();
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
            .FirstOrDefault(prop => prop.PropertyInfo.Name == propertyName);
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

    private bool TryBuildAiDetectionPreviewRequest(out AiDetectionPreviewRequest request)
    {
        request = new AiDetectionPreviewRequest(string.Empty, string.Empty, string.Empty, null, 0.5f, 0.25f);
        switch (Item)
        {
            case IIfImageExistAIItem ifExist:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(ifExist.ModelPath),
                    ifExist.WindowTitle,
                    ifExist.WindowClassName,
                    ifExist.ClassID,
                    (float)ifExist.ConfThreshold,
                    (float)ifExist.IoUThreshold);
                return true;
            case IIfImageNotExistAIItem ifNotExist:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(ifNotExist.ModelPath),
                    ifNotExist.WindowTitle,
                    ifNotExist.WindowClassName,
                    ifNotExist.ClassID,
                    (float)ifNotExist.ConfThreshold,
                    (float)ifNotExist.IoUThreshold);
                return true;
            case IClickImageAIItem clickAi:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(clickAi.ModelPath),
                    clickAi.WindowTitle,
                    clickAi.WindowClassName,
                    clickAi.ClassID,
                    (float)clickAi.ConfThreshold,
                    (float)clickAi.IoUThreshold);
                return true;
            case ISetVariableAIItem setVariableAi:
                request = new AiDetectionPreviewRequest(
                    ToAbsolutePathIfAny(setVariableAi.ModelPath),
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

    private static string BuildAiDetectionRecoveryGuide(AiDetectionPreviewRequest request, int totalCount)
    {
        var hints = new List<string>();
        if (request.TargetClassId is { } classId)
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

    private readonly record struct AiDetectionPreviewRequest(
        string ModelPath,
        string WindowTitle,
        string WindowClassName,
        int? TargetClassId,
        float ConfThreshold,
        float IoUThreshold);
}
