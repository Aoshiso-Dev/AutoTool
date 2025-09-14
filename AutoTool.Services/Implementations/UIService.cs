using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MessageBoxResult = AutoTool.Services.Abstractions.MessageBoxResult;
using MessageBoxButton = AutoTool.Services.Abstractions.MessageBoxButton;
using MessageBoxImage = AutoTool.Services.Abstractions.MessageBoxImage;

namespace AutoTool.Services.Implementations;

/// <summary>
/// UI関連サービスの実装
/// </summary>
public class UIService : IUIService
{
    private readonly ILogger<UIService> _logger;

    public UIService(ILogger<UIService> logger)
    {
        _logger = logger;
    }

    public async Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Showing message box: {Title} - {Message}", title, message);

            // WPFのMessageBoxを使用
            var wpfButton = ConvertToWpfButton(button);
            var wpfIcon = ConvertToWpfIcon(icon);

            var result = System.Windows.MessageBox.Show(message, title, wpfButton, wpfIcon);
            return ConvertFromWpfResult(result);
        });
    }

    public async Task<string?> ShowOpenFileDialogAsync(string filter = "", string initialDirectory = "")
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Showing open file dialog");

            var dialog = new Microsoft.Win32.OpenFileDialog();

            if (!string.IsNullOrEmpty(filter))
                dialog.Filter = filter;

            if (!string.IsNullOrEmpty(initialDirectory))
                dialog.InitialDirectory = initialDirectory;

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    public async Task<string?> ShowSaveFileDialogAsync(string filter = "", string initialDirectory = "", string defaultFileName = "")
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Showing save file dialog");

            var dialog = new Microsoft.Win32.SaveFileDialog();

            if (!string.IsNullOrEmpty(filter))
                dialog.Filter = filter;

            if (!string.IsNullOrEmpty(initialDirectory))
                dialog.InitialDirectory = initialDirectory;

            if (!string.IsNullOrEmpty(defaultFileName))
                dialog.FileName = defaultFileName;

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    public async Task<string?> ShowFolderBrowserDialogAsync(string description = "")
    {
        return await Task.Run(() =>
        {
            _logger.LogDebug("Showing folder browser dialog");

            // WPFネイティブのフォルダ選択ダイアログ（.NET 8でサポート）
            var dialog = new Microsoft.Win32.OpenFolderDialog();

            if (!string.IsNullOrEmpty(description))
                dialog.Title = description;

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        });
    }

    public IProgressDialog ShowProgressDialog(string title, string message, bool cancellable = false)
    {
        _logger.LogDebug("Showing progress dialog: {Title}", title);
        return new ProgressDialogImpl(title, message, cancellable);
    }

    public void ShowToast(string message, ToastType type = ToastType.Information)
    {
        _logger.LogDebug("Showing toast: {Type} - {Message}", type, message);

        // UI スレッドでトーストを表示する。Application.Current が null の場合はフォールバックで MessageBox を使用。
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                // フォールバック（コンソール or 非UI環境）
                _ = Task.Run(() => System.Windows.MessageBox.Show(message, type.ToString(), System.Windows.MessageBoxButton.OK, ConvertToWpfIcon(ConvertToastTypeToMessageBoxImage(type))));
                return;
            }

            dispatcher.InvokeAsync(() =>
            {
                ToastManager.Instance.Show(message, type);
            }, DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "トースト表示中に例外が発生したためフォールバック表示します");
            _ = Task.Run(() => System.Windows.MessageBox.Show(message, type.ToString(), System.Windows.MessageBoxButton.OK, ConvertToWpfIcon(ConvertToastTypeToMessageBoxImage(type))));
        }
    }

    #region Helper Methods

    private System.Windows.MessageBoxButton ConvertToWpfButton(MessageBoxButton button)
    {
        return button switch
        {
            MessageBoxButton.OK => System.Windows.MessageBoxButton.OK,
            MessageBoxButton.OKCancel => System.Windows.MessageBoxButton.OKCancel,
            MessageBoxButton.YesNo => System.Windows.MessageBoxButton.YesNo,
            MessageBoxButton.YesNoCancel => System.Windows.MessageBoxButton.YesNoCancel,
            _ => System.Windows.MessageBoxButton.OK
        };
    }

    private System.Windows.MessageBoxImage ConvertToWpfIcon(MessageBoxImage icon)
    {
        return icon switch
        {
            MessageBoxImage.Information => System.Windows.MessageBoxImage.Information,
            MessageBoxImage.Warning => System.Windows.MessageBoxImage.Warning,
            MessageBoxImage.Error => System.Windows.MessageBoxImage.Error,
            MessageBoxImage.Question => System.Windows.MessageBoxImage.Question,
            _ => System.Windows.MessageBoxImage.None
        };
    }

    private MessageBoxResult ConvertFromWpfResult(System.Windows.MessageBoxResult result)
    {
        return result switch
        {
            System.Windows.MessageBoxResult.OK => MessageBoxResult.OK,
            System.Windows.MessageBoxResult.Cancel => MessageBoxResult.Cancel,
            System.Windows.MessageBoxResult.Yes => MessageBoxResult.Yes,
            System.Windows.MessageBoxResult.No => MessageBoxResult.No,
            _ => MessageBoxResult.None
        };
    }

    private MessageBoxImage ConvertToastTypeToMessageBoxImage(ToastType type)
        => type switch
        {
            ToastType.Warning => MessageBoxImage.Warning,
            ToastType.Error => MessageBoxImage.Error,
            ToastType.Success => MessageBoxImage.Information,
            _ => MessageBoxImage.Information
        };

    #endregion
}

/// <summary>
/// トーストの表示を管理するシンプルなマネージャ
/// </summary>
internal sealed class ToastManager
{
    public static ToastManager Instance { get; } = new ToastManager();
    private readonly object _lock = new();
    private readonly List<ToastWindow> _active = new();

    private const int Margin = 12;
    private const int Gap = 8;

    public void Show(string message, ToastType type)
    {
        lock (_lock)
        {
            var toast = new ToastWindow(message, type);
            // 右下に積み上げる配置を計算
            var screen = SystemParameters.WorkArea;
            double width = toast.Width;
            double height = toast.Height;

            double x = screen.Right - width - Margin;
            double y = screen.Bottom - Margin - height;

            // 上に既存トースト分だけずらす
            foreach (var t in _active.ToArray())
            {
                y -= (t.Height + Gap);
            }

            toast.Left = x;
            toast.Top = y;

            _active.Add(toast);
            toast.Closed += (s, e) =>
            {
                lock (_lock)
                {
                    _active.Remove(toast);
                    // 再配置
                    RepositionAll();
                }
            };

            toast.Show();
        }
    }

    private void RepositionAll()
    {
        var screen = SystemParameters.WorkArea;
        double margin = Margin;
        double gap = Gap;
        double y = screen.Bottom - margin;
        // 下から順に配置（最後のものが下端）
        foreach (var toast in _active.AsEnumerable().Reverse())
        {
            y -= toast.Height;
            toast.Top = y;
            y -= gap;
        }
    }
}

/// <summary>
/// 個別トーストウィンドウ
/// </summary>
internal sealed class ToastWindow : Window
{
    private const int DefaultWidth = 320;
    private const int DefaultHeight = 72;
    private readonly TimeSpan _visibleDuration = TimeSpan.FromSeconds(3);
    private readonly TimeSpan _fadeDuration = TimeSpan.FromMilliseconds(300);

    public ToastWindow(string message, ToastType type)
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        // Do not take focus
        Focusable = false;

        // コンテンツ
        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Background = GetBackgroundBrush(type),
            Opacity = 0.0,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 12,
                ShadowDepth = 2,
                Opacity = 0.25
            }
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var icon = new TextBlock
        {
            Text = GetIconGlyph(type),
            FontSize = 20,
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White
        };
        var txt = new TextBlock
        {
            Text = message,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = DefaultWidth - 80
        };

        panel.Children.Add(icon);
        panel.Children.Add(txt);
        border.Child = panel;
        Content = border;

        Loaded += ToastWindow_Loaded;
        Deactivated += (s, e) => { /* ignore */ };
    }

    private void ToastWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // フェードイン
        if (Content is Border b)
        {
            var fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(_fadeDuration)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            b.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        // 自動クローズ
        var timer = new DispatcherTimer { Interval = _visibleDuration };
        timer.Tick += (s, ev) =>
        {
            timer.Stop();
            BeginCloseAnimation();
        };
        timer.Start();
    }

    private void BeginCloseAnimation()
    {
        if (Content is Border b)
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, new Duration(_fadeDuration)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            fadeOut.Completed += (s, e) => Close();
            b.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else
        {
            Close();
        }
    }

    private Brush GetBackgroundBrush(ToastType type)
    {
        return type switch
        {
            ToastType.Warning => new SolidColorBrush(Color.FromRgb(234, 179, 8)), // amber-400
            ToastType.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // red-500
            ToastType.Success => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // emerald-500
            _ => new SolidColorBrush(Color.FromRgb(59, 130, 246)) // blue-500
        };
    }

    private string GetIconGlyph(ToastType type)
    {
        return type switch
        {
            ToastType.Warning => "⚠",
            ToastType.Error => "✖",
            ToastType.Success => "✔",
            _ => "ℹ"
        };
    }
}

/// <summary>
/// 進捗ダイアログの実装
/// </summary>
internal class ProgressDialogImpl : IProgressDialog
{
    private readonly string _title;
    private readonly string _initialMessage;
    private readonly bool _cancellable;
    private bool _disposed = false;

    public ProgressDialogImpl(string title, string initialMessage, bool cancellable)
    {
        _title = title;
        _initialMessage = initialMessage;
        _cancellable = cancellable;
    }

    public bool IsCancelled { get; private set; }

    public void UpdateProgress(int percentage, string? message = null)
    {
        // シンプルな実装：実際のプログレスダイアログは別途実装が必要
        Console.WriteLine($"Progress: {percentage}% - {message ?? _initialMessage}");
    }

    public void Close()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Close();
    }
}