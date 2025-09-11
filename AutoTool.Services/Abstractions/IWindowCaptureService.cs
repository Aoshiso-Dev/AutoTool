using System.Drawing;
using AutoTool.Services.ColorPicking;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// ウィンドウキャプチャサービスのインターフェース
/// </summary>
public interface IWindowCaptureService
{
    /// <summary>
    /// 右クリック位置のウィンドウ情報を取得
    /// </summary>
    Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync();

    /// <summary>
    /// 右クリック位置の座標を取得
    /// </summary>
    Task<Point?> CaptureCoordinateAtRightClickAsync();

    /// <summary>
    /// 指定位置のウィンドウ情報を取得
    /// </summary>
    WindowCaptureResult? GetWindowInfoAt(Point position);
}