using System.Drawing;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;


namespace YoloWinLib;


internal static class WindowCapturer
{
    private const int SRCCOPY = 0x00CC0020;


    public static Bitmap? CaptureByTitle(string windowTitle)
    {
        IntPtr hWnd = Native.FindWindow(null, windowTitle);
        if (hWnd == IntPtr.Zero) return null;
        if (!Native.GetWindowRect(hWnd, out var rect)) return null;
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;


        IntPtr hWndDC = Native.GetWindowDC(hWnd);
        if (hWndDC == IntPtr.Zero) hWndDC = Native.GetDC(IntPtr.Zero);
        IntPtr memDC = Native.CreateCompatibleDC(hWndDC);
        IntPtr hBmp = Native.CreateCompatibleBitmap(hWndDC, width, height);
        IntPtr old = Native.SelectObject(memDC, hBmp);


        bool ok = false;
        try
        {
            ok = Native.PrintWindow(hWnd, memDC, 0);
            if (!ok) ok = Native.BitBlt(memDC, 0, 0, width, height, hWndDC, 0, 0, SRCCOPY);
        }
        finally
        {
            Native.SelectObject(memDC, old);
            Native.ReleaseDC(hWnd, hWndDC);
            Native.DeleteDC(memDC);
        }
        if (!ok) { Native.DeleteObject(hBmp); return null; }


        var bmp = Image.FromHbitmap(hBmp);
        Native.DeleteObject(hBmp);
        return bmp;
    }
}


internal static class DpiUtil
{
    public static void TryEnablePerMonitorDpi()
    {
        try { Native.SetProcessDpiAwarenessContext((IntPtr)(-4)); } catch { }
    }
}


internal static class Native
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);


    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }


    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);


    [DllImport("user32.dll")] public static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr ho);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr hdcDest, int x, int y, int w, int h, IntPtr hdcSrc, int sx, int sy, int rop);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
}