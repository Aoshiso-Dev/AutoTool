using OpenCvSharp;

namespace AutoTool.Services.ObjectDetection;

/// <summary>
/// YOLO検出結果描画ユーティリティ
/// </summary>
internal static class YoloDrawer
{
    public static void Draw(Mat img, IReadOnlyList<Detection> dets, string[]? labels = null)
    {
        foreach (var d in dets)
        {
            var p1 = new OpenCvSharp.Point((int)d.Rect.X, (int)d.Rect.Y);
            var p2 = new OpenCvSharp.Point((int)(d.Rect.X + d.Rect.Width), (int)(d.Rect.Y + d.Rect.Height));
            Cv2.Rectangle(img, p1, p2, Scalar.Lime, 2);
            
            string name = labels is { Length: > 0 } && d.ClassId >= 0 && d.ClassId < labels.Length 
                ? labels[d.ClassId] 
                : $"id:{d.ClassId}";
            string label = $"{name} {d.Score:0.00}";
            
            int baseLine;
            var size = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.5, 1, out baseLine);
            var tl = new OpenCvSharp.Point(p1.X, Math.Max(0, p1.Y - size.Height - 4));
            var br = new OpenCvSharp.Point(p1.X + size.Width + 4, tl.Y + size.Height + 4);
            
            Cv2.Rectangle(img, tl, br, Scalar.Lime, -1);
            Cv2.PutText(img, label, new OpenCvSharp.Point(tl.X + 2, tl.Y + size.Height + 1),
                HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 1, LineTypes.AntiAlias);
        }
    }
}