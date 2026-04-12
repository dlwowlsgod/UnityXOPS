using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 무기에 적용되는 공통 정확도 페널티, 반동, 손상 파라미터를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class WeaponGeneralData
    {
        public int noneWeaponIndex;
        public List<int> caseWeaponIndex;
        public int walkAccuracyPenalty;
        public int forwardAccuracyPenalty;
        public int backAccuracyPenalty;
        public int strafeAccuracyPenalty;
        public int airborneAccuracyPenalty;
        public int injuryAccuracyPenalty;
        public int injuryHpThreshold;
        public int reactionDecayPerSecond;
        public float errorAngleDegrees;
    }
}
