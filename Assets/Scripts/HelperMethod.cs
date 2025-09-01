using System.IO;
using System.Text;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 헬퍼 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다. (싱글톤으로 구현할 필요는 없지만 일관성을 위함)
    /// </remarks>
    public class HelperMethod : Singleton<HelperMethod>
    {
        public Encoding DetectEncoding(string path)
        {
            if (!File.Exists(path))
            {
                return Encoding.Default;
            }
            
            var bom = new byte[4];
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                _ = file.Read(bom, 0, 4);
            }
            
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return Encoding.UTF8;
            if (bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode; //UTF-16 LE
            if (bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode; //UTF-16 BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xFE && bom[3] == 0xFF)
                return Encoding.UTF32;
            
            var encoding = ProfileManager.Instance.GetProfileValue("General", "ANSIEncoding", "kr");
            switch (encoding)
            {
                case "jp":
                    return Encoding.GetEncoding(932); // Shift-JIS
                case "kr":
                    return Encoding.GetEncoding(949); // EUC-KR
                default:
                    return Encoding.Default; // 시스템 기본
            }
        }
    }
}