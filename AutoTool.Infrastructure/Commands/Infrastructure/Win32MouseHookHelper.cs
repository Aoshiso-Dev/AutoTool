using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using AutoTool.Commands.Threading;

namespace AutoTool.Commands.Infrastructure;

public static partial class Win32MouseHookHelper
{
    public enum MouseHookEventKind
    {
        LButtonDown,
        LButtonUp,
        RButtonUp,
        MouseMove
    }

    public sealed record MouseHookEvent(MouseHookEventKind Kind, MouseEventArgs Args);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowsHookExW")]
    private static partial IntPtr NativeSetWindowsHookEx(int idHook, LowLevelMouseProc callback, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "UnhookWindowsHookEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeUnhookWindowsHookEx(IntPtr hook);

    [LibraryImport("user32.dll", EntryPoint = "CallNextHookEx")]
    private static partial IntPtr NativeCallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "GetModuleHandleW")]
    private static partial IntPtr NativeGetModuleHandle(string moduleName);

    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;
    private const int SubscriberBufferSize = 1024;

    private static readonly LowLevelMouseProc Proc = HookCallback;
    private static readonly object Gate = new();
    private static IntPtr _hookId = IntPtr.Zero;
    private static readonly Dictionary<int, Channel<MouseHookEvent>> Subscribers = [];
    private static int _nextSubscriberId;
    private static int _hookUserCount;
    private static long _droppedEventCount;

    public sealed class MouseEventArgs(int x, int y, int delta, int hWheel) : EventArgs
    {
        public int X { get; } = x;
        public int Y { get; } = y;
        public int Delta { get; } = delta;
        public int HWheel { get; } = hWheel;
    }

    public static event EventHandler<MouseEventArgs>? LButtonDown;
    public static event EventHandler<MouseEventArgs>? LButtonUp;
    public static event EventHandler<MouseEventArgs>? RButtonUp;
    public static event EventHandler<MouseEventArgs>? MouseMove;
    public static long DroppedEventCount => Interlocked.Read(ref _droppedEventCount);

    public static int SubscriberCount
    {
        get
        {
            lock (Gate)
            {
                return Subscribers.Count;
            }
        }
    }

    public static IAsyncEnumerable<MouseHookEvent> ReadEventsAsync(CancellationToken cancellationToken = default)
    {
        return ReadEventsInternalAsync(cancellationToken);
    }

    public static void StartHook()
    {
        lock (Gate)
        {
            _hookUserCount++;
            if (_hookId != IntPtr.Zero)
            {
                return;
            }
        }

        using var process = Process.GetCurrentProcess();
        if (process.MainModule is null)
        {
            lock (Gate)
            {
                _hookUserCount = Math.Max(0, _hookUserCount - 1);
            }
            return;
        }

        var hookId = NativeSetWindowsHookEx(WhMouseLl, Proc, NativeGetModuleHandle(process.MainModule.ModuleName), 0);
        lock (Gate)
        {
            if (hookId == IntPtr.Zero)
            {
                _hookUserCount = Math.Max(0, _hookUserCount - 1);
                return;
            }

            _hookId = hookId;
        }
    }

    public static void StopHook()
    {
        IntPtr hookToRelease = IntPtr.Zero;

        lock (Gate)
        {
            if (_hookUserCount > 0)
            {
                _hookUserCount--;
            }

            if (_hookUserCount != 0 || _hookId == IntPtr.Zero)
            {
                return;
            }

            hookToRelease = _hookId;
            _hookId = IntPtr.Zero;
        }

        NativeUnhookWindowsHookEx(hookToRelease);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var args = new MouseEventArgs(hookStruct.Point.X, hookStruct.Point.Y, 0, 0);

            var kind = ((int)wParam) switch
            {
                WmLButtonDown => MouseHookEventKind.LButtonDown,
                WmLButtonUp => MouseHookEventKind.LButtonUp,
                WmRButtonUp => MouseHookEventKind.RButtonUp,
                _ => MouseHookEventKind.MouseMove
            };

            PublishToSubscribers(new MouseHookEvent(kind, args));

            var handler = kind switch
            {
                MouseHookEventKind.LButtonDown => LButtonDown,
                MouseHookEventKind.LButtonUp => LButtonUp,
                MouseHookEventKind.RButtonUp => RButtonUp,
                MouseHookEventKind.MouseMove => MouseMove,
                _ => null
            };

            handler?.Invoke(null, args);
        }

        return NativeCallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static async IAsyncEnumerable<MouseHookEvent> ReadEventsInternalAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriberChannel = Channel.CreateBounded<MouseHookEvent>(
            new BoundedChannelOptions(SubscriberBufferSize)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite
            });

        var subscriberId = AddSubscriber(subscriberChannel);
        try
        {
            await foreach (var ev in subscriberChannel.Reader.ReadAllAsync().ConfigureAwaitFalse(cancellationToken))
            {
                yield return ev;
            }
        }
        finally
        {
            RemoveSubscriber(subscriberId, subscriberChannel);
        }
    }

    private static int AddSubscriber(Channel<MouseHookEvent> subscriberChannel)
    {
        lock (Gate)
        {
            var subscriberId = ++_nextSubscriberId;
            Subscribers[subscriberId] = subscriberChannel;
            return subscriberId;
        }
    }

    private static void RemoveSubscriber(int subscriberId, Channel<MouseHookEvent> subscriberChannel)
    {
        lock (Gate)
        {
            Subscribers.Remove(subscriberId);
        }

        subscriberChannel.Writer.TryComplete();
    }

    private static void PublishToSubscribers(MouseHookEvent ev)
    {
        Channel<MouseHookEvent>[] channels;
        lock (Gate)
        {
            if (Subscribers.Count == 0)
            {
                return;
            }

            channels = Subscribers.Values.ToArray();
        }

        foreach (var channel in channels)
        {
            if (!channel.Writer.TryWrite(ev))
            {
                var dropped = Interlocked.Increment(ref _droppedEventCount);
                if (dropped is 1 || dropped % 100 == 0)
                {
                    Trace.TraceWarning($"Win32MouseHookHelper でイベントを破棄しました。dropped={dropped}, subscribers={SubscriberCount}");
                }
            }
        }
    }
}
