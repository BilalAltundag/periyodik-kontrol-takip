using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Periyodik Kontrol Takip")]
[assembly: AssemblyDescription("Periyodik Kontrol Takip")]
[assembly: AssemblyProduct("Periyodik Kontrol Takip")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyVersion("4.3.0.0")]
[assembly: AssemblyFileVersion("4.3.0.0")]

namespace PKT
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        static string LogPath()
        {
            return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "hata-kaydi.txt");
        }
        static void Log(Exception ex)
        {
            try { File.AppendAllText(LogPath(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" + ex + "\r\n\r\n"); } catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            try { if (Environment.OSVersion.Platform == PlatformID.Win32NT) SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                Log(e.Exception);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Ayrıntılar: hata-kaydi.txt\n\n" + e.Exception.Message, "Periyodik Kontrol Takip", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { var ex = e.ExceptionObject as Exception; if (ex != null) Log(ex); };

            using (var g = Graphics.FromHwnd(IntPtr.Zero)) Th.K = Math.Max(1f, g.DpiX / 96f);

            bool created;
            using (var mx = new Mutex(true, "PeriyodikKontrolTakip_TekPencere", out created))
            {
                if (!created)
                {
                    MessageBox.Show("Program zaten açık. Görev çubuğundaki pencereyi kullanın.", "Periyodik Kontrol Takip", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var st = new Store();
                st.Init();
                st.Load();
                var f = new MainForm(st);
                if (args.Length >= 2 && args[0] == "--screenshot") { MainForm.NoPopup = true; f.Shot(args[1]); }
                Application.Run(f);
            }
        }
    }
}
