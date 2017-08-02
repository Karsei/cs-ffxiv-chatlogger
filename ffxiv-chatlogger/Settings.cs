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
    internal static class Settings
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
            public int enableTransService;
            public string overlayFontName;
            public int overlayFontSize;
            public string overlayFontStyle;
            public string overlayFontWeight;
            //public ICollection<ChatType> chatFilter;
            //public ICollection<ChatType> transFilter;
        }

        private const string DEFAULT_SETTING_FILE = @"\ffxiv_chatlogger_settings.json";
        public static SettingFile globalSetting;

        public static void Save(SettingFile sf)
        {
            if (File.Exists(Environment.CurrentDirectory + DEFAULT_SETTING_FILE))
                File.Delete(Environment.CurrentDirectory + DEFAULT_SETTING_FILE);

            string json = new JavaScriptSerializer().Serialize(sf);
            File.WriteAllText(Environment.CurrentDirectory + DEFAULT_SETTING_FILE, json);

            globalSetting = sf;
        }

        public static SettingFile Load()
        {
            globalSetting = new SettingFile();

            if (File.Exists(Environment.CurrentDirectory + DEFAULT_SETTING_FILE))
            {
                string json = File.ReadAllText(Environment.CurrentDirectory + DEFAULT_SETTING_FILE);
                globalSetting = new JavaScriptSerializer().Deserialize<SettingFile>(json);
            }
            else
            {
                globalSetting.windowOpacity = 100.0;
                globalSetting.selectAPIService = 0;
                globalSetting.naverClientKey = "";
                globalSetting.naverSecretKey = "";
                globalSetting.googleAPIKey = "";
                globalSetting.sourceLang = "ko";
                globalSetting.destLang = "ja";
                globalSetting.enableTransService = 0;
                globalSetting.overlayFontName = "돋움";
                globalSetting.overlayFontSize = 12;
                globalSetting.overlayFontStyle = "normal";
                globalSetting.overlayFontWeight = "normal";
                //sf.transFilter = ChatType.TypeList.Values;

                Save(globalSetting);
            }
            return globalSetting;
        }
    }
}
