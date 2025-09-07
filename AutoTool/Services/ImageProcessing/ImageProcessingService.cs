using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Services.Mouse;
using AutoTool.Services.Window;
using System.Drawing;
using System.Drawing.Imaging;

namespace AutoTool.Services.ImageProcessing
{
    /// <summary>
    /// 画像処理サービスの実装
    /// </summary>
    public class ImageProcessingService : IImageProcessingService, IDisposable
    {
        private readonly ILogger<ImageProcessingService> _logger;
        private readonly IMouseService _mouseService;
        private readonly IWindowInfoService _windowInfoService;
        private bool _disposed = false;

        public ImageProcessingService(
            ILogger<ImageProcessingService> logger,
            IMouseService mouseService,
            IWindowInfoService windowInfoService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
            _windowInfoService = windowInfoService ?? throw new ArgumentNullException(nameof(windowInfoService));

            _logger.LogInformation("ImageProcessingService が初期化されました");
        }

        #region Image Search Methods

        public async Task<System.Windows.Point?> SearchImageAsync(string templateImagePath, double threshold = 0.8, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("SearchImageAsync 開始: {TemplatePath}, 閾値: {Threshold}, Window: {Window}", templateImagePath, threshold, string.IsNullOrEmpty(windowTitle) ? "(screen)" : windowTitle);

                if (string.IsNullOrEmpty(templateImagePath) || !File.Exists(templateImagePath))
                {
                    _logger.LogWarning("テンプレート画像が見つかりません: {Path}", templateImagePath);
                    return null;
                }

                if (!IsValidImageFile(templateImagePath))
                {
                    _logger.LogWarning("サポートされていない画像形式です: {Path}", templateImagePath);
                    return null;
                }

                if (!string.IsNullOrEmpty(windowTitle))
                {
                    return await SearchImageInWindowAsync(templateImagePath, windowTitle, windowClassName, threshold, cancellationToken);
                }
                else
                {
                    return await SearchImageOnScreenAsync(templateImagePath, threshold, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SearchImageAsync がキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchImageAsync エラー: {TemplatePath}", templateImagePath);
                return null;
            }
        }

        public async Task<List<ImageSearchResult>> SearchMultipleImagesAsync(IEnumerable<string> templateImagePaths, double threshold = 0.8, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default)
        {
            var results = new List<ImageSearchResult>();
            try
            {
                _logger.LogInformation("複数画像検索開始: {Count}のテンプレート", templateImagePaths.Count());
                foreach (var templatePath in templateImagePaths)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var pos = await SearchImageAsync(templatePath, threshold, windowTitle, windowClassName, cancellationToken);
                    sw.Stop();
                    results.Add(new ImageSearchResult
                    {
                        ImagePath = templatePath,
                        FoundPosition = pos,
                        MatchScore = pos.HasValue ? 1.0 : 0.0,
                        SearchDuration = sw.Elapsed
                    });
                }
                _logger.LogInformation("複数画像検索完了: {ResultCount}の結果", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "複数画像検索エラー");
            }
            return results;
        }

        public async Task<System.Windows.Point?> SearchImageWithColorFilterAsync(string imagePath, System.Drawing.Color searchColor, double threshold = 0.8, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("色フィルタ画像検索開始: {ImagePath}, 色: RGB({R},{G},{B}), 閾値:{T}", imagePath, searchColor.R, searchColor.G, searchColor.B, threshold);
                if (!File.Exists(imagePath)) return null;
                using var templColor = Cv2.ImRead(imagePath, ImreadModes.Color);
                if (templColor.Empty()) return null;

                Mat src;
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    var m = await CaptureWindowUsingBitBltAsync(windowTitle, windowClassName);
                    if (m == null) return null;
                    src = m;
                }
                else
                {
                    var m = await CaptureScreenAsync();
                    if (m == null) return null;
                    src = m;
                }

                cancellationToken.ThrowIfCancellationRequested();

                using var hsv = new Mat();
                Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);
                var hsvColor = RgbToHsv(searchColor);
                var lower = new Scalar(Math.Max(0, hsvColor.H - 10), Math.Max(0, hsvColor.S - 60), Math.Max(0, hsvColor.V - 60));
                var upper = new Scalar(Math.Min(179, hsvColor.H + 10), Math.Min(255, hsvColor.S + 60), Math.Min(255, hsvColor.V + 60));
                using var mask = new Mat();
                Cv2.InRange(hsv, lower, upper, mask);
                using var masked = new Mat();
                Cv2.BitwiseAnd(src, src, masked, mask);
                using var grayMasked = new Mat();
                Cv2.CvtColor(masked, grayMasked, ColorConversionCodes.BGR2GRAY);
                using var templGray = new Mat();
                Cv2.CvtColor(templColor, templGray, ColorConversionCodes.BGR2GRAY);
                using var result = new Mat();
                Cv2.MatchTemplate(grayMasked, templGray, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
                if (maxVal >= threshold)
                {
                    var centerX = maxLoc.X + templGray.Width / 2.0;
                    var centerY = maxLoc.Y + templGray.Height / 2.0;
                    return new System.Windows.Point(centerX, centerY);
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("色フィルタ画像検索がキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色フィルタ画像検索エラー: {ImagePath}", imagePath);
                return null;
            }
        }

        public async Task<System.Windows.Point?> SearchImageOnScreenAsync(string imagePath, double threshold = 0.8, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(imagePath)) return null;
                using var templ = Cv2.ImRead(imagePath, ImreadModes.Color);
                if (templ.Empty()) return null;
                var screen = await CaptureScreenAsync();
                if (screen == null) return null;
                cancellationToken.ThrowIfCancellationRequested();
                using var result = new Mat();
                Cv2.MatchTemplate(screen, templ, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
                if (maxVal >= threshold)
                {
                    return new System.Windows.Point(maxLoc.X + templ.Width / 2.0, maxLoc.Y + templ.Height / 2.0);
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("スクリーン画像検索がキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "スクリーン画像検索エラー: {ImagePath}", imagePath);
                return null;
            }
        }

        public async Task<System.Windows.Point?> SearchImageInWindowAsync(string imagePath, string windowTitle, string windowClassName = "", double threshold = 0.8, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(imagePath)) return null;
                using var templ = Cv2.ImRead(imagePath, ImreadModes.Color);
                if (templ.Empty()) return null;
                var windowMat = await CaptureWindowUsingBitBltAsync(windowTitle, windowClassName);
                if (windowMat == null) return null;
                cancellationToken.ThrowIfCancellationRequested();
                using var result = new Mat();
                Cv2.MatchTemplate(windowMat, templ, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
                if (maxVal >= threshold)
                {
                    return new System.Windows.Point(maxLoc.X + templ.Width / 2.0, maxLoc.Y + templ.Height / 2.0);
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ウィンドウ内画像検索がキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ内画像検索エラー: {ImagePath}, {WindowTitle}", imagePath, windowTitle);
                return null;
            }
        }

        #endregion

        #region Feature / Edge / Contour

        public async Task<IEnumerable<System.Windows.Point>> DetectFeaturePointsAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(imagePath)) return Enumerable.Empty<System.Windows.Point>();
                using var templ = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                var orb = OpenCvSharp.ORB.Create();
                OpenCvSharp.KeyPoint[] keypoints;
                using var descriptors = new Mat();
                orb.DetectAndCompute(templ, null, out keypoints, descriptors);
                return keypoints.Select(k => new System.Windows.Point(k.Pt.X, k.Pt.Y)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "特徴点検出エラー: {ImagePath}", imagePath);
                return Enumerable.Empty<System.Windows.Point>();
            }
        }

        public async Task<string?> DetectEdgesAsync(string imagePath, double threshold1 = 100, double threshold2 = 200, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(imagePath)) return null;
                using var src = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                using var edges = new Mat();
                Cv2.Canny(src, edges, threshold1, threshold2);
                var outputPath = Path.Combine(Path.GetDirectoryName(imagePath) ?? string.Empty, Path.GetFileNameWithoutExtension(imagePath) + "_edges.png");
                Cv2.ImWrite(outputPath, edges);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "エッジ検出エラー: {ImagePath}", imagePath);
                return null;
            }
        }

        public async Task<IEnumerable<IEnumerable<System.Windows.Point>>> DetectContoursAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(imagePath)) return Enumerable.Empty<IEnumerable<System.Windows.Point>>();
                using var src = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                using var thresh = new Mat();
                Cv2.Threshold(src, thresh, 128, 255, ThresholdTypes.Binary);
                OpenCvSharp.Point[][] contours;
                OpenCvSharp.HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                return contours.Select(c => c.Select(p => new System.Windows.Point(p.X, p.Y)) as IEnumerable<System.Windows.Point>);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "輪郭検出エラー: {ImagePath}", imagePath);
                return Enumerable.Empty<IEnumerable<System.Windows.Point>>();
            }
        }

        #endregion

        #region Screen / Region Capture

        public async Task<OpenCvSharp.Mat?> CaptureScreenAsync()
        {
            try
            {
                await Task.Yield();
                var width = (int)SystemParameters.PrimaryScreenWidth;
                var height = (int)SystemParameters.PrimaryScreenHeight;
                using var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                }
                return BitmapConverter.ToMat(bmp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "スクリーンキャプチャエラー");
                return null;
            }
        }

        // Capture region and return saved file path (interface requires this overload)
        public async Task<string?> CaptureRegionAsync(System.Windows.Rect region, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rect = new System.Drawing.Rectangle((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height);
                using var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
                using var g = Graphics.FromImage(bmp);
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
                var outputPath = Path.Combine(Environment.CurrentDirectory, $"capture_{timestamp}.png");
                bmp.Save(outputPath, ImageFormat.Png);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "領域キャプチャエラー: {Region}", region);
                return null;
            }
        }

        public async Task<OpenCvSharp.Mat?> CaptureRegionAsync(System.Windows.Rect region)
        {
            try
            {
                await Task.Yield();
                var rect = new System.Drawing.Rectangle((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height);
                using var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
                }
                return BitmapConverter.ToMat(bmp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "領域キャプチャエラー: {Region}", region);
                return null;
            }
        }

        #endregion

        #region Window Capture (BitBlt / PrintWindow)

        // Capture window and save to file (interface requires CancellationToken overload)
        public async Task<string?> CaptureWindowAsync(string windowTitle, string windowClassName = "", CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mat = await CaptureWindowUsingBitBltAsync(windowTitle, windowClassName);
                if (mat == null) return null;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
                var outputPath = Path.Combine(Environment.CurrentDirectory, $"window_capture_{timestamp}.png");
                Cv2.ImWrite(outputPath, mat);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウキャプチャエラー: {WindowTitle}", windowTitle);
                return null;
            }
        }

        // Capture window and return Mat
        public async Task<OpenCvSharp.Mat?> CaptureWindowAsync(string windowTitle, string windowClassName = "")
        {
            return await CaptureWindowUsingBitBltAsync(windowTitle, windowClassName);
        }

        public async Task<OpenCvSharp.Mat?> CaptureWindowUsingBitBltAsync(string windowTitle, string windowClassName = "")
        {
            try
            {
                await Task.Yield();
                var hWnd = FindWindowHandleByTitle(windowTitle);
                if (hWnd == IntPtr.Zero)
                {
                    _logger.LogWarning("ウィンドウが見つかりません: {WindowTitle}", windowTitle);
                    return null;
                }
                if (!GetWindowRect(hWnd, out RECT rect))
                {
                    _logger.LogWarning("ウィンドウ矩形の取得に失敗しました: {WindowTitle}", windowTitle);
                    return null;
                }
                int width = Math.Max(1, rect.Right - rect.Left);
                int height = Math.Max(1, rect.Bottom - rect.Top);
                using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr hdcDest = g.GetHdc();
                    try
                    {
                        // Try PrintWindow
                        if (PrintWindow(hWnd, hdcDest, 0))
                        {
                            // success
                        }
                        else
                        {
                            // Fallback to BitBlt from window DC
                            IntPtr windowDc = GetWindowDC(hWnd);
                            if (windowDc != IntPtr.Zero)
                            {
                                // Use GDI BitBlt
                                bool ok = BitBlt(hdcDest, 0, 0, width, height, windowDc, 0, 0, SRCCOPY);
                                ReleaseDC(hWnd, windowDc);
                                if (!ok)
                                {
                                    // fallback to screen copy
                                    g.ReleaseHdc(hdcDest);
                                    g.Dispose();
                                    using var screenBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                                    using (var sg = Graphics.FromImage(screenBmp))
                                    {
                                        sg.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                                    }
                                    return BitmapConverter.ToMat(screenBmp);
                                }
                            }
                            else
                            {
                                // fallback to screen copy
                                g.ReleaseHdc(hdcDest);
                                g.Dispose();
                                using var screenBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                                using (var sg = Graphics.FromImage(screenBmp))
                                {
                                    sg.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                                }
                                return BitmapConverter.ToMat(screenBmp);
                            }
                        }
                    }
                    finally
                    {
                        try { g.ReleaseHdc(hdcDest); } catch { }
                    }
                }
                return BitmapConverter.ToMat(bmp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BitBltウィンドウキャプチャエラー: {WindowTitle}", windowTitle);
                return null;
            }
        }

        #endregion

        #region Image Processing Helpers

        public async Task<bool> SaveImageAsync(OpenCvSharp.Mat image, string filePath)
        {
            try
            {
                Cv2.ImWrite(filePath, image);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像保存エラー: {FilePath}", filePath);
                return false;
            }
        }

        public System.Windows.Media.Imaging.BitmapSource MatToBitmapSource(OpenCvSharp.Mat mat)
        {
            try
            {
                var bmp = BitmapConverter.ToBitmap(mat);
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                return bitmapSource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mat->BitmapSource変換エラー");
                return new System.Windows.Media.Imaging.WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            }
        }

        public OpenCvSharp.Mat BitmapSourceToMat(System.Windows.Media.Imaging.BitmapSource bitmapSource)
        {
            try
            {
                using var ms = new MemoryStream();
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                using var bmp = new Bitmap(ms);
                return BitmapConverter.ToMat(bmp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BitmapSource->Mat変換エラー");
                return new OpenCvSharp.Mat();
            }
        }

        public OpenCvSharp.Mat ResizeImage(OpenCvSharp.Mat source, System.Drawing.Size newSize, OpenCvSharp.InterpolationFlags interpolation = OpenCvSharp.InterpolationFlags.Linear)
        {
            try
            {
                var dst = new Mat();
                Cv2.Resize(source, dst, new OpenCvSharp.Size(newSize.Width, newSize.Height), 0, 0, interpolation);
                return dst;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像リサイズエラー");
                return source.Clone();
            }
        }

        public OpenCvSharp.Mat ConvertToGrayscale(OpenCvSharp.Mat source)
        {
            try
            {
                var dst = new Mat();
                Cv2.CvtColor(source, dst, ColorConversionCodes.BGR2GRAY);
                return dst;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "グレースケール変換エラー");
                return source.Clone();
            }
        }

        public OpenCvSharp.Mat ConvertColor(OpenCvSharp.Mat source, OpenCvSharp.ColorConversionCodes colorConversion)
        {
            try
            {
                var dst = new Mat();
                Cv2.CvtColor(source, dst, colorConversion);
                return dst;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色空間変換エラー");
                return source.Clone();
            }
        }

        #endregion

        #region Utility Methods

        public bool IsValidImageFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return false;
                var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".gif" };
                return supportedExtensions.Contains(extension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像ファイル検証エラー: {FilePath}", filePath);
                return false;
            }
        }

        public IEnumerable<string> GetSupportedImageExtensions() => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".gif" };

        public bool IsProcessingActive { get; private set; } = false;
        public void CancelProcessing() { IsProcessingActive = false; _logger.LogInformation("画像処理をキャンセルしました"); }
        public double ProcessingProgress { get; private set; } = 0.0;
        public event EventHandler<double>? ProgressChanged;

        #endregion

        #region Native Window Helpers & PInvoke

        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")] 
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        private static IntPtr FindWindowHandleByTitle(string windowTitle)
        {
            IntPtr found = IntPtr.Zero;
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    int length = GetWindowTextLength(hWnd);
                    var sb = new System.Text.StringBuilder(length + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    var title = sb.ToString();
                    if (string.IsNullOrEmpty(title)) return true;
                    if (title.IndexOf(windowTitle, StringComparison.OrdinalIgnoreCase) >= 0) { found = hWnd; return false; }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return found;
        }

        private static (double H, double S, double V) RgbToHsv(System.Drawing.Color c)
        {
            double r = c.R / 255.0; double g = c.G / 255.0; double b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)); double min = Math.Min(r, Math.Min(g, b));
            double h = 0.0;
            if (max == min) h = 0;
            else if (max == r) h = (60 * ((g - b) / (max - min)) + 360) % 360;
            else if (max == g) h = (60 * ((b - r) / (max - min)) + 120) % 360;
            else if (max == b) h = (60 * ((r - g) / (max - min)) + 240) % 360;
            double s = (max == 0) ? 0 : (1 - min / max); double v = max;
            return (H: h / 2.0, S: s * 255.0, V: v * 255.0);
        }

        #endregion

        #region IDisposable

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing) { if (!_disposed && disposing) { _disposed = true; _logger.LogInformation("ImageProcessingService がリソース解放されました"); } }

        #endregion

        public async Task<System.Windows.Point?> SearchColorAsync(System.Drawing.Color targetColor, int tolerance = 10, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("色検索開始: {Color}, 許容差: {Tolerance}", targetColor, tolerance);

                Mat src;
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    var m = await CaptureWindowUsingBitBltAsync(windowTitle, windowClassName);
                    if (m == null) return null;
                    src = m;
                }
                else
                {
                    var m = await CaptureScreenAsync();
                    if (m == null) return null;
                    src = m;
                }

                cancellationToken.ThrowIfCancellationRequested();

                using var bmp = BitmapConverter.ToBitmap(src);
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        var c = bmp.GetPixel(x, y);
                        if (Math.Abs(c.R - targetColor.R) <= tolerance && Math.Abs(c.G - targetColor.G) <= tolerance && Math.Abs(c.B - targetColor.B) <= tolerance)
                        {
                            var found = new System.Windows.Point(x, y);
                            _logger.LogInformation("色検索で見つかりました: {Result}", found);
                            return found;
                        }
                    }
                }

                _logger.LogInformation("色検索で一致するピクセルは見つかりませんでした");
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("色検索がキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色検索エラー: {Color}", targetColor);
                return null;
            }
        }
    }
}