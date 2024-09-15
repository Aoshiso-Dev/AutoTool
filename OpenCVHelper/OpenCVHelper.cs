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
        /// <summary>
        /// 指定された画像が見つかるまで待機し、見つかった場合はその座標を返します。
        /// </summary>
        /// <param name="imagePath">検索する画像のパス</param>
        /// <param name="threshold">マッチングの閾値</param>
        /// <param name="timeoutMs">タイムアウトまでの時間（秒）</param>
        /// <param name="waitTimeMs">再試行間隔（ミリ秒）</param>
        /// <param name="cancellationToken">キャンセルを処理するトークン</param>
        /// <returns>画像の中心座標（見つからなかった場合は null）</returns>
        public static async Task<OpenCvSharp.Point?> WaitForImageAsync(string imagePath, double threshold = 0.8, int timeoutMs = 5000, int waitTimeMs = 1000, CancellationToken cancellationToken = default)
        {
            // ファイル存在確認
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("Image file not found.", imagePath);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            while (true)
            {
                // キャンセルがリクエストされたかを確認
                if (cancellationToken.IsCancellationRequested)
                {
                    return null; // ループを終了
                }

                // タイムアウト
                if (cts.Token.IsCancellationRequested)
                {
                    return null; // ループを終了
                }

                // スクリーンショットを取得
                using Mat screenMat = ScreenCaptureHelper.CaptureScreen();

                // 対象の画像を読み込む
                using Mat template = Cv2.ImRead(imagePath, ImreadModes.Color);

                // 色空間を揃える
                Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2BGR);
                Cv2.CvtColor(template, template, ColorConversionCodes.BGRA2BGR);

                // マッチングを実行
                using Mat result = new Mat();
                Cv2.MatchTemplate(screenMat, template, result, TemplateMatchModes.CCoeffNormed);

                // マッチング結果から最大値とその位置を取得
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal >= threshold)
                {
                    // 対象画像の中心座標を計算して返す
                    OpenCvSharp.Point center = new OpenCvSharp.Point(maxLoc.X + template.Width / 2, maxLoc.Y + template.Height / 2);
                    return center;
                }

                // 画像が見つからない場合、指定した時間待機して再試行
                await Task.Delay(waitTimeMs, cancellationToken); // 非同期で待機
            }
        }

        public static OpenCvSharp.Point? SearchImage(string imagePath, double threshold = 0.8)
        {
            // ファイル存在確認
            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException("ファイルが見つかりません。", imagePath);
            }

            // スクリーンショットを取得
            using Mat screenMat = ScreenCaptureHelper.CaptureScreen();

            // 対象の画像を読み込む
            using Mat template = Cv2.ImRead(imagePath, ImreadModes.Color);

            // 色空間を揃える
            Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(template, template, ColorConversionCodes.BGRA2BGR);

            // マッチングを実行
            using Mat result = new Mat();
            Cv2.MatchTemplate(screenMat, template, result, TemplateMatchModes.CCoeffNormed);

            // マッチング結果から最大値とその位置を取得
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
            {
                // 対象画像の中心座標を計算して返す
                OpenCvSharp.Point center = new OpenCvSharp.Point(maxLoc.X + template.Width / 2, maxLoc.Y + template.Height / 2);
                return center;
            }

            return null;
        }
    }
}
