using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;

namespace OpenCVHelper
{
    public static class ScreenCaptureHelper
    {
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
    }
}
