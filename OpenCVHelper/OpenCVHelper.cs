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
using LogHelper; // For GlobalLogger
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
                // 画像存在チェック
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    return (OpenCvSharp.Point?)null;
                }

                using var targetMat = string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName)
                    ? ScreenCaptureHelper.CaptureScreen()
                    : ScreenCaptureHelper.CaptureWindow(windowTitle, windowClassName);

                using var templateMat = new Mat(imagePath);

                if (searchColor == null)
                {
                    Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2GRAY);
                    Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2GRAY);
                }
                else
                {
                    Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2BGR);
                    Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2BGR);

                    var lowerR = Math.Max(searchColor.Value.R - 20, 0);
                    var lowerG = Math.Max(searchColor.Value.G - 20, 0);
                    var lowerB = Math.Max(searchColor.Value.B - 20, 0);
                    var lower = new Scalar(lowerR, lowerG, lowerB);

                    var upperR = Math.Min(searchColor.Value.R + 20, 255);
                    var upperG = Math.Min(searchColor.Value.G + 20, 255);
                    var upperB = Math.Min(searchColor.Value.B + 20, 255);
                    var upper = new Scalar(upperR, upperG, upperB);

                    using var mask = new Mat();

                    Cv2.InRange(targetMat, lower, upper, mask);
                    Cv2.BitwiseAnd(targetMat, targetMat, targetMat, mask);

                    Cv2.InRange(templateMat, lower, upper, mask);
                    Cv2.BitwiseAnd(templateMat, templateMat, templateMat, mask);
                }

                using var result = new Mat();
                Cv2.MatchTemplate(targetMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal >= threshold)
                {
                    var center = new OpenCvSharp.Point(maxLoc.X + templateMat.Width / 2, maxLoc.Y + templateMat.Height / 2);

                    var projectName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                    var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                    var resultMessage = $"マッチング成功: {center.X}, {center.Y}";
                    if (!(string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName)))
                    {
                        resultMessage += $" ({windowTitle}[{windowClassName}])";
                    }
                    GlobalLogger.Instance.Write("", "", projectName, methodName, resultMessage);

                    return center;
                }

                return (OpenCvSharp.Point?)null;
            }, token);
        }
    }
}
