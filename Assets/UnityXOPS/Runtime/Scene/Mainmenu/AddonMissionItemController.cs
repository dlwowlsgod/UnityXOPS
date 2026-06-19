using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 애드온 미션 목록을 표시하고 선택 시 로드하는 컨트롤러.
    /// </summary>
    public class AddonMissionItemController : MissionItemController<AddonMissionData>
    {
        protected override List<AddonMissionData> LoadData() => DataManager.Instance.MissionData.addonMissions;

        protected override int ScrollIndexState
        {
            get => MainmenuScene.AddonScrollIndex;
            set => MainmenuScene.AddonScrollIndex = value;
        }

        protected override bool IsAddon => true;
    }
}
