using System.Windows.Input;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// キーボード操作サービスのインターフェース
/// </summary>
public interface IKeyboardService
{
    /// <summary>
    /// 指定されたキーを送信します
    /// </summary>
    Task SendKeyAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定されたキーコンビネーションを送信します
    /// </summary>
    Task SendKeyComboAsync(string[] keys, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// テキストを入力します
    /// </summary>
    Task SendTextAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// ホットキーを送信します
    /// </summary>
    Task SendHotkeyAsync(string hotkey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定されたキーが押されているかどうかを確認します
    /// </summary>
    bool IsKeyPressed(string key);
}