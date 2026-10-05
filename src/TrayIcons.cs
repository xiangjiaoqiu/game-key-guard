using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameKeyGuard
{
    internal static class TrayIcons
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon Create(bool locked, bool compat)
        {
            Bitmap bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                Color bg = locked ? Color.FromArgb(21, 26, 38) : Color.FromArgb(27, 30, 38);
                Theme.FillRound(g, new Rectangle(0, 0, 31, 31), 8, bg);
                Color accent = locked ? (compat ? Theme.Warn : Theme.Accent) : Theme.Muted;
                using (Pen pen = new Pen(accent, 2.6f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, 9.5f, 5.5f, 13f, 13f, 180f, 180f);
                }
                Theme.FillRound(g, new Rectangle(8, 14, 16, 12), 3, accent);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(210, bg)))
                {
                    g.FillEllipse(b, 14.5f, 18f, 3f, 3f);
                }
            }
            IntPtr h = bmp.GetHicon();
            Icon ic = (Icon)Icon.FromHandle(h).Clone();
            DestroyIcon(h);
            bmp.Dispose();
            return ic;
        }
    }
}