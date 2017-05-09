using System;
using System.Windows.Media;

namespace ffxiv_chatlogger
{
    internal class Chat
    {
        public Chat(ChatType chatType, string msg)
        {
            this.m_chatType = chatType;
            this.m_msg = msg;
        }

        private readonly ChatType m_chatType;
        private readonly string m_msg;

        public ChatType GetChatType { get { return this.m_chatType; } }
        public string GetChatMsg { get { return this.m_msg; } }
        public Brush GetChatColor { get { return CreateBrushFromHtml(this.m_chatType.GetColor); } }

        /*******************************************
         * HTML 코드로부터 브러시 생성
         * 
         * @param colorCode         색깔 코드
         * @return  색상 브러시 객체
        ********************************************/
        private static Brush CreateBrushFromHtml(string colorCode)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorCode));
        }
    }
}
