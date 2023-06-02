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
            try
            {
                var app = new App();
                app.Startup += Application_Startup;
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show(ex.Message);
            }

        }

        private static void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow window = new();

            bool runDirectly = false;
            var args = e.Args.ToList();
            while(args.Count > 0)
            {
                var option = args[0];
                string value = option switch
                {
                    "--direct" => string.Empty,
                    _ => args.Count > 1 ? args[1] : throw new Exception($"{option} に値が指定されていません。({option})")
                };
                if (option != "--direct")
                {
                    value = args[1];
                }
                switch (option)
                {
                    case "--email":
                        window.EMail = value;
                        break;
                    case "--password":
                        window.Password = value;
                        break;
                    case "--coupon-file":
                        window.InputFilePath = value;
                        break;
                    case "--group-id":
                        CheckArgIsInt(option, value);
                        window.GroupId = value;
                        break;
                    case "--event-id":
                        CheckArgIsInt(option, value);
                        window.EventId = value;
                        break;
                    case "--show-id":
                        CheckArgIsInt(option, value);
                        window.ShowId = value;
                        break;
                    case "--ticket-name":
                        window.TicketName = value;
                        break;
                    case "--direct":
                        runDirectly = true;
                        break;
                    default:
                        throw new Exception($"不正なオプションです。 ({args[0]})");
                }
                args.RemoveAt(0);
                if (option != "--direct")
                {
                    args.RemoveAt(0);
                }
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

        private static void CheckArgIsInt(string optionName, string arg)
        {
            if (!int.TryParse(arg, out _))
            {
                throw new Exception($"{optionName} の値が不正です。");
            }
        }
    }
}
