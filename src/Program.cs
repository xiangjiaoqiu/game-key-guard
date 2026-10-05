using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GameKeyGuard
{
    internal static class Program
    {
        private static Mutex _mtx;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [STAThread]
        private static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { Log(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Log(e.ExceptionObject as Exception); };

            bool shot = Has(args, "--shot");
            if (!shot)
            {
                bool created = false;
                for (int i = 0; i < 15; i++)
                {
                    try
                    {
                        _mtx = new Mutex(true, "GameKeyGuard_Single_v1", out created);
                    }
                    catch { created = true; }
                    if (created) break;
                    Thread.Sleep(200);
                }
                if (!created)
                {
                    IntPtr h = FindWindow(null, "游戏快捷键锁");
                    if (h != IntPtr.Zero) SendMessage(h, MainForm.WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
            }

            try { SetProcessDPIAware(); }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            float k = 1f;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) k = g.DpiX / 96f;
            }
            catch { k = 1f; }
            if (k < 0.5f || k > 4f) k = 1f;
            Theme.K = k;

            AppSettings s = AppSettings.Load();

            if (shot)
            {
                if (Has(args, "--shot-custom")) ShotCustom(s, args);
                else Shot(s, args);
                return;
            }

            MainForm f = new MainForm(s, Has(args, "--min"), false);
            Application.Run(f);
        }

        private static void Shot(AppSettings s, string[] args)
        {
            string path = Arg(args, "--shot");
            if (path.Length == 0) path = "shot.png";
            using (MainForm f = new MainForm(s, false, true))
            {
                f.Show();
                Application.DoEvents();
                if (Has(args, "--shot-locked")) f.PreviewLocked();
                Thread.Sleep(350);
                Application.DoEvents();
                using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(path, ImageFormat.Png);
                }
                f.Hide();
            }
        }

        private static void ShotCustom(AppSettings s, string[] args)
        {
            string path = Arg(args, "--shot");
            if (path.Length == 0) path = "shot_custom.png";
            if (s.Custom.Count == 0)
            {
                CustomCombo c1 = new CustomCombo(); c1.Ctrl = true; c1.Alt = true; c1.Vk = 0x51; s.Custom.Add(c1);
                CustomCombo c2 = new CustomCombo(); c2.Shift = true; c2.Alt = true; c2.Vk = 0x41; s.Custom.Add(c2);
                CustomCombo c3 = new CustomCombo(); c3.Ctrl = true; c3.Shift = true; c3.Vk = 0x73; c3.Enabled = false; s.Custom.Add(c3);
            }
            using (KeyboardGuard guard = new KeyboardGuard())
            using (CustomKeysForm f = new CustomKeysForm(guard, s))
            {
                f.Show();
                Application.DoEvents();
                Thread.Sleep(300);
                Application.DoEvents();
                using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(path, ImageFormat.Png);
                }
                f.Hide();
            }
        }

        private static bool Has(string[] a, string name)
        {
            for (int i = 0; i < a.Length; i++) if (string.Equals(a[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string Arg(string[] a, string name)
        {
            for (int i = 0; i < a.Length - 1; i++) if (string.Equals(a[i], name, StringComparison.OrdinalIgnoreCase)) return a[i + 1];
            return "";
        }

        private static void Log(Exception ex)
        {
            if (ex == null) return;
            try
            {
                System.IO.File.AppendAllText(AppSettings.LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + ex.ToString() + Environment.NewLine);
            }
            catch { }
        }
    }
}