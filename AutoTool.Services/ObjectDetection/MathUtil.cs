using OpenCvSharp;

namespace AutoTool.Services.ObjectDetection;

/// <summary>
/// 数学・幾何計算ユーティリティ
/// </summary>
internal static class MathUtil
{
    public static OpenCvSharp.Rect2f Xywh2Xyxy(float x, float y, float w, float h)
        => new(x - w / 2f, y - h / 2f, w, h);

    public static OpenCvSharp.Rect2f UndoLetterbox(OpenCvSharp.Rect2f r, LetterboxResult lb)
        => new((r.X - lb.PadX) / lb.Gain, (r.Y - lb.PadY) / lb.Gain, r.Width / lb.Gain, r.Height / lb.Gain);

    public static OpenCvSharp.Rect2f ClipRect(OpenCvSharp.Rect2f r, int w, int h)
    {
        float x1 = Math.Clamp(r.X, 0, w - 1);
        float y1 = Math.Clamp(r.Y, 0, h - 1);
        float x2 = Math.Clamp(r.X + r.Width, 0, w - 1);
        float y2 = Math.Clamp(r.Y + r.Height, 0, h - 1);
        return new(x1, y1, x2 - x1, y2 - y1);
    }

    public static List<Detection> Nms(List<Detection> dets, float iouTh)
    {
        var keep = new List<Detection>();
        dets.Sort((a, b) => b.Score.CompareTo(a.Score));
        var removed = new bool[dets.Count];
        for (int i = 0; i < dets.Count; i++)
        {
            if (removed[i]) continue;
            var a = dets[i];
            keep.Add(a);
            for (int j = i + 1; j < dets.Count; j++)
            {
                if (removed[j]) continue;
                var b = dets[j];
                if (IoU(a.Rect, b.Rect) > iouTh) removed[j] = true;
            }
        }
        return keep;
    }

    private static float IoU(OpenCvSharp.Rect2f a, OpenCvSharp.Rect2f b)
    {
        float x1 = Math.Max(a.X, b.X);
        float y1 = Math.Max(a.Y, b.Y);
        float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
        float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
        float inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        float ua = a.Width * a.Height + b.Width * b.Height - inter + 1e-6f;
        return inter / ua;
    }
}