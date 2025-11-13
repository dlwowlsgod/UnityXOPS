using System.IO;
using System.Text;

namespace UnityXOPS
{
    public static class EncodingHelper
    {
        public static string[] ReadAllLinesWithEncoding(string filePath)
        {
            if (File.Exists(filePath))
            {
                var lang = ProfileLoader.GetProfileValue("Common", "Language", "en");
                Encoding fallbackEncoding;
                switch (lang.ToLower())
                {
                    case "kr":
                        fallbackEncoding = Encoding.GetEncoding("euc-kr");
                        break;
                    case "jp":
                        fallbackEncoding = Encoding.GetEncoding("shift_jis");
                        break;
                    case "cn_s":
                        fallbackEncoding = Encoding.GetEncoding("gb2312");
                        break;
                    case "cn_t":
                        fallbackEncoding = Encoding.GetEncoding("big5");
                        break;
                    default:
                        fallbackEncoding = Encoding.Default; 
                        break;
                }
                
                byte[] fileBytes = File.ReadAllBytes(filePath);

                //try to read utf-8 (no byte order mark)
                try
                {
                    // detect bom if exists
                    var utf8Checker = new UTF8Encoding(true, true);
                    
                    return utf8Checker.GetString(fileBytes).Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                }
                catch (DecoderFallbackException)
                {
                    // if utf-8 decoding failed, use fallback encoding
                    return fallbackEncoding.GetString(fileBytes).Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                }
            }

            return new string[0];

        }
    }
}