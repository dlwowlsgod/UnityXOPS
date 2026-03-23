using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 데모, 공식 미션, 어드온 미션 목록을 담는 최상위 미션 데이터 클래스.
    /// </summary>
    [Serializable]
    public class MissionData
    {
        public List<DemoData> demoData;
        public List<OfficialMissionData> officialMissions;
#if !UNITY_EDITOR
        [NonSerialized] 
#endif
        public List<AddonMissionData> addonMissions;
    }
}