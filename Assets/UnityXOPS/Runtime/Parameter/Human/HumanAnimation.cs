using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 인간 애니메이션 프레임 인덱스와 이동 속도 정보를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class HumanAnimation
    {
        public string name;
        public List<int> index;
        public float forwardSpeed;
        public float strafeSpeed;
        public float backwardSpeed;
    }
}