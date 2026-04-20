using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 前提条件を検証し、失敗時は例外を送出して処理を保護します。
/// </summary>
internal static class Win32NativeGuards
{
    public static void ThrowIfFalse(bool result, string apiName)
    {
        if (result)
        {
            return;
        }

        throw new Win32Exception(Marshal.GetLastPInvokeError(), $"{apiName} の呼び出しに失敗しました。");
    }

    public static IntPtr ThrowIfZero(IntPtr handle, string apiName, string? detail = null)
    {
        if (handle != IntPtr.Zero)
        {
            return handle;
        }

        var suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        throw new Win32Exception(Marshal.GetLastPInvokeError(), $"{apiName} の呼び出しに失敗しました。{suffix}");
    }
}
