using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

internal class MouseControlHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll", SetLastError = true)]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    const uint MOUSEEVENTF_WHEEL = 0x0800;
    const uint MOUSEEVENTF_HWHEEL = 0x01000;

    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(100);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public static void RightClick(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        Thread.Sleep(100);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
    }

    public static void MiddleClick(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
        Thread.Sleep(100);
        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
    }

    public static void Wheel(int x, int y, int delta)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0);
    }

    public static void HWheel(int x, int y, int delta)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)delta, 0);
    }

    public static void Move(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public static void Drag(int x1, int y1, int x2, int y2)
    {
        SetCursorPos(x1, y1);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(100);
        SetCursorPos(x2, y2);
        Thread.Sleep(100);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public static void RightDrag(int x1, int y1, int x2, int y2)
    {
        SetCursorPos(x1, y1);
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        Thread.Sleep(100);
        SetCursorPos(x2, y2);
        Thread.Sleep(100);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
    }

}