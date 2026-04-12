using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// PD1 파일에서 파싱된 포인트 데이터를 담는 컨테이너 클래스.
    /// </summary>
    public class RawPointData
    {
        public Vector3 position;
        public float look;
        public int param0;
        public int param1;
        public int param2;
        public int param3;
    }
}
