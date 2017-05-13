using System.Windows;
using MahApps.Metro.Controls;
using static ffxiv_chatlogger.Settings;

namespace ffxiv_chatlogger
{
    /// <summary>
    /// ChatOption.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatOption : MetroWindow
    {
        private static SettingFile settFormat = Settings.globalSetting;

        public ChatOption()
        {
            InitializeComponent();

            // 프로세스 목록 갱신
            this.op_listProcess.ItemsSource = Logger.ProcessList;

            // 필터에 처음에는 모두 적용
            foreach (var item in ChatType.TypeList.Values)
            {
                Logger.ChatFilterLog.Add(item);
            }

            // API 갱신
            this.op_listAPIService.ItemsSource = TranslateServiceList.TypeList.Values;
            this.op_apiSourceLang.ItemsSource = Translate.TypeList.Values;
            this.op_apiDestLang.ItemsSource = Translate.TypeList.Values;
            this.op_listAPIService.SelectedIndex = 0;
            this.op_apiSourceLang.SelectedIndex = 0;
            this.op_apiDestLang.SelectedIndex = 1;

            // 필터쪽 갱신
            this.op_chatFilter.ItemsSource = Logger.ChatFilterLog;
            this.op_chatTransFilter.ItemsSource = Logger.ChatFilterTransLog;

            // 설정 로드
            settFormat = Settings.Load();
            this.op_slOpacity.Value = settFormat.windowOpacity;
            this.op_listAPIService.SelectedIndex = settFormat.selectAPIService;

            HiddenControl();
            foreach (var item in TranslateServiceList.TypeList)
            {
                if (this.op_listAPIService.SelectedIndex == item.Value.GetId)
                {
                    if (item.Value.GetCode.Equals("naver"))
                    {
                        this.op_APIKey_Label.Visibility = Visibility.Visible;
                        this.op_APISecretKey_Label.Visibility = Visibility.Visible;

                        this.op_APIKey_Naver.Visibility = Visibility.Visible;
                        this.op_APISecretKey.Visibility = Visibility.Visible;
                    }
                    else if (item.Value.GetCode.Equals("google"))
                    {
                        this.op_APIKey_Label.Visibility = Visibility.Visible;

                        this.op_APIKey_Google.Visibility = Visibility.Visible;
                    }
                }

                if (item.Value.GetCode.Equals("naver"))
                {
                    item.Value.ClientKey = settFormat.naverClientKey;
                    item.Value.SecretKey = settFormat.naverSecretKey;

                    this.op_APIKey_Naver.Text = settFormat.naverClientKey;
                    this.op_APISecretKey.Text = settFormat.naverSecretKey;
                }
                else if (item.Value.GetCode.Equals("google"))
                {
                    item.Value.ClientKey = settFormat.googleAPIKey;

                    this.op_APIKey_Google.Text = settFormat.googleAPIKey;
                }
            }
            this.op_APISwitch.IsChecked = settFormat.enableTransService;
        }

        public void ApplyOpacity()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).Opacity = (op_slOpacity.Value / 100);
                }
            }
        }

        private void HiddenControl()
        {
            this.op_APIKey_Label.Visibility = Visibility.Hidden;
            this.op_APISecretKey_Label.Visibility = Visibility.Hidden;
            this.op_APIKey_Naver.Visibility = Visibility.Hidden;
            this.op_APIKey_Google.Visibility = Visibility.Hidden;
            this.op_APISecretKey.Visibility = Visibility.Hidden;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyOpacity();
        }

        private void op_btnGoRight_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_chatFilter.SelectedItem != null)
            {
                Logger.ChatFilterTransLog.Add((ChatType)this.op_chatFilter.SelectedItem);
            }
        }

        private void op_btnGoLeft_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_chatTransFilter.SelectedItem != null)
            {
                Logger.ChatFilterTransLog.Remove((ChatType)this.op_chatTransFilter.SelectedItem);
            }
        }

        private void op_listAPIService_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            HiddenControl();
            if (((TranslateServiceList)this.op_listAPIService.SelectedItem).GetCode.Equals("naver"))
            {
                this.op_APIKey_Label.Visibility = Visibility.Visible;
                this.op_APISecretKey_Label.Visibility = Visibility.Visible;

                this.op_APIKey_Naver.Visibility = Visibility.Visible;
                this.op_APISecretKey.Visibility = Visibility.Visible;

                this.op_APIKey_Naver.Text = ((TranslateServiceList)this.op_listAPIService.SelectedItem).ClientKey;
                this.op_APISecretKey.Text = ((TranslateServiceList)this.op_listAPIService.SelectedItem).SecretKey;
            }
            else if (((TranslateServiceList)this.op_listAPIService.SelectedItem).GetCode.Equals("google"))
            {
                this.op_APIKey_Label.Visibility = Visibility.Visible;

                this.op_APIKey_Google.Visibility = Visibility.Visible;

                this.op_APIKey_Google.Text = ((TranslateServiceList)this.op_listAPIService.SelectedItem).ClientKey;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            settFormat.windowOpacity = this.op_slOpacity.Value;
            settFormat.selectAPIService = this.op_listAPIService.SelectedIndex;
            foreach (var item in TranslateServiceList.TypeList)
            {
                if (item.Value.GetCode.Equals("naver"))
                {
                    settFormat.naverClientKey = this.op_APIKey_Naver.Text;
                    settFormat.naverSecretKey = this.op_APISecretKey.Text;
                }
                else if (item.Value.GetCode.Equals("google"))
                {
                    settFormat.googleAPIKey = this.op_APIKey_Google.Text;
                }
            }

            settFormat.sourceLang = ((Translate)this.op_apiSourceLang.SelectedItem).GetCode;
            settFormat.destLang = ((Translate)this.op_apiDestLang.SelectedItem).GetCode;
            settFormat.enableTransService = this.op_APISwitch.IsChecked;

            Settings.Save(settFormat);

            this.Hide();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void op_btnResetProcess_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
