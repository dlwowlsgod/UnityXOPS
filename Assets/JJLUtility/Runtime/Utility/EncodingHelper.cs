using System.IO;
using System.Text;
using UnityEngine;

namespace JJLUtility
{
    public static class EncodingHelper
    {
        public static Encoding GetEncoding()
        {
            var language = Application.systemLanguage;
            switch (language)
            {
                case SystemLanguage.Korean:
                    return Encoding.GetEncoding(949);
                case SystemLanguage.Japanese:
                    return Encoding.GetEncoding(932);
                default:
                    return Encoding.GetEncoding(1252);
            }
        }
    }
}
