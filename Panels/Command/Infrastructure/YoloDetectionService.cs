using System.Drawing;
using MacroPanels.Command.Services;
using YoloWinLib;

namespace MacroPanels.Command.Infrastructure;

/// <summary>
/// YOLOを使用したAI物体検出サービスの実装
/// </summary>
public class YoloDetectionService : IAIDetectionService
{
    private string? _currentModelPath;

    public void Initialize(string modelPath, int inputSize = 640, bool useGpu = true)
    {
        if (_currentModelPath != modelPath)
        {
            YoloWin.Init(modelPath, inputSize, useGpu);
            _currentModelPath = modelPath;
        }
    }

    public IReadOnlyList<Services.DetectionResult> Detect(string? windowTitle, float confThreshold, float iouThreshold)
    {
        var result = YoloWin.DetectFromWindowTitle(windowTitle, confThreshold, iouThreshold);
        
        return result.Detections
            .Select(d => new Services.DetectionResult(
                d.ClassId,
                d.Score,
                new System.Drawing.Rectangle((int)d.Rect.X, (int)d.Rect.Y, (int)d.Rect.Width, (int)d.Rect.Height)))
            .ToList()
            .AsReadOnly();
    }
}
