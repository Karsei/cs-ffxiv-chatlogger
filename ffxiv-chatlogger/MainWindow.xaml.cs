using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Interop;
using MahApps.Metro.Controls;
using WinForms = System.Windows.Forms;
using System.Windows.Media;

namespace ffxiv_chatlogger
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //private DispatcherTimer timer;

        //private IntPtr m_hookHwnd;
        //private NativeMethods.WinEventDelegate m_hookProc;

        private readonly ICollectionView m_chatList;

        private bool m_isExplicitClose = false;
        private bool m_listIsBottom = false;

        public static MainWindow Instance { get; private set; }

        /****************************************************
         * 초기
        *****************************************************/
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            // 설정 로드
            Settings.Load();

            // 배경 투명 허용 (대신 최대화할 때 이상하게 동작함)
            this.AllowsTransparency = true;
            this.Opacity = Settings.globalSetting.windowOpacity / 100;

            // 로드 후 이벤트 설정
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            // 목록 추가
            this.m_chatList = CollectionViewSource.GetDefaultView(Logger.ChatLog);
            
            // 채팅 로그 컴포넌트에 목록 삽입
            this.svChatList.ItemsSource = m_chatList;
            // 채팅 로그 폰트 설정
            SetFont(Settings.globalSetting.overlayFontName, Settings.globalSetting.overlayFontSize, Settings.globalSetting.overlayFontStyle, Settings.globalSetting.overlayFontWeight);
        }

        /****************************************************
         * 창이 열렸을 때의 이벤트
        *****************************************************/
        // http://glorymind.tistory.com/entry/WPF-Tray-%ED%8A%B8%EB%A0%88%EC%9D%B4-%EC%83%9D%EC%84%B1%ED%95%98%EA%B3%A0-%EC%A1%B0%EC%9E%91%ED%95%98%EA%B8%B0
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 프로세스 찾기 시작
            Logger.Init();

            try
            {
                // 시스템 트레이
                WinForms.ContextMenu menu = new System.Windows.Forms.ContextMenu();

                Logger.notify.DoubleClick += delegate (object senders, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
                Logger.notify.ContextMenu = menu;

                WinForms.MenuItem item_exit = new WinForms.MenuItem();
                WinForms.MenuItem item_show = new WinForms.MenuItem();
                WinForms.MenuItem item_option = new WinForms.MenuItem();
                WinForms.MenuItem item_top = new WinForms.MenuItem();
                WinForms.MenuItem item_clickthru = new WinForms.MenuItem();
                WinForms.MenuItem item_about = new WinForms.MenuItem();
                WinForms.MenuItem item_sep = new WinForms.MenuItem();
                WinForms.MenuItem item_sep2 = new WinForms.MenuItem();
                WinForms.MenuItem item_clear = new WinForms.MenuItem();

                // 항목 추가
                item_sep.Index = 1;
                item_sep.Text = "-";
                item_sep2.Index = 2;
                item_sep2.Text = "-";

                item_show.Index = 0;
                item_show.Text = "보이기/감추기";
                item_show.Click += delegate (object click, EventArgs eClick)
                {
                    if (this.Visibility == Visibility.Visible)
                    {
                        this.Visibility = Visibility.Collapsed;
                    }
                    else if (this.Visibility == Visibility.Collapsed)
                    {
                        this.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                    }
                };

                item_top.Index = 0;
                item_top.Text = "항상 위";
                item_top.Click += delegate (object click, EventArgs eClick)
                {
                    if (item_top.Checked)
                    {
                        item_top.Checked = false;
                        this.Topmost = false;
                    }
                    else
                    {
                        item_top.Checked = true;
                        this.Topmost = true;
                    }
                };

                item_clickthru.Index = 0;
                item_clickthru.Text = "클릭 무시";
                item_clickthru.Click += delegate (object click, EventArgs eClick)
                {
                    if (item_clickthru.Checked)
                    {
                        item_clickthru.Checked = false;
                        IntPtr wHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                        int winState = BaseMethod.GetWindowLong(wHandle, BaseMethod.GWL_EXSTYLE);
                        BaseMethod.SetWindowLong(wHandle, BaseMethod.GWL_EXSTYLE, winState & ~BaseMethod.WS_EX_LAYERED & ~BaseMethod.WS_EX_TRANSPARENT);
                    }
                    else
                    {
                        item_clickthru.Checked = true;
                        IntPtr wHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                        int winState = BaseMethod.GetWindowLong(wHandle, BaseMethod.GWL_EXSTYLE);
                        BaseMethod.SetWindowLong(wHandle, BaseMethod.GWL_EXSTYLE, winState | BaseMethod.WS_EX_LAYERED | BaseMethod.WS_EX_TRANSPARENT);
                    }
                };

                item_clear.Index = 0;
                item_clear.Text = "내용 비우기";
                item_clear.Click += delegate (object click, EventArgs eClick)
                {
                    lock (Logger.ChatLog)
                    {
                        Logger.ChatLog.Clear();
                        GC.Collect();
                    }
                };

                item_option.Index = 0;
                item_option.Text = "옵션";
                item_option.Click += delegate (object click, EventArgs eClick)
                {
                    var ChatOption = new ChatOption() { /* Owner = this */ };
                    ChatOption.Show();
                };

                item_about.Index = 0;
                item_about.Text = "정보";
                item_about.Click += delegate (object click, EventArgs eClick)
                {
                    var ChatAbout = new ChatAbout() {  };
                    ChatAbout.Show();
                };

                item_exit.Index = 0;
                item_exit.Text = "프로그램 종료";
                item_exit.Click += delegate (object click, EventArgs eClick)
                {
                    m_isExplicitClose = true;
                    this.Close();
                };

                // 항목 첨부!
                menu.MenuItems.Add(item_show);
                menu.MenuItems.Add(item_sep2);
                menu.MenuItems.Add(item_top);
                menu.MenuItems.Add(item_clickthru);
                menu.MenuItems.Add(item_clear);
                menu.MenuItems.Add(item_option);
                menu.MenuItems.Add(item_sep);
                menu.MenuItems.Add(item_about);
                menu.MenuItems.Add(item_exit);

                // 스래드를 사용한 알림 출현
                /*timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 2);
                timer.Tick += new EventHandler(timer_Tick);
                timer.Start();*/
                //Logger.notify.BalloonTipText = "시스템 트레이에서 이용하세요.";
                //Logger.notify.ShowBalloonTip(1000);
            }
            catch
            {
            }
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            //notify.BalloonTipTitle = "FFXIV Chat Logger";
            //notify.BalloonTipText = "시스템 트레이에서 이용할 수 있습니다!";
            //notify.ShowBalloonTip(1000);
        }

        /****************************************************
         * 폰트
        *****************************************************/
        public void SetFont(string name, int size, string style = "", string weight = "")
        {
            FontStyle setstyle;
            FontWeight setweight;

            this.svChatList.FontFamily = new FontFamily(name);
            this.svChatList.FontSize = size;
            switch (style)
            {
                case "italic":
                    setstyle = FontStyles.Italic;
                    break;
                default:
                    setstyle = FontStyles.Normal;
                    break;
            }
            this.svChatList.FontStyle = setstyle;
            switch (weight)
            {
                case "extralight":
                    setweight = FontWeights.ExtraLight;
                    break;
                case "light":
                    setweight = FontWeights.Light;
                    break;
                case "semibold":
                    setweight = FontWeights.SemiBold;
                    break;
                case "extrabold":
                    setweight = FontWeights.ExtraBold;
                    break;
                case "regular":
                    setweight = FontWeights.Regular;
                    break;
                default:
                    setweight = FontWeights.Normal;
                    break;
            }
            this.svChatList.FontWeight = setweight;
        }

        /****************************************************
         * 스크롤 이벤트
        *****************************************************/
        public void ScrollToBottom()
        {
            if (this.m_listIsBottom)
                this.svChatLog.ScrollToBottom();
        }
        private void svChatLog_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.m_listIsBottom = this.svChatLog.VerticalOffset == this.svChatLog.ScrollableHeight;
        }

        /****************************************************
         * 윈도우 상태 이벤트
        *****************************************************/
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState.Minimized.Equals(WindowState))
            {
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        /****************************************************
         * 닫기 이벤트
        *****************************************************/
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // 의도하지 않은 닫기라면 트래이로 가도록 설정
            if (m_isExplicitClose == false)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void MetroWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                // 윈도우 이동
                BaseMethod.ReleaseCapture();
                BaseMethod.SendMessage(new WindowInteropHelper(this).Handle, BaseMethod.WM_NCLBUTTONDOWN, BaseMethod.HT_CAPTION, 0);
            }
        }
    }
}
