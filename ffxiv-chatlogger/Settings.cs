using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace ffxiv_chatlogger
{
    internal class Settings
    {
        public class SettingFile
        {
            public double windowOpacity;
            public int selectAPIService;
            public string naverClientKey;
            public string naverSecretKey;
            public string googleAPIKey;
            public string sourceLang;
            public string destLang;
            //public ICollection<ChatType> chatFilter;
            //public ICollection<ChatType> transFilter;
        }

        public Settings()
        {
        }

        private const string DEFAULT_SETTING_FILE = @"\ffxiv_chatlogger_settings.json";

        public void Save(SettingFile sf)
        {
            if (File.Exists(Environment.CurrentDirectory + DEFAULT_SETTING_FILE))
                File.Delete(Environment.CurrentDirectory + DEFAULT_SETTING_FILE);

            string json = new JavaScriptSerializer().Serialize(sf);
            File.WriteAllText(Environment.CurrentDirectory + DEFAULT_SETTING_FILE, json);
        }

        public SettingFile Load()
        {
            SettingFile sf = new SettingFile();

            if (File.Exists(Environment.CurrentDirectory + DEFAULT_SETTING_FILE))
            {
                string json = File.ReadAllText(Environment.CurrentDirectory + DEFAULT_SETTING_FILE);
                sf = new JavaScriptSerializer().Deserialize<SettingFile>(json);
            }
            else
            {
                sf.windowOpacity = 100.0;
                sf.selectAPIService = 0;
                sf.naverClientKey = "";
                sf.naverSecretKey = "";
                sf.googleAPIKey = "";
                sf.sourceLang = "ko";
                sf.destLang = "ja";
                //sf.transFilter = ChatType.TypeList.Values;

                this.Save(sf);
            }
            return sf;
        }
    }
}
