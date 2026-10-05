using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GameKeyGuard
{
    public class CustomCombo
    {
        public bool Ctrl, Alt, Shift, Win;
        public int Vk;
        public bool Enabled = true;

        public string Mods
        {
            get { return (Ctrl ? "C" : "-") + (Alt ? "A" : "-") + (Shift ? "S" : "-") + (Win ? "W" : "-"); }
        }

        public string Text
        {
            get
            {
                string s = "";
                if (Ctrl) s += "Ctrl + ";
                if (Alt) s += "Alt + ";
                if (Shift) s += "Shift + ";
                if (Win) s += "Win + ";
                return s + KeyName(Vk);
            }
        }

        public static string KeyName(int vk)
        {
            if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
            if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
            if (vk >= 0x60 && vk <= 0x69) return "小键盘" + (vk - 0x60);
            switch (vk)
            {
                case 0x08: return "Backspace";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x14: return "CapsLock";
                case 0x1B: return "Esc";
                case 0x20: return "空格";
                case 0x21: return "PageUp";
                case 0x22: return "PageDown";
                case 0x23: return "End";
                case 0x24: return "Home";
                case 0x25: return "←";
                case 0x26: return "↑";
                case 0x27: return "→";
                case 0x28: return "↓";
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0x2C: return "PrintScreen";
            }
            if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);
            string[] oem1 = new string[] { ";", "=", ",", "-", ".", "/", "`" };
            if (vk >= 0xBA && vk <= 0xC0) return oem1[vk - 0xBA];
            string[] oem2 = new string[] { "[", "\\", "]", "'" };
            if (vk >= 0xDB && vk <= 0xDE) return oem2[vk - 0xDB];
            return "VK" + vk.ToString("X2");
        }

        public string Serialize()
        {
            return (Enabled ? "1" : "0") + "|" + Mods + "|" + Vk;
        }

        public static CustomCombo Parse(string s)
        {
            try
            {
                string[] p = s.Split('|');
                if (p.Length != 3) return null;
                string m = p[1];
                CustomCombo c = new CustomCombo();
                c.Enabled = p[0] == "1";
                c.Ctrl = m.Length > 0 && m[0] == 'C';
                c.Alt = m.Length > 1 && m[1] == 'A';
                c.Shift = m.Length > 2 && m[2] == 'S';
                c.Win = m.Length > 3 && m[3] == 'W';
                c.Vk = int.Parse(p[2]);
                if (c.Vk <= 0) return null;
                return c;
            }
            catch { return null; }
        }

        public bool SameAs(CustomCombo o)
        {
            return o != null && Ctrl == o.Ctrl && Alt == o.Alt && Shift == o.Shift && Win == o.Win && Vk == o.Vk;
        }
    }

    public class GuardOptions
    {
        public bool BlockWin = true;
        public bool BlockAltTab = true;
        public bool BlockAltF4 = true;
        public bool BlockCtrlEsc = true;
    }

    public class KeyboardGuard : IDisposable
    {
        public const int VK_TAB = 0x09;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_LSHIFT = 0xA0;
        public const int VK_RSHIFT = 0xA1;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_LMENU = 0xA4;
        public const int VK_RMENU = 0xA5;
        public const int VK_F4 = 0x73;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const uint LLKHF_INJECTED = 0x10;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public GuardOptions Options = new GuardOptions();
        public List<CustomCombo> Custom = new List<CustomCombo>();
        public string Report = "";
        public bool Suspended = false;
        public bool Capturing = false;
        public CustomCombo Panic = new CustomCombo();
        private bool _panic;
        private CustomCombo _captured;
        private bool _captureCancel;
        private bool _captureHook;
        public bool IgnoreInjected = true;
        public int SeenCount = 0;
        public int BlockedCount = 0;
        public bool Active { get { return _hHook != IntPtr.Zero; } }

        private IntPtr _hHook = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private List<int> _blockedDown = new List<int>();
        private List<string> _log = new List<string>();
        private bool _win, _alt, _ctrl, _shift;

        public List<string> DrainLog()
        {
            List<string> copy = new List<string>();
            lock (_log) { copy.AddRange(_log); _log.Clear(); }
            return copy;
        }

        private void Log(string line)
        {
            lock (_log) { if (_log.Count < 400) _log.Add(line); }
        }

        public static bool IsDown(int vk) { return (GetAsyncKeyState(vk) & 0x8000) != 0; }

        public bool BeginCapture()
        {
            Capturing = true;
            Suspended = true;
            _captured = null;
            _captureCancel = false;
            if (_hHook == IntPtr.Zero)
            {
                _proc = HookCallback;
                _hHook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
                if (_hHook == IntPtr.Zero)
                {
                    _proc = null;
                    Capturing = false;
                    Suspended = false;
                    return false;
                }
                _captureHook = true;
            }
            return true;
        }

        public void EndCapture()
        {
            Capturing = false;
            Suspended = false;
            _captured = null;
            _captureCancel = false;
            if (_captureHook)
            {
                _captureHook = false;
                if (_hHook != IntPtr.Zero) UnhookWindowsHookEx(_hHook);
                _hHook = IntPtr.Zero;
                _proc = null;
                ResetState();
            }
        }

        public void FeedCapture(int vk, bool down)
        {
            UpdateMods(vk, down);
            if (down && vk == VK_ESCAPE) _captureCancel = true;
            else if (down && !IsModifierVk(vk) && (_ctrl || _alt || _shift || _win))
            {
                CustomCombo cc = new CustomCombo();
                cc.Ctrl = _ctrl; cc.Alt = _alt; cc.Shift = _shift; cc.Win = _win; cc.Vk = vk;
                _captured = cc;
            }
        }

        public CustomCombo TakeCaptured()
        {
            CustomCombo c = _captured;
            _captured = null;
            return c;
        }

        public bool TakeCaptureCancel()
        {
            if (_captureCancel) { _captureCancel = false; return true; }
            return false;
        }

        public bool TakePanic()
        {
            if (_panic) { _panic = false; return true; }
            return false;
        }

        public bool CheckPanic(int vk, bool down)
        {
            UpdateMods(vk, down);
            if (!down || Panic == null || Panic.Vk == 0) return false;
            if (Panic.Ctrl != _ctrl || Panic.Alt != _alt || Panic.Shift != _shift || Panic.Win != _win) return false;
            if (Panic.Vk != vk) return false;
            _panic = true;
            return true;
        }

        private static bool IsModifierVk(int vk)
        {
            return vk == VK_SHIFT || vk == VK_LSHIFT || vk == VK_RSHIFT
                || vk == VK_CONTROL || vk == VK_LCONTROL || vk == VK_RCONTROL
                || vk == VK_MENU || vk == VK_LMENU || vk == VK_RMENU
                || vk == VK_LWIN || vk == VK_RWIN;
        }

        public bool Start(out string report)
        {
            Stop();
            ResetState();
            _proc = HookCallback;
            _hHook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
            if (_hHook != IntPtr.Zero)
            {
                Report = "键盘拦截已生效";
                report = Report;
                return true;
            }
            Report = "拦截安装失败（错误码 " + Marshal.GetLastWin32Error() + "）";
            report = Report;
            _proc = null;
            return false;
        }

        public void Stop()
        {
            Capturing = false;
            Suspended = false;
            _captureHook = false;
            _captured = null;
            _captureCancel = false;
            _panic = false;
            if (_hHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hHook);
                _hHook = IntPtr.Zero;
            }
            _proc = null;
            ResetState();
        }

        public void Dispose() { Stop(); }

        private void ResetState()
        {
            _win = false; _alt = false; _ctrl = false; _shift = false;
            _blockedDown.Clear();
        }

        public void ResetCounters() { SeenCount = 0; BlockedCount = 0; }

        public void ResyncModifiers()
        {
            if (!Active) return;
            _win = IsDown(VK_LWIN) || IsDown(VK_RWIN);
            _alt = IsDown(VK_MENU);
            _ctrl = IsDown(VK_CONTROL);
            _shift = IsDown(VK_SHIFT);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    KBDLLHOOKSTRUCT k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    int msg = wParam.ToInt32();
                    bool down = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                    bool up = (msg == WM_KEYUP || msg == WM_SYSKEYUP);
                    if (down || up) UpdateMods((int)k.vkCode, down);
                    if (Capturing)
                    {
                        FeedCapture((int)k.vkCode, down);
                        return CallNextHookEx(_hHook, nCode, wParam, lParam);
                    }
                    if (Suspended) return CallNextHookEx(_hHook, nCode, wParam, lParam);
                    if ((IgnoreInjected && (k.flags & LLKHF_INJECTED) != 0)) return CallNextHookEx(_hHook, nCode, wParam, lParam);
                    SeenCount++;
                    bool hit = (down || up) && Decide((int)k.vkCode, down);
                    if (down && CheckPanic((int)k.vkCode, true)) hit = true;
                    if (down && (_ctrl || _alt || _shift || _win))
                    {
                        Log("vk=0x" + ((int)k.vkCode).ToString("X2") + " ctrl=" + (_ctrl ? 1 : 0) + " alt=" + (_alt ? 1 : 0)
                            + " shift=" + (_shift ? 1 : 0) + " win=" + (_win ? 1 : 0) + " custom=" + Custom.Count + " -> " + (hit ? "BLOCK" : "pass"));
                    }
                    if (hit)
                    {
                        if (down)
                        {
                            if (_blockedDown.IndexOf((int)k.vkCode) < 0) _blockedDown.Add((int)k.vkCode);
                        }
                        BlockedCount++;
                        return new IntPtr(1);
                    }
                    if (!down)
                    {
                        int i = _blockedDown.IndexOf((int)k.vkCode);
                        if (i >= 0)
                        {
                            _blockedDown.RemoveAt(i);
                            BlockedCount++;
                            return new IntPtr(1);
                        }
                    }
                }
            }
            catch { }
            return CallNextHookEx(_hHook, nCode, wParam, lParam);
        }

        public bool ShouldBlock(int vk, bool down)
        {
            UpdateMods(vk, down);
            bool block = Decide(vk, down);
            if (down)
            {
                if (block && _blockedDown.IndexOf(vk) < 0) _blockedDown.Add(vk);
            }
            else
            {
                int i = _blockedDown.IndexOf(vk);
                if (i >= 0) { _blockedDown.RemoveAt(i); block = true; }
            }
            return block;
        }

        private bool Decide(int vk, bool down)
        {
            if (Options.BlockWin && IsWin(vk)) return true;
            if (!down) return false;
            if (Options.BlockWin && _win) return true;
            if (Options.BlockAltTab && _alt && (vk == VK_TAB || vk == VK_ESCAPE)) return true;
            if (Options.BlockAltF4 && _alt && vk == VK_F4) return true;
            if (Options.BlockCtrlEsc && _ctrl && vk == VK_ESCAPE) return true;
            for (int i = 0; i < Custom.Count; i++)
            {
                CustomCombo c = Custom[i];
                if (!c.Enabled || c.Vk != vk) continue;
                if (c.Ctrl != _ctrl || c.Alt != _alt || c.Shift != _shift || c.Win != _win) continue;
                return true;
            }
            return false;
        }

        private void UpdateMods(int vk, bool down)
        {
            switch (vk)
            {
                case VK_LWIN:
                case VK_RWIN: _win = down; break;
                case VK_MENU:
                case VK_LMENU:
                case VK_RMENU: _alt = down; break;
                case VK_CONTROL:
                case VK_LCONTROL:
                case VK_RCONTROL: _ctrl = down; break;
                case VK_SHIFT:
                case VK_LSHIFT:
                case VK_RSHIFT: _shift = down; break;
            }
        }

        private static bool IsWin(int vk) { return vk == VK_LWIN || vk == VK_RWIN; }
    }
}