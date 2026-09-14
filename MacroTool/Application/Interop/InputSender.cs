using System.Runtime.InteropServices;

namespace MacroTool.Application.Interop;

internal static class InputSender
{
    public static bool SendKey(int scan, bool up, bool ext)
    {
        var input = new Native.INPUT { type = Native.INPUT_KEYBOARD };
        input.u.ki = new Native.KEYBDINPUT
        {
            wVk = 0,
            wScan = (ushort)scan,
            dwFlags = Native.KEYEVENTF_SCANCODE
                      | (ext ? Native.KEYEVENTF_EXTENDEDKEY : 0)
                      | (up ? Native.KEYEVENTF_KEYUP : 0)
        };
        return Native.SendInput(1, [input], Marshal.SizeOf<Native.INPUT>()) == 1;
    }

    public static bool SendMouseDown(int x, int y)
    {
        Normalize(x, y, out int nx, out int ny);
        var move = new Native.INPUT { type = Native.INPUT_MOUSE };
        move.u.mi = new Native.MOUSEINPUT
        {
            dx = nx,
            dy = ny,
            dwFlags = Native.MOUSEEVENTF_MOVE | Native.MOUSEEVENTF_ABSOLUTE | Native.MOUSEEVENTF_VIRTUALDESK
        };
        if (Native.SendInput(1, [move], Marshal.SizeOf<Native.INPUT>()) != 1)
            return false;

        var down = new Native.INPUT { type = Native.INPUT_MOUSE };
        down.u.mi = new Native.MOUSEINPUT { dwFlags = Native.MOUSEEVENTF_LEFTDOWN };
        return Native.SendInput(1, [down], Marshal.SizeOf<Native.INPUT>()) == 1;
    }

    public static bool SendMouseUp()
    {
        var up = new Native.INPUT { type = Native.INPUT_MOUSE };
        up.u.mi = new Native.MOUSEINPUT { dwFlags = Native.MOUSEEVENTF_LEFTUP };
        return Native.SendInput(1, [up], Marshal.SizeOf<Native.INPUT>()) == 1;
    }

    private static void Normalize(int x, int y, out int nx, out int ny)
    {
        int vx = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
        int vy = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
        int vw = Math.Max(1, Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN) - 1);
        int vh = Math.Max(1, Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN) - 1);
        nx = (int)Math.Clamp((long)(x - vx) * 65535 / vw, 0, 65535);
        ny = (int)Math.Clamp((long)(y - vy) * 65535 / vh, 0, 65535);
    }
}
