using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoTool.Commands.Infrastructure;
using DrawingRectangle = System.Drawing.Rectangle;

namespace AutoTool.Desktop.Panels.Services;

public sealed class DetectionHighlightService : IDetectionHighlightService
{
    public async Task BlinkAsync(DrawingRectangle bounds, CancellationToken cancellationToken = default)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var window = await dispatcher.InvokeAsync(() => CreateWindow(bounds));
        await dispatcher.InvokeAsync(window.Show);

        try
        {
            var isVisible = true;
            for (var i = 0; i < 6; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                isVisible = !isVisible;
                await dispatcher.InvokeAsync(() => window.Opacity = isVisible ? 1.0 : 0.15);
                await Task.Delay(120, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        finally
        {
            await dispatcher.InvokeAsync(window.Close);
        }
    }

    private static Window CreateWindow(DrawingRectangle bounds)
    {
        var centerX = bounds.Left + (bounds.Width / 2);
        var centerY = bounds.Top + (bounds.Height / 2);
        var (dpiX, dpiY) = Win32DpiHelper.GetMonitorDpiAt(centerX, centerY);
        var scaleX = 96d / dpiX;
        var scaleY = 96d / dpiY;

        var window = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.Manual,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            ShowActivated = false,
            MinWidth = 1,
            MinHeight = 1,
            Left = bounds.Left * scaleX,
            Top = bounds.Top * scaleY,
            Width = Math.Max(1, bounds.Width) * scaleX,
            Height = Math.Max(1, bounds.Height) * scaleY,
            IsHitTestVisible = false
        };

        window.Content = new Border
        {
            BorderBrush = System.Windows.Media.Brushes.Red,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0),
            Background = System.Windows.Media.Brushes.Transparent
        };

        return window;
    }
}
