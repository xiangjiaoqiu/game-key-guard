using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using GameKeyGuard;

internal static class HookProbe
{
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public MOUSEKEYBDINPUT ki; public int e1; public int e2; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEKEYBDINPUT
    {
        public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void Key(ushort vk, bool up)
    {
        INPUT[] a = new INPUT[1];
        a[0].type = 1;
        a[0].ki.wVk = vk;
        a[0].ki.dwFlags = up ? KEYEVENTF_KEYUP : 0;
        SendInput(1, a, Marshal.SizeOf(typeof(INPUT)));
    }

    private static void Pump(int ms)
    {
        int n = ms / 25;
        for (int i = 0; i < n; i++) { Application.DoEvents(); Thread.Sleep(25); }
    }

    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int fail = 0;
        KeyboardGuard g = new KeyboardGuard();
        g.Options.BlockWin = true;
        g.Options.BlockAltTab = true;
        g.IgnoreInjected = false;
        string rep;
        bool ok = g.Start(out rep);
        Console.WriteLine("install = " + ok + " / " + rep);
        if (!ok) { Console.WriteLine("FAIL install"); return 1; }
        Pump(300);

        g.ResetCounters();
        Key(0x5B, false); Pump(120); Key(0x5B, true); Pump(120);
        Console.WriteLine("Win: seen=" + g.SeenCount + " blocked=" + g.BlockedCount);
        if (g.SeenCount == 0) { Console.WriteLine("FAIL hook never received events"); fail++; }
        if (g.BlockedCount < 2) { Console.WriteLine("FAIL Win not blocked"); fail++; }

        g.ResetCounters();
        Key(0x12, false); Pump(120); Key(0x09, false); Pump(120); Key(0x09, true); Pump(120); Key(0x12, true); Pump(120);
        Console.WriteLine("Alt+Tab: seen=" + g.SeenCount + " blocked=" + g.BlockedCount);
        if (g.BlockedCount < 4) { Console.WriteLine("FAIL Alt+Tab not fully blocked"); fail++; }

        g.ResetCounters();
        Key(0x41, false); Pump(120); Key(0x41, true); Pump(120);
        Console.WriteLine("plain A: seen=" + g.SeenCount + " blocked=" + g.BlockedCount);
        if (g.BlockedCount != 0) { Console.WriteLine("FAIL plain key was blocked"); fail++; }

        g.Stop();
        Console.WriteLine(fail == 0 ? "HOOK E2E PASSED" : "HOOK E2E FAILED");
        return fail;
    }
}