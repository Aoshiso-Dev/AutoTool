using System.Drawing;
using System.Runtime.InteropServices;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// Win32 API��g�p�����E�B���h�E����T�[�r�X�̎���
/// </summary>
public partial class Win32WindowService : IWindowService
{
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, EntryPoint = "FindWindowW")]
    private static partial IntPtr NativeFindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public IntPtr GetWindowHandle(string? windowTitle, string? windowClassName)
    {
        if (string.IsNullOrEmpty(windowClassName) && string.IsNullOrEmpty(windowTitle))
        {
            return IntPtr.Zero;
        }

        var className = string.IsNullOrEmpty(windowClassName) ? null : windowClassName;
        var title = string.IsNullOrEmpty(windowTitle) ? null : windowTitle;
        return NativeFindWindow(className, title);
    }

    public Rectangle? GetWindowRect(IntPtr windowHandle)
    {
        return windowHandle == IntPtr.Zero
            ? null
            : NativeGetWindowRect(windowHandle, out var rect)
                ? new Rectangle(
                    rect.Left,
                    rect.Top,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top)
                : null;
    }

    public (int relativeX, int relativeY, bool success, string? errorMessage) ConvertToRelativeCoordinates(
        int absoluteX, int absoluteY, string? windowTitle, string? windowClassName)
    {
        if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName))
        {
            return (absoluteX, absoluteY, true, null);
        }

        var windowHandle = GetWindowHandle(windowTitle, windowClassName);
        if (windowHandle == IntPtr.Zero)
        {
            return (absoluteX, absoluteY, false, "�w�肳�ꂽ�E�B���h�E��������܂���B");
        }

        var windowRect = GetWindowRect(windowHandle);
        if (windowRect is null)
        {
            return (absoluteX, absoluteY, false, "�E�B���h�E�̈ʒu��񂪎擾�ł��܂���ł����B");
        }

        var relativeX = absoluteX - windowRect.Value.Left;
        var relativeY = absoluteY - windowRect.Value.Top;

        return (relativeX, relativeY, true, null);
    }
}
