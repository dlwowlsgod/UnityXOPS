using System;

namespace UnityXOPS
{
    /// <summary>
    /// 메인메뉴 배경에서 사용되는 데모 맵의 BD1, PD1 경로와 스카이 인덱스를 담는 데이터 클래스.
    /// </summary>
    [Serializable]
    public class DemoData
    {
        public string bd1Path;
        public string pd1Path;
        public int skyIndex;
    }
}
