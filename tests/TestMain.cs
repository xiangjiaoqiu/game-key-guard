using System;
using GameKeyGuard;

internal static class TestMain
{
    private static int _fail;

    private static void Check(string name, bool cond)
    {
        Console.WriteLine((cond ? "PASS  " : "FAIL  ") + name);
        if (!cond) _fail++;
    }


    [STAThread]
    private static int Main()
    {
        KeyboardGuard g = new KeyboardGuard();

        Check("Win down blocked", g.ShouldBlock(KeyboardGuard.VK_LWIN, true));
        Check("Win up blocked", g.ShouldBlock(KeyboardGuard.VK_LWIN, false));
        Check("plain A down allowed", !g.ShouldBlock(0x41, true));
        Check("plain A up allowed", !g.ShouldBlock(0x41, false));

        Check("Alt down allowed", !g.ShouldBlock(KeyboardGuard.VK_MENU, true));
        Check("Alt+Tab blocked", g.ShouldBlock(KeyboardGuard.VK_TAB, true));
        Check("Alt+Tab up blocked", g.ShouldBlock(KeyboardGuard.VK_TAB, false));
        Check("Alt+Esc blocked", g.ShouldBlock(KeyboardGuard.VK_ESCAPE, true));
        g.ShouldBlock(KeyboardGuard.VK_ESCAPE, false);
        Check("Alt+F4 blocked", g.ShouldBlock(KeyboardGuard.VK_F4, true));
        g.ShouldBlock(KeyboardGuard.VK_F4, false);
        Check("Alt+B allowed", !g.ShouldBlock(0x42, true));
        g.ShouldBlock(0x42, false);
        Check("Alt up allowed", !g.ShouldBlock(KeyboardGuard.VK_MENU, false));

        Check("Ctrl down allowed", !g.ShouldBlock(KeyboardGuard.VK_CONTROL, true));
        Check("Ctrl+Esc blocked", g.ShouldBlock(KeyboardGuard.VK_ESCAPE, true));
        g.ShouldBlock(KeyboardGuard.VK_ESCAPE, false);
        Check("Shift down allowed", !g.ShouldBlock(KeyboardGuard.VK_SHIFT, true));
        Check("Ctrl+Shift+Esc blocked", g.ShouldBlock(KeyboardGuard.VK_ESCAPE, true));
        g.ShouldBlock(KeyboardGuard.VK_ESCAPE, false);
        g.ShouldBlock(KeyboardGuard.VK_SHIFT, false);
        g.ShouldBlock(KeyboardGuard.VK_CONTROL, false);
        Check("plain Esc allowed", !g.ShouldBlock(KeyboardGuard.VK_ESCAPE, true));
        g.ShouldBlock(KeyboardGuard.VK_ESCAPE, false);

        g.Options.BlockAltF4 = false;
        g.ShouldBlock(KeyboardGuard.VK_MENU, true);
        Check("Alt+F4 allowed when disabled", !g.ShouldBlock(KeyboardGuard.VK_F4, true));
        g.ShouldBlock(KeyboardGuard.VK_F4, false);
        g.ShouldBlock(KeyboardGuard.VK_MENU, false);
        g.Options.BlockAltF4 = true;

        g.Options.BlockWin = false;
        Check("Win allowed when disabled", !g.ShouldBlock(KeyboardGuard.VK_LWIN, true));
        g.ShouldBlock(KeyboardGuard.VK_LWIN, false);
        g.Options.BlockWin = true;

        // ---- custom combos ----
        CustomCombo c = new CustomCombo();
        c.Shift = true; c.Alt = true; c.Vk = 0x41;
        g.Custom.Add(c);
        Check("custom text renders", c.Text == "Alt + Shift + A");
        Check("Shift down allowed", !g.ShouldBlock(KeyboardGuard.VK_SHIFT, true));
        Check("Shift+Alt down allowed", !g.ShouldBlock(KeyboardGuard.VK_MENU, true));
        Check("Shift+Alt+A blocked", g.ShouldBlock(0x41, true));
        Check("Shift+Alt+A up blocked", g.ShouldBlock(0x41, false));
        Check("Shift+Alt+B allowed", !g.ShouldBlock(0x42, true));
        g.ShouldBlock(0x42, false);
        g.ShouldBlock(KeyboardGuard.VK_MENU, false);
        g.ShouldBlock(KeyboardGuard.VK_SHIFT, false);

        g.ShouldBlock(KeyboardGuard.VK_CONTROL, true);
        g.ShouldBlock(KeyboardGuard.VK_SHIFT, true);
        Check("Ctrl+Shift+A allowed (no Alt)", !g.ShouldBlock(0x41, true));
        g.ShouldBlock(0x41, false);
        g.ShouldBlock(KeyboardGuard.VK_SHIFT, false);
        g.ShouldBlock(KeyboardGuard.VK_CONTROL, false);

        c.Enabled = false;
        g.ShouldBlock(KeyboardGuard.VK_SHIFT, true);
        g.ShouldBlock(KeyboardGuard.VK_MENU, true);
        Check("disabled combo allowed", !g.ShouldBlock(0x41, true));
        g.ShouldBlock(0x41, false);
        g.ShouldBlock(KeyboardGuard.VK_MENU, false);
        g.ShouldBlock(KeyboardGuard.VK_SHIFT, false);
        c.Enabled = true;

        CustomCombo parsed = CustomCombo.Parse(c.Serialize());
        Check("combo serialize roundtrip", parsed != null && parsed.SameAs(c) && parsed.Enabled);
        c.Enabled = false;
        parsed = CustomCombo.Parse(c.Serialize());
        Check("disabled flag roundtrip", parsed != null && !parsed.Enabled);
        c.Enabled = true;
        Check("bad string rejected", CustomCombo.Parse("garbage") == null);
        Check("key names", CustomCombo.KeyName(0x41) == "A" && CustomCombo.KeyName(0x31) == "1" && CustomCombo.KeyName(0x73) == "F4" && CustomCombo.KeyName(0x20) == "空格");

        // ---- wiring: MainForm must share the settings custom list with the guard ----
        AppSettings st = new AppSettings();
        CustomCombo wc = new CustomCombo();
        wc.Shift = true; wc.Alt = true; wc.Vk = 0x41;
        st.Custom.Add(wc);
        bool shared = false, blocks = false;
        using (MainForm mf = new MainForm(st, false, true))
        {
            shared = object.ReferenceEquals(mf.Guard.Custom, st.Custom) && mf.Guard.Custom.Count == 1;
            mf.Guard.ShouldBlock(KeyboardGuard.VK_SHIFT, true);
            mf.Guard.ShouldBlock(KeyboardGuard.VK_MENU, true);
            blocks = mf.Guard.ShouldBlock(0x41, true);
            mf.Guard.ShouldBlock(0x41, false);
            mf.Guard.ShouldBlock(KeyboardGuard.VK_MENU, false);
            mf.Guard.ShouldBlock(KeyboardGuard.VK_SHIFT, false);
        }
        Check("MainForm shares custom list with guard", shared);
        Check("guard blocks combo added via settings", blocks);

        // ---- 钩子捕获模式 ----
        KeyboardGuard cg = new KeyboardGuard();
        bool captureOk = cg.BeginCapture();
        Check("capture hook starts", captureOk && cg.Active);
        cg.FeedCapture(KeyboardGuard.VK_SHIFT, true);
        cg.FeedCapture(KeyboardGuard.VK_MENU, true);
        cg.FeedCapture(0x41, true);
        CustomCombo got = cg.TakeCaptured();
        Check("capture grabs Shift+Alt+A", got != null && got.Shift && got.Alt && !got.Ctrl && !got.Win && got.Vk == 0x41);
        Check("captured combo takes only once", cg.TakeCaptured() == null);
        cg.FeedCapture(0x41, false);
        cg.FeedCapture(KeyboardGuard.VK_MENU, false);
        cg.FeedCapture(KeyboardGuard.VK_SHIFT, false);
        cg.FeedCapture(KeyboardGuard.VK_CONTROL, true);
        cg.FeedCapture(0x41, true);
        Check("plain modifier combo still captured", cg.TakeCaptured() != null);
        cg.FeedCapture(0x41, false);
        cg.FeedCapture(KeyboardGuard.VK_CONTROL, false);
        cg.EndCapture();
        Check("capture hook released", !cg.Active);
        // ---- 回桌面热键（Ctrl+Alt+Q）----
        KeyboardGuard pg = new KeyboardGuard();
        pg.Panic = new CustomCombo();
        pg.Panic.Win = true;
        pg.Panic.Vk = 0x0D;
        pg.CheckPanic(0x5B, true);
        Check("Win+Enter panic fires", pg.CheckPanic(0x0D, true) && pg.TakePanic());
        Check("panic taken once", !pg.TakePanic());
        pg.CheckPanic(0x0D, false);
        pg.CheckPanic(0x5B, false);
        pg.CheckPanic(0xA2, true);
        Check("Ctrl+Enter is not panic", !pg.CheckPanic(0x0D, true));
        pg.CheckPanic(0x0D, false);
        pg.CheckPanic(0xA2, false);
        Check("plain Enter is not panic", !pg.CheckPanic(0x0D, true));
        pg.CheckPanic(0x0D, false);
        // ---- engine ----
        string rep;
        Check("hook installs", g.Start(out rep) && g.Active);
        g.Stop();
        Check("hook removed", !g.Active);

        bool threw = false;
        try
        {
            Random rnd = new Random(7);
            for (int i = 0; i < 3000; i++) g.ShouldBlock(rnd.Next(0x08, 0xFE), rnd.Next(2) == 0);
        }
        catch { threw = true; }
        Check("decision loop stable", !threw);

        Console.WriteLine(_fail == 0 ? "ALL TESTS PASSED" : (_fail + " TEST(S) FAILED"));
        return _fail == 0 ? 0 : 1;
    }
}