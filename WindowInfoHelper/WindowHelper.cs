using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowHelper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static class Info
    {
        #region Win32API
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        #endregion

        #region GetWindowHandle
        public static IntPtr GetWindowHandle(int x, int y)
        {
            var pointStruct = new POINT(x, y);

            IntPtr hWnd = WindowFromPoint(pointStruct);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return hWnd;
        }

        public static IntPtr GetWindowHandle(string windowTitle)
        {
            var hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return hWnd;
        }

        public static IntPtr GetWindowHandle()
        {
            POINT point;

            if (!GetCursorPos(out point))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = WindowFromPoint(point);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return hWnd;
        }
        #endregion

        #region GetWindowTitle
        public static string GetWindowTitle(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);

            return sb.ToString();
        }

        public static string GetWindowTitle(System.Drawing.Point point)
        {
            var hWnd = GetWindowHandle(point.X, point.Y);
            var title = GetWindowTitle(hWnd);

            return title;
        }

        public static string GetWindowTitle(int x, int y)
        {
            var hWnd = GetWindowHandle(x, y);
            var title = GetWindowTitle(hWnd);

            return title;
        }

        public static string GetWindowTitle(string windowTitle)
        {
            var hWnd = GetWindowHandle(windowTitle);
            var title = GetWindowTitle(hWnd);

            return title;
        }

        public static string GetWindowTitle()
        {
            POINT point;

            if (!GetCursorPos(out point))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = WindowFromPoint(point);
            var title = GetWindowTitle(hWnd);

            return title;
        }
        #endregion

        #region GetWindowClassName
        public static string GetWindowClassName(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);

            return sb.ToString();
        }

        public static string GetWindowClassName(System.Drawing.Point point)
        {
            var hWnd = GetWindowHandle(point.X, point.Y);
            var className = GetWindowClassName(hWnd);

            return className;
        }

        public static string GetWindowClassName()
        {
            POINT point;

            if (!GetCursorPos(out point))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = WindowFromPoint(point);
            var className = GetWindowClassName(hWnd);

            return className;
        }
        #endregion

        #region IsWindowForeground
        public static bool IsWindowForeground(IntPtr hWnd)
        {
            var foregroundHWnd = GetForegroundWindow();

            return hWnd == foregroundHWnd;
        }

        public static bool IsWindowForeground(string windowTitle)
        {
            var hWnd = GetWindowHandle(windowTitle);
            var isForeground = IsWindowForeground(hWnd);

            return isForeground;
        }

        public static bool IsWindowForeground(System.Drawing.Point point)
        {
            var hWnd = GetWindowHandle(point.X, point.Y);
            var isForeground = IsWindowForeground(hWnd);

            return isForeground;
        }

        public static bool IsWindowForeground()
        {
            POINT point;

            if (!GetCursorPos(out point))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = WindowFromPoint(point);
            var isForeground = IsWindowForeground(hWnd);

            return isForeground;
        }
        #endregion
    }

    public static class Operation
    {
        #region Win32API
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);     // 最前面
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);  // 最前面を解除

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        public static extern bool SetFocus(IntPtr hWnd, int nCmdShow);
        #endregion

        #region SetForegroundWindow
        public static void SetForegroundWindow(System.Drawing.Point point)
        {
            var hWnd = Info.GetWindowHandle(point.X, point.Y);
            SetForegroundWindow(hWnd);
        }

        public static void SetForegroundWindow(string windowTitle)
        {
            var hWnd = Info.GetWindowHandle(windowTitle);
            SetForegroundWindow(hWnd);
        }

        public static void SetForegroundWindow()
        {
            POINT point;

            if (!Info.GetCursorPos(out point))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = Info.GetWindowHandle(point.X, point.Y);
            SetForegroundWindow(hWnd);
        }
        #endregion

        #region SetTopMost
        public static void SetTopMost(string windowTitle)
        {
            var hWnd = Info.GetWindowHandle(windowTitle);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        public static void ReleaseTopMost(string windowTitle)
        {
            var hWnd = Info.GetWindowHandle(windowTitle);
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
        #endregion
    }

    public static class Coordinate
    {
        #region Win32API
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        #endregion

        #region ScreenToClient / ClientToScreen
        public static (int,int) ClientToScreen(IntPtr hWnd, int clientX, int clientY)
        {
            if(!GetWindowRect(hWnd, out RECT rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return new(clientX + rect.Left, clientY + rect.Top);
        }

        public static (int, int) ClientToScreen(string windowTitle, int clientX, int clientY)
        {
            var hWnd = Info.GetWindowHandle(windowTitle);
            return ClientToScreen(hWnd, clientX, clientY);
        }

        public static (int, int) ScreenToClient(IntPtr hWnd, int screenX, int screenY)
        {
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return new(screenY - rect.Left, screenX - rect.Top);
        }

        public static (int, int) ScreenToClient(string windowTitle, int screenX, int screenY)
        {
            var hWnd = Info.GetWindowHandle(windowTitle);
            return ScreenToClient(hWnd, screenX, screenY);
        }
        #endregion
    }
}
