using System.Runtime.InteropServices;
using System.Threading;

namespace AutoTool.Commands.Infrastructure;

public static partial class Win32MouseInterop
{
    public const string ClickInjectionModeMouseEvent = "MouseEvent";
    public const string ClickInjectionModeSendInput = "SendInput";

    private const int InputMouse = 0;
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenQuery = 0x0008;
    private const int TokenElevationClass = 20;
    private const int SwShow = 5;
    private const int SwRestore = 9;
    private const int SimulatedMoveSteps = 8;
    private const int SimulatedMoveStepDelayMs = 8;
    private const uint GwHwndPrev = 3;
    private const uint GwHwndNext = 2;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr HwndNotTopMost = new(-2);
    private static readonly IntPtr HwndTop = IntPtr.Zero;
    private static int _dpiAwarenessInitialized;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int Type;
        public MOUSEINPUT MouseInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_ELEVATION
    {
        public int TokenIsElevated;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private sealed class WindowSearchState
    {
        public required string? ExpectedTitle { get; init; }
        public required string? ExpectedClassName { get; init; }
        public IntPtr BestHandle { get; set; }
        public int BestScore { get; set; } = int.MinValue;
    }

    private readonly record struct WindowZOrderState(IntPtr NextWindow, IntPtr PrevWindow);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "mouse_event")]
    private static partial void NativeMouseEvent(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SendInput")]
    private static partial uint NativeSendInput(uint cInputs, INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetCursorPos(out POINT point);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetCursorPos(int x, int y);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "FindWindowW")]
    private static partial IntPtr NativeFindWindow(string? className, string? windowName);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "EnumWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeEnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "IsWindowVisible")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeIsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowTextLengthW")]
    private static partial int NativeGetWindowTextLength(IntPtr hWnd);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "GetWindowTextW")]
    private static partial int NativeGetWindowText(IntPtr hWnd, char[] text, int maxCount);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "GetClassNameW")]
    private static partial int NativeGetClassName(IntPtr hWnd, char[] className, int maxCount);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetWindowRect(IntPtr hWnd, out RECT rect);

    [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "BringWindowToTop")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeBringWindowToTop(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static partial IntPtr NativeGetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindow")]
    private static partial IntPtr NativeGetWindow(IntPtr hWnd, uint uCmd);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    private static partial uint NativeGetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    private static partial uint NativeGetCurrentThreadId();

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "AttachThreadInput")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeAttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [LibraryImport("user32.dll", EntryPoint = "IsIconic")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeIsIconic(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeSetProcessDpiAwarenessContext(IntPtr value);

    [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "OpenProcess")]
    private static partial IntPtr NativeOpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
    private static partial IntPtr NativeGetCurrentProcess();

    [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "CloseHandle")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeCloseHandle(IntPtr handle);

    [LibraryImport("advapi32.dll", SetLastError = true, EntryPoint = "OpenProcessToken")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeOpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [LibraryImport("advapi32.dll", SetLastError = true, EntryPoint = "GetTokenInformation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeGetTokenInformation(IntPtr tokenHandle, int tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    public static Task ClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", int holdDurationMs = 20, string clickInjectionMode = ClickInjectionModeMouseEvent, bool simulateMouseMove = false)
    {
        return PerformClickAsync(x, y, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, windowTitle, windowClassName, holdDurationMs, clickInjectionMode, simulateMouseMove);
    }

    public static System.Drawing.Point GetCursorPosition()
    {
        Win32NativeGuards.ThrowIfFalse(NativeGetCursorPos(out var point), nameof(NativeGetCursorPos));
        return new System.Drawing.Point(point.X, point.Y);
    }

    public static Task RightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", int holdDurationMs = 20, string clickInjectionMode = ClickInjectionModeMouseEvent, bool simulateMouseMove = false)
    {
        return PerformClickAsync(x, y, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, windowTitle, windowClassName, holdDurationMs, clickInjectionMode, simulateMouseMove);
    }

    public static Task MiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", int holdDurationMs = 20, string clickInjectionMode = ClickInjectionModeMouseEvent, bool simulateMouseMove = false)
    {
        return PerformClickAsync(x, y, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, windowTitle, windowClassName, holdDurationMs, clickInjectionMode, simulateMouseMove);
    }

    private static async Task PerformClickAsync(
        int x,
        int y,
        uint downEvent,
        uint upEvent,
        string windowTitle,
        string windowClassName,
        int holdDurationMs,
        string clickInjectionMode,
        bool simulateMouseMove)
    {
        EnsurePerMonitorDpiAwareness();

        var targetX = x;
        var targetY = y;
        var normalizedMode = NormalizeClickInjectionMode(clickInjectionMode);

        Win32NativeGuards.ThrowIfFalse(NativeGetCursorPos(out var originalPos), nameof(NativeGetCursorPos));

        IntPtr hWnd = IntPtr.Zero;
        WindowZOrderState? zOrderState = null;

        if (!string.IsNullOrWhiteSpace(windowTitle) || !string.IsNullOrWhiteSpace(windowClassName))
        {
            hWnd = ResolveWindowHandle(windowTitle, windowClassName);
            if (hWnd == IntPtr.Zero)
            {
                var display = FormatWindowDisplayName(windowTitle, windowClassName);
                throw new InvalidOperationException($"ウィンドウが見つかりません: {display}");
            }

            Win32NativeGuards.ThrowIfFalse(NativeGetWindowRect(hWnd, out var rect), nameof(NativeGetWindowRect));
            EnsurePrivilegeCompatible(hWnd, windowTitle, windowClassName);
            zOrderState = SaveWindowZOrder(hWnd);

            targetX += rect.Left;
            targetY += rect.Top;
            _ = TryActivateWindowWithRetry(hWnd);
            _ = BringToFrontLegacy(hWnd);

            await Task.Delay(40).ConfigureAwait(false);
        }

        CancellationTokenSource? cursorLockTokenSource = null;
        Task? cursorLockTask = null;
        try
        {
            if (simulateMouseMove)
            {
                await SimulateMouseMoveAsync(originalPos, targetX, targetY).ConfigureAwait(false);
                await Task.Delay(20).ConfigureAwait(false);
            }
            else
            {
                Win32NativeGuards.ThrowIfFalse(NativeSetCursorPos(targetX, targetY), nameof(NativeSetCursorPos));
            }
            (cursorLockTokenSource, cursorLockTask) = StartCursorLock(targetX, targetY);

            SendMouseClickEvent(downEvent, normalizedMode);
            if (holdDurationMs > 0)
            {
                await Task.Delay(holdDurationMs).ConfigureAwait(false);
            }

            SendMouseClickEvent(upEvent, normalizedMode);
        }
        finally
        {
            if (cursorLockTokenSource is not null)
            {
                cursorLockTokenSource.Cancel();
                try
                {
                    if (cursorLockTask is not null)
                    {
                        await cursorLockTask.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    cursorLockTokenSource.Dispose();
                }
            }

            _ = NativeSetCursorPos(originalPos.X, originalPos.Y);
            if (hWnd != IntPtr.Zero && zOrderState.HasValue)
            {
                RestoreWindowZOrder(hWnd, zOrderState.Value);
            }
        }
    }

    private static (CancellationTokenSource TokenSource, Task LockTask) StartCursorLock(int x, int y)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        var lockTask = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                _ = NativeSetCursorPos(x, y);
                Thread.Sleep(1);
            }
        }, token);

        return (tokenSource, lockTask);
    }

    private static async Task SimulateMouseMoveAsync(POINT from, int toX, int toY)
    {
        var deltaX = toX - from.X;
        var deltaY = toY - from.Y;

        if (deltaX == 0 && deltaY == 0)
        {
            return;
        }

        for (var step = 1; step <= SimulatedMoveSteps; step++)
        {
            var x = from.X + (deltaX * step / SimulatedMoveSteps);
            var y = from.Y + (deltaY * step / SimulatedMoveSteps);
            Win32NativeGuards.ThrowIfFalse(NativeSetCursorPos(x, y), nameof(NativeSetCursorPos));
            await Task.Delay(SimulatedMoveStepDelayMs).ConfigureAwait(false);
        }
    }

    private static void SendMouseClickEvent(uint flags, string normalizedMode)
    {
        if (string.Equals(normalizedMode, ClickInjectionModeSendInput, StringComparison.Ordinal))
        {
            INPUT[] input =
            [
                new()
                {
                    Type = InputMouse,
                    MouseInput = new MOUSEINPUT
                    {
                        DwFlags = flags
                    }
                }
            ];

            var sent = NativeSendInput((uint)input.Length, input, Marshal.SizeOf<INPUT>());
            if (sent != input.Length)
            {
                throw new InvalidOperationException($"SendInput に失敗しました。flags=0x{flags:X}");
            }

            return;
        }

        NativeMouseEvent(flags, 0, 0, 0, 0);
    }

    private static string NormalizeClickInjectionMode(string mode)
    {
        if (string.Equals(mode, ClickInjectionModeSendInput, StringComparison.OrdinalIgnoreCase))
        {
            return ClickInjectionModeSendInput;
        }

        return ClickInjectionModeMouseEvent;
    }

    private static void EnsurePerMonitorDpiAwareness()
    {
        if (Interlocked.Exchange(ref _dpiAwarenessInitialized, 1) != 0)
        {
            return;
        }

        try
        {
            _ = NativeSetProcessDpiAwarenessContext((IntPtr)(-4));
        }
        catch
        {
        }
    }

    private static IntPtr ResolveWindowHandle(string windowTitle, string windowClassName)
    {
        var title = string.IsNullOrWhiteSpace(windowTitle) ? null : windowTitle.Trim();
        var className = string.IsNullOrWhiteSpace(windowClassName) ? null : windowClassName.Trim();

        if (title is not null)
        {
            var exact = NativeFindWindow(className, title);
            if (exact != IntPtr.Zero)
            {
                return exact;
            }
        }

        WindowSearchState state = new()
        {
            ExpectedTitle = title,
            ExpectedClassName = className
        };

        var handle = GCHandle.Alloc(state);
        try
        {
            _ = NativeEnumWindows(EnumWindowsCallback, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }

        return state.BestHandle;
    }

    private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        if (!NativeIsWindowVisible(hWnd))
        {
            return true;
        }

        var handle = GCHandle.FromIntPtr(lParam);
        if (handle.Target is not WindowSearchState state)
        {
            return false;
        }

        var className = ReadClassName(hWnd);
        var title = ReadWindowTitle(hWnd);
        var score = MatchWindowScore(state.ExpectedTitle, state.ExpectedClassName, title, className);
        if (score > state.BestScore)
        {
            state.BestScore = score;
            state.BestHandle = hWnd;
        }

        return true;
    }

    private static int MatchWindowScore(string? expectedTitle, string? expectedClassName, string actualTitle, string actualClassName)
    {
        var hasTitleCondition = !string.IsNullOrWhiteSpace(expectedTitle);
        var hasClassCondition = !string.IsNullOrWhiteSpace(expectedClassName);
        if (!hasTitleCondition && !hasClassCondition)
        {
            return int.MinValue;
        }

        var score = 0;
        if (hasClassCondition)
        {
            if (!string.Equals(expectedClassName, actualClassName, StringComparison.OrdinalIgnoreCase))
            {
                return int.MinValue;
            }

            score += 4;
        }

        if (hasTitleCondition)
        {
            if (string.Equals(expectedTitle, actualTitle, StringComparison.OrdinalIgnoreCase))
            {
                score += 8;
            }
            else if (actualTitle.Contains(expectedTitle!, StringComparison.OrdinalIgnoreCase)
                     || expectedTitle!.Contains(actualTitle, StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }
            else
            {
                return int.MinValue;
            }
        }

        return score;
    }

    private static string ReadWindowTitle(IntPtr hWnd)
    {
        var length = NativeGetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = new char[length + 1];
        var copied = NativeGetWindowText(hWnd, buffer, buffer.Length);
        return copied > 0 ? new string(buffer, 0, copied) : string.Empty;
    }

    private static string ReadClassName(IntPtr hWnd)
    {
        var buffer = new char[256];
        var copied = NativeGetClassName(hWnd, buffer, buffer.Length);
        return copied > 0 ? new string(buffer, 0, copied) : string.Empty;
    }

    private static bool TryActivateWindowWithRetry(IntPtr hWnd)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (TryActivateWindow(hWnd))
            {
                return true;
            }

            Thread.Sleep(40);
        }

        return false;
    }

    private static bool TryActivateWindow(IntPtr hWnd)
    {
        if (NativeIsIconic(hWnd))
        {
            _ = NativeShowWindow(hWnd, SwRestore);
        }

        if (NativeSetForegroundWindow(hWnd))
        {
            return true;
        }

        var currentThreadId = NativeGetCurrentThreadId();
        var targetThreadId = NativeGetWindowThreadProcessId(hWnd, out _);
        var foregroundHwnd = NativeGetForegroundWindow();
        var foregroundThreadId = foregroundHwnd == IntPtr.Zero ? 0u : NativeGetWindowThreadProcessId(foregroundHwnd, out _);

        var attachedTarget = false;
        var attachedForeground = false;
        try
        {
            if (targetThreadId != 0 && targetThreadId != currentThreadId)
            {
                attachedTarget = NativeAttachThreadInput(currentThreadId, targetThreadId, true);
            }

            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId && foregroundThreadId != targetThreadId)
            {
                attachedForeground = NativeAttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            _ = NativeBringWindowToTop(hWnd);
            _ = NativeShowWindow(hWnd, SwShow);
            return NativeSetForegroundWindow(hWnd);
        }
        finally
        {
            if (attachedForeground)
            {
                _ = NativeAttachThreadInput(currentThreadId, foregroundThreadId, false);
            }

            if (attachedTarget)
            {
                _ = NativeAttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }
    }

    private static bool BringToFrontLegacy(IntPtr hWnd)
    {
        var foregroundWindow = NativeGetForegroundWindow();
        var targetThreadId = NativeGetWindowThreadProcessId(hWnd, out _);
        var foregroundThreadId = foregroundWindow == IntPtr.Zero ? 0u : NativeGetWindowThreadProcessId(foregroundWindow, out _);
        var attached = false;
        try
        {
            if (foregroundThreadId != 0 && targetThreadId != 0 && targetThreadId != foregroundThreadId)
            {
                attached = NativeAttachThreadInput(foregroundThreadId, targetThreadId, true);
            }

            _ = NativeSetWindowPos(hWnd, HwndTop, 0, 0, 0, 0, SwpNoMove | SwpNoSize);
            _ = NativeSetWindowPos(hWnd, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize);
            _ = NativeSetForegroundWindow(hWnd);
            _ = NativeSetWindowPos(hWnd, HwndNotTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize);
            return NativeGetForegroundWindow() == hWnd;
        }
        finally
        {
            if (attached)
            {
                _ = NativeAttachThreadInput(foregroundThreadId, targetThreadId, false);
            }
        }
    }

    private static WindowZOrderState SaveWindowZOrder(IntPtr hWnd)
    {
        var nextWindow = NativeGetWindow(hWnd, GwHwndNext);
        var prevWindow = NativeGetWindow(hWnd, GwHwndPrev);
        return new WindowZOrderState(nextWindow, prevWindow);
    }

    private static void RestoreWindowZOrder(IntPtr hWnd, WindowZOrderState state)
    {
        if (state.NextWindow != IntPtr.Zero)
        {
            _ = NativeSetWindowPos(hWnd, state.NextWindow, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
            return;
        }

        if (state.PrevWindow != IntPtr.Zero)
        {
            _ = NativeSetWindowPos(hWnd, state.PrevWindow, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
            return;
        }

        _ = NativeSetWindowPos(hWnd, HwndTop, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private static void EnsurePrivilegeCompatible(IntPtr hWnd, string windowTitle, string windowClassName)
    {
        if (!TryGetCurrentProcessElevation(out var currentElevated))
        {
            return;
        }

        if (!TryGetWindowProcessElevation(hWnd, out var targetElevated))
        {
            return;
        }

        if (!currentElevated && targetElevated)
        {
            var display = FormatWindowDisplayName(windowTitle, windowClassName);
            throw new InvalidOperationException($"対象ウィンドウは管理者権限で実行されています。AutoTool も管理者として実行してください。対象: {display}");
        }
    }

    private static bool TryGetCurrentProcessElevation(out bool elevated)
    {
        return TryGetProcessElevation(NativeGetCurrentProcess(), shouldCloseHandle: false, out elevated);
    }

    private static bool TryGetWindowProcessElevation(IntPtr hWnd, out bool elevated)
    {
        elevated = false;
        _ = NativeGetWindowThreadProcessId(hWnd, out var processId);
        if (processId == 0)
        {
            return false;
        }

        var processHandle = NativeOpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            return false;
        }

        return TryGetProcessElevation(processHandle, shouldCloseHandle: true, out elevated);
    }

    private static bool TryGetProcessElevation(IntPtr processHandle, bool shouldCloseHandle, out bool elevated)
    {
        elevated = false;
        IntPtr tokenHandle = IntPtr.Zero;
        IntPtr elevationPtr = IntPtr.Zero;

        try
        {
            if (!NativeOpenProcessToken(processHandle, TokenQuery, out tokenHandle))
            {
                return false;
            }

            var size = Marshal.SizeOf<TOKEN_ELEVATION>();
            elevationPtr = Marshal.AllocHGlobal(size);
            if (!NativeGetTokenInformation(tokenHandle, TokenElevationClass, elevationPtr, size, out _))
            {
                return false;
            }

            var tokenElevation = Marshal.PtrToStructure<TOKEN_ELEVATION>(elevationPtr);
            elevated = tokenElevation.TokenIsElevated != 0;
            return true;
        }
        finally
        {
            if (elevationPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(elevationPtr);
            }

            if (tokenHandle != IntPtr.Zero)
            {
                _ = NativeCloseHandle(tokenHandle);
            }

            if (shouldCloseHandle && processHandle != IntPtr.Zero)
            {
                _ = NativeCloseHandle(processHandle);
            }
        }
    }

    private static string FormatWindowDisplayName(string windowTitle, string windowClassName)
    {
        var title = string.IsNullOrWhiteSpace(windowTitle) ? "(未指定)" : windowTitle;
        var className = string.IsNullOrWhiteSpace(windowClassName) ? "(未指定)" : windowClassName;
        return $"{title}[{className}]";
    }
}
