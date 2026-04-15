using System.Drawing;
using System.Windows;
using System.Windows.Media;
using AutoTool.Commands.Infrastructure;
using Color = System.Windows.Media.Color;

namespace AutoTool.Panels.View;

public partial class ColorPickWindow : Window
{
    private bool _hookRegistered;

    public Color? Color { get; private set; }

    public ColorPickWindow()
    {
        InitializeComponent();
        StartHook();
    }

    private void StartHook()
    {
        if (_hookRegistered)
        {
            return;
        }

        Win32MouseHookHelper.LButtonUp += OnLButtonUp;
        Win32MouseHookHelper.RButtonUp += OnRButtonUp;
        Win32MouseHookHelper.MouseMove += OnMouseMove;
        Win32MouseHookHelper.StartHook();
        _hookRegistered = true;
    }

    private void StopHook()
    {
        if (!_hookRegistered)
        {
            return;
        }

        Win32MouseHookHelper.LButtonUp -= OnLButtonUp;
        Win32MouseHookHelper.RButtonUp -= OnRButtonUp;
        Win32MouseHookHelper.MouseMove -= OnMouseMove;
        Win32MouseHookHelper.StopHook();
        _hookRegistered = false;
    }

    private void OnMouseMove(object? sender, Win32MouseHookHelper.MouseEventArgs e)
    {
        var cursorPos = Win32MouseInterop.GetCursorPosition();
        Left = cursorPos.X + 10;
        Top = cursorPos.Y + 10;

        Color = GetColorAt(cursorPos);
        ColorPreview.Fill = new SolidColorBrush(Color ?? Colors.Transparent);
    }

    private void OnLButtonUp(object? sender, Win32MouseHookHelper.MouseEventArgs e)
    {
        StopHook();
        DialogResult = true;
        Close();
    }

    private void OnRButtonUp(object? sender, Win32MouseHookHelper.MouseEventArgs e)
    {
        StopHook();
        Color = null;
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        StopHook();
        base.OnClosed(e);
    }

    private static System.Windows.Media.Color GetColorAt(System.Drawing.Point cursorPos)
    {
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(cursorPos.X, cursorPos.Y, 0, 0, new System.Drawing.Size(1, 1));

        var drawingColor = bitmap.GetPixel(0, 0);
        return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
    }
}


