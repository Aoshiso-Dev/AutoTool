using System.Runtime.InteropServices;

namespace AutoTool.Commands.Infrastructure;

public static partial class Win32KeyStateHelper
{
    [LibraryImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
    private static partial short NativeGetAsyncKeyState(int vKey);

    public static bool IsKeyPressed(int virtualKey) => (NativeGetAsyncKeyState(virtualKey) & 0x8000) != 0;
}
