using System;

namespace UnityXOPS
{
    /// <summary>
    /// 어드온 미션의 이름과 .mif 파일 경로를 담는 데이터 클래스.
    /// </summary>
    [Serializable]
    public class AddonMissionData
    {
        public string name;
        public string mifPath;
    }
}