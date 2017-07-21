using System.Windows;
using MahApps.Metro.Controls;
using static ffxiv_chatlogger.Settings;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.Windows.Controls;

namespace ffxiv_chatlogger
{
    /// <summary>
    /// ChatOption.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatOption : MetroWindow
    {
        private static SettingFile settFormat = Settings.globalSetting;

        private static MainWindow overlay;

        private static string selectedAPIService = "";

        private static List<string> fontList = new List<string>();
        private static List<string> fontsizeList = new List<string>();

        public ChatOption()
        {
            // 오버레이 윈도우 핸들러 설정
            FindOverlayWindow();

            // 외부 설정 로드
            settFormat = Settings.Load();

            // 컴포넌트 초기화
            InitializeComponent();

            /**********************************************************
             * 
             * [일반]
             * 
            ***********************************************************/
            //** 프로세스 목록 **//
            this.op_listProcess.ItemsSource = Logger.ProcessList;

            /**********************************************************
             * 
             * [번역]
             * 
            ***********************************************************/
            // 메세지 종류 목록
            this.op_listChatType.ItemsSource = ChatType.TypeList.Values;
            // 채팅 종류 필터 목록
            this.op_chatFilter.ItemsSource = Logger.ChatFilterLog;
            this.op_chatTransFilter.ItemsSource = Logger.ChatFilterTransLog;

            //** 번역 설정 목록 **//
            this.op_listAPIService.ItemsSource = TranslateServiceList.TypeList.Values;
            this.op_listAPIService.SelectedIndex = 0;

            //** 언어 설정 목록 **//
            this.op_apiSourceLang_Naver.ItemsSource = Translate.TypeList.Values;
            this.op_apiSourceLang_Naver.SelectedIndex = 0;
            this.op_apiDestLang_Naver.ItemsSource = Translate.TypeList.Values;
            this.op_apiDestLang_Naver.SelectedIndex = 1;
            this.op_apiDestLang_Google.ItemsSource = Translate.TypeList.Values;
            this.op_apiDestLang_Google.SelectedIndex = 1;

            //** 이용하는 API 서비스에 따른 출력 **//
            // 제어 컴포넌트 모두 숨김
            //Toggle_TranslationComponents("", false);

            // API 서비스 구분
            foreach (var item in TranslateServiceList.TypeList)
            {
                //* API 서비스 별 Key 입력 부분 *//
                // 네이버
                if (item.Value.GetCode.Equals("naver"))
                {
                    // Client 키와 Secrey 키 필요
                    item.Value.ClientKey = settFormat.naverClientKey;
                    item.Value.SecretKey = settFormat.naverSecretKey;

                    this.op_APIKey_Naver.Text = settFormat.naverClientKey;
                    this.op_APISecretKey.Text = settFormat.naverSecretKey;
                }
                // 구글
                else if (item.Value.GetCode.Equals("google"))
                {
                    // API 키만 필요
                    item.Value.ClientKey = settFormat.googleAPIKey;

                    this.op_APIKey_Google.Text = settFormat.googleAPIKey;
                }
            }

            //** 언어 선택 **//
            this.op_listAPIService.SelectedIndex = settFormat.selectAPIService;
            // 입력, 출력에 따른 언어 설정 구분
            foreach (var item in Translate.TypeList)
            {
                // 입력 언어
                if (item.Value.GetCode == settFormat.sourceLang)
                {
                    this.op_apiSourceLang_Naver.SelectedIndex = item.Value.GetId;
                }
                // 출력 언어
                if (item.Value.GetCode == settFormat.destLang)
                {
                    this.op_apiDestLang_Naver.SelectedIndex = item.Value.GetId;
                    this.op_apiDestLang_Google.SelectedIndex = item.Value.GetId;
                }
            }

            //** 번역 API 서비스 스위치 **//
            this.op_APISwitch.IsChecked = settFormat.enableTransService != 0 ? true : false;
            if (this.op_APISwitch.IsChecked ?? true)
            {
                Toggle_TranslateService(true);
                Toggle_TranslationComponents(selectedAPIService, true);
            }
            else
            {
                Toggle_TranslateService(false);
                Toggle_TranslationComponents("", false);
            }

            /**********************************************************
             * 
             * [오버레이]
             * 
            ***********************************************************/
            //** 폰트 목록 **//
            if (fontList.Count <= 0)
            {
                foreach (FontFamily font in System.Drawing.FontFamily.Families)
                {
                    fontList.Add(font.Name);
                }
            }
            // 폰트 목록 삽입
            this.op_listFont.ItemsSource = fontList;
            // 현재 폰트 설정
            this.op_listFont.SelectedIndex = fontList.FindIndex(x => x.StartsWith(settFormat.overlayFontName));

            //** 폰트 크기 목록 **//
            if (fontsizeList.Count <= 0)
            {
                for (int i = 6; i < 30; i++)
                {
                    fontsizeList.Add(i.ToString());
                }
            }
            // 폰트 크기 목록 삽입
            this.op_listFontSize.ItemsSource = fontsizeList;
            // 현재 폰트 크기 설정
            this.op_listFontSize.SelectedIndex = fontsizeList.FindIndex(x => x.StartsWith(settFormat.overlayFontSize.ToString()));

            //** 폰트 스타일 및 굵기 **//
            this.op_listFontStyle.IsChecked = settFormat.overlayFontStyle.Equals("italic") ? true : false;
            this.op_listFontWeight.IsChecked = settFormat.overlayFontWeight.Equals("extrabold") ? true : false;

            //** 투명도 **//
            this.op_slOpacity.Value = settFormat.windowOpacity;
        }

        /// <summary>
        /// 프로그램 오버레이의 투명도를 설정합니다.
        /// </summary>
        public void ApplyOpacity()
        {
            overlay.Opacity = (op_slOpacity.Value / 100);
        }

        /// <summary>
        /// 프로그램 오버레이 핸들러를 찾습니다.
        /// </summary>
        private void FindOverlayWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    overlay = (window as MainWindow);
                }
            }
        }

        /// <summary>
        /// 오버레이 투명도를 조절할 때 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyOpacity();
        }

        /// <summary>
        /// 채팅 필터 목록에서 오른쪽으로 보낼 때 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void op_btnGoRight_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_chatFilter.SelectedItem != null)
            {
                Logger.ChatFilterTransLog.Add((ChatType)this.op_chatFilter.SelectedItem);
            }
        }

        /// <summary>
        /// 번역 필터 목록에서 왼쪽으로 보낼 때 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void op_btnGoLeft_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_chatTransFilter.SelectedItem != null)
            {
                Logger.ChatFilterTransLog.Remove((ChatType)this.op_chatTransFilter.SelectedItem);
            }
        }

        private void op_btnGoRight_ChatMsg_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_listChatType.SelectedItem != null)
            {
                bool exists = false;
                foreach (var item in Logger.ChatFilterLog)
                {
                    if (item.GetId == ((ChatType)this.op_listChatType.SelectedItem).GetId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    Logger.ChatFilterLog.Add((ChatType)this.op_listChatType.SelectedItem);
                }
            }
        }

        private void op_btnGoLeft_ChatMsg_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_chatFilter.SelectedItem != null)
            {
                bool exists = false;
                foreach (var item in Logger.ChatFilterLog)
                {
                    if (item.GetId == ((ChatType)this.op_chatFilter.SelectedItem).GetId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) Logger.ChatFilterLog.Remove((ChatType)this.op_chatFilter.SelectedItem);
            }
        }

        /// <summary>
        /// 번역 서비스의 목록을 변경하면 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void op_listAPIService_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Toggle_TranslationComponents("", false);
            if (((TranslateServiceList)this.op_listAPIService.SelectedItem).GetCode.Equals("naver"))
            {
                selectedAPIService = "naver";
                Toggle_TranslationComponents("naver", true);

                this.op_APIKey_Naver.Text = ((TranslateServiceList)this.op_listAPIService.SelectedItem).ClientKey;
                this.op_APISecretKey.Text = ((TranslateServiceList)this.op_listAPIService.SelectedItem).SecretKey;
            }
            else if (((TranslateServiceList)this.op_listAPIService.SelectedItem).GetCode.Equals("google"))
            {
                selectedAPIService = "google";
                Toggle_TranslationComponents("google", true);

                this.op_APIKey_Google.Text = ((TranslateServiceList)this.op_listAPIService.SelectedItem).ClientKey;
            }
        }

        /// <summary>
        /// 설정 창의 확인 버튼을 누르면 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            settFormat.selectAPIService = this.op_listAPIService.SelectedIndex;
            foreach (var item in TranslateServiceList.TypeList)
            {
                if (item.Value.GetCode.Equals("naver"))
                {
                    settFormat.naverClientKey = this.op_APIKey_Naver.Text;
                    settFormat.naverSecretKey = this.op_APISecretKey.Text;
                    settFormat.sourceLang = ((Translate)this.op_apiSourceLang_Naver.SelectedItem).GetCode;
                    settFormat.destLang = ((Translate)this.op_apiDestLang_Naver.SelectedItem).GetCode;
                }
                else if (item.Value.GetCode.Equals("google"))
                {
                    settFormat.googleAPIKey = this.op_APIKey_Google.Text;
                    settFormat.destLang = ((Translate)this.op_apiDestLang_Google.SelectedItem).GetCode;
                }
            }
            settFormat.enableTransService = (this.op_APISwitch.IsChecked.HasValue ? this.op_APISwitch.IsChecked.Value : false) ? 1 : 0;

            // 폰트
            settFormat.overlayFontName = (this.op_listFont.SelectedItem.ToString());
            settFormat.overlayFontSize = Convert.ToInt32(this.op_listFontSize.SelectedItem.ToString());
            settFormat.overlayFontStyle = (bool)(this.op_listFontStyle.IsChecked) ? "italic" : "normal";
            settFormat.overlayFontWeight = (bool)(this.op_listFontWeight.IsChecked) ? "extrabold" : "normal";
            // 폰트를 오버레이에 적용
            overlay.SetFont(settFormat.overlayFontName, settFormat.overlayFontSize, settFormat.overlayFontStyle, settFormat.overlayFontWeight);

            // 투명도
            settFormat.windowOpacity = this.op_slOpacity.Value;

            // 파일로 설정 저장
            Settings.Save(settFormat);

            this.Hide();
        }

        /// <summary>
        /// 설정 창의 취소 버튼을 누르면 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// FFXIV 프로세스의 선택 버튼을 클릭하면 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void op_btnSetProcess_Click(object sender, RoutedEventArgs e)
        {
            if (this.op_listProcess.SelectedIndex > -1)
            {
                Logger.SelectFFXIVProcess(int.Parse(((string)this.op_listProcess.SelectedItem).Split(':')[1]));
            }
            else
            {
                MessageBox.Show("프로세스를 선택해주세요!", "설정", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// FFXIV 프로세스의 재설정 버튼을 클릭하면 실행됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void op_btnResetProcess_Click(object sender, RoutedEventArgs e)
        {
            Logger.ResetFFXIVProcess();
        }

        private void Toggle_TranslationComponents(string service, bool isShow)
        {
            Visibility status = isShow ? Visibility.Visible : Visibility.Hidden;

            switch (service)
            {
                case "naver":
                    // API 입력 부분 라벨
                    this.op_APIKey_Label.Visibility = status;
                    this.op_APISecretKey_Label.Visibility = status;
                    // API 입력 부분
                    this.op_APIKey_Naver.Visibility = status;
                    this.op_APISecretKey.Visibility = status;

                    // 언어 설정 부분 라벨
                    this.op_apiLangLabel.Visibility = status;
                    this.op_apiLangArrowLabel.Visibility = status;
                    // 언어 설정 부분
                    this.op_apiSourceLang_Naver.Visibility = status;
                    this.op_apiDestLang_Naver.Visibility = status;

                    // 스위치
                    //this.op_APISwitch.Visibility = status;
                    break;
                case "google":
                    // API 입력 부분 라벨
                    this.op_APIKey_Label.Visibility = status;
                    // API 입력 부분
                    this.op_APIKey_Google.Visibility = status;

                    // 언어 설정 부분 라벨
                    this.op_apiLangLabel.Visibility = status;
                    // 언어 설정 부분
                    this.op_apiDestLang_Google.Visibility = status;

                    // 스위치
                    //this.op_APISwitch.Visibility = status;
                    break;
                default:
                    Toggle_TranslationComponents("google", false);
                    Toggle_TranslationComponents("naver", false);
                    break;
            }
        }

        private void Toggle_TranslateService(bool isShow)
        {
            Visibility status = isShow ? Visibility.Visible : Visibility.Hidden;

            this.op_APIService_Label.Visibility = status;
            this.op_listAPIService.Visibility = status;
        }

        private void op_listFont_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            settFormat.overlayFontName = (this.op_listFont.SelectedItem.ToString());
            this.op_FontStyleTestLabel.FontFamily = new System.Windows.Media.FontFamily(settFormat.overlayFontName);
            //overlay.SetFont(settFormat.overlayFontName, settFormat.overlayFontSize, settFormat.overlayFontStyle, settFormat.overlayFontWeight);
        }

        private void op_listFontSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            settFormat.overlayFontSize = Convert.ToInt32(this.op_listFontSize.SelectedItem.ToString());
            this.op_FontStyleTestLabel.FontSize = Convert.ToInt32(this.op_listFontSize.SelectedItem.ToString());
        }

        private void op_listFontStyle_Checked(object sender, RoutedEventArgs e)
        {
            settFormat.overlayFontStyle = (bool)(this.op_listFontStyle.IsChecked) ? "italic" : "normal";
            if ((bool)this.op_listFontStyle.IsChecked)
                this.op_FontStyleTestLabel.FontStyle = FontStyles.Italic;
            else
                this.op_FontStyleTestLabel.FontStyle = FontStyles.Normal;
        }

        private void op_listFontWeight_Checked(object sender, RoutedEventArgs e)
        {
            settFormat.overlayFontWeight = (bool)(this.op_listFontWeight.IsChecked) ? "extrabold" : "normal";
            if ((bool)this.op_listFontWeight.IsChecked)
                this.op_FontStyleTestLabel.FontWeight = FontWeights.ExtraBold;
            else
                this.op_FontStyleTestLabel.FontWeight = FontWeights.Normal;
        }

        private void op_APISwitch_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)this.op_APISwitch.IsChecked)
            {
                Toggle_TranslateService(true);
                Toggle_TranslationComponents(selectedAPIService, true);
            }
            else
            {
                Toggle_TranslateService(false);
                Toggle_TranslationComponents("", false);
            }
        }

        private void op_SwitchTopMost_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void op_SwitchClickThru_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
