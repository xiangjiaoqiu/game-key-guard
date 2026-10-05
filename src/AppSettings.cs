using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace GameKeyGuard
{
    public class AppSettings
    {
        public bool BlockWin = true;
        public bool BlockAltTab = true;
        public bool BlockAltF4 = true;
        public bool BlockCtrlEsc = true;
        public bool AutoStart = false;
        public bool FloatBall = false;
        public int X = -99999;
        public int Y = -99999;
        public List<CustomCombo> Custom = new List<CustomCombo>();

        public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string RunName = "GameKeyGuard";

        public static string Dir
        {
            get
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameKeyGuard");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string IniPath { get { return Path.Combine(Dir, "settings.ini"); } }
        public static string LogPath { get { return Path.Combine(Dir, "error.log"); } }
        public static string KeysLogPath { get { return Path.Combine(Dir, "keys.log"); } }

        public static void Note(string s)
        {
            try { File.AppendAllText(KeysLogPath, DateTime.Now.ToString("MM-dd HH:mm:ss") + "  " + s + Environment.NewLine); }
            catch { }
        }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                if (File.Exists(IniPath))
                {
                    string[] lines = File.ReadAllLines(IniPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string k = line.Substring(0, eq).Trim();
                        string v = line.Substring(eq + 1).Trim();
                        bool b = v == "1" || v.ToLower() == "true";
                        switch (k)
                        {
                            case "BlockWin": s.BlockWin = b; break;
                            case "BlockAltTab": s.BlockAltTab = b; break;
                            case "BlockAltF4": s.BlockAltF4 = b; break;
                            case "BlockCtrlEsc": s.BlockCtrlEsc = b; break;
                            case "AutoStart": s.AutoStart = b; break;
                            case "FloatBall": s.FloatBall = b; break;
                            case "X": { int n; if (int.TryParse(v, out n)) s.X = n; break; }
                            case "Y": { int n; if (int.TryParse(v, out n)) s.Y = n; break; }
                            case "Custom":
                                {
                                    string[] parts = v.Split(';');
                                    for (int j = 0; j < parts.Length; j++)
                                    {
                                        CustomCombo c = CustomCombo.Parse(parts[j]);
                                        if (c != null) s.Custom.Add(c);
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("BlockWin=" + B(BlockWin));
                sb.AppendLine("BlockAltTab=" + B(BlockAltTab));
                sb.AppendLine("BlockAltF4=" + B(BlockAltF4));
                sb.AppendLine("BlockCtrlEsc=" + B(BlockCtrlEsc));
                sb.AppendLine("AutoStart=" + B(AutoStart));
                sb.AppendLine("FloatBall=" + B(FloatBall));
                sb.AppendLine("X=" + X);
                sb.AppendLine("Y=" + Y);
                StringBuilder cs = new StringBuilder();
                for (int i = 0; i < Custom.Count; i++)
                {
                    if (i > 0) cs.Append(";");
                    cs.Append(Custom[i].Serialize());
                }
                sb.AppendLine("Custom=" + cs.ToString());
                File.WriteAllText(IniPath, sb.ToString());
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  save settings failed: " + ex.Message + Environment.NewLine); }
                catch { }
            }
        }

        private static string B(bool b) { return b ? "1" : "0"; }

        public void ApplyAutoStart(string exePath)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (AutoStart) k.SetValue(RunName, "\"" + exePath + "\" --min");
                    else k.DeleteValue(RunName, false);
                }
            }
            catch { }
        }
    }
}