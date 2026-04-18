using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

internal static partial class Win32CaptureCore
{
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;
    private const int SrcCopy = 0x00CC0020;
    private const int CaptureBlt = 0x40000000;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static Bitmap CapturePrimaryScreen()
    {
        var width = NativeGetSystemMetrics(SmCxScreen);
        var height = NativeGetSystemMetrics(SmCyScreen);

        return CaptureDesktopRegion(new Rectangle(0, 0, width, height));
    }

    public static Bitmap CaptureDesktopRegion(Rectangle region)
    {
        using var bitmap = new Bitmap(region.Width, region.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.X, region.Y, 0, 0, bitmap.Size);
        return (Bitmap)bitmap.Clone();
    }

    public static Bitmap CaptureWindow(string windowTitle, string windowClassName = "", bool preferPrintWindow = false)
    {
        var hWnd = NativeFindWindow(string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"ウィンドウが見つかりません。Title='{windowTitle}', ClassName='{windowClassName}'。");
        }

        if (!NativeGetWindowRect(hWnd, out var rect))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("ウィンドウサイズが不正です。");
        }

        return CaptureWindowBitmap(hWnd, width, height, preferPrintWindow)
            ?? throw new InvalidOperationException("対象ウィンドウのキャプチャに失敗しました。");
    }

    public static Bitmap? TryCaptureWindowByTitle(string windowTitle)
    {
        var hWnd = NativeFindWindow(null, windowTitle);
        if (hWnd == IntPtr.Zero)
        {
            return null;
        }

        if (!NativeGetWindowRect(hWnd, out var rect))
        {
            return null;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        return CaptureWindowBitmap(hWnd, width, height, preferPrintWindow: true);
    }

    public static void TryEnablePerMonitorDpi()
    {
        try
        {
            _ = NativeSetProcessDpiAwarenessContext((IntPtr)(-4));
        }
        catch
        {
        }
    }

    private static Bitmap? CaptureWindowBitmap(IntPtr hWnd, int width, int height, bool preferPrintWindow)
    {
        var hWndDC = NativeGetWindowDC(hWnd);
        if (hWndDC == IntPtr.Zero)
        {
            hWndDC = NativeGetDC(IntPtr.Zero);
        }

        var memDC = NativeCreateCompatibleDC(hWndDC);
        var hBmp = NativeCreateCompatibleBitmap(hWndDC, width, height);
        var old = NativeSelectObject(memDC, hBmp);

        var ok = false;
        try
        {
            if (preferPrintWindow)
            {
                ok = NativePrintWindow(hWnd, memDC, 0);
            }

            if (!ok)
            {
                ok = NativeBitBlt(memDC, 0, 0, width, height, hWndDC, 0, 0, SrcCopy | CaptureBlt);
            }
        }
        finally
        {
            _ = NativeSelectObject(memDC, old);
            _ = NativeReleaseDC(hWnd, hWndDC);
            _ = NativeDeleteDC(memDC);
        }

        if (!ok)
        {
            _ = NativeDeleteObject(hBmp);
            return null;
        }

        using var image = Image.FromHbitmap(hBmp);
        _ = NativeDeleteObject(hBmp);
        return new Bitmap(image);
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "FindWindowW")]
    private static partial IntPtr NativeFindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetWindowRect(IntPtr hWnd, out RECT lpRect);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowDC")]
    private static partial IntPtr NativeGetWindowDC(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetDC")]
    private static partial IntPtr NativeGetDC(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "ReleaseDC")]
    private static partial int NativeReleaseDC(IntPtr hWnd, IntPtr hdc);

    [LibraryImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
    private static partial IntPtr NativeCreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll", EntryPoint = "DeleteDC")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeDeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
    private static partial IntPtr NativeCreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [LibraryImport("gdi32.dll", EntryPoint = "SelectObject")]
    private static partial IntPtr NativeSelectObject(IntPtr hdc, IntPtr hgdiObj);

    [LibraryImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeDeleteObject(IntPtr hObject);

    [LibraryImport("gdi32.dll", EntryPoint = "BitBlt")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeBitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [LibraryImport("user32.dll", EntryPoint = "PrintWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativePrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetSystemMetrics")]
    private static partial int NativeGetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetProcessDpiAwarenessContext(IntPtr value);
}
