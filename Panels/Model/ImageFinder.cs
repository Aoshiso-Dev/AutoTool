using OpenCvSharp;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

public class ImageFinder
{
    /// <summary>
    /// 指定された画像をクリックします。画像が見つからない場合、タイムアウトするかキャンセルされます。
    /// </summary>
    /// <param name="imagePath">検索する画像のパス</param>
    /// <param name="threshold">マッチングの閾値</param>
    /// <param name="timeoutMs">タイムアウトまでの時間（秒）</param>
    /// <param name="waitTimeMs">再試行間隔（ミリ秒）</param>
    /// <param name="cancellationToken">キャンセルを処理するトークン</param>
    public static async Task ClickImageAsync(string imagePath, double threshold = 0.8, int timeoutMs = 5000, int waitTimeMs = 1000, CancellationToken cancellationToken = default)
    {
        var position = await WaitForImageAsync(imagePath, threshold, timeoutMs, waitTimeMs, cancellationToken);

        if (position != null)
        {
            MouseControlHelper.Click(position.Value.X, position.Value.Y);
        }
        else
        {
            // 画像が見つからなかった場合やキャンセルされた場合の処理
            Console.WriteLine("Image not found or operation cancelled.");
        }
    }

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
}
