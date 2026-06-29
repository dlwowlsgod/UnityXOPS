using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 이펙트 파라미터(공용, 데이터)를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class EffectParameterData
    {
        public EffectGeneralData effectGeneralData;
        public List<EffectData> effectData;
    }
}
