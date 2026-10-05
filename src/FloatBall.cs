using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameKeyGuard
{
    internal class FloatBall : Form
    {
        public event EventHandler Unlock;
        private bool _down;
        private bool _moved;
        private Point _p0;
        private Point _w0;

        public FloatBall()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(46, 46);
            BackColor = Color.Black;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Text = "一键回桌面";
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), Width / 2)) Region = new Region(p);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), Width / 2)) Region = new Region(p);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (LinearGradientBrush b = new LinearGradientBrush(r, Theme.AccentHi, Theme.Accent, 60f))
                g.FillEllipse(b, r);
            float cx = Width / 2f;
            using (Pen pen = new Pen(Color.White, 2.4f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, cx - 6f, Height * 0.28f, 12f, 12f, 180f, 180f);
            }
            Theme.FillRound(g, new Rectangle((int)(cx - 7f), (int)(Height * 0.46f), 14, 11), 3, Color.White);
            using (SolidBrush b = new SolidBrush(Theme.Accent))
                g.FillEllipse(b, cx - 1.5f, Height * 0.46f + 4f, 3f, 3f);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _down = true;
            _moved = false;
            _p0 = Cursor.Position;
            _w0 = Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_down && e.Button == MouseButtons.Left)
            {
                Point p = Cursor.Position;
                int dx = p.X - _p0.X;
                int dy = p.Y - _p0.Y;
                if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3) _moved = true;
                if (_moved) Location = new Point(_w0.X + dx, _w0.Y + dy);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _down = false;
            if (!_moved && e.Button == MouseButtons.Left && Unlock != null) Unlock(this, EventArgs.Empty);
        }
    }
}