using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 低レベル API 呼び出しをラップして共通化し、呼び出し側の実装を簡潔にします。
/// </summary>

public static partial class Win32KeyStateHelper
{
    [LibraryImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
    private static partial short NativeGetAsyncKeyState(int vKey);

    public static bool IsKeyPressed(int virtualKey) => (NativeGetAsyncKeyState(virtualKey) & 0x8000) != 0;
}
