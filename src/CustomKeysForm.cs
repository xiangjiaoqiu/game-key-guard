using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameKeyGuard
{
    internal class RoundedPanel : Panel
    {
        public int Radius = 10;
        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), Radius, Theme.Card);
        }
    }

    internal class CustomKeysForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private KeyboardGuard _guard;
        private AppSettings _s;
        private Panel _list;
        private FlatButton _btnAdd;
        private Label _hint, _tip;
        private bool _capturing;
        private Timer _poll;

        private int P(int v) { return (int)Math.Round(v * Theme.K); }

        public CustomKeysForm(KeyboardGuard guard, AppSettings s)
        {
            _guard = guard;
            _s = s;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.FBody;
            KeyPreview = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            Text = "自定义拦截";

            int W = P(380);
            ClientSize = new Size(W, P(436));

            Panel bar = new Panel();
            bar.Bounds = new Rectangle(0, 0, W, P(46));
            bar.BackColor = Theme.Bg;
            bar.MouseDown += Drag;
            Controls.Add(bar);

            Label title = new Label();
            title.Text = "自定义拦截组合键";
            title.Font = Theme.FTitle;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.Bounds = new Rectangle(P(18), P(10), P(260), P(26));
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.MouseDown += Drag;
            bar.Controls.Add(title);

            FlatButton close = new FlatButton();
            close.Text = "×";
            close.Font = Theme.FBodyB;
            close.Bounds = new Rectangle(W - P(50), P(11), P(30), P(26));
            close.Fill = Theme.Bg;
            close.FillHover = Theme.Card;
            close.FillDown = Theme.CardHi;
            close.TextColor = Theme.Sub;
            close.Radius = P(8);
            close.Click += delegate { Close(); };
            bar.Controls.Add(close);

            _hint = new Label();
            _hint.Bounds = new Rectangle(P(18), P(50), W - P(36), P(26));
            _hint.Font = Theme.FSmall;
            _hint.ForeColor = Theme.Muted;
            _hint.BackColor = Theme.Bg;
            _hint.Text = "点下面按钮，然后按下要拦的组合键";
            Controls.Add(_hint);

            _list = new Panel();
            _list.Bounds = new Rectangle(P(18), P(88), W - P(36), P(240));
            _list.BackColor = Theme.Bg;
            _list.AutoScroll = true;
            Controls.Add(_list);

            _btnAdd = new FlatButton();
            _btnAdd.Bounds = new Rectangle(P(18), P(340), W - P(36), P(48));
            _btnAdd.Radius = P(12);
            _btnAdd.Font = Theme.FBtn;
            _btnAdd.Click += delegate { if (_capturing) EndCapture(""); else BeginCapture(); };
            Controls.Add(_btnAdd);

            _tip = new Label();
            _tip.Bounds = new Rectangle(P(18), P(396), W - P(36), P(24));
            _tip.Font = Theme.FSmall;
            _tip.ForeColor = Theme.Muted;
            _tip.BackColor = Theme.Bg;
            _tip.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_tip);

            _poll = new Timer();
            _poll.Interval = 60;
            _poll.Tick += delegate { PollCapture(); };

            SetAddIdle();
            Rebuild();
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
                using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), P(16))) Region = new Region(p);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Theme.Bg)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void SetAddIdle()
        {
            _btnAdd.Text = "+ 按下快捷键添加";
            _btnAdd.Fill = Theme.Accent;
            _btnAdd.FillHover = Theme.AccentHi;
            _btnAdd.FillDown = Theme.Accent;
            _btnAdd.TextColor = Color.White;
            _tip.Text = _s.Custom.Count == 0 ? "还没有自定义组合键" : "例如 Shift + Alt + A，至少要带一个修饰键";
            _btnAdd.Invalidate();
        }

        private void BeginCapture()
        {
            if (!_guard.BeginCapture())
            {
                _tip.Text = "无法启动按键捕获（钩子安装失败）";
                return;
            }
            _capturing = true;
            _btnAdd.Text = "请按下组合键…（Esc 取消）";
            _btnAdd.Fill = Theme.Danger;
            _btnAdd.FillHover = Theme.DangerHi;
            _btnAdd.FillDown = Theme.Danger;
            _btnAdd.Invalidate();
            _tip.Text = "现在按下组合键…只按修饰键不会生效";
            _poll.Start();
            Focus();
        }

        private void EndCapture(string message)
        {
            _capturing = false;
            if (_poll != null) _poll.Stop();
            _guard.EndCapture();
            SetAddIdle();
            if (message.Length > 0) _tip.Text = message;
        }

        private void PollCapture()
        {
            if (!_capturing) return;
            if (_guard.TakeCaptureCancel()) { EndCapture("已取消"); return; }
            CustomCombo c = _guard.TakeCaptured();
            if (c != null) AddCombo(c);
        }

        private void AddCombo(CustomCombo c)
        {
            c.Enabled = true;
            for (int i = 0; i < _s.Custom.Count; i++)
            {
                if (_s.Custom[i].SameAs(c))
                {
                    EndCapture(c.Text + " 已经在列表里了");
                    return;
                }
            }
            _s.Custom.Add(c);
            _s.Save();
            Rebuild();
            EndCapture("已添加 " + c.Text);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_capturing) EndCapture("");
            base.OnFormClosing(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_capturing)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    EndCapture("已取消");
                }
                return;
            }
            if (e.KeyCode == Keys.Escape) { Close(); return; }
            base.OnKeyDown(e);
        }
        private void Rebuild()
        {
            _list.Controls.Clear();
            int rowW = _list.Width - P(20);

            if (_s.Custom.Count == 0)
            {
                Label empty = new Label();
                empty.Text = "还没有自定义组合键";
                empty.Font = Theme.FBody;
                empty.ForeColor = Theme.Muted;
                empty.BackColor = Theme.Bg;
                empty.Bounds = new Rectangle(0, P(92), rowW, P(26));
                empty.TextAlign = ContentAlignment.MiddleCenter;
                _list.Controls.Add(empty);
                return;
            }

            int y = 0;
            for (int i = 0; i < _s.Custom.Count; i++)
            {
                CustomCombo c = _s.Custom[i];
                RoundedPanel row = new RoundedPanel();
                row.Radius = P(10);
                row.Bounds = new Rectangle(0, y, rowW, P(38));
                _list.Controls.Add(row);

                Label l = new Label();
                l.Text = c.Text;
                l.Font = Theme.FBody;
                l.ForeColor = c.Enabled ? Theme.Text : Theme.Muted;
                l.BackColor = Theme.Card;
                l.Bounds = new Rectangle(P(14), P(6), P(200), P(26));
                l.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(l);

                ToggleSwitch sw = new ToggleSwitch();
                sw.BackColor = Theme.Card;
                sw.Size = new Size(P(40), P(20));
                sw.Location = new Point(rowW - P(96), P(9));
                sw.On = c.Enabled;
                CustomCombo cap = c;
                sw.Toggled += delegate
                {
                    cap.Enabled = sw.On;
                    l.ForeColor = cap.Enabled ? Theme.Text : Theme.Muted;
                    _s.Save();
                };
                row.Controls.Add(sw);

                FlatButton del = new FlatButton();
                del.Text = "×";
                del.Font = Theme.FBodyB;
                del.Bounds = new Rectangle(rowW - P(44), P(7), P(30), P(24));
                del.Fill = Theme.Card;
                del.FillHover = Theme.Danger;
                del.FillDown = Theme.Danger;
                del.TextColor = Theme.Sub;
                del.Radius = P(7);
                CustomCombo cap2 = c;
                del.Click += delegate
                {
                    _s.Custom.Remove(cap2);
                    _s.Save();
                    Rebuild();
                    SetAddIdle();
                };
                row.Controls.Add(del);

                y += P(42);
            }
        }
    }
}