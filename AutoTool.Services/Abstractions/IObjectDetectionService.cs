using System.Drawing;
using AutoTool.Services.ObjectDetection;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// YOLO物体検出サービスのインターフェース
/// </summary>
public interface IObjectDetectionService
{
    /// <summary>
    /// YOLOモデルを初期化します
    /// </summary>
    /// <param name="onnxPath">ONNXモデルファイルのパス</param>
    /// <param name="inputSize">入力画像サイズ（デフォルト: 640）</param>
    /// <param name="useDirectML">DirectMLを使用するかどうか</param>
    /// <param name="labels">クラスラベル配列（オプション）</param>
    void InitializeModel(string onnxPath, int inputSize = 640, bool useDirectML = false, string[]? labels = null);

    /// <summary>
    /// ウィンドウタイトルを指定して物体検出を実行します
    /// </summary>
    /// <param name="windowTitle">対象ウィンドウのタイトル</param>
    /// <param name="confidenceThreshold">信頼度閾値</param>
    /// <param name="iouThreshold">IoU閾値</param>
    /// <param name="drawResults">結果を描画するかどうか</param>
    /// <returns>検出結果</returns>
    Task<DetectionResult> DetectFromWindowAsync(string windowTitle, float confidenceThreshold = 0.45f, float iouThreshold = 0.15f, bool drawResults = true);

    /// <summary>
    /// Bitmapから物体検出を実行します
    /// </summary>
    /// <param name="bitmap">入力画像</param>
    /// <param name="confidenceThreshold">信頼度閾値</param>
    /// <param name="iouThreshold">IoU閾値</param>
    /// <param name="drawResults">結果を描画するかどうか</param>
    /// <returns>検出結果</returns>
    Task<DetectionResult> DetectFromBitmapAsync(Bitmap bitmap, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f, bool drawResults = true);

    /// <summary>
    /// ファイルパスから画像を読み込んで物体検出を実行します
    /// </summary>
    /// <param name="imagePath">画像ファイルのパス</param>
    /// <param name="confidenceThreshold">信頼度閾値</param>
    /// <param name="iouThreshold">IoU閾値</param>
    /// <param name="drawResults">結果を描画するかどうか</param>
    /// <returns>検出結果</returns>
    Task<DetectionResult> DetectFromFileAsync(string imagePath, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f, bool drawResults = true);

    /// <summary>
    /// モデルが初期化済みかどうかを確認します
    /// </summary>
    bool IsModelInitialized { get; }
}