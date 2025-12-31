using System.Drawing;

namespace MacroPanels.Command.Services;

/// <summary>
/// AI検出結果
/// </summary>
/// <param name="ClassId">検出されたクラスID</param>
/// <param name="Score">信頼度スコア</param>
/// <param name="Rect">検出された領域</param>
public record DetectionResult(int ClassId, float Score, Rectangle Rect);

/// <summary>
/// AI物体検出サービスのインターフェース
/// </summary>
public interface IAIDetectionService
{
    /// <summary>
    /// モデルを初期化します
    /// </summary>
    /// <param name="modelPath">ONNXモデルのパス</param>
    /// <param name="inputSize">入力サイズ</param>
    /// <param name="useGpu">GPU使用フラグ</param>
    void Initialize(string modelPath, int inputSize = 640, bool useGpu = true);

    /// <summary>
    /// ウィンドウから物体を検出します
    /// </summary>
    /// <param name="windowTitle">対象ウィンドウのタイトル</param>
    /// <param name="confThreshold">信頼度閾値</param>
    /// <param name="iouThreshold">IoU閾値</param>
    /// <returns>検出結果のリスト</returns>
    IReadOnlyList<DetectionResult> Detect(string? windowTitle, float confThreshold, float iouThreshold);
}
