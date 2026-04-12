using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 인간 캐릭터 파라미터(공용, 데이터, 모델)를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class HumanParameterData
    {
        public HumanGeneralData humanGeneralData;
        public List<HumanData> humanData;
        public List<HumanModelData> humanModelData;
        public List<HumanArmModelData> humanArmModelData;
        public List<HumanLegModelData> humanLegModelData;
    }
}