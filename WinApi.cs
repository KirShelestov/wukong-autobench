using System.Runtime.InteropServices;

namespace WukongAutoBench;

static class WinApi
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const ushort VK_MENU = 0x12;
    private const int SW_RESTORE = 9;

    public const ushort ScanEnter = 0x1C;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    public static void EnableDpiAwareness()
    {
        try
        {
            SetProcessDpiAwarenessContext(new IntPtr(-4));
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    public static bool IsForeground(IntPtr hwnd) => GetForegroundWindow() == hwnd;

    public static void Activate(IntPtr hwnd)
    {
        if (IsIconic(hwnd))
            ShowWindow(hwnd, SW_RESTORE);

        SendKeyVk(VK_MENU, false);
        SendKeyVk(VK_MENU, true);
        SetForegroundWindow(hwnd);
    }

    public static void ClickRelative(IntPtr hwnd, double rx, double ry)
    {
        if (!GetClientRect(hwnd, out var rect))
            return;

        var p = new POINT
        {
            X = (int)((rect.Right - rect.Left) * rx),
            Y = (int)((rect.Bottom - rect.Top) * ry),
        };
        ClientToScreen(hwnd, ref p);
        SetCursorPos(p.X, p.Y);
        Thread.Sleep(80);

        SendMouse(MOUSEEVENTF_LEFTDOWN);
        Thread.Sleep(60);
        SendMouse(MOUSEEVENTF_LEFTUP);
    }

    private static void SendMouse(uint flags)
    {
        var input = new INPUT { type = INPUT_MOUSE };
        input.u.mi.dwFlags = flags;
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public static void PressKey(ushort scanCode)
    {
        SendScan(scanCode, false);
        Thread.Sleep(60);
        SendScan(scanCode, true);
    }

    private static void SendScan(ushort scan, bool up)
    {
        var input = new INPUT { type = INPUT_KEYBOARD };
        input.u.ki.wScan = scan;
        input.u.ki.dwFlags = KEYEVENTF_SCANCODE | (up ? KEYEVENTF_KEYUP : 0);
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyVk(ushort vk, bool up)
    {
        var input = new INPUT { type = INPUT_KEYBOARD };
        input.u.ki.wVk = vk;
        input.u.ki.dwFlags = up ? KEYEVENTF_KEYUP : 0;
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }
}
