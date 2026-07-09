using UnityEngine;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 전역 데이터, 버전 및 기타 정보를 담는 클래스.
    /// </summary>
    [Serializable]
    public class GlobalData
    {
        public string productName;
        public string companyName;
        public string licenseType;
        public string licenseName;
        public string[] licenseLines;
        public string versionMajor;
        public string versionMinor;
        public string versionPatch;

        // 표시용 버전 문자열(major.minor.patch). JsonUtility는 필드만 직렬화하므로 프로퍼티는 무시된다.
        public string Version => $"{versionMajor}.{versionMinor}.{versionPatch}";
    }
}
