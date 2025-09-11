using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;

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
        
        // シンプルな実装：将来的にはより高度なトースト通知システムに置き換え可能
        var icon = type switch
        {
            ToastType.Warning => MessageBoxImage.Warning,
            ToastType.Error => MessageBoxImage.Error,
            ToastType.Success => MessageBoxImage.Information,
            _ => MessageBoxImage.Information
        };
        
        // 非ブロッキングで表示
        _ = Task.Run(() => System.Windows.MessageBox.Show(message, type.ToString(), 
            System.Windows.MessageBoxButton.OK, ConvertToWpfIcon(icon)));
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

    #endregion
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