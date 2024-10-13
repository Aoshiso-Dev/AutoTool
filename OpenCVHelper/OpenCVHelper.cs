using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Media;
using System.Drawing;

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
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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
        public static Mat CaptureWindowUsingBitBlt(string windowTitle)
        {
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
        public static Mat CaptureWindow(string windowTitle)
        {
            return CaptureWindowUsingBitBlt(windowTitle);

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
        public static async Task<OpenCvSharp.Point?> SearchImage(Mat targetMat, Mat templateMat, CancellationToken token, double threshold = 0.8, Color? searchColor = null)
        {
            return await Task.Run(() =>
            {
                if(searchColor == null)
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
        // 負荷が高いため非推奨
        public static async Task<OpenCvSharp.Point?> SearchImageMultiScale(Mat targetMat, Mat templateMat, CancellationToken token, double threshold = 0.8, double minScale = 0.2, double maxScale = 2.5, double scaleStep = 0.05)
        {
            // スケールを調整しながらテンプレートマッチングを実行
            for (double scale = minScale; scale <= maxScale; scale += scaleStep)
            {
                if(token.IsCancellationRequested)
                {
                    break;
                }

                using Mat resizedTemplateMat = new Mat();
                Cv2.Resize(templateMat, resizedTemplateMat, new OpenCvSharp.Size(templateMat.Width * scale, templateMat.Height * scale));

                var matchLocation = await SearchImage(targetMat, resizedTemplateMat, token, threshold);

                if (matchLocation != null)
                {
                    token.ThrowIfCancellationRequested();
                    return matchLocation;
                }
            }

            return null;
        }
        */

        public static async Task<OpenCvSharp.Point?> SearchImageFromScreen(Mat templateMat, CancellationToken token, double threshold = 0.8, Color? searchColor = null, bool multiScale = false)
        {
            // スクリーンショットを取得
            using Mat screenMat = ScreenCaptureHelper.CaptureScreen();

            //return false ? await SearchImageMultiScale(screenMat, templateMat, token, threshold) : await SearchImage(screenMat, templateMat, token, threshold, searchColor);
            return await SearchImage(screenMat, templateMat, token, threshold, searchColor);
        }

        public static async Task<OpenCvSharp.Point?> SearchImageFromScreen(string imagePath, CancellationToken token, double threshold = 0.8, Color? searchColor = null, bool multiScale = false)
        {
            // ファイル存在確認
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            return await SearchImageFromScreen(new Mat(imagePath), cts.Token, threshold, searchColor, multiScale);
        }

        /*
        public static async Task<OpenCvSharp.Point?> SearchImageFromWindow(string windowTitle, Mat templateMat, CancellationToken token, double threshold = 0.8, bool multiScale = false)
        {
            // ウィンドウキャプチャ
            using Mat windowMat = ScreenCaptureHelper.CaptureWindow(windowTitle);

            return multiScale ? await SearchImageMultiScale(windowMat, templateMat, token, threshold) : await SearchImage(windowMat, templateMat, token, threshold);
        }
        */


        public static async Task<OpenCvSharp.Point?> SearchImageFromWindow(string windowTitle, string imagePath, CancellationToken token, double threshold = 0.8, Color? searchColor = null, bool multiScale = false)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            using Mat windowMat = ScreenCaptureHelper.CaptureWindow(windowTitle);
            using Mat templateMat = new Mat(imagePath);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            //return false ? await SearchImageMultiScale(windowMat, templateMat, cts.Token, threshold) : await SearchImage(windowMat, templateMat, cts.Token, threshold);
            return await SearchImage(windowMat, templateMat, cts.Token, threshold, searchColor);
        }
    }
}
