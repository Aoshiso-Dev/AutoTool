using System;
using Tesseract;
using OpenCvSharp;

public class GameScreenOCR
{
    // Tesseract OCRのエンジンを初期化
    private TesseractEngine ocrEngine;

    public GameScreenOCR()
    {
        // Tesseractエンジンの初期化 (日本語のデータセットが必要なら "jpn" とする)
        ocrEngine = new TesseractEngine(@"./tessdata", "jpn", EngineMode.Default);
    }

    // Mat形式の画像を受け取り、数字を認識するメソッド
    public string RecognizeDigits(Mat image)
    {
        // 画像を前処理
        Mat processedImage = PreprocessImage(image);

        // Tesseractを使って数字を認識
        return PerformOCR(processedImage);
    }

    // 画像の前処理（グレースケール変換と二値化）
    private Mat PreprocessImage(Mat image)
    {
        Mat gray = new Mat();

        // グレースケールに変換
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // 二値化（閾値を指定して画像を前処理）
        Mat thresholded = new Mat();
        Cv2.Threshold(gray, thresholded, 150, 255, ThresholdTypes.BinaryInv);

        return thresholded;
    }

    // Tesseractを使ってOCRを実行
    private string PerformOCR(Mat processedImage)
    {
        // MatからPix形式に変換してOCR処理
        using (var img = Pix.LoadFromMemory(processedImage.ToBytes()))
        {
            using (var page = ocrEngine.Process(img))
            {
                // 認識されたテキストを返す
                return page.GetText().Trim();
            }
        }
    }

    // 解放処理
    public void Dispose()
    {
        ocrEngine.Dispose();
    }
}