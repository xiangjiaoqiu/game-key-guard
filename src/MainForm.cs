using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameKeyGuard
{
    public class MainForm : Form
    {
        public const int WM_SHOWME = 0x8001;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private AppSettings _s;
        private KeyboardGuard _guard = new KeyboardGuard();
        private StatusCard _card;
        private FlatButton _btnMain, _btnCustom;
        private Label _note, _foot, _lblCustom;
        private ToggleSwitch _swWin, _swTab, _swF4, _swEsc, _swAuto, _swBall;
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _miShow, _miToggle, _miAdmin, _miExit;
        private Timer _tmr;
        private FloatBall _ball;
        private Icon _icoOff, _icoOn;
        private ToolTip _tip;
        private bool _locked;
        private bool _exit;
        private bool _startHidden;
        private string _lastExe;

        private int P(int v) { return (int)Math.Round(v * Theme.K); }

        public MainForm(AppSettings s, bool startHidden, bool headless)
        {
            _s = s;
            _guard.Custom = _s.Custom;
            _guard.Panic = new CustomCombo();
            _guard.Panic.Win = true;
            _guard.Panic.Vk = 0x0D;
            _startHidden = startHidden;
            BuildUi();
            if (!headless) BuildTray();
            ApplySettings();
            if (!headless)
            {
                AppSettings.Note("app start; custom=" + DumpCustom());
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_startHidden)
            {
                _startHidden = false;
                base.SetVisibleCore(false);
                return;
            }
            base.SetVisibleCore(value);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRegion();
        }

        private void ApplyRegion()
        {
            if (Width > 0 && Height > 0)
            {
                using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), P(18))) Region = new Region(p);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Theme.Bg)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) return;
            if (m.Msg == WM_SHOWME) { ShowUp(); return; }
            base.WndProc(ref m);
        }

        private void BuildUi()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.FBody;
            Text = "游戏快捷键锁";
            MaximizeBox = false;
            MinimizeBox = false;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;

            int padX = P(18);
            int W = P(400);
            int inner = W - padX * 2;
            ClientSize = new Size(W, P(694));

            _tmr = new Timer();
            _tmr.Interval = 700;
            _tmr.Tick += delegate
            {
                _guard.ResyncModifiers();
                if (_locked) FlushKeyLog();
                if (_locked && _guard.TakePanic()) BackToDesktop();
            };

            Panel bar = new Panel();
            bar.Bounds = new Rectangle(0, 0, W, P(48));
            bar.BackColor = Theme.Bg;
            bar.MouseDown += DragStart;
            Controls.Add(bar);

            Label title = new Label();
            title.Text = "游戏快捷键锁";
            title.Font = Theme.FTitle;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.Bounds = new Rectangle(padX, P(11), P(240), P(26));
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.MouseDown += DragStart;
            bar.Controls.Add(title);

            _tip = new ToolTip();

            FlatButton bMin = new FlatButton();
            bMin.Text = "—";
            bMin.Font = Theme.FBodyB;
            bMin.Bounds = new Rectangle(W - padX - P(66), P(11), P(30), P(26));
            bMin.Fill = Theme.Bg;
            bMin.FillHover = Theme.Card;
            bMin.FillDown = Theme.CardHi;
            bMin.TextColor = Theme.Sub;
            bMin.Radius = P(8);
            bMin.Click += delegate { WindowState = FormWindowState.Minimized; };
            bar.Controls.Add(bMin);
            _tip.SetToolTip(bMin, "最小化");

            FlatButton bClose = new FlatButton();
            bClose.Text = "×";
            bClose.Font = Theme.FBodyB;
            bClose.Bounds = new Rectangle(W - padX - P(32), P(11), P(30), P(26));
            bClose.Fill = Theme.Bg;
            bClose.FillHover = Theme.Card;
            bClose.FillDown = Theme.CardHi;
            bClose.TextColor = Theme.Sub;
            bClose.Radius = P(8);
            bClose.Click += delegate { Hide(); };
            bar.Controls.Add(bClose);
            _tip.SetToolTip(bClose, "隐藏到托盘");

            _card = new StatusCard();
            _card.Bounds = new Rectangle(padX, P(56), inner, P(88));
            Controls.Add(_card);

            _btnMain = new FlatButton();
            _btnMain.Bounds = new Rectangle(padX, P(152), inner, P(52));
            _btnMain.Radius = P(14);
            _btnMain.Font = Theme.FBtn;
            _btnMain.Click += delegate { if (_locked) StopLock(); else StartLock(); };
            Controls.Add(_btnMain);

            _note = new Label();
            _note.Bounds = new Rectangle(padX, P(216), inner, P(24));
            _note.Font = Theme.FSmall;
            _note.ForeColor = Theme.Muted;
            _note.BackColor = Theme.Bg;
            _note.AutoSize = false;
            _note.Text = "只拦勾选项，关闭窗口后程序驻留托盘。";
            Controls.Add(_note);
            _tip.SetToolTip(_note, "本程序用底层键盘钩子拦截，全局生效但只针对被勾选的组合键。锁定期间按 Win + Enter 可随时回桌面。");

            Section("拦截内容", P(248), padX + P(2));
            _swWin = Row("Win 键及其全部组合", P(272), _s.BlockWin);
            _swTab = Row("Alt + Tab / Alt + Esc", P(310), _s.BlockAltTab);
            _swF4 = Row("Alt + F4（防误关游戏）", P(348), _s.BlockAltF4);
            _swEsc = Row("Ctrl + Esc / Ctrl + Shift + Esc", P(386), _s.BlockCtrlEsc);

            Divider(P(432), padX, inner);

            Section("自定义拦截", P(444), padX + P(2));
            _lblCustom = new Label();
            _lblCustom.Font = Theme.FBody;
            _lblCustom.ForeColor = Theme.Text;
            _lblCustom.BackColor = Theme.Bg;
            _lblCustom.Bounds = new Rectangle(P(20), P(474), P(250), P(24));
            _lblCustom.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_lblCustom);

            _btnCustom = new FlatButton();
            _btnCustom.Text = "管理";
            _btnCustom.Font = Theme.FBodyB;
            _btnCustom.Bounds = new Rectangle(W - padX - P(88), P(471), P(88), P(32));
            _btnCustom.Radius = P(9);
            StyleSecondary(_btnCustom);
            _btnCustom.Click += delegate { OpenCustom(); };
            Controls.Add(_btnCustom);

            Divider(P(514), padX, inner);

            Section("其他", P(526), padX + P(2));
            _swAuto = Row("开机自动启动", P(550), _s.AutoStart);
            _swBall = Row("悬浮球（一键回桌面）", P(588), _s.FloatBall);

            _foot = new Label();
            _foot.Bounds = new Rectangle(padX, P(636), inner, P(44));
            _foot.Font = Theme.FSmall;
            _foot.ForeColor = Theme.Muted;
            _foot.BackColor = Theme.Bg;
            _foot.AutoSize = false;
            _foot.Text = "锁定中按 Win + Enter 回桌面\r\n游戏模式保持开启，拦截不解除";
            _tip.SetToolTip(_foot, "Win + Enter 只是把桌面调出来，游戏模式仍然开着、快捷键仍然被拦。想真正解除锁定，请点上面的红色按钮，或用托盘菜单的「结束游戏模式」。");
            Controls.Add(_foot);

            if (_s.X == -99999)
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2 - P(16));
                if (Location.Y < wa.Top) Location = new Point(Location.X, wa.Top);
            }
            else
            {
                Location = new Point(_s.X, _s.Y);
            }
        }

        private void BackToDesktop()
        {
            // 只把桌面调出来，游戏模式（钩子拦截）保持开启。
            // 注入的按键带 LLKHF_INJECTED 标记，本程序自己的钩子会放行，所以无需解除锁定。
            AppSettings.Note("panic hotkey: show desktop (still locked)");
            SendWinD();
        }

        private static void SendWinD()
        {
            const uint UP = 0x0002;
            try
            {
                keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                keybd_event(0x44, 0, 0, UIntPtr.Zero);
                keybd_event(0x44, 0, UP, UIntPtr.Zero);
                keybd_event(0x5B, 0, UP, UIntPtr.Zero);
            }
            catch { }
        }

        private string DumpCustom()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_s.Custom.Count);
            sb.Append(" [");
            for (int i = 0; i < _s.Custom.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(_s.Custom[i].Text);
                sb.Append(_s.Custom[i].Enabled ? "" : "(off)");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private void FlushKeyLog()
        {
            List<string> lines = _guard.DrainLog();
            if (lines.Count == 0) return;
            try
            {
                string p = AppSettings.KeysLogPath;
                if (File.Exists(p) && new FileInfo(p).Length > 262144) File.WriteAllText(p, "");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < lines.Count; i++)
                {
                    sb.Append(DateTime.Now.ToString("MM-dd HH:mm:ss"));
                    sb.Append("  ");
                    sb.AppendLine(lines[i]);
                }
                File.AppendAllText(p, sb.ToString());
            }
            catch { }
        }

        private static void StyleSecondary(FlatButton b)
        {
            b.Fill = Theme.Card;
            b.FillHover = Theme.CardHi;
            b.FillDown = Theme.CardHi;
            b.TextColor = Theme.Accent;
            b.BorderColor = Theme.Line;
        }

        private void Section(string text, int y, int x)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.FSmall;
            l.ForeColor = Theme.Muted;
            l.BackColor = Theme.Bg;
            l.Bounds = new Rectangle(x, y, P(200), P(20));
            l.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(l);
        }

        private void Divider(int y, int x, int w)
        {
            Panel line = new Panel();
            line.Bounds = new Rectangle(x, y, w, 1);
            line.BackColor = Theme.Line;
            Controls.Add(line);
        }

        private ToggleSwitch Row(string text, int y, bool on)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.FBody;
            l.ForeColor = Theme.Text;
            l.BackColor = Theme.Bg;
            l.Bounds = new Rectangle(P(20), y + P(6), P(290), P(24));
            l.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(l);

            ToggleSwitch sw = new ToggleSwitch();
            sw.BackColor = Theme.Bg;
            sw.Size = new Size(P(48), P(24));
            sw.Location = new Point(P(400) - P(18) - sw.Width, y + P(7));
            sw.On = on;
            sw.Toggled += delegate { PersistToggles(); if (_locked) RefreshCard(); };
            Controls.Add(sw);
            return sw;
        }

        private void DragStart(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void BuildTray()
        {
            _icoOff = TrayIcons.Create(false, false);
            _icoOn = TrayIcons.Create(true, false);

            _menu = new ContextMenuStrip();
            _menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            _menu.ForeColor = Theme.Text;
            _menu.ShowImageMargin = false;
            _menu.Font = Theme.FBody;

            _miShow = new ToolStripMenuItem("显示主界面");
            _miShow.Font = Theme.FBodyB;
            _miShow.Click += delegate { ShowUp(); };
            _menu.Items.Add(_miShow);

            _miToggle = new ToolStripMenuItem("启动游戏模式");
            _miToggle.Click += delegate { if (_locked) StopLock(); else StartLock(); };
            _menu.Items.Add(_miToggle);

            _menu.Items.Add(new ToolStripSeparator());

            _miAdmin = new ToolStripMenuItem("以管理员身份重启");
            _miAdmin.Click += delegate { RestartAsAdmin(); };
            _menu.Items.Add(_miAdmin);

            _miExit = new ToolStripMenuItem("退出程序");
            _miExit.Click += delegate { ExitApp(); };
            _menu.Items.Add(_miExit);

            _tray = new NotifyIcon();
            _tray.Icon = _icoOff;
            _tray.Text = "游戏快捷键锁 · 未启用";
            _tray.ContextMenuStrip = _menu;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { ShowUp(); };
        }

        public void ShowUp()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            TopMost = true;
            TopMost = false;
            Activate();
        }

        private void ApplySettings()
        {
            _swWin.On = _s.BlockWin;
            _swTab.On = _s.BlockAltTab;
            _swF4.On = _s.BlockAltF4;
            _swEsc.On = _s.BlockCtrlEsc;
            _swAuto.On = _s.AutoStart;
            _swBall.On = _s.FloatBall;
            RefreshCustomRow();
            RefreshMainButton();
            RefreshCard();
            RefreshMenu();
            RefreshTrayIcon();
        }

        private void PersistToggles()
        {
            _s.BlockWin = _swWin.On;
            _s.BlockAltTab = _swTab.On;
            _s.BlockAltF4 = _swF4.On;
            _s.BlockCtrlEsc = _swEsc.On;
            _s.AutoStart = _swAuto.On;
            _s.FloatBall = _swBall.On;
            _s.Save();

            string exe = Application.ExecutablePath;
            if (exe != _lastExe)
            {
                _lastExe = exe;
                _s.ApplyAutoStart(exe);
            }
            else
            {
                _s.ApplyAutoStart(exe);
            }
            if (!_s.FloatBall && _ball != null) _ball.Hide();
            if (_s.FloatBall && _locked) ShowBall();
            RefreshCard();
        }

        private void OpenCustom()
        {
            try
            {
                using (CustomKeysForm dlg = new CustomKeysForm(_guard, _s))
                {
                    dlg.ShowDialog(this);
                }
            }
            catch { }
            _guard.Suspended = false;
            RefreshCustomRow();
            RefreshCard();
            AppSettings.Note("custom changed; custom=" + DumpCustom());
        }

        private void RefreshCustomRow()
        {
            int n = _s.Custom.Count;
            _lblCustom.Text = n == 0 ? "自定义组合键（未设置）" : ("自定义组合键（" + n + " 个）");
        }

        private void StartLock()
        {
            _guard.Options.BlockWin = _s.BlockWin;
            _guard.Options.BlockAltTab = _s.BlockAltTab;
            _guard.Options.BlockAltF4 = _s.BlockAltF4;
            _guard.Options.BlockCtrlEsc = _s.BlockCtrlEsc;

            string rep;
            if (!_guard.Start(out rep))
            {
                MessageBox.Show(this, rep, "无法启用", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _locked = true;
            AppSettings.Note("lock ON; custom=" + DumpCustom() + "; blockWin=" + _s.BlockWin + " blockAltTab=" + _s.BlockAltTab);
            _tmr.Start();
            RefreshMainButton();
            RefreshCard();
            RefreshMenu();
            RefreshTrayIcon();
            if (_s.FloatBall) ShowBall();
            Activate();
            if (_tray != null)
            {
                _tray.BalloonTipTitle = "游戏模式已开启";
                _tray.BalloonTipText = "系统快捷键已拦截。右键托盘图标可随时结束。";
                _tray.ShowBalloonTip(2000);
            }
        }

        private void StopLock()
        {
            _guard.Stop();
            FlushKeyLog();
            _locked = false;
            AppSettings.Note("lock OFF");
            if (_tmr != null) _tmr.Stop();
            HideBall();
            RefreshMainButton();
            RefreshCard();
            RefreshMenu();
            RefreshTrayIcon();
        }

        private void ShowBall()
        {
            if (_ball == null)
            {
                _ball = new FloatBall();
                _ball.Unlock += delegate { BackToDesktop(); };
            }
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            _ball.Location = new Point(wa.Right - P(76), wa.Bottom - P(170));
            _ball.Show();
            _ball.TopMost = true;
        }

        private void HideBall()
        {
            if (_ball != null) _ball.Hide();
        }

        private void RefreshMainButton()
        {
            if (_locked)
            {
                _btnMain.Text = "结束游戏模式";
                _btnMain.Fill = Theme.Danger;
                _btnMain.FillHover = Theme.DangerHi;
                _btnMain.FillDown = Theme.Danger;
            }
            else
            {
                _btnMain.Text = "启动游戏模式";
                _btnMain.Fill = Theme.Accent;
                _btnMain.FillHover = Theme.AccentHi;
                _btnMain.FillDown = Theme.Accent;
            }
            _btnMain.TextColor = Color.White;
            _btnMain.Invalidate();
        }

        private void RefreshCard()
        {
            if (!_locked)
            {
                _card.Title = "未启用";
                _card.Sub = "系统快捷键当前可正常使用";
                _card.Dot = Theme.Muted;
            }
            else
            {
                int sys = 0;
                if (_s.BlockWin) sys++;
                if (_s.BlockAltTab) sys++;
                if (_s.BlockAltF4) sys++;
                if (_s.BlockCtrlEsc) sys++;
                int cy = 0;
                for (int i = 0; i < _s.Custom.Count; i++) if (_s.Custom[i].Enabled) cy++;
                string sub = "已拦截 " + sys + " 类系统键";
                if (cy > 0) sub += " + " + cy + " 个自定义";
                _card.Title = "已锁定 · 游戏模式";
                _card.Sub = sub;
                _card.Dot = Theme.Accent;
            }
            _card.Invalidate();
        }

        private void RefreshMenu()
        {
            if (_menu == null) return;
            _miToggle.Text = _locked ? "结束游戏模式" : "启动游戏模式";
        }

        private void RefreshTrayIcon()
        {
            if (_tray == null) return;
            _tray.Icon = _locked ? _icoOn : _icoOff;
            _tray.Text = _locked ? "游戏快捷键锁 · 已锁定" : "游戏快捷键锁 · 未启用";
        }

        private void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Application.ExecutablePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
            }
            catch { return; }
            ExitApp();
        }

        private void ExitApp()
        {
            _exit = true;
            _guard.Stop();
            if (_tray != null) _tray.Visible = false;
            _s.X = Location.X;
            _s.Y = Location.Y;
            _s.Save();
            _tmr.Stop();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }

        public KeyboardGuard Guard { get { return _guard; } }

        public void PreviewLocked()
        {
            _locked = true;
            RefreshMainButton();
            RefreshCard();
        }
    }
}