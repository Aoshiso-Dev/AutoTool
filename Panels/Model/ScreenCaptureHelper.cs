using OpenCvSharp;
using System;
using System.Drawing;
using System.Windows;

public class ScreenCaptureHelper
{
    // �X�N���[���S�̂��L���v�`�����郁�\�b�h
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

    // �w��̈���L���v�`�����郁�\�b�h
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

    // �L���v�`�������摜��ۑ����郁�\�b�h
    public static void SaveCapture(Mat image, string filePath)
    {
        // �t�H���_�����݂��Ȃ���΍쐬
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
