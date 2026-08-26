using PluginCore;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace PluginStandalone
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string pluginFilename = ConfigurationManager.AppSettings["Plugin"];

                if (string.IsNullOrWhiteSpace(pluginFilename))
                {
                    throw new Exception("必须在应用程序配置的“Plugin”键中指定插件文件名。");
                }

                PluginLoader.Instance.Log += Loader_Log;

                var plugin = PluginLoader.LoadPlugin(pluginFilename);

                if (plugin == null)
                {
                    throw new Exception($"加载 {pluginFilename} 失败。");
                }

                if (plugin.Type is not IPluginForm form || !form.SupportsStandaloneLoading)
                {
                    throw new Exception($"{pluginFilename} 不支持作为独立应用程序加载。");
                }

                Console.WriteLine($"正在加载 {pluginFilename}...");
                Application.Run(form.CreateStandaloneForm());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private static void Loader_Log(object sender, LogEventArgs e)
        {
            Console.WriteLine(e.Message);

            //TODO - Save to log file
        }
    }
}
