using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Commands.Infrastructure;
using AutoTool.Panels.Attributes;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.View;

namespace AutoTool.Panels.ViewModel;

public partial class EditPanelViewModel
{
    #region Commands
    [RelayCommand] private void Enter(KeyEventArgs e) { if (e.Key == Key.Enter) UpdateProperties(); }

    [RelayCommand]
    public void GetWindowInfo()
    {
        var prop = FindProperty(nameof(WindowTitle));
        if (prop != null)
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
        if (prop != null)
        {
            BrowseImageForProperty(prop);
            return;
        }

        var f = _panelDialogService.SelectImageFile();
        if (f != null) ImagePath = f;
    }

    [RelayCommand]
    public void Capture()
    {
        var prop = FindProperty(nameof(WaitImageItem.ImagePath));
        if (prop != null)
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
        if (prop != null)
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
        if (xProp != null)
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

    [RelayCommand] public void BrowseModel() { var f = _panelDialogService.SelectModelFile(); if (f != null) ModelPath = f; }
    [RelayCommand] public void BrowseProgram() { var f = _panelDialogService.SelectExecutableFile(); if (f != null) ProgramPath = f; }
    [RelayCommand] public void BrowseWorkingDirectory() { var d = _panelDialogService.SelectFolder(); if (d != null) WorkingDirectory = d; }
    [RelayCommand] public void BrowseSaveDirectory() { var d = _panelDialogService.SelectFolder(); if (d != null) SaveDirectory = d; }
    #endregion

    private PropertyMetadata? FindProperty(string propertyName)
    {
        return PropertyGroups
            .SelectMany(group => group.Properties)
            .FirstOrDefault(prop => prop.PropertyInfo.Name == propertyName);
    }
}
