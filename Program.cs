using System;
using System.Threading;
using System.Windows.Forms;

namespace ApexSyncTool
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // 全局异常捕获，防止静默崩溃
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show("发生未处理的异常:\n\n" + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                    "ApexSync 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show("发生严重错误:\n\n" + ex?.Message + "\n\n" + ex?.StackTrace,
                    "ApexSync 严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "ApexSync 启动错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
