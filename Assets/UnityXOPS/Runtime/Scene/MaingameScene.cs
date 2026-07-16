using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인게임 씬 부트스트랩 — 사람 동작을 켜고 미션 이벤트/판정을 가동한다.
    /// 플레이어가 없는 동안(스폰 전·사망 구간) 카메라를 원점에 세워두는 일도 하며, 이는 플레이어가 있을 때
    /// 카메라를 붙잡는 PlayerController.LateUpdate 와 짝을 이룬다. HUD·조준·입력·씬 전환은 maingame.lua 가 담당한다.
    /// </summary>
    public class MaingameScene : MonoBehaviour
    {
        private void Start()
        {
            HumanController.TickEnabled = true;
            EventManager.Instance.BeginMission();
        }

        private void LateUpdate()
        {
            // 씬을 떠나는 프레임엔 플레이어가 먼저 사라져 여기로 들어오는데, 그때 카메라를 루트로 올리면
            // 카메라가 DontDestroyOnLoad 쪽에 남아 다음 씬으로 샌다. SceneAPI.Load 가 전환 직전 카메라를 꺼서
            // Camera.main(켜진 카메라만 반환)이 null 이 되고 아래에서 멈추는 덕에 그 일이 없다.
            if (MapLoader.Player != null) return;

            Camera main = Camera.main;
            if (main == null) return;

            float height = DataManager.Instance.HumanParameterData.humanGeneralData.cameraAttachPosition;
            Transform tr = main.transform;

            if (tr.parent != null) tr.SetParent(null, true);
            tr.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.identity);
        }
    }
}
