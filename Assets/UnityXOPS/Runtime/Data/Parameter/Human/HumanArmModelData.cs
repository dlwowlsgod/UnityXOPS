using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 인간 팔 모델의 좌우 메시 경로를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class HumanArmModelData
    {
        public string name;
        public List<string> leftArms;
        public List<string> rightArms;
    }
}
