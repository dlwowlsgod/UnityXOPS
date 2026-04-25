using UnityEngine;

namespace UnityXOPS
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera    playerCamera;
        [SerializeField] private float     mouseSensitivity          = 0.1f;
        [SerializeField] private float     pitchLimit                = 70f;
        [SerializeField] private bool      firstPerson               = true;
        [SerializeField] private LayerMask thirdPersonCollisionMask  = ~0;

        // OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 상수 (원본 × 0.1)
        private const float k_thirdPersonPivotBack    = 0.30f;  // 원본 3.0f
        private const float k_thirdPersonMaxDist      = 1.40f;  // 원본 VIEW_F1MODE_DIST 14.0f
        private const float k_thirdPersonHeightBias   = 0.25f;  // 원본 2.5f
        private const float k_thirdPersonSphereRadius = 0.10f;

        private Human           m_player;
        private HumanController m_controller;

        private float m_yaw;
        private float m_pitch;

        public bool FirstPerson => firstPerson;

        /// <summary>
        /// 시점을 설정. 1인칭이면 Body/Leg 숨김 + 카메라를 CameraRoot에 부착, 3인칭이면 언패런트.
        /// </summary>
        public void SetFirstPerson(bool value)
        {
            firstPerson = value;
            ApplyViewpoint();
        }

        public void ToggleFirstPerson() => SetFirstPerson(!firstPerson);

        private void ApplyViewpoint()
        {
            if (m_player == null) return;

            HumanVisual visual = m_player.HumanVisual;
            if (visual != null) visual.SetBodyVisible(!firstPerson);

            if (playerCamera == null) return;
            if (firstPerson)
            {
                playerCamera.transform.SetParent(m_player.CameraRoot, false);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
            else
            {
                playerCamera.transform.SetParent(null, true);
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

            // 원본 OpenXOPS: F1 키로 1인칭/3인칭 시점 전환
            if (InputManager.Keyboard.f1Key.wasPressedThisFrame)
                ToggleFirstPerson();
        }

        private void LateUpdate()
        {
            if (m_player == null || playerCamera == null) return;

            if (firstPerson)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(m_pitch, 0, 0);
                return;
            }

            ApplyThirdPersonCamera();
        }

        /// <summary>
        /// OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 포팅.
        /// 플레이어 눈높이 뒤쪽으로 주시점을 잡고 최대 거리만큼 SphereCast로 벽 침투 방지.
        /// </summary>
        private void ApplyThirdPersonCamera()
        {
            float   eyeHeight = DataManager.Instance.HumanParameterData.humanGeneralData.cameraAttachPosition;
            Vector3 playerPos = m_player.transform.position;

            Quaternion viewRot  = Quaternion.Euler(m_pitch, m_yaw, 0f);
            Vector3    viewBack = viewRot * Vector3.back;
            float      pitchRad = m_pitch * Mathf.Deg2Rad;

            Vector3 pivot = playerPos;
            pivot.y += eyeHeight;
            pivot   += viewBack * k_thirdPersonPivotBack;
            pivot.y += Mathf.Sin(-pitchRad) * k_thirdPersonHeightBias;

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
