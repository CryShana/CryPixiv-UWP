using System;
using System.Net;
using System.Text;

namespace CryPixiv2.Classes
{
    public static class Translator
    {
        public static string Translate(string text)
        {
            string source = "ja";
            string target = "en";

            string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=" + source + "&tl=" + target + "&hl=en&dt=at&dt=bd&dt=ex&dt=ld&dt=md&dt=qca&dt=rw&dt=rm&dt=ss&dt=t&ie=UTF-8&oe=UTF-8&source=bh&ssel=0&tsel=0&kc=1&q=" + System.Web.HttpUtility.UrlEncode(text);
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                try
                {
                    var content = client.DownloadString(url);

                    var part1 = content.Substring(content.IndexOf('"') + 1);
                    var result = part1.Substring(0, part1.IndexOf('"'));
                    return result;
                }
                catch (Exception ex)
                {
                    string msg = ex.GetBaseException().Message;
                    return "";
                }
            }
        }
    }
}
