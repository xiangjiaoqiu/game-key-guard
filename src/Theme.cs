using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GameKeyGuard
{
    internal static class Theme
    {
        public static float K = 1f;
        public static readonly Color Bg = Color.FromArgb(15, 17, 21);
        public static readonly Color Card = Color.FromArgb(23, 26, 33);
        public static readonly Color CardHi = Color.FromArgb(32, 37, 47);
        public static readonly Color Line = Color.FromArgb(38, 43, 54);
        public static readonly Color Text = Color.FromArgb(232, 235, 241);
        public static readonly Color Sub = Color.FromArgb(140, 147, 163);
        public static readonly Color Muted = Color.FromArgb(99, 106, 121);
        public static readonly Color Accent = Color.FromArgb(79, 140, 255);
        public static readonly Color AccentHi = Color.FromArgb(108, 160, 255);
        public static readonly Color Danger = Color.FromArgb(226, 78, 88);
        public static readonly Color DangerHi = Color.FromArgb(242, 104, 112);
        public static readonly Color Warn = Color.FromArgb(238, 168, 70);
        public static readonly Color Green = Color.FromArgb(60, 199, 131);
        public static readonly Color Track = Color.FromArgb(45, 50, 62);

        public static readonly Font FTitle = new Font("Microsoft YaHei UI", 11.5F, FontStyle.Bold);
        public static readonly Font FBody = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
        public static readonly Font FBodyB = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        public static readonly Font FSmall = new Font("Microsoft YaHei UI", 8.25F, FontStyle.Regular);
        public static readonly Font FBtn = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
        public static readonly Font FBig = new Font("Microsoft YaHei UI", 11.5F, FontStyle.Bold);

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d <= 1) { p.AddRectangle(r); return p; }
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
        }

        public static void DrawRound(Graphics g, Rectangle r, int radius, Color c, float w)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(c, w)) g.DrawPath(pen, p);
        }

        public static void DrawText(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            System.Windows.Forms.TextRenderer.DrawText(g, s, f, r, c,
                System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.NoPadding);
        }

        public static void DrawTextC(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            System.Windows.Forms.TextRenderer.DrawText(g, s, f, r, c,
                System.Windows.Forms.TextFormatFlags.HorizontalCenter | System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.NoPadding);
        }
    }
}