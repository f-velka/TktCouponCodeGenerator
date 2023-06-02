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
        private static readonly string[] LineFeedCodes = new []{ "\r\n", "\r", "\n" };

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

        private List<CouponInfo>? couponInfos = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void RunSelenium()
        {
            if (!EMail.Any() || !Password.Any() || this.couponInfos == null)
            {
                return;
            }

            try
            {
                SeleniumRunner.Run(this.couponInfos);
            }
            catch (Exception ex)
            {
                throw new Exception("クーポンコードの登録に失敗しました。", ex);
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
            if (result == false)
            {
                return;
            }

            try
            {
                this.couponInfos = ReadCouponFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                throw new Exception("ファイルの読み込みに失敗しました。", ex);
            }

            InputFilePath = dialog.FileName;
        }

        private static List<CouponInfo> ReadCouponFile(string fileName)
        {
            string content;
            using (var reader = new StreamReader(fileName))
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

        private void OnInputChanged()
        {
            // Binding とか面倒なのでしない
            runButton.IsEnabled =EMail.Any() && Password.Any() && InputFilePath.Any();
        }

        private void emailTextBox_TextChanged(object sender, TextChangedEventArgs e) => OnInputChanged();
        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e) => OnInputChanged();
        private void inputFileNameTextBox_TextChanged(object sender, TextChangedEventArgs e) => OnInputChanged();
    }
}
