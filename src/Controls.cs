using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameKeyGuard
{
    internal class ToggleSwitch : Control
    {
        private bool _on;
        private bool _hover;
        public event EventHandler Toggled;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(48, 24);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public bool On
        {
            get { return _on; }
            set
            {
                if (_on != value)
                {
                    _on = value;
                    Invalidate();
                    if (Toggled != null) Toggled(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) On = !On;
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int rad = r.Height / 2;
            Color c = _on ? (_hover ? Theme.AccentHi : Theme.Accent) : (_hover ? Theme.CardHi : Theme.Track);
            Theme.FillRound(g, r, rad, c);
            int m = 3;
            int d = r.Height - m * 2;
            int x = _on ? r.Right - m - d : r.Left + m;
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, x, r.Top + m, d, d);
        }
    }

    internal class FlatButton : Control
    {
        private bool _hover, _down;
        public Color Fill = Theme.Accent;
        public Color FillHover = Theme.AccentHi;
        public Color FillDown = Theme.Accent;
        public Color TextColor = Color.White;
        public Color BorderColor = Color.Empty;
        public int Radius = 10;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = Theme.FBtn;
            Size = new Size(120, 40);
            TabStop = false;
        }

        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Fill;
            if (!Enabled) fill = Theme.Track;
            else if (_down) fill = FillDown;
            else if (_hover) fill = FillHover;
            using (GraphicsPath p = Theme.Round(r, Radius))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                if (BorderColor != Color.Empty && BorderColor.A > 0)
                {
                    using (Pen pen = new Pen(BorderColor)) g.DrawPath(pen, p);
                }
            }
            Theme.DrawTextC(g, Text, Font, new Rectangle(0, 0, Width, Height), Enabled ? TextColor : Theme.Muted);
        }
    }

    internal class StatusCard : Control
    {
        public string Title = "";
        public string Sub = "";
        public Color Dot = Theme.Muted;

        public StatusCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRound(g, r, 16, Theme.Card);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, Dot))) g.FillEllipse(b, 23, 33, 16, 16);
            using (SolidBrush b = new SolidBrush(Dot)) g.FillEllipse(b, 26, 36, 10, 10);
            Theme.DrawText(g, Title, Theme.FBig, new Rectangle(50, 24, Width - 66, 30), Theme.Text);
            Theme.DrawText(g, Sub, Theme.FBody, new Rectangle(50, 53, Width - 66, 24), Theme.Sub);
        }
    }

    internal class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.Card; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Card; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Card; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Card; } }
        public override Color MenuBorder { get { return Theme.Line; } }
        public override Color MenuItemBorder { get { return Theme.Accent; } }
        public override Color MenuItemSelected { get { return Theme.CardHi; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.CardHi; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.CardHi; } }
        public override Color MenuItemPressedGradientBegin { get { return Theme.Card; } }
        public override Color MenuItemPressedGradientEnd { get { return Theme.Card; } }
        public override Color SeparatorDark { get { return Theme.Line; } }
        public override Color SeparatorLight { get { return Theme.Line; } }
    }
}