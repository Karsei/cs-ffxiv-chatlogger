using System.Collections.Generic;
using System.Windows.Media;

namespace ffxiv_chatlogger
{
    internal class ChatType
    {
        /**
         * 0 시간
         * 1 분
         * 2 캐릭터 이름
         * 3 메세지
         * 4 링크셸 인덱스
         **/
        private const string ChatFormat_TxtOnly     = "[{0:00}:{1:00}] {3}";
        private const string ChatFormat_Say         = "[{0:00}:{1:00}] {2}: {3}";
        private const string ChatFormat_Party       = "[{0:00}:{1:00}] ({2}) {3}";
        private const string ChatFormat_AiParty     = "[{0:00}:{1:00}] (({2})) {3}";
        private const string ChatFormat_FC          = "[{0:00}:{1:00}] [자유부대] <{2}> {3}";
        private const string ChatFormat_Novice      = "[{0:00}:{1:00}] [초보자] <{2}> {3}";
        private const string ChatFormat_LS          = "[{0:00}:{1:00}] [{4}]<{2}> {3}";
        private const string ChatFormat_Tell_S      = "[{0:00}:{1:00}] >>{2}: {3}";
        private const string ChatFormat_Tell_R      = "[{0:00}:{1:00}] {2} >> {3}";

        public readonly static IDictionary<int, ChatType> TypeList = new Dictionary<int, ChatType>
        {
            { 0x0003, new ChatType(0x0003, ChatFormat_TxtOnly,  "#ae70f9", "시스템 일반") },
            { 0x0048, new ChatType(0x0048, ChatFormat_TxtOnly,  "#cccccc", "시스템 파티찾기") },
            { 0x2239, new ChatType(0x2239, ChatFormat_TxtOnly,  "#cccccc", "시스템 링크셸") },
            { 0x0039, new ChatType(0x0039, ChatFormat_TxtOnly,  "#cccccc", "시스템 상태") },
            { 0x003c, new ChatType(0x003c, ChatFormat_TxtOnly,  "#ff0000", "시스템 오류") },
            { 0x003d, new ChatType(0x003d, ChatFormat_Say,      "#cccccc", "시스템 이벤트") },
            { 0x0044, new ChatType(0x0044, ChatFormat_Say,      "#66a574", "NPC 이벤트") },
            { 0x000a, new ChatType(0x000a, ChatFormat_Say,      "#f7f7f7", "말하기") },
            { 0x000e, new ChatType(0x000e, ChatFormat_Party,    "#66e5ff", "파티") },
            { 0x0018, new ChatType(0x0018, ChatFormat_FC,       "#abdbe5", "자유부대") },
            { 0x001e, new ChatType(0x001e, ChatFormat_Say,      "#ffff00", "떠들기") },
            { 0x000b, new ChatType(0x000b, ChatFormat_Say,      "#ffa666", "외치기") },
            { 0x000f, new ChatType(0x000e, ChatFormat_AiParty,  "#ff7f00", "연합파티") },
            { 0x001b, new ChatType(0x001b, ChatFormat_Novice,   "#97ef98", "초보자") },
            { 0x0038, new ChatType(0x0038, ChatFormat_TxtOnly,  "#cccccc", "혼잣말") },
            { 0x000c, new ChatType(0x000c, ChatFormat_Tell_S,   "#ffb8de", "귓 보냄") },
            { 0x000d, new ChatType(0x000d, ChatFormat_Tell_R,   "#ffb8de", "귓 받음") },
            { 0x0010, new ChatType(0x0010, ChatFormat_LS,       "#d4ff7d", "링크셸 1") },
            { 0x0011, new ChatType(0x0011, ChatFormat_LS,       "#d4ff7d", "링크셸 2") },
            { 0x0012, new ChatType(0x0012, ChatFormat_LS,       "#d4ff7d", "링크셸 3") },
            { 0x0013, new ChatType(0x0013, ChatFormat_LS,       "#d4ff7d", "링크셸 4") },
            { 0x0014, new ChatType(0x0014, ChatFormat_LS,       "#d4ff7d", "링크셸 5") },
            { 0x0015, new ChatType(0x0015, ChatFormat_LS,       "#d4ff7d", "링크셸 6") },
            { 0x0016, new ChatType(0x0016, ChatFormat_LS,       "#d4ff7d", "링크셸 7") },
            { 0x0017, new ChatType(0x0017, ChatFormat_LS,       "#d4ff7d", "링크셸 8") },
            { 0x001d, new ChatType(0x001d, ChatFormat_TxtOnly,  "#bafff0", "감정표현") },
            { 0x2245, new ChatType(0x2245, ChatFormat_TxtOnly,  "#abdbe5", "자유부대 알림") },
            { 0x2246, new ChatType(0x2246, ChatFormat_TxtOnly,  "#abdbe5", "자유부대원 접속") },
            { 0x2040, new ChatType(0x2040, ChatFormat_TxtOnly,  "#edf49a", "업적달성") },
        };

        /*******************************************
         * 채팅 종류
         * 
         * @param Id                채팅 종류 번호
         * @param charFormat        채팅 메세지 형식
         * @param color             메세지 색깔
         * @param tag               메세지 이름
        ********************************************/
        public ChatType(int Id, string format, string color, string tag)
        {
            this.m_Id       = Id;
            this.m_format   = format;
            this.m_color    = color;
            this.m_tag      = tag;
        }

        private readonly int        m_Id;
        private readonly string     m_format;
        private readonly string     m_color;
        private readonly string     m_tag;

        public int      GetId       { get { return this.m_Id; } }
        public string   GetFormat   { get { return this.m_format; } }
        public string   GetColor    { get { return this.m_color; } }
        public string   GetTag      { get { return this.m_tag; } }
    }
}
