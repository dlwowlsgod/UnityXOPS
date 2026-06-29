using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 인간 다리 모델의 메시 경로를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class HumanLegModelData
    {
        public string name;
        public List<string> legs;
    }
}
