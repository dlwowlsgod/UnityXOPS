using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 공식 미션 목록을 표시하고 선택 시 로드하는 컨트롤러.
    /// </summary>
    public class OfficialMissionItemController : MissionItemController<OfficialMissionData>
    {
        protected override List<OfficialMissionData> LoadData() => DataManager.Instance.MissionData.officialMissions;

        protected override int ScrollIndexState
        {
            get => MainmenuScene.OfficialScrollIndex;
            set => MainmenuScene.OfficialScrollIndex = value;
        }

        protected override bool IsAddon => false;
    }
}
