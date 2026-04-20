using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 低レベル API 呼び出しをラップして共通化し、呼び出し側の実装を簡潔にします。
/// </summary>

public static partial class Win32WindowInfoHelper
{
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// 処理で利用する値を軽量に保持し、受け渡し時のオーバーヘッドを抑えます。
    /// </summary>

    private struct POINT
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "WindowFromPoint")]
    private static partial IntPtr NativeWindowFromPoint(POINT point);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "GetWindowTextW")]
    private static partial int NativeGetWindowText(IntPtr hWnd, char[] text, int maxCount);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "GetClassNameW")]
    private static partial int NativeGetClassName(IntPtr hWnd, char[] className, int maxCount);

    public static string GetWindowTitle(Point point)
    {
        var hWnd = GetWindowHandle(point);
        return ReadWindowString((buffer, maxCount) => NativeGetWindowText(hWnd, buffer, maxCount));
    }

    public static string GetWindowClassName(Point point)
    {
        var hWnd = GetWindowHandle(point);
        return ReadWindowString((buffer, maxCount) => NativeGetClassName(hWnd, buffer, maxCount));
    }

    private static string ReadWindowString(Func<char[], int, int> reader)
    {
        var buffer = new char[256];
        var length = reader(buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    private static IntPtr GetWindowHandle(Point point)
    {
        return Win32NativeGuards.ThrowIfZero(
            NativeWindowFromPoint(new POINT { X = point.X, Y = point.Y }),
            nameof(NativeWindowFromPoint));
    }
}
