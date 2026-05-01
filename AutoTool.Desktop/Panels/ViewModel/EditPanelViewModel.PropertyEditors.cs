using CommunityToolkit.Mvvm.Input;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Interface;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Desktop.Panels.View;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 画面状態とユーザー操作を管理する ViewModel です。
/// </summary>
public partial class EditPanelViewModel
{
    private const string TessdataFastRepositoryUrl = "https://github.com/tesseract-ocr/tessdata_fast";
    private const string AiModelReferencePageUrl = "https://github.com/ultralytics/ultralytics";
    private const string RecommendedAiModelFileName = "yolo11n.onnx";
    private const string JsonFileFilter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
    private static readonly string[] RecommendedAiModelDownloadUrls =
    [
        "https://github.com/ultralytics/assets/releases/download/v8.4.0/yolo11n.onnx",
        "https://github.com/ultralytics/assets/releases/download/v8.3.0/yolo11n.onnx"
    ];

    private void UpdatePropertyGroups()
    {
        PropertyGroups.Clear();
        if (Item is null) return;

        foreach (var group in _metadataProvider.GetGroupedMetadata(Item))
        {
            PropertyGroups.Add(group);
        }

        foreach (var group in PropertyGroups)
        {
            foreach (var prop in group.Properties)
            {
                SetupPropertyCommands(prop);
                prop.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName is nameof(PropertyMetadata.Value)
                        or nameof(PropertyMetadata.StringValue)
                        or nameof(PropertyMetadata.IntValue)
                        or nameof(PropertyMetadata.DoubleValue)
                        or nameof(PropertyMetadata.BoolValue)
                        or nameof(PropertyMetadata.MouseButtonValue)
                        or nameof(PropertyMetadata.ColorValue)
                        or nameof(PropertyMetadata.KeyValue))
                    {
                        ValidateCurrentItemSettings();
                        if (string.Equals(prop.PropertyName, "LabelName", StringComparison.Ordinal))
                        {
                            SyncClassIdFromLabelSelection(prop);
                        }
                        if (IsAiLabelRelatedProperty(prop.PropertyName))
                        {
                            RefreshAiLabelOptions();
                        }
                    }
                };
            }
        }

        RefreshAiLabelOptions();
        ValidateCurrentItemSettings();
    }

    private void SetupPropertyCommands(PropertyMetadata prop)
    {
        Action setup = prop.EditorType switch
        {
            EditorType.ImagePicker => () =>
            {
                prop.BrowseCommand = new RelayCommand(() => BrowseImageForProperty(prop));
                prop.CaptureCommand = new RelayCommand(() => CaptureImageForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; prop.NotifyAllValueProperties(); });
            },
            EditorType.ColorPicker => () =>
            {
                prop.PickColorCommand = new RelayCommand(() => PickColorForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => prop.Value = null);
            },
            EditorType.WindowInfo => () =>
            {
                prop.GetWindowInfoCommand = new RelayCommand(() => GetWindowInfoForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; prop.NotifyAllValueProperties(); });
            },
            EditorType.FilePicker => () =>
            {
                prop.BrowseCommand = new RelayCommand(() => BrowseFileForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; prop.NotifyAllValueProperties(); });
                ConfigureFilePickerMetadata(prop);
            },
            EditorType.DirectoryPicker => () =>
            {
                prop.BrowseCommand = new RelayCommand(() => BrowseDirectoryForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; prop.NotifyAllValueProperties(); });
                ConfigureDirectoryPickerMetadata(prop);
            },
            EditorType.ComboBox => () => ConfigureComboBoxMetadata(prop),
            EditorType.KeyPicker => () => prop.PickKeyCommand = new RelayCommand(() => PickKeyForProperty(prop)),
            EditorType.PointPicker => () =>
            {
                prop.PickPointCommand = new RelayCommand(() => PickPointForProperty(prop));
                var yProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyName == "Y" && p.Target == prop.Target);
                prop.RelatedProperty = yProp;
                var widthProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyName == "Width" && p.Target == prop.Target);
                var heightProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyName == "Height" && p.Target == prop.Target);
                prop.RelatedProperty2 = widthProp;
                prop.RelatedProperty3 = heightProp;
            },
            _ => static () => { }
        };

        setup();
    }

    private void BrowseImageForProperty(PropertyMetadata prop)
    {
        var path = _panelDialogService.SelectImageFile();
        if (!string.IsNullOrEmpty(path))
        {
            prop.Value = _pathResolver.ToRelativePath(path);
            RefreshEditorState(nameof(ImagePath), nameof(PreviewImagePath), nameof(HasImagePreview));
        }
    }

    private void CaptureImageForProperty(PropertyMetadata prop)
    {
        var cw = new CaptureWindow { Mode = 0 };
        if (cw.ShowDialog() == true)
        {
            var path = _capturePathProvider.CreateCaptureFilePath();
            var mat = Win32ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion.X, cw.SelectedRegion.Y, cw.SelectedRegion.Width, cw.SelectedRegion.Height);
            Win32ScreenCaptureHelper.SaveCapture(mat, path);
            prop.Value = _pathResolver.ToRelativePath(path);
            RefreshEditorState(nameof(ImagePath), nameof(PreviewImagePath), nameof(HasImagePreview));
        }
    }

    private void PickColorForProperty(PropertyMetadata prop)
    {
        var w = new ColorPickWindow();
        w.ShowDialog();
        if (w.Color.HasValue)
        {
            prop.Value = w.Color.Value;
            RefreshEditorState();
        }
    }

    private void GetWindowInfoForProperty(PropertyMetadata prop)
    {
        var w = new GetWindowInfoWindow();
        if (w.ShowDialog() == true)
        {
            prop.Value = w.WindowTitle;

            var classNameProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyName == "WindowClassName" && p.Target == prop.Target);

            if (classNameProp is not null)
            {
                classNameProp.Value = w.WindowClassName;
            }

            RefreshEditorState();
        }
    }

    private void BrowseFileForProperty(PropertyMetadata prop)
    {
        var path = SelectFileForProperty(prop);
        if (!string.IsNullOrEmpty(path))
        {
            prop.Value = _pathResolver.ToRelativePath(path);
            RefreshEditorState();
        }
    }

    private string? SelectFileForProperty(PropertyMetadata prop)
    {
        if (!string.IsNullOrWhiteSpace(prop.FileFilter))
        {
            return _panelDialogService.SelectFile(prop.FileFilter);
        }

        return prop.PropertyName switch
        {
            "LabelsPath" => _panelDialogService.SelectLabelFile(),
            "settingsFile" => _panelDialogService.SelectFile(JsonFileFilter),
            _ => _panelDialogService.SelectModelFile()
        };
    }

    private void BrowseDirectoryForProperty(PropertyMetadata prop)
    {
        var path = _panelDialogService.SelectFolder();
        if (!string.IsNullOrEmpty(path))
        {
            prop.Value = _pathResolver.ToRelativePath(path);
            RefreshEditorState();
        }
    }

    private void ConfigureDirectoryPickerMetadata(PropertyMetadata prop)
    {
        if (!string.Equals(prop.PropertyName, "TessdataPath", StringComparison.Ordinal))
        {
            return;
        }

        prop.HelperText = "未指定の場合は ./Settings/tessdata を使用します。";
        prop.OpenReferenceCommand = new RelayCommand(OpenTessdataReferencePage);
        prop.DownloadRecommendedCommand = new AsyncRelayCommand(() => DownloadRecommendedTessdataAsync(prop));
    }

    private void ConfigureFilePickerMetadata(PropertyMetadata prop)
    {
        if (string.Equals(prop.PropertyName, "ModelPath", StringComparison.Ordinal))
        {
            prop.HelperText = "必要なら公式ページや推奨取得でONNXモデルを用意できます。";
            prop.OpenReferenceCommand = new RelayCommand(OpenAiModelReferencePage);
            prop.DownloadRecommendedCommand = new AsyncRelayCommand(() => DownloadRecommendedAiModelAsync(prop));
            return;
        }

        if (string.Equals(prop.PropertyName, "LabelsPath", StringComparison.Ordinal))
        {
            prop.HelperText = "未指定時はモデルmetadata、次に同階層の labels/coco.names/data.yaml を利用します。";
        }
    }

    private static void ConfigureComboBoxMetadata(PropertyMetadata prop)
    {
        if (string.Equals(prop.PropertyName, "LabelName", StringComparison.Ordinal))
        {
            prop.HelperText = "未選択ならクラスIDを使用します。モデル変更時に候補を再読み込みします。";
        }
    }

    private void OpenTessdataReferencePage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(TessdataFastRepositoryUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _notifier.ShowError($"公式ページを開けませんでした。\n{ex.Message}", "エラー");
        }
    }

    private void OpenAiModelReferencePage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(AiModelReferencePageUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _notifier.ShowError($"公式ページを開けませんでした。\n{ex.Message}", "エラー");
        }
    }

    private async Task DownloadRecommendedTessdataAsync(PropertyMetadata prop)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var targetDirectory = ResolveTessdataTargetDirectory(prop.StringValue);

        try
        {
            Directory.CreateDirectory(targetDirectory);

            using var client = new HttpClient();
            var files = new (string FileName, string Url)[]
            {
                ("jpn.traineddata", "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/jpn.traineddata"),
                ("eng.traineddata", "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/eng.traineddata")
            };

            foreach (var (fileName, url) in files)
            {
                await using var source = await client.GetStreamAsync(url);
                await using var destination = File.Create(Path.Combine(targetDirectory, fileName));
                await source.CopyToAsync(destination);
            }

            prop.Value = _pathResolver.ToRelativePath(targetDirectory);
            prop.NotifyAllValueProperties();
            RefreshEditorState();

            _notifier.ShowInfo(
                $"tessdata を取得しました。\n保存先: {targetDirectory}\n取得: jpn.traineddata / eng.traineddata",
                "OCRデータ取得完了");
        }
        catch (Exception ex)
        {
            _notifier.ShowError($"tessdata の取得に失敗しました。\n{ex.Message}", "エラー");
        }
    }

    private async Task DownloadRecommendedAiModelAsync(PropertyMetadata prop)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var targetFilePath = ResolveAiModelTargetPath(prop.StringValue);
        var targetDirectory = Path.GetDirectoryName(targetFilePath);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            _notifier.ShowError($"保存先フォルダを特定できませんでした。\n{targetFilePath}", "エラー");
            return;
        }

        try
        {
            Directory.CreateDirectory(targetDirectory);

            using var client = new HttpClient();
            var downloaded = false;
            List<string> errors = [];

            foreach (var url in RecommendedAiModelDownloadUrls)
            {
                try
                {
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    await using var source = await response.Content.ReadAsStreamAsync();
                    await using var destination = File.Create(targetFilePath);
                    await source.CopyToAsync(destination);
                    downloaded = true;
                    break;
                }
                catch (Exception ex)
                {
                    errors.Add($"{url} => {ex.Message}");
                }
            }

            if (!downloaded)
            {
                throw new InvalidOperationException(
                    "推奨モデルの取得先に接続できませんでした。候補URLを確認してください。\n"
                    + string.Join('\n', errors));
            }

            prop.Value = _pathResolver.ToRelativePath(targetFilePath);
            prop.NotifyAllValueProperties();
            RefreshEditorState();

            _notifier.ShowInfo(
                $"推奨AIモデルを取得しました。\n保存先: {targetFilePath}\n取得: {RecommendedAiModelFileName}",
                "AIモデル取得完了");
        }
        catch (Exception ex)
        {
            _notifier.ShowError($"推奨AIモデルの取得に失敗しました。\n{ex.Message}", "エラー");
        }
    }

    private string ResolveTessdataTargetDirectory(string configuredValue)
    {
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            var absolute = _pathResolver.ToAbsolutePath(configuredValue);
            if (!string.IsNullOrWhiteSpace(absolute))
            {
                return absolute;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "Settings", "tessdata");
    }

    private string ResolveAiModelTargetPath(string configuredValue)
    {
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            var absolute = _pathResolver.ToAbsolutePath(configuredValue);
            if (!string.IsNullOrWhiteSpace(absolute))
            {
                return Path.HasExtension(absolute)
                    ? absolute
                    : Path.Combine(absolute, RecommendedAiModelFileName);
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "Settings", "models", RecommendedAiModelFileName);
    }

    private bool IsAiLabelRelatedProperty(string propertyName)
    {
        return propertyName is "ModelPath" or "LabelsPath";
    }

    private void SyncClassIdFromLabelSelection(PropertyMetadata labelNameProp)
    {
        if (!TryParseClassIdFromLabelSelection(labelNameProp.StringValue, out var classId))
        {
            return;
        }

        var classIdProp = PropertyGroups
            .SelectMany(group => group.Properties)
            .FirstOrDefault(prop => string.Equals(prop.PropertyName, "ClassID", StringComparison.Ordinal) && ReferenceEquals(prop.Target, labelNameProp.Target));
        if (classIdProp is null || classIdProp.IntValue == classId)
        {
            return;
        }

        classIdProp.Value = classId;
        classIdProp.NotifyAllValueProperties();
    }

    private void RefreshAiLabelOptions()
    {
        if (Item is not IClickImageAIItem and not IIfImageExistAIItem and not IIfImageNotExistAIItem and not ISetVariableAIItem)
        {
            return;
        }

        var allProps = PropertyGroups.SelectMany(g => g.Properties).ToList();
        var modelPathProp = allProps.FirstOrDefault(p => p.PropertyName == "ModelPath");
        var labelsPathProp = allProps.FirstOrDefault(p => p.PropertyName == "LabelsPath");
        var labelNameProp = allProps.FirstOrDefault(p => p.PropertyName == "LabelName");
        if (modelPathProp is null || labelNameProp is null)
        {
            return;
        }

        var absoluteModelPath = ResolveExistingPath(modelPathProp.StringValue);
        if (string.IsNullOrWhiteSpace(absoluteModelPath))
        {
            labelNameProp.DynamicOptions = [];
            labelNameProp.HelperText = "モデルを読み込めないため、ラベル候補を表示できません。モデルパスを確認してください。";
            return;
        }

        var absoluteLabelsPath = string.Empty;
        if (labelsPathProp is not null && !string.IsNullOrWhiteSpace(labelsPathProp.StringValue))
        {
            absoluteLabelsPath = ResolveExistingPath(labelsPathProp.StringValue) ?? string.Empty;
        }

        try
        {
            var labels = _objectDetector.GetLabels(absoluteModelPath, string.IsNullOrWhiteSpace(absoluteLabelsPath) ? null : absoluteLabelsPath);
            if (labels.Count == 0)
            {
                labelNameProp.DynamicOptions = [];
                labelNameProp.HelperText = "ラベル情報を読み込めませんでした。モデルmetadataまたはラベルファイルを確認してください。";
                return;
            }

            labelNameProp.DynamicOptions = [.. labels.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}: {pair.Value}")];
            labelNameProp.HelperText = "候補を選択するとクラスIDへ同期されます。";
        }
        catch
        {
            labelNameProp.DynamicOptions = [];
            labelNameProp.HelperText = "ラベル情報の読み込みに失敗しました。モデルmetadataまたはラベルファイルを確認してください。";
        }
    }

    private string? ResolveExistingPath(string configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return null;
        }

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddCandidate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            candidates.Add(value);
            try
            {
                candidates.Add(Path.GetFullPath(value));
            }
            catch
            {
                // ignore invalid path
            }
        }

        AddCandidate(configuredValue);
        AddCandidate(_pathResolver.ToAbsolutePath(configuredValue));
        if (!Path.IsPathRooted(configuredValue))
        {
            AddCandidate(Path.Combine(Environment.CurrentDirectory, configuredValue));
            AddCandidate(Path.Combine(AppContext.BaseDirectory, configuredValue));
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool TryParseClassIdFromLabelSelection(string selectedLabel, out int classId)
    {
        classId = -1;
        if (string.IsNullOrWhiteSpace(selectedLabel))
        {
            return false;
        }

        var separatorIndex = selectedLabel.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return int.TryParse(selectedLabel.Trim(), out classId);
        }

        var idPart = selectedLabel[..separatorIndex].Trim();
        return int.TryParse(idPart, out classId);
    }

    private void ValidateCurrentItemSettings()
    {
        if (Item is not ICommandSettings settings)
        {
            ClearValidationState();
            return;
        }

        var issues = CommandSettingsValidator.GetIssues(
            settings,
            _pathResolver,
            includeExistenceChecks: true);

        ApplyValidationState(issues);
    }

    private void ClearValidationState()
    {
        HasValidationErrors = false;
        ValidationSummary = string.Empty;

        foreach (var prop in PropertyGroups.SelectMany(group => group.Properties))
        {
            prop.HasValidationError = false;
            prop.ValidationMessage = string.Empty;
        }
    }

    private void ApplyValidationState(IReadOnlyList<CommandValidationIssue> issues)
    {
        ClearValidationState();
        if (issues.Count == 0)
        {
            return;
        }

        HasValidationErrors = true;
        ValidationSummary = string.Join(
            Environment.NewLine,
            issues.Select(i => $"[{i.Code}] {i.Message}"));

        foreach (var issue in issues)
        {
            var prop = PropertyGroups
                .SelectMany(group => group.Properties)
                .FirstOrDefault(x => string.Equals(x.PropertyName, issue.PropertyName, StringComparison.Ordinal));

            if (prop is null)
            {
                continue;
            }

            prop.HasValidationError = true;
            prop.ValidationMessage = $"[{issue.Code}] {issue.Message}";
        }
    }

    private void PickKeyForProperty(PropertyMetadata prop)
    {
        var keyPickerWindow = new KeyPickerWindow();
        if (keyPickerWindow.ShowDialog() == true)
        {
            prop.Value = keyPickerWindow.SelectedKey;
            var allProps = PropertyGroups.SelectMany(g => g.Properties).Where(p => p.Target == prop.Target).ToList();
            var ctrlProp = allProps.FirstOrDefault(p => p.PropertyName == "Ctrl");
            var altProp = allProps.FirstOrDefault(p => p.PropertyName == "Alt");
            var shiftProp = allProps.FirstOrDefault(p => p.PropertyName == "Shift");

            if (ctrlProp is not null) ctrlProp.Value = keyPickerWindow.SelectedCtrl;
            if (altProp is not null) altProp.Value = keyPickerWindow.SelectedAlt;
            if (shiftProp is not null) shiftProp.Value = keyPickerWindow.SelectedShift;
            RefreshEditorState();
        }
    }

    private void PickPointForProperty(PropertyMetadata prop)
    {
        var allProps = PropertyGroups
            .SelectMany(g => g.Properties)
            .Where(p => p.Target == prop.Target)
            .ToList();

        var yProp = allProps.FirstOrDefault(p => p.PropertyName == "Y");
        var widthProp = allProps.FirstOrDefault(p => p.PropertyName == "Width");
        var heightProp = allProps.FirstOrDefault(p => p.PropertyName == "Height");
        var isRegionPicker = prop.PropertyName == "X" && widthProp is not null && heightProp is not null;

        var cw = new CaptureWindow { Mode = isRegionPicker ? 0 : 1 };
        if (cw.ShowDialog() != true) return;

        var absoluteX = isRegionPicker ? (int)cw.SelectedRegion.X : (int)cw.SelectedPoint.X;
        var absoluteY = isRegionPicker ? (int)cw.SelectedRegion.Y : (int)cw.SelectedPoint.Y;
        var regionWidth = isRegionPicker ? Math.Max(1, (int)cw.SelectedRegion.Width) : 0;
        var regionHeight = isRegionPicker ? Math.Max(1, (int)cw.SelectedRegion.Height) : 0;

        var windowTitleProp = PropertyGroups
            .SelectMany(g => g.Properties)
            .FirstOrDefault(p => p.PropertyName == "WindowTitle" && p.Target == prop.Target);
        var windowClassNameProp = PropertyGroups
            .SelectMany(g => g.Properties)
            .FirstOrDefault(p => p.PropertyName == "WindowClassName" && p.Target == prop.Target);

        var windowTitle = windowTitleProp?.Value?.ToString() ?? string.Empty;
        var windowClassName = windowClassNameProp?.Value?.ToString() ?? string.Empty;

        var (relativeX, relativeY, success, errorMessage) = _windowService.ConvertToRelativeCoordinates(
            absoluteX, absoluteY, windowTitle, windowClassName);

        if (prop.PropertyName == "X")
        {
            prop.Value = relativeX;
            if (yProp is not null)
            {
                yProp.Value = relativeY;
            }
            if (isRegionPicker)
            {
                widthProp!.Value = regionWidth;
                heightProp!.Value = regionHeight;
            }
            prop.NotifyRelatedValueChanged();
        }
        else if (prop.PropertyName == "Y")
        {
            prop.Value = relativeY;
            var xProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyName == "X" && p.Target == prop.Target);
            if (xProp is not null)
            {
                xProp.Value = relativeX;
                xProp.NotifyRelatedValueChanged();
            }
        }

        RefreshEditorState();

        if (!string.IsNullOrEmpty(windowTitle) || !string.IsNullOrEmpty(windowClassName))
        {
            if (success)
            {
                var message = isRegionPicker
                    ? $"ウィンドウ相対領域を設定しました: ({relativeX}, {relativeY}, {regionWidth}, {regionHeight})\nウィンドウ: {windowTitle}[{windowClassName}]"
                    : $"ウィンドウ相対座標を設定しました: ({relativeX}, {relativeY})\nウィンドウ: {windowTitle}[{windowClassName}]";

                _notifier.ShowInfo(message, "座標設定完了");
            }
            else
            {
                var message = isRegionPicker
                    ? $"{errorMessage}\n絶対領域 ({relativeX}, {relativeY}, {regionWidth}, {regionHeight}) を設定しました。"
                    : $"{errorMessage}\n絶対座標 ({relativeX}, {relativeY}) を設定しました。";

                _notifier.ShowWarning(message, "警告");
            }
        }
        else if (isRegionPicker)
        {
            _notifier.ShowInfo(
                $"領域を設定しました: ({relativeX}, {relativeY}, {regionWidth}, {regionHeight})",
                "座標設定完了");
        }
    }

    private void RefreshEditorState(params string[] additionalPropertyNames)
    {
        SetAndRefresh(() => { }, additionalPropertyNames);
    }
}


