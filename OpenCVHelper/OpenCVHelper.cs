using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace OpenCVHelper
{

    public static class ScreenCaptureHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, CopyPixelOperation rop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);


        // ウィンドウをキャプチャするメソッド
        public static Mat CaptureWindowUsingBitBlt(string windowTitle, string windowClassName = "")
        {
            // ウィンドウハンドルを取得
            IntPtr hWnd = FindWindow(string.IsNullOrEmpty(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            // ウィンドウの位置とサイズを取得
            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            // ウィンドウのデバイスコンテキストを取得
            IntPtr hdcSrc = GetWindowDC(hWnd);
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);

            // ウィンドウ全体をキャプチャ（隠れている部分も含む）
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            // Bitmapを作成し、OpenCVSharpのMatに変換
            Bitmap bmp = Image.FromHbitmap(hBitmap);
            Mat mat = BitmapConverter.ToMat(bmp);

            // リソースを解放
            SelectObject(hdcDest, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            ReleaseDC(hWnd, hdcSrc);

            return mat;
        }

        // ウィンドウをキャプチャするメソッド
        public static Mat CaptureWindow(string windowTitle, string windowClassName = "")
        {
            return CaptureWindowUsingBitBlt(windowTitle, windowClassName);

            // ※ ウィンドウ裏に隠れている部分がキャプチャできないため、BitBlt方式を使用

            /*
            // ウィンドウハンドルを取得
            IntPtr hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            // ウィンドウの位置とサイズを取得
            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            // スクリーンショットを格納するためのBitmapを作成
            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // グラフィックスハンドルを取得
                    IntPtr hdc = g.GetHdc();

                    // PrintWindowを使用してウィンドウの内容をキャプチャ
                    //if (!PrintWindow(hWnd, hdc, 0))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

                    }

                    // BitmapをMat形式に変換
                    Mat mat = BitmapConverter.ToMat(bmp);

                    // ハンドルを解放
                    g.ReleaseHdc(hdc);

                    return mat;
                }
            }
            */
        }

        // スクリーン全体をキャプチャするメソッド
        public static Mat CaptureScreen()
        {
            var screenWidth = (int)SystemParameters.VirtualScreenWidth;
            var screenHeight = (int)SystemParameters.VirtualScreenHeight;

            using (var bitmap = new Bitmap(screenWidth, screenHeight))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                }

                return OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
            }
        }

        // 指定領域をキャプチャするメソッド
        public static Mat CaptureRegion(System.Windows.Rect region)
        {
            using (var bitmap = new Bitmap((int)region.Width, (int)region.Height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen((int)region.X, (int)region.Y, 0, 0, bitmap.Size);
                }

                return OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
            }
        }

        // キャプチャした画像を保存するメソッド
        public static void SaveCapture(Mat image, string filePath)
        {
            // フォルダが存在しなければ作成
            var directory = System.IO.Path.GetDirectoryName(filePath);
            if (!System.IO.Directory.Exists(directory))
            {
                if (directory != null)
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
            }

            image.SaveImage(filePath);
        }
    }


    public static class ImageSearchHelper
    {
        public static async Task<OpenCvSharp.Point?> SearchImage(string imagePath, CancellationToken token, double threshold = 0.8, Color? searchColor = null, string windowTitle = "", string windowClassName = "")
        {
            return await Task.Run(() =>
            {
                Mat targetMat = string.IsNullOrEmpty(windowTitle) ? ScreenCaptureHelper.CaptureScreen() : ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);
                Mat templateMat = new Mat(imagePath);

                if (searchColor == null)
                {
                    // グレースケールに変換
                    Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2GRAY);
                    Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2GRAY);
                }
                else
                {
                    // 色空間を揃える
                    Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2BGR);
                    Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2BGR);

                    // 指定した色に近い色のみ検出

                    var lowerR = searchColor.Value.R - 20; if (lowerR < 0) lowerR = 0;
                    var lowerG = searchColor.Value.G - 20; if (lowerG < 0) lowerG = 0;
                    var lowerB = searchColor.Value.B - 20; if (lowerB < 0) lowerB = 0;
                    var lower = new Scalar(lowerR, lowerG, lowerR);

                    var upperR = searchColor.Value.R + 20; if (upperR > 255) upperR = 255;
                    var upperG = searchColor.Value.G + 20; if (upperG > 255) upperG = 255;
                    var upperB = searchColor.Value.B + 20; if (upperB > 255) upperB = 255;
                    var upper = new Scalar(upperR, upperG, upperB);

                    using Mat mask = new Mat();

                    Cv2.InRange(targetMat, lower, upper, mask);
                    Cv2.BitwiseAnd(targetMat, targetMat, targetMat, mask);

                    Cv2.InRange(templateMat, lower, upper, mask);
                    Cv2.BitwiseAnd(templateMat, templateMat, templateMat, mask);
                }

                // マッチングを実行
                using Mat result = new Mat();
                Cv2.MatchTemplate(targetMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                // マッチング結果から最大値とその位置を取得
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal >= threshold)
                {
                    // 対象画像の中心座標を計算して返す
                    OpenCvSharp.Point center = new OpenCvSharp.Point(maxLoc.X + templateMat.Width / 2, maxLoc.Y + templateMat.Height / 2);
                    return center;
                }

                return (OpenCvSharp.Point?)null;
            }, token);
        }

        /*
        public static async Task<OpenCvSharp.Point?> SearchImage(Mat templateMat, CancellationToken token, double threshold = 0.8, Color? searchColor = null, string windowTitle = "", string windowClassName = "")
        {
            // スクリーンショットを取得
            using Mat screenMat = ScreenCaptureHelper.CaptureScreen();

            return await SearchImage(screenMat, templateMat, token, threshold, searchColor, windowTitle, windowClassName);
        }

        public static async Task<OpenCvSharp.Point?> SearchImage(string imagePath, CancellationToken token, double threshold = 0.8, Color? searchColor = null, string windowTitle = "", string windowClassName = "")
        {
            // ファイル存在確認
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            return await SearchImage(new Mat(imagePath), cts.Token, threshold, searchColor, windowTitle, windowClassName);
        }
        */

        /*
        public static async Task<OpenCvSharp.Point?> SearchImageFromWindow(string windowTitle, Mat templateMat, CancellationToken token, double threshold = 0.8, bool multiScale = false)
        {
            // ウィンドウキャプチャ
            using Mat windowMat = ScreenCaptureHelper.CaptureWindow(windowTitle);

            return multiScale ? await SearchImageMultiScale(windowMat, templateMat, token, threshold) : await SearchImage(windowMat, templateMat, token, threshold);
        }
        */

        /*
        public static async Task<OpenCvSharp.Point?> SearchImageFromWindow(string imagePath, CancellationToken token, double threshold = 0.8, Color? searchColor = null, bool multiScale = false)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            using Mat windowMat = ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);
            using Mat templateMat = new Mat(imagePath);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            //return false ? await SearchImageMultiScale(windowMat, templateMat, cts.Token, threshold) : await SearchImage(windowMat, templateMat, cts.Token, threshold);
            return await SearchImage(windowMat, templateMat, cts.Token, threshold, searchColor);
        }
        */



    }

    // ===== ONNX YOLOv8 簡易ラッパ =====
    public sealed class YoloOnnxDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly int _inW, _inH;

        public YoloOnnxDetector(string onnxPath, int inputW = 640, int inputH = 640, bool useDirectML = false)
        {
            var so = new SessionOptions();
            if (useDirectML)
            {
                // 使える環境ならGPU(DirectML)で実行、なければ自動でCPUにフォールバック
                try { so.AppendExecutionProvider_DML(); } catch { /* ignore */ }
            }
            _session = new InferenceSession(onnxPath, so);
            _inputName = _session.InputMetadata.Keys.First();
            _inW = inputW; _inH = inputH;
        }

        public sealed class Detection
        {
            public OpenCvSharp.Rect Box { get; init; }
            public int ClassId { get; init; }
            public float Score { get; init; }
        }

        public List<Detection> Detect(Mat srcBgr, float conf = 0.25f, float iou = 0.45f,
                              Color? maskColor = null, int? numClasses = null)
        {
            using var img = Preprocess(srcBgr, maskColor, out float ratio, out int padX, out int padY);
            // BGR -> RGB, HWC -> CHW, [0..1]
            var input = new DenseTensor<float>(new[] { 1, 3, _inH, _inW });
            for (int y = 0; y < _inH; y++)
                for (int x = 0; x < _inW; x++)
                {
                    var bgr = img.Get<Vec3b>(y, x);
                    input[0, 0, y, x] = bgr.Item2 / 255f;
                    input[0, 1, y, x] = bgr.Item1 / 255f;
                    input[0, 2, y, x] = bgr.Item0 / 255f;
                }

            using var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_inputName, input) });
            var tensor = results.First().AsTensor<float>();
            var dims = tensor.Dimensions;
            var data = tensor.ToArray();

            int d1 = dims[1], d2 = dims[2];
            int A = Math.Min(d1, d2);
            int N = Math.Max(d1, d2);
            bool transposed = (d1 == A);

            // === ここがポイント: objectness の有無判定 ===
            bool hasObj;
            if (numClasses.HasValue)
                hasObj = (A == 5 + numClasses.Value);      // 期待形: 4+obj+nc か 4+nc
            else
                hasObj = (A == 85 || A == 5 + 80);         // COCO系のヒューリスティック（任意）

            int clsStart = hasObj ? 5 : 4;

            var boxes = new List<OpenCvSharp.Rect>();
            var scores = new List<float>();
            var clsIds = new List<int>();

            if (!transposed) // [1, N, A]
            {
                for (int i = 0; i < N; i++)
                {
                    int off = i * A;
                    float x = data[off + 0], y = data[off + 1], w = data[off + 2], h = data[off + 3];
                    float obj = hasObj ? data[off + 4] : 1f;

                    int best = -1; float bestScore = 0f;
                    for (int c = clsStart; c < A; c++)
                    {
                        float s = data[off + c] * obj;
                        if (s > bestScore) { bestScore = s; best = c - clsStart; }
                    }
                    if (best < 0 || bestScore < conf) continue;

                    float x0 = x - w / 2, y0 = y - h / 2;
                    float rx = (x0 - padX) / ratio, ry = (y0 - padY) / ratio;
                    float rw = w / ratio, rh = h / ratio;
                    boxes.Add(new OpenCvSharp.Rect((int)rx, (int)ry, (int)rw, (int)rh));
                    scores.Add(bestScore);
                    clsIds.Add(best);
                }
            }
            else // [1, A, N]
            {
                for (int i = 0; i < N; i++)
                {
                    float x = data[0 * N + i], y = data[1 * N + i], w = data[2 * N + i], h = data[3 * N + i];
                    float obj = hasObj ? data[4 * N + i] : 1f;

                    int best = -1; float bestScore = 0f;
                    for (int c = clsStart; c < A; c++)
                    {
                        float s = data[c * N + i] * obj;
                        if (s > bestScore) { bestScore = s; best = c - clsStart; }
                    }
                    if (best < 0 || bestScore < conf) continue;

                    float x0 = x - w / 2, y0 = y - h / 2;
                    float rx = (x0 - padX) / ratio, ry = (y0 - padY) / ratio;
                    float rw = w / ratio, rh = h / ratio;
                    boxes.Add(new OpenCvSharp.Rect((int)rx, (int)ry, (int)rw, (int)rh));
                    scores.Add(bestScore);
                    clsIds.Add(best);
                }
            }

            var keep = Nms(boxes, scores, (float)iou);
            var dets = new List<Detection>(keep.Count);
            foreach (var i in keep)
                dets.Add(new Detection { Box = boxes[i], ClassId = clsIds[i], Score = scores[i] });
            return dets;
        }

        private Mat Preprocess(Mat srcBgr, Color? maskColor, out float ratio, out int padX, out int padY)
        {
            Mat input = srcBgr;

            // 任意: 色マスク（テンプレート版互換。必要なときだけ使う）
            if (maskColor != null)
            {
                // BGRA/BGR どちらでもBGRへ
                if (srcBgr.Channels() == 4)
                    Cv2.CvtColor(input, input, ColorConversionCodes.BGRA2BGR);

                var c = maskColor.Value;
                var lower = new Scalar(
                    Math.Max(0, c.B - 20),
                    Math.Max(0, c.G - 20),
                    Math.Max(0, c.R - 20));
                var upper = new Scalar(
                    Math.Min(255, c.B + 20),
                    Math.Min(255, c.G + 20),
                    Math.Min(255, c.R + 20));
                using var mask = new Mat();
                Cv2.InRange(input, lower, upper, mask);
                Cv2.BitwiseAnd(input, input, input, mask);
            }

            // letterbox (YOLO前処理)
            double r = Math.Min((double)_inW / input.Width, (double)_inH / input.Height);
            int nw = (int)Math.Round(input.Width * r);
            int nh = (int)Math.Round(input.Height * r);
            var resized = new Mat();
            Cv2.Resize(input, resized, new OpenCvSharp.Size(nw, nh));
            var canvas = new Mat(new OpenCvSharp.Size(_inW, _inH), MatType.CV_8UC3, new Scalar(114, 114, 114));
            padX = (_inW - nw) / 2; padY = (_inH - nh) / 2;
            resized.CopyTo(new Mat(canvas, new OpenCvSharp.Rect(padX, padY, nw, nh)));
            ratio = (float)r;

            return canvas;
        }

        // 修正内容: OpenCvSharp.Rect型のリストを使うため、型名の曖昧さを解消
        // すべての 'Rect' を 'OpenCvSharp.Rect' に変更

        private static List<int> Nms(List<OpenCvSharp.Rect> boxes, List<float> scores, float iouThresh)
        {
            var idx = Enumerable.Range(0, boxes.Count).OrderByDescending(i => scores[i]).ToList();
            var keep = new List<int>();
            while (idx.Count > 0)
            {
                int i = idx[0]; keep.Add(i);
                idx.RemoveAt(0);
                idx = idx.Where(j => IoU(boxes[i], boxes[j]) < iouThresh).ToList();
            }
            return keep;
        }
        private static float IoU(OpenCvSharp.Rect a, OpenCvSharp.Rect b)
        {
            int xx1 = Math.Max(a.X, b.X), yy1 = Math.Max(a.Y, b.Y);
            int xx2 = Math.Min(a.Right, b.Right), yy2 = Math.Min(a.Bottom, b.Bottom);
            int w = Math.Max(0, xx2 - xx1), h = Math.Max(0, yy2 - yy1);
            float inter = w * h;
            float uni = a.Width * a.Height + b.Width * b.Height - inter;
            return uni <= 0 ? 0f : inter / uni;
        }

        public void Dispose() => _session.Dispose();

        // モデルパスごとにセッションをキャッシュ
        private static readonly ConcurrentDictionary<string, YoloOnnxDetector> _detectorCache = new();

        /// <summary>
        /// YOLO(ONNX)でアクティブ画面/ウィンドウから対象を検出し、最良検出の中心Pointを返す。
        /// クラスIDまたはクラス名で対象を絞り込み可能。どちらも未指定なら「最高スコアの検出」を返す。
        /// </summary>
        public static async Task<OpenCvSharp.Point?> SearchObjectOnnx(
            string onnxPath,
            CancellationToken token,
            double confThreshold = 0.25,
            double iouThreshold = 0.45,
            int? targetClassId = null,
            string? targetClassName = null,
            string? namesFilePath = null,
            Color? searchColor = null,
            string windowTitle = "",
            string windowClassName = "",
            int inputW = 640, int inputH = 640,
            bool useDirectML = false)
        {
            return await Task.Run(() =>
            {
                // 1) 画面/ウィンドウキャプチャ（BGRA想定）
                using Mat frame = string.IsNullOrEmpty(windowTitle)
                    ? ScreenCaptureHelper.CaptureScreen()
                    : ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);

                // BGRA -> BGR
                if (frame.Channels() == 4)
                    Cv2.CvtColor(frame, frame, ColorConversionCodes.BGRA2BGR);

                // 2) 検出器取得（キャッシュ or 新規）
                var key = $"{onnxPath}|{inputW}x{inputH}|dml={useDirectML}";
                var det = _detectorCache.GetOrAdd(key, _ => new YoloOnnxDetector(onnxPath, inputW, inputH, useDirectML));

                // 3) 検出
                // namesFilePath があれば行数から numClasses を算出
                int? numClasses = null;
                if (!string.IsNullOrWhiteSpace(namesFilePath) && File.Exists(namesFilePath))
                    numClasses = File.ReadAllLines(namesFilePath).Count(l => !string.IsNullOrWhiteSpace(l));

                // 検出呼び出し時に numClasses を渡す
                var dets = det.Detect(
                    frame,
                    conf: (float)confThreshold,
                    iou: (float)iouThreshold,
                    maskColor: searchColor,
                    numClasses: numClasses);

                if (dets.Count == 0)
                    return (OpenCvSharp.Point?)null;

                // 4) クラス名→ID解決（必要なら）
                int? classId = targetClassId;
                if (classId == null && !string.IsNullOrWhiteSpace(targetClassName) && !string.IsNullOrWhiteSpace(namesFilePath))
                {
                    try
                    {
                        var names = File.ReadAllLines(namesFilePath)
                                        .Select((n, i) => (Name: n.Trim(), Id: i))
                                        .ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);
                        if (names.TryGetValue(targetClassName!.Trim(), out var id))
                            classId = id;
                    }
                    catch { /* 無視 */ }
                }

                // 5) フィルタリング（クラス指定があれば）
                if (classId != null)
                    dets = dets.Where(d => d.ClassId == classId.Value).ToList();

                if (dets.Count == 0)
                    return (OpenCvSharp.Point?)null;

                // 6) 最良（最高スコア）を採用 → 中心Point
                var best = dets.OrderByDescending(d => d.Score).First();
                var center = new OpenCvSharp.Point(best.Box.X + best.Box.Width / 2,
                                                   best.Box.Y + best.Box.Height / 2);
                return (OpenCvSharp.Point?)center;
            }, token);
        }
    }
}
