using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TktCouponCodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string[] LineFeedCodes = new[] { "\r\n", "\r", "\n" };

        public string EMail
        {
            get => this.emailTextBox.Text;
            set => this.emailTextBox.Text = value;
        }

        public string Password
        {
            get => this.passwordBox.Password;
            set => this.passwordBox.Password = value;
        }

        public string InputFilePath
        {
            get => inputFileNameTextBox.Text;
            set => inputFileNameTextBox.Text = value;
        }

        public string GroupId
        {
            get => groupIdTextBox.Text;
            set => groupIdTextBox.Text = value;
        }

        public string EventId
        {
            get => eventIdTextBox.Text;
            set => eventIdTextBox.Text = value;
        }

        public string ShowId
        {
            get => showIdTextBox.Text;
            set => showIdTextBox.Text = value;
        }

        public string TicketName
        {
            get => ticketNameTextBox.Text;
            set => ticketNameTextBox.Text = value;
        }

        private bool IsFormFilled => EMail.Any() && Password.Any() && InputFilePath.Any() &&
                                     GroupId.Any() && EventId.Any() && ShowId.Any() && TicketName.Any();

        public MainWindow()
        {
            InitializeComponent();
        }

        public void RunSelenium()
        {
            if (!IsFormFilled)
            {
                return;
            }

            List<CouponInfo> couponInfos;
            try
            {
                couponInfos = ReadCouponFile(this.InputFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception("ファイルの読み込みに失敗しました。" + ex.Message, ex);
            }

            try
            {
                var config = new TargetConfig(
                    this.EMail,
                    this.Password,
                    // XXX: parse できることを信じる
                    int.Parse(this.GroupId),
                    int.Parse(this.EventId),
                    int.Parse(this.ShowId),
                    this.TicketName
                );
                SeleniumRunner.Run(config, couponInfos);
            }
            catch (Exception ex)
            {
                throw new Exception("クーポンコードの登録に失敗しました。" + ex.Message, ex);
            }
        }

        private static List<CouponInfo> ReadCouponFile(string fileName)
        {
            string content;
            using (var reader = new StreamReader(fileName, new FileStreamOptions() { Access = FileAccess.Read }))
            {
                content = reader.ReadToEnd();
            }
            return MakeCouponInfos(content).ToList();
        }

        private static IEnumerable<CouponInfo> MakeCouponInfos(string fileContent)
        {
            static void ThrowException(int lineNo, string line) =>
                throw new Exception($"入力ファイルに不正な行が存在します。({lineNo}: {line}");

            int lineNo = 1;
            // 念のため重複チェック
            var seenCoupon = new HashSet<string>();
            foreach (var line in fileContent.Split(LineFeedCodes, StringSplitOptions.None))
            {
                if (!line.Any())
                {
                    yield break;
                }

                var values = line.Split(",")
                    .Select(x => x.Trim())
                    .ToList();
                if (values.Count != 3)
                {
                    ThrowException(lineNo, line);
                }
                var couponCode = values[0];
                if (seenCoupon.Contains(couponCode) ||
                    !int.TryParse(values[1], out var disCountedFee) ||
                    !int.TryParse(values[2], out var count))
                {
                    ThrowException(lineNo, line);
                }
                seenCoupon.Add(couponCode);
                yield return new CouponInfo(couponCode, disCountedFee, count);
                lineNo++;
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) => RunSelenium();

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                InputFilePath = dialog.FileName;
            }
        }

        private void OnInputChanged()
        {
            // Binding は面倒なのでしない
            runButton.IsEnabled = IsFormFilled;
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e) => OnInputChanged();
        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e) => OnInputChanged();
        private void intTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    }
}
