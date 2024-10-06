using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;
using OpenCvSharp.Extensions;

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
        public static OpenCvSharp.Point? SearchImage(Mat targetMat, Mat templateMat, double threshold = 0.8)
        {
            // 色空間を揃える
            Cv2.CvtColor(targetMat, targetMat, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGRA2BGR);

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

            return null;
        }

        public static OpenCvSharp.Point? SearchImageFromScreen(Mat templateMat, double threshold = 0.8)
        {
            // スクリーンショットを取得
            using Mat screenMat = ScreenCaptureHelper.CaptureScreen();

            return SearchImage(screenMat, templateMat, threshold);
        }

        public static OpenCvSharp.Point? SearchImageFromScreen(string imagePath, double threshold = 0.8)
        {
            // ファイル存在確認
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            return SearchImageFromScreen(new Mat(imagePath), threshold);
        }

        public static OpenCvSharp.Point? SearchImageFromWindow(string windowTitle, Mat templateMat, double threshold = 0.8)
        {
            // ウィンドウキャプチャ
            using Mat windowMat = ScreenCaptureHelper.CaptureWindow(windowTitle);

            return SearchImage(windowMat, templateMat, threshold);
        }

        public static OpenCvSharp.Point? SearchImageFromWindow(string windowTitle, string imagePath, double threshold = 0.8)
        {
            // ファイル存在確認
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            return SearchImageFromWindow(windowTitle, new Mat(imagePath), threshold);
        }
    }
}
