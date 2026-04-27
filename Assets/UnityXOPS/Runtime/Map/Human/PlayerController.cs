using UnityEngine;

namespace UnityXOPS
{
    public enum ViewMode
    {
        FirstPerson      = 0,
        ThirdPersonRight = 1,
        ThirdPersonLeft  = 2,
    }

    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera    playerCamera;
        [SerializeField] private float     mouseSensitivity         = 0.1f;
        [SerializeField] private float     pitchLimit               = 70f;
        [SerializeField] private ViewMode  viewMode                 = ViewMode.FirstPerson;
        [SerializeField] private LayerMask thirdPersonCollisionMask = ~0;

        // OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 상수 (원본 × 0.1)
        private const float k_thirdPersonPivotBack      = 0.30f;  // 원본 3.0f
        private const float k_thirdPersonMaxDist        = 1.40f;  // 원본 VIEW_F1MODE_DIST 14.0f
        private const float k_thirdPersonHeightBias     = 0.25f;  // 원본 2.5f
        private const float k_thirdPersonSphereRadius   = 0.10f;
        private const float k_thirdPersonShoulderOffset = 0.40f;  // UnityXOPS 추가: over-the-shoulder 가로 오프셋

        private Human           m_player;
        private HumanController m_controller;

        private float m_yaw;
        private float m_pitch;

        public ViewMode ViewMode => viewMode;
        public bool     FirstPerson => viewMode == ViewMode.FirstPerson;

        /// <summary>
        /// 시점을 직접 설정. Body/Leg 가시성과 카메라 부모를 즉시 반영.
        /// </summary>
        public void SetViewMode(ViewMode value)
        {
            viewMode = value;
            ApplyViewpoint();
        }

        /// <summary>
        /// 1인칭 → 3인칭 우 → 3인칭 좌 → 1인칭 순서로 사이클 (F1 키 기본).
        /// </summary>
        public void CycleViewMode()
        {
            ViewMode next = viewMode switch
            {
                ViewMode.FirstPerson      => ViewMode.ThirdPersonRight,
                ViewMode.ThirdPersonRight => ViewMode.ThirdPersonLeft,
                _                         => ViewMode.FirstPerson,
            };
            SetViewMode(next);
        }

        private void ApplyViewpoint()
        {
            if (m_player == null) return;

            bool isFirstPerson = viewMode == ViewMode.FirstPerson;

            HumanVisual visual = m_player.HumanVisual;
            if (visual != null) visual.SetBodyVisible(!isFirstPerson);

            if (playerCamera == null) return;

            // 카메라는 항상 CameraRoot 자식으로 유지 (씬 언로드 시 함께 파괴되도록).
            // 3인칭은 LateUpdate에서 world TRS를 직접 덮어쓰므로 부모 좌표는 영향 없음.
            if (playerCamera.transform.parent != m_player.CameraRoot)
                playerCamera.transform.SetParent(m_player.CameraRoot, false);

            if (isFirstPerson)
            {
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            if (!TryAcquirePlayer()) return;

            var input = InputManager.Instance;

            Vector2 look = input.Look.ReadValue<Vector2>();
            m_yaw   += look.x * mouseSensitivity;
            m_pitch -= look.y * mouseSensitivity;
            m_pitch  = Mathf.Clamp(m_pitch, -pitchLimit, pitchLimit);
            m_controller.SetYawPitch(m_yaw, m_pitch);

            Vector2 move = input.Move.ReadValue<Vector2>();
            if (move.y > 0f) m_controller.SetMoveFlag(HumanMoveFlag.Forward);
            if (move.y < 0f) m_controller.SetMoveFlag(HumanMoveFlag.Back);
            if (move.x < 0f) m_controller.SetMoveFlag(HumanMoveFlag.Left);
            if (move.x > 0f) m_controller.SetMoveFlag(HumanMoveFlag.Right);

            if (input.Walk.IsPressed())           m_controller.SetMoveFlag(HumanMoveFlag.Walk);
            if (input.Jump.WasPressedThisFrame()) m_controller.SetMoveFlag(HumanMoveFlag.Jump);

            // 무기 슬롯 직접 선택: First → 보조(0), Second → 주(1)
            if (input.First .WasPressedThisFrame()) m_player.SetSelectWeapon(0);
            if (input.Second.WasPressedThisFrame()) m_player.SetSelectWeapon(1);

            if (input.Drop    .WasPressedThisFrame()) m_player.DropCurrentWeapon();
            if (input.Previous.WasPressedThisFrame()) m_player.SwitchWeaponPrevious();
            if (input.Next    .WasPressedThisFrame()) m_player.SwitchWeaponNext();

            // F1: 1인칭 → 3인칭 우 → 3인칭 좌 → 1인칭 사이클
            if (InputManager.Keyboard.f1Key.wasPressedThisFrame)
                CycleViewMode();
        }

        private void LateUpdate()
        {
            if (m_player == null || playerCamera == null) return;

            if (viewMode == ViewMode.FirstPerson)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(m_pitch, 0, 0);
                return;
            }

            ApplyThirdPersonCamera();
        }

        /// <summary>
        /// OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 + UnityXOPS over-the-shoulder 오프셋.
        /// 플레이어 눈높이 뒤쪽으로 주시점을 잡고 좌/우 어깨로 이동시킨 뒤 SphereCast로 벽 침투 방지.
        /// </summary>
        private void ApplyThirdPersonCamera()
        {
            float   eyeHeight = DataManager.Instance.HumanParameterData.humanGeneralData.cameraAttachPosition;
            Vector3 playerPos = m_player.transform.position;

            Quaternion viewRot   = Quaternion.Euler(m_pitch, m_yaw, 0f);
            Vector3    viewBack  = viewRot * Vector3.back;
            Vector3    viewRight = viewRot * Vector3.right;
            float      pitchRad  = m_pitch * Mathf.Deg2Rad;

            float shoulderSign = viewMode == ViewMode.ThirdPersonLeft ? -1f : 1f;

            Vector3 pivot = playerPos;
            pivot.y += eyeHeight;
            pivot   += viewBack  * k_thirdPersonPivotBack;
            pivot.y += Mathf.Sin(-pitchRad) * k_thirdPersonHeightBias;
            pivot   += viewRight * (k_thirdPersonShoulderOffset * shoulderSign);

            float dist = k_thirdPersonMaxDist;
            if (Physics.SphereCast(pivot, k_thirdPersonSphereRadius, viewBack,
                                   out RaycastHit hit, k_thirdPersonMaxDist, thirdPersonCollisionMask))
            {
                dist = hit.distance;
            }

            Vector3 cameraPos = pivot + viewBack * dist;
            playerCamera.transform.position = cameraPos;
            playerCamera.transform.rotation = Quaternion.LookRotation(pivot - cameraPos, Vector3.up);
        }

        private bool TryAcquirePlayer()
        {
            Human player = MapLoader.Player;
            if (player == null) return false;

            if (player != m_player)
            {
                m_player     = player;
                m_controller = player.GetComponent<HumanController>();
                m_yaw        = player.transform.eulerAngles.y;
                m_pitch      = 0f;
                m_controller.SetYawPitch(m_yaw, m_pitch);

                ApplyViewpoint();
            }
            return m_controller != null;
        }
    }
}
