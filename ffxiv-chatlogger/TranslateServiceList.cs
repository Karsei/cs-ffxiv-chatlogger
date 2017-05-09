using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffxiv_chatlogger
{
    internal class TranslateServiceList
    {
        public readonly static IDictionary<int, TranslateServiceList> TypeList = new Dictionary<int, TranslateServiceList>
        {
            { 0x0000, new TranslateServiceList(0x0000, "네이버", "naver", "", "") },
            { 0x0001, new TranslateServiceList(0x0001, "구글", "google", "", "") },
        };

        public TranslateServiceList(int id, string name, string code, string clientKey, string secretKey)
        {
            this.m_id = id;
            this.m_name = name;
            this.m_code = code;
            this.ClientKey = clientKey;
            this.SecretKey = secretKey;
        }

        private readonly int m_id;
        private readonly string m_name;
        private readonly string m_code;

        public int GetId { get { return this.m_id; } }
        public string GetName { get { return this.m_name; } }
        public string GetCode { get { return this.m_code; } }
        public string ClientKey { get; set; }
        public string SecretKey { get; set; }
    }
}
