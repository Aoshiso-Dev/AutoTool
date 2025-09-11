using System.IO;
using AutoTool.Services.Abstractions;
using AutoTool.Services.ObjectDetection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Examples;

/// <summary>
/// ObjectDetectionServiceの使用例
/// </summary>
public class ObjectDetectionExample
{
    private readonly IObjectDetectionService _objectDetectionService;
    private readonly ILogger<ObjectDetectionExample> _logger;

    public ObjectDetectionExample(IObjectDetectionService objectDetectionService, ILogger<ObjectDetectionExample> logger)
    {
        _objectDetectionService = objectDetectionService ?? throw new ArgumentNullException(nameof(objectDetectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// YOLO物体検出の基本的な使用例
    /// </summary>
    public async Task BasicDetectionExampleAsync()
    {
        try
        {
            // 1. モデルの初期化
            var modelPath = "yolov8n.onnx"; // モデルファイルのパス
            var labels = new[] { "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train" }; // COCO80クラス（一部）
            
            _objectDetectionService.InitializeModel(modelPath, inputSize: 640, useDirectML: false, labels: labels);
            
            if (!_objectDetectionService.IsModelInitialized)
            {
                _logger.LogError("YOLOモデルの初期化に失敗しました");
                return;
            }

            // 2. ウィンドウからの検出
            var windowResult = await _objectDetectionService.DetectFromWindowAsync(
                windowTitle: "メモ帳",
                confidenceThreshold: 0.5f,
                iouThreshold: 0.4f,
                drawResults: true
            );

            _logger.LogInformation("ウィンドウから {Count} 個のオブジェクトを検出しました", windowResult.Detections.Count);

            // 検出結果の詳細表示
            foreach (var detection in windowResult.Detections)
            {
                var className = detection.ClassId < labels.Length ? labels[detection.ClassId] : $"Class{detection.ClassId}";
                _logger.LogInformation("検出: {ClassName} (信頼度: {Score:F2}, 位置: {X:F0},{Y:F0},{Width:F0},{Height:F0})",
                    className, detection.Score, detection.Rect.X, detection.Rect.Y, detection.Rect.Width, detection.Rect.Height);
            }

            // 3. 画像ファイルからの検出
            if (File.Exists("sample.jpg"))
            {
                var fileResult = await _objectDetectionService.DetectFromFileAsync(
                    imagePath: "sample.jpg",
                    confidenceThreshold: 0.3f,
                    iouThreshold: 0.5f,
                    drawResults: true
                );

                _logger.LogInformation("画像ファイルから {Count} 個のオブジェクトを検出しました", fileResult.Detections.Count);

                // 結果画像の保存
                if (fileResult.AnnotatedBgr != null)
                {
                    fileResult.SaveAnnotated("detected_result.jpg");
                    _logger.LogInformation("検出結果を detected_result.jpg に保存しました");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "物体検出の実行中にエラーが発生しました");
        }
    }

    /// <summary>
    /// リアルタイム検出の例（ウィンドウキャプチャ）
    /// </summary>
    public async Task RealtimeDetectionExampleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_objectDetectionService.IsModelInitialized)
            {
                _logger.LogWarning("YOLOモデルが初期化されていません");
                return;
            }

            var windowTitle = "対象ウィンドウ";
            var detectionInterval = TimeSpan.FromMilliseconds(500); // 500ms間隔

            _logger.LogInformation("リアルタイム物体検出を開始します: {WindowTitle}", windowTitle);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _objectDetectionService.DetectFromWindowAsync(
                        windowTitle: windowTitle,
                        confidenceThreshold: 0.6f,
                        iouThreshold: 0.4f,
                        drawResults: false // リアルタイムでは描画なし
                    );

                    if (result.Detections.Count > 0)
                    {
                        _logger.LogDebug("リアルタイム検出: {Count} 個のオブジェクト", result.Detections.Count);
                        
                        // 特定のクラス（例：人）が検出された場合の処理
                        var personDetections = result.Detections.Where(d => d.ClassId == 0).ToList(); // Class 0 = person
                        if (personDetections.Count > 0)
                        {
                            _logger.LogInformation("人を {Count} 人検出しました", personDetections.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "リアルタイム検出中にエラー（続行します）");
                }

                await Task.Delay(detectionInterval, cancellationToken);
            }

            _logger.LogInformation("リアルタイム物体検出を終了しました");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("リアルタイム物体検出がキャンセルされました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "リアルタイム物体検出中にエラーが発生しました");
        }
    }
}