using System.Drawing;
using AutoTool.Commands.Services;
using AutoTool.Infrastructure.Vision.Yolo;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// YOLOを使用したAI物体検出の実装
/// </summary>
public class YoloObjectDetector : IObjectDetector
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

    public IReadOnlyDictionary<int, string> GetLabels(string modelPath, string? labelsPath = null)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            return new Dictionary<int, string>();
        }

        return YoloLabelCatalog.Load(modelPath, labelsPath);
    }

    public bool TryResolveClassId(string modelPath, string labelName, string? labelsPath, out int classId)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            classId = -1;
            return false;
        }

        return YoloLabelCatalog.TryResolveClassId(modelPath, labelName, labelsPath, out classId);
    }
}


