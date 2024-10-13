using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace ColorPickHelper
{
    public partial class ColorPickWindow : Window
    {
        #region Win32API
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // フックタイプ（低レベルのマウスフック）
        private const int WH_MOUSE_LL = 14;

        // フックハンドル
        private static IntPtr hookID = IntPtr.Zero;

        // デリゲートを保持するための変数
        private static LowLevelMouseProc proc = HookCallback;

        // マウスイベントのコールバックデリゲート
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONDOWN = 0x0207;
        const int WM_MBUTTONUP = 0x0208;
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_MOUSEHWHEEL = 0x020E;
        const int WM_MOUSEMOVE = 0x0200;

        #endregion

        #region Event
        public class MouseEventArgs : EventArgs
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Delta { get; set; }
            public int HWheel { get; set; }

            public MouseEventArgs(int x, int y, int delta, int hWheel)
            {
                X = x;
                Y = y;
                Delta = delta;
                HWheel = hWheel;
            }
        }

        public static EventHandler<MouseEventArgs>? LButtonDown { get; set; }
        public static EventHandler<MouseEventArgs>? LButtonUp { get; set; }
        public static EventHandler<MouseEventArgs>? RButtonDown { get; set; }
        public static EventHandler<MouseEventArgs>? RButtonUp { get; set; }
        public static EventHandler<MouseEventArgs>? MButtonDown { get; set; }
        public static EventHandler<MouseEventArgs>? MButtonUp { get; set; }
        public static EventHandler<MouseEventArgs>? MouseWheel { get; set; }
        public static EventHandler<MouseEventArgs>? MouseHWheel { get; set; }
        public static EventHandler<MouseEventArgs>? MouseMove { get; set; }

        #endregion

        #region Hook
        private static bool SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                if (curProcess?.MainModule == null)
                {
                    return false;
                }

                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hookID = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);

                    return hookID != IntPtr.Zero;
                }
            }
        }

        private static bool Unhook()
        {
            return UnhookWindowsHookEx(hookID);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // nCodeが0以上の場合は、マウスイベントを処理
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        LButtonDown?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_LBUTTONUP:
                        LButtonUp?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_RBUTTONDOWN:
                        RButtonDown?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_RBUTTONUP:
                        RButtonUp?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_MBUTTONDOWN:
                        MButtonDown?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_MBUTTONUP:
                        MButtonUp?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                    case WM_MOUSEWHEEL:
                        MouseWheel?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, (int)hookStruct.mouseData, 0));
                        break;
                    case WM_MOUSEHWHEEL:
                        MouseHWheel?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, (int)hookStruct.mouseData));
                        break;
                    case WM_MOUSEMOVE:
                        MouseMove?.Invoke(null, new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y, 0, 0));
                        break;
                }
            }

            // 次のフックに処理を渡す
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        #endregion

        public Color? Color { get; private set; } = null;

        public ColorPickWindow()
        {
            InitializeComponent();

            StartHook();
        }
        public void StartHook()
        {
            LButtonUp += OnLButtonUp;
            RButtonUp += OnRButtonUp;
            MouseMove += OnMouseMove;

            SetHook(proc);
        }

        public void StopHook()
        {
            LButtonUp -= OnLButtonUp;
            RButtonUp -= OnRButtonUp;
            MouseMove -= OnMouseMove;

            Unhook();
        }

        private void OnMouseMove(object? sender, EventArgs e)
        {
            POINT cursorPos;
            if (GetCursorPos(out cursorPos))
            {
                // ウィンドウをマウスカーソルの位置へ移動
                Left = cursorPos.x + 10;
                Top = cursorPos.y + 10;

                // カーソル位置のスクリーンからの色を取得
                Color = GetColorAt(cursorPos);
                ColorPreview.Fill = new SolidColorBrush(Color ?? Colors.Transparent);
            }
        }

        private void OnLButtonUp(object? sender, EventArgs e)
        {
            StopHook();
            Close();
        }

        private void OnRButtonUp(object? sender, EventArgs e)
        {
            StopHook();
            Color = null;
            Close();
        }

        private Color GetColorAt(POINT cursorPos)
        {
            using (var bitmap = new System.Drawing.Bitmap(1, 1))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    // スクリーンからの色を取得
                    g.CopyFromScreen((int)cursorPos.x, (int)cursorPos.y, 0, 0, new System.Drawing.Size(1, 1));
                }

                // 取得したピクセルの色を取得
                System.Drawing.Color drawingColor = bitmap.GetPixel(0, 0);

                // System.Drawing.ColorからSystem.Windows.Media.Colorに変換
                return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            }
        }
    }
}
