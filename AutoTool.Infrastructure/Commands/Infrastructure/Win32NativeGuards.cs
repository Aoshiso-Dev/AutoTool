using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

internal static class Win32NativeGuards
{
    public static void ThrowIfFalse(bool result, string apiName)
    {
        if (result)
        {
            return;
        }

        throw new Win32Exception(Marshal.GetLastPInvokeError(), $"{apiName} failed.");
    }

    public static IntPtr ThrowIfZero(IntPtr handle, string apiName, string? detail = null)
    {
        if (handle != IntPtr.Zero)
        {
            return handle;
        }

        var suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        throw new Win32Exception(Marshal.GetLastPInvokeError(), $"{apiName} failed.{suffix}");
    }
}
