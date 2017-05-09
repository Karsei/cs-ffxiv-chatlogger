using System;
using System.IO;
using System.Text;
using System.Windows;

namespace ffxiv_chatlogger
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private readonly StreamWriter m_out;

        public App()
        {
            Console.SetOut(m_out = new StreamWriter("chat.log", true, Encoding.UTF8) { AutoFlush = true });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this.m_out.Flush();
            this.m_out.Close();
        }
    }
}
