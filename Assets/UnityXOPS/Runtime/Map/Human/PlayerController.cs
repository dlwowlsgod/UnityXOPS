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
        [SerializeField] private float     pitchLimit               = 70f;
        [SerializeField] private ViewMode  viewMode                 = ViewMode.FirstPerson;
        [SerializeField] private LayerMask thirdPersonCollisionMask = ~0;
        [SerializeField] private LayerMask aimMask                  = ~0;   // 조준 표적점 ray 대상 (블록/사람/소품). 자기 자신 hitbox 는 코드에서 제외.
        [SerializeField] private MaingameUIDynamicLayout uiLayout;          // F4 로 Normal ↔ Simple UI 토글

        // 카메라 중앙 ray 가 아무것도 안 맞을 때 쓰는 먼 표적 거리.
        private const float k_aimRayMaxDist = 1000f;

        // OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 상수 (원본 × 0.1)
        private const float k_thirdPersonPivotBack      = 0.30f;  // 원본 3.0f
        private const float k_thirdPersonMaxDist        = 1.40f;  // 원본 VIEW_F1MODE_DIST 14.0f
        private const float k_thirdPersonHeightBias     = 0.25f;  // 원본 2.5f
        private const float k_thirdPersonSphereRadius   = 0.10f;
        private const float k_thirdPersonShoulderOffset = 0.40f;  // UnityXOPS 추가: over-the-shoulder 가로 오프셋

        // OpenXOPS gamemain.cpp:2633-2647 사망 카메라 상수 (원본 33.33fps 기준 → 시간 기반 변환).
        private const float k_deathCamFps             = 33.3333f;
        private const float k_deathCamYawRate         = 1f * k_deathCamFps; // 1°/frame × 33.33fps = 33.33 deg/s orbit yaw
        private const float k_deathCamPitchTarget     = 89f;     // 원본 -89° down → Unity는 +X 회전이 down
        private const float k_deathCamPitchBlendBase  = 0.95f;   // 매 프레임 95% 유지 → 시간 기반 1 - 0.95^(dt × 33.33)
        private const float k_deathCamRadius          = 0.312f;  // 원본 r = 3.12 × 0.1
        private const float k_deathCamHeight          = 3.33f;   // 원본 33.3 × 0.1

        private Human           m_player;
        private HumanController m_controller;

        // 사용자가 고른 시점(F1/F2/F3). 스코프 조준 중에는 viewMode 가 1인칭으로 강제되지만 이 값은 유지 → 해제 시 복원.
        private ViewMode m_desiredViewMode;

        private void Start() => m_desiredViewMode = viewMode;

        private float m_yaw;
        private float m_pitch;

        // 씬 전환 입력 누수 방지 — 조작권 획득 후 Fire 를 한 번 떼야 발사 허용.
        // 브리핑을 닫은 좌클릭(BriefingScene 의 leftButton)이 Maingame 첫 프레임 Fire 로 새는 것 차단.
        private bool m_fireReady;

        private float m_deathCamYaw;
        private float m_deathCamPitch;
        private bool  m_deathCamInitialized;

        public ViewMode ViewMode => viewMode;
        public bool     FirstPerson => viewMode == ViewMode.FirstPerson;

        /// <summary>
        /// 사용자 시점 선택(F1/F2/F3). 스코프 조준 중이면 1인칭이 유지되고 이 선택은 해제 후 복원된다.
        /// </summary>
        public void SetViewMode(ViewMode value)
        {
            m_desiredViewMode = value;
            ResolveViewMode();
        }

        /// <summary>
        /// 실제 적용 시점 결정 — 스코프 조준 중이면 1인칭 강제, 아니면 사용자 선택(m_desiredViewMode). 변경 시에만 ApplyViewpoint.
        /// </summary>
        private void ResolveViewMode()
        {
            ViewMode target = (m_player != null && m_player.IsScoping) ? ViewMode.FirstPerson : m_desiredViewMode;
            if (target == viewMode) return;
            viewMode = target;
            ApplyViewpoint();
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

            // UI 토글은 카메라(사망 시 Death Camera로 전환)와 무관하므로 사망 중에도 허용.
            // F4 = Normal UI ↔ Simple UI 토글
            if (uiLayout != null && InputManager.Keyboard.f4Key.wasPressedThisFrame) uiLayout.ToggleUIMode();

            // 사망 시 모든 입력 무시 (이동/회전/무기 전환/시점 사이클).
            // 카메라는 LateUpdate가 별도 분기로 ApplyDeathCamera 처리.
            if (!m_player.Alive) return;

            var input = InputManager.Instance;

            float sensitivity = ConfigManager.Instance.MouseSensitivity; // 외부 config(0~1) 감도
            Vector2 look = input.Look.ReadValue<Vector2>();
            m_yaw   += look.x * sensitivity;
            m_pitch -= look.y * sensitivity;
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
            if (input.Reload  .WasPressedThisFrame()) m_player.ReloadCurrentWeapon();

            // 발사 — burstMode 에 따라 입력 폴링 방식 분기. FullAuto = 누르고 있으면 연사, 그 외 = 1회 입력당 1발.
            // Weapon.Shoot 자체가 fireRate 쿨다운으로 연사 속도 제한.
            // Fire 를 떼면 발사 무장. 조작권 획득 직후엔 비무장(m_fireReady=false)이라, 브리핑에서 넘어온 눌림은 무시되고 한 번 뗀 뒤부터 발사된다.
            if (!input.Fire.IsPressed()) m_fireReady = true;

            Weapon currentWeapon = m_player.CurrentWeapon;
            if (m_fireReady && currentWeapon != null && currentWeapon.WeaponData != null)
            {
                bool fire = currentWeapon.WeaponData.burstMode == WeaponBurstMode.FullAuto
                          ? input.Fire.IsPressed()
                          : input.Fire.WasPressedThisFrame();
                if (fire)
                {
                    UpdateAimPoint();   // 발사 직전 카메라 중앙 표적점 갱신 → 총알이 크로스헤어로 수렴
                    currentWeapon.Shoot(m_player);
                }
            }

            // 발사 시 무기가 컨트롤러 시점각에 누적한 에임 킥을 마우스 누적값으로 되읽어 영구 반영.
            // 원본 OpenXOPS gamemain.cpp:2240-2245 — ShotWeapon 후 GetRxRy 역동기화. 킥이 없으면 무변화(no-op).
            m_yaw   = m_controller.Yaw;
            m_pitch = Mathf.Clamp(m_controller.Pitch, -pitchLimit, pitchLimit);
        }

        private void LateUpdate()
        {
            if (m_player == null || playerCamera == null) return;

            if (!m_player.Alive)
            {
                ApplyDeathCamera();
                return;
            }

            // 부활 시 사망 카메라 종료 후처리: viewMode에 맞춰 body 가시성 복원.
            if (m_deathCamInitialized)
            {
                HumanVisual visual = m_player.HumanVisual;
                if (visual != null) visual.SetBodyVisible(viewMode != ViewMode.FirstPerson);
                m_deathCamInitialized = false;
            }

            // 스코프 조준 상태에 맞춰 시점 동기화 (스코프 ON→1인칭 강제, OFF→사용자 선택 복원). 토글은 별도 스크립트라 매 프레임 폴링.
            ResolveViewMode();

            if (viewMode == ViewMode.FirstPerson)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(m_pitch, 0, 0);
                return;
            }

            ApplyThirdPersonCamera();
        }

        /// <summary>
        /// OpenXOPS gamemain.cpp:2633-2647 사망 카메라 포팅.
        /// 플레이어 위쪽 일정 높이에서 일정 반경으로 orbit yaw 회전, pitch는 89° (정수 down)로 지수 보간.
        /// 첫 프레임은 현재 view yaw/pitch에서 시작해 매끄럽게 전환.
        /// </summary>
        private void ApplyDeathCamera()
        {
            if (!m_deathCamInitialized)
            {
                m_deathCamYaw         = m_yaw;
                m_deathCamPitch       = m_pitch;
                m_deathCamInitialized = true;

                // 사망 카메라는 3인칭 뷰이므로 1인칭이었더라도 body/leg를 강제 표시.
                HumanVisual visual = m_player.HumanVisual;
                if (visual != null) visual.SetBodyVisible(true);
            }

            float dt = Time.deltaTime;

            m_deathCamYaw  += k_deathCamYawRate * dt;
            float blend     = 1f - Mathf.Pow(k_deathCamPitchBlendBase, dt * k_deathCamFps);
            m_deathCamPitch = Mathf.Lerp(m_deathCamPitch, k_deathCamPitchTarget, blend);

            Vector3 playerPos = m_player.transform.position;
            float   yawRad    = m_deathCamYaw * Mathf.Deg2Rad;

            // Unity yaw 0 = +Z, yaw 90 = +X 이므로 X = sin, Z = cos 으로 orbit.
            Vector3 cameraPos = new Vector3(
                playerPos.x + Mathf.Sin(yawRad) * k_deathCamRadius,
                playerPos.y + k_deathCamHeight,
                playerPos.z + Mathf.Cos(yawRad) * k_deathCamRadius
            );

            playerCamera.transform.position = cameraPos;
            playerCamera.transform.rotation = Quaternion.Euler(m_deathCamPitch, m_deathCamYaw, 0f);
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

        /// <summary>
        /// 카메라 중앙(크로스헤어)이 실제로 닿는 월드 표적점을 구해 플레이어에 주입.
        /// 총알이 총구에서 이 점으로 향하면 3인칭 어깨 오프셋과 무관하게 크로스헤어에 명중한다.
        /// 어깨 너머 자기 몸이 ray 에 걸리면 건너뛰고 그 너머를 표적으로 잡는다.
        /// </summary>
        private void UpdateAimPoint()
        {
            if (playerCamera == null) return;

            Vector3 origin = playerCamera.transform.position;
            Vector3 fwd    = playerCamera.transform.forward;
            Vector3 target = origin + fwd * k_aimRayMaxDist;

            RaycastHit[] hits = Physics.RaycastAll(origin, fwd, k_aimRayMaxDist, aimMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                // 자기 자신(플레이어) 콜라이더는 통과 — 어깨 너머 화면에 잡힌 자기 몸을 표적으로 삼지 않도록.
                HumanHitbox hb = hits[i].collider.GetComponent<HumanHitbox>();
                if (hb != null && hb.Human == m_player) continue;
                target = hits[i].point;
                break;
            }

            m_player.SetAimPoint(target);
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
                m_fireReady  = false; // 조작권 획득 시 비무장 — Fire 를 한 번 떼야 발사 (씬 전환 클릭 누수 차단)
                m_controller.SetYawPitch(m_yaw, m_pitch);

                ApplyViewpoint();
            }
            return m_controller != null;
        }
    }
}
