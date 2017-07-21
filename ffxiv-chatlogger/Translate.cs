using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Services;
using Google.Apis.Translate.v2;
using TranslationsResource = Google.Apis.Translate.v2.Data.TranslationsResource;

using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections;
using System.Windows;

namespace ffxiv_chatlogger
{
    internal class Translate
    {
        public readonly static IDictionary<int, Translate> TypeList = new Dictionary<int, Translate>
        {
            { 0x0000, new Translate(0x0000, "한국어", "ko") },
            { 0x0001, new Translate(0x0001, "영어", "en") },
            { 0x0002, new Translate(0x0002, "일본어", "ja") },
            { 0x0003, new Translate(0x0003, "중국어(간체)", "zh-CN") },
        };

        public Translate(int id, string name, string code)
        {
            this.m_id = id;
            this.m_name = name;
            this.m_code = code;
        }

        private readonly int m_id;
        private readonly string m_name;
        private readonly string m_code;

        public int GetId { get { return this.m_id; } }
        public string GetName { get { return this.m_name; } }
        public string GetCode { get { return this.m_code; } }

        public static async Task<string> Run(TranslateServiceList service, string msg, string sourceLang, string targetLang)
        {
            try
            {
                if (service.GetCode.Equals("google"))
                {
                    /**************
                     * Google API를 이용하려면 사용료를 지불해야함
                    ***************/
                    var serviceGoogle = new TranslateService(new BaseClientService.Initializer()
                    {
                        ApiKey = service.ClientKey,
                        ApplicationName = "FFXIV Chat Logger"
                    });

                    var translations = new List<string>();
                    var response = await serviceGoogle.Translations.List(msg, targetLang).ExecuteAsync();

                    return response.Translations[0].TranslatedText;
                    /*foreach (TranslationsResource translation in response.Translations)
                    {
                        translations.Add(translation.TranslatedText);
                        MessageBox.Show(translation.TranslatedText);
                    }*/
                }
                else if (service.GetCode.Equals("naver"))
                {
                    string url = "https://openapi.naver.com/v1/language/translate";
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Headers.Add("X-Naver-Client-Id", service.ClientKey);
                    req.Headers.Add("X-Naver-Client-Secret", service.SecretKey);
                    req.Method = "POST";

                    byte[] byteDataParams = Encoding.UTF8.GetBytes("source=" + sourceLang + "&target=" + targetLang + "&text=" + msg);
                    req.ContentType = "application/x-www-form-urlencoded";
                    req.ContentLength = byteDataParams.Length;

                    Stream st = req.GetRequestStream();
                    st.Write(byteDataParams, 0, byteDataParams.Length);
                    st.Close();

                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                    Stream stream = res.GetResponseStream();
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                    string text = reader.ReadToEnd();
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    var result = json.Deserialize<dynamic>(text);

                    stream.Close();
                    res.Close();
                    reader.Close();

                    return result["message"]["result"]["translatedText"];
                }
            }
            catch (Exception e)
            {
                //Logger.notify.BalloonTipText = "번역에 오류가 발생했습니다. 자세한 사항은 로그를 참고하세요.";
                //Logger.notify.ShowBalloonTip(1000);
                LogWriter.Error("번역 과정에서 오류가 발생했습니다.", e);
                LogWriter.Info("클라이언트 키: " + service.ClientKey);
            }

            return "(번역 오류 발생)";
        }
    }
}
