using AutoTool.Services.ColorPicking;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// キーキャプチャサービスのインターフェース
/// </summary>
public interface IKeyCaptureService
{
    /// <summary>
    /// キーキャプチャを実行
    /// </summary>
    Task<KeyCaptureResult?> CaptureKeyAsync(string title);

    /// <summary>
    /// KeyHelperサービスが現在アクティブかどうかを取得
    /// </summary>
    bool IsKeyHelperActive { get; }

    /// <summary>
    /// KeyHelperサービスをキャンセル
    /// </summary>
    void CancelKeyCapture();
}