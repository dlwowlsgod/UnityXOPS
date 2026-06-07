using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인메뉴(데모 배경) 전용 카메라. OpenXOPS gamemain.cpp:760-767 mainmenu::Process 포팅.
    /// Player인 Human을 매 프레임 따라다니되, 고정 월드 오프셋 + 고정 각도를 유지한다.
    /// maingame 카메라와 달리 충돌 처리(벽 관통 방지)가 없고, LookAt 없이 고정각을 쓴다.
    /// </summary>
    public class MainmenuCamera : MonoBehaviour
    {
        [SerializeField] private Camera menuCamera;

        // OpenXOPS gamemain.cpp:761-763 카메라 위치 오프셋 (플레이어 발 origin 기준, 원본 × 0.1 + 좌표 변환 X·Z 반전).
        // 원본 (-4, +22, -12) → Unity (+0.4, +2.2, +1.2). 플레이어 회전과 무관한 월드 오프셋.
        private static readonly Vector3 k_positionOffset = new Vector3(0.4f, 2.2f, 1.2f);

        // OpenXOPS gamemain.cpp:764-765 고정 카메라 각도. 원본 rx=45°/ry=-25° → Unity yaw 45+180=225°, pitch -(-25)=25°(아래로).
        private const float k_pitch = 25f;
        private const float k_yaw   = 225f;

        // OpenXOPS gamemain.cpp:782 VIEWANGLE_NORMAL = DegreeToRadian(65).
        private const float k_fieldOfView = 65f;

        private Human m_player;

        private void LateUpdate()
        {
            if (menuCamera == null) return;

            Human player = MapLoader.Player;
            if (player == null) return;

            if (player != m_player)
            {
                m_player = player;
                menuCamera.fieldOfView = k_fieldOfView;
            }

            menuCamera.transform.SetPositionAndRotation(
                m_player.transform.position + k_positionOffset,
                Quaternion.Euler(k_pitch, k_yaw, 0f));
        }
    }
}
