using OpenCvSharp;
using System;
using System.Drawing;
using System.Windows;

public class ScreenCaptureHelper
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
