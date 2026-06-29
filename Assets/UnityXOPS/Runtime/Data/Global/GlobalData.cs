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
    }
}
