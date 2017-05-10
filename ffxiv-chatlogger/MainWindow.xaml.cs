using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using WinForms = System.Windows.Forms;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MahApps.Metro.Controls;
using System.Windows.Interop;
using static ffxiv_chatlogger.Settings;

namespace ffxiv_chatlogger
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //private DispatcherTimer timer;
        private WinForms.NotifyIcon notify;
        private bool m_isExplicitClose = false;
        private IntPtr m_hookHwnd;
        private NativeMethods.WinEventDelegate m_hookProc;

        private readonly ICollectionView m_chatList;

        private bool m_listIsBottom = false;

        public static MainWindow Instance { get; private set; }
        private static Settings sett = new Settings();

        /****************************************************
         * DLL 기본
        *****************************************************/
        static class NativeMethods
        {
            public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            // 항상 위 관련
            public const int EVENT_SYSTEM_FOREGROUND = 3;
            public const int WINEVENT_OUTOFCONTEXT = 0;
            public const int WINEVENT_SKIPOWNPROCESS = 2;
            public const int HWND_TOPMOST = -1;
        }

        /****************************************************
         * 초기
        *****************************************************/
        public MainWindow()
        {
            Instance = this;

            InitializeComponent();

            // 설정 로드
            sett.Load();

            // 배경 투명 허용 (대신 최대화할 때 이상하게 동작함)
            this.AllowsTransparency = true;
            this.Opacity = Settings.globalSetting.windowOpacity / 100;

            // 로드 후 이벤트 설정
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            // 목록 추가
            this.m_chatList = CollectionViewSource.GetDefaultView(Logger.ChatLog);
            this.svChatList.ItemsSource = m_chatList;
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

                // 트레이 아이콘
                notify = new WinForms.NotifyIcon();
                notify.Icon = ffxiv_chatlogger.Properties.Resources.hp_notepad2_pencil;
                notify.Visible = true;
                notify.DoubleClick += delegate (object senders, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
                notify.ContextMenu = menu;
                notify.Text = "FFXIV Chat Logger";
                WinForms.MenuItem item_exit = new WinForms.MenuItem();
                WinForms.MenuItem item_show = new WinForms.MenuItem();
                WinForms.MenuItem item_option = new WinForms.MenuItem();
                WinForms.MenuItem item_top = new WinForms.MenuItem();
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
                        NativeMethods.UnhookWinEvent(this.m_hookHwnd);
                    }
                    else
                    {
                        item_top.Checked = true;
                        this.Topmost = true;
                        this.m_hookHwnd = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, this.m_hookProc, 0, 0, NativeMethods.WINEVENT_SKIPOWNPROCESS | NativeMethods.WINEVENT_OUTOFCONTEXT);
                    }
                };

                item_clear.Index = 0;
                item_clear.Text = "내용 비우기";
                item_clear.Click += delegate (object click, EventArgs eClick)
                {
                    Logger.ChatLog.Clear();
                };

                item_option.Index = 0;
                item_option.Text = "옵션";
                item_option.Click += delegate (object click, EventArgs eClick)
                {
                    /*this.Show();
                    this.WindowState = WindowState.Normal;*/
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
                notify.BalloonTipTitle = "FFXIV Chat Logger";
                notify.BalloonTipText = "시스템 트레이에서 이용할 수 있습니다!";
                notify.ShowBalloonTip(1000);
            }
            catch
            {
            }
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            notify.BalloonTipTitle = "FFXIV Chat Logger";
            notify.BalloonTipText = "시스템 트레이에서 이용할 수 있습니다!";
            notify.ShowBalloonTip(1000);
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
            BaseMethod.ReleaseCapture();
            BaseMethod.SendMessage(new WindowInteropHelper(this).Handle, BaseMethod.WM_NCLBUTTONDOWN, BaseMethod.HT_CAPTION, 0);
        }
    }
}
