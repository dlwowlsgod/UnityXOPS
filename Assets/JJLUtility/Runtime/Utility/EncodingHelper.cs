using System.IO;
using System.Text;
using UnityEngine;

namespace JJLUtility
{
    /// <summary>
    /// 시스템 언어에 맞는 텍스트 인코딩을 제공하는 유틸리티 클래스.
    /// </summary>
    public static class EncodingHelper
    {
        /// <summary>
        /// 현재 시스템 언어에 적합한 인코딩을 반환한다.
        /// </summary>
        /// <returns>한국어: CP949, 일본어: Shift-JIS, 기타: Windows-1252 인코딩.</returns>
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
