using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TktCouponCodeGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThreadAttribute()]
        public static void Main()
        {
            var app = new App();
            app.Startup += Application_Startup;
            app.Run();
        }

        private static void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow window = new();

            bool runDirectly = false;
            var args = e.Args.ToList();
            while(args.Count > 0)
            {
                var arg = args[0];
                switch (args[0])
                {
                    case "--email":
                        window.EMail = arg;
                        break;
                    case "--password":
                        window.Password = arg;
                        break;
                    case "--coupon-file":
                        window.InputFilePath = arg;
                        break;
                    case "--direct":
                        runDirectly = true;
                        break;
                    default:
                        throw new Exception($"不正なオプション ({args[0]})");
                }
                args.RemoveAt(0);
            }

            if (runDirectly)
            {
                window.RunSelenium();
            }
            else
            {
                window.Show();
            }
        }
    }
}
