using CommunityToolkit.Mvvm.Input;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Interface;
using AutoTool.Panels.Attributes;
using AutoTool.Panels.View;

namespace AutoTool.Panels.ViewModel;

public partial class EditPanelViewModel
{
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
                    }
                };
            }
        }

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
            },
            EditorType.DirectoryPicker => () =>
            {
                prop.BrowseCommand = new RelayCommand(() => BrowseDirectoryForProperty(prop));
                prop.ClearCommand = new RelayCommand(() => { prop.Value = string.Empty; prop.NotifyAllValueProperties(); });
                ConfigureDirectoryPickerMetadata(prop);
            },
            EditorType.KeyPicker => () => prop.PickKeyCommand = new RelayCommand(() => PickKeyForProperty(prop)),
            EditorType.PointPicker => () =>
            {
                prop.PickPointCommand = new RelayCommand(() => PickPointForProperty(prop));
                var yProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyInfo.Name == "Y" && p.Target == prop.Target);
                prop.RelatedProperty = yProp;
                var widthProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyInfo.Name == "Width" && p.Target == prop.Target);
                var heightProp = PropertyGroups
                    .SelectMany(g => g.Properties)
                    .FirstOrDefault(p => p.PropertyInfo.Name == "Height" && p.Target == prop.Target);
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
            prop.Value = w.WindowTitle;

            var classNameProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyInfo.Name == "WindowClassName" && p.Target == prop.Target);

            if (classNameProp is not null)
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

    private void ConfigureDirectoryPickerMetadata(PropertyMetadata prop)
    {
        if (!string.Equals(prop.PropertyInfo.Name, "TessdataPath", StringComparison.Ordinal))
        {
            return;
        }

        prop.HelperText = "未指定の場合は ./tessdata を使用します。";
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
                .FirstOrDefault(x => string.Equals(x.PropertyInfo.Name, issue.PropertyName, StringComparison.Ordinal));

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
            var ctrlProp = allProps.FirstOrDefault(p => p.PropertyInfo.Name == "Ctrl");
            var altProp = allProps.FirstOrDefault(p => p.PropertyInfo.Name == "Alt");
            var shiftProp = allProps.FirstOrDefault(p => p.PropertyInfo.Name == "Shift");

            if (ctrlProp is not null) ctrlProp.Value = keyPickerWindow.SelectedCtrl;
            if (altProp is not null) altProp.Value = keyPickerWindow.SelectedAlt;
            if (shiftProp is not null) shiftProp.Value = keyPickerWindow.SelectedShift;
            UpdateProperties();
        }
    }

    private void PickPointForProperty(PropertyMetadata prop)
    {
        var allProps = PropertyGroups
            .SelectMany(g => g.Properties)
            .Where(p => p.Target == prop.Target)
            .ToList();

        var yProp = allProps.FirstOrDefault(p => p.PropertyInfo.Name == "Y");
        var widthProp = allProps.FirstOrDefault(p => p.PropertyInfo.Name == "Width");
        var heightProp = allProps.FirstOrDefault(p => p.PropertyInfo.Name == "Height");
        var isRegionPicker = prop.PropertyInfo.Name == "X" && widthProp is not null && heightProp is not null;

        var cw = new CaptureWindow { Mode = isRegionPicker ? 0 : 1 };
        if (cw.ShowDialog() != true) return;

        var absoluteX = isRegionPicker ? (int)cw.SelectedRegion.X : (int)cw.SelectedPoint.X;
        var absoluteY = isRegionPicker ? (int)cw.SelectedRegion.Y : (int)cw.SelectedPoint.Y;
        var regionWidth = isRegionPicker ? Math.Max(1, (int)cw.SelectedRegion.Width) : 0;
        var regionHeight = isRegionPicker ? Math.Max(1, (int)cw.SelectedRegion.Height) : 0;

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

        if (prop.PropertyInfo.Name == "X")
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
        else if (prop.PropertyInfo.Name == "Y")
        {
            prop.Value = relativeY;
            var xProp = PropertyGroups
                .SelectMany(g => g.Properties)
                .FirstOrDefault(p => p.PropertyInfo.Name == "X" && p.Target == prop.Target);
            if (xProp is not null)
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
}
