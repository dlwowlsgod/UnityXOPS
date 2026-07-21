using UnityEngine;

namespace UnityXOPS
{
    public enum ViewMode
    {
        FirstPerson = 0,
        ThirdPerson = 1,
    }

    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float pitchLimit = 70f;
        [SerializeField] private ViewMode viewMode = ViewMode.FirstPerson;
        [SerializeField] private LayerMask thirdPersonCollisionMask = ~0;

        // OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 상수 (원본 × 0.1)
        private const float k_thirdPersonPivotBack = 0.30f; // 원본 3.0f
        private const float k_thirdPersonMaxDist = 1.40f; // 원본 VIEW_F1MODE_DIST 14.0f
        private const float k_thirdPersonHeightBias = 0.25f; // 원본 2.5f
        private const float k_thirdPersonSphereRadius = 0.10f;
        private const float k_thirdPersonPivotHeight = 1.95f; // 원본 HUMAN_HEIGHT(20.0)-0.5=19.5 × 0.1 (1인칭 eye 1.9 와 별개)
        private const float k_thirdPersonInitialPitch = 22.5f; // 원본 VIEW_F1MODE_ANGLE (3인칭 진입 시 살짝 내려다봄)
        private const float k_thirdPersonNumpadDegPerFrame = 2f; // 원본 INPUT_F1NUMKEYS_ANGLE 프레임당 2°
        private const float k_thirdPersonSmoothBase = 0.8f; // 원본 카메라각 8:2 관성 (프레임당 목표각 20% 접근)
        private const float k_referenceFps = 33.3333f; // 원본 프레임레이트 (관성/넘버패드 프레임독립 변환)

        // 치트(F9) 복제 스폰 오프셋 — 원본 gamemain.cpp:2436-2438 (정면 10.0 / 위 5.0) × 0.1.
        private const float k_cloneFrontDist = 1.0f;
        private const float k_cloneUpOffset = 0.5f;

        // OpenXOPS gamemain.cpp:2633-2647 사망 카메라 상수 (원본 33.33fps 기준 → 시간 기반 변환).
        private const float k_deathCamFps = 33.3333f;
        private const float k_deathCamYawRate = 1f * k_deathCamFps; // 1°/frame × 33.33fps = 33.33 deg/s orbit yaw
        private const float k_deathCamPitchTarget = 89f; // 원본 -89° down → Unity는 +X 회전이 down
        private const float k_deathCamPitchBlendBase = 0.95f; // 매 프레임 95% 유지 → 시간 기반 1 - 0.95^(dt × 33.33)
        private const float k_deathCamRadius = 0.312f; // 원본 r = 3.12 × 0.1
        private const float k_deathCamHeight = 3.33f; // 원본 33.3 × 0.1

        private Human m_player;
        private HumanController m_controller;

        // 3인칭 카메라 시선 오프셋 (넘버패드 조정, 마우스 시선 위에 얹힘). 원본 view_rx/view_ry.
        private float m_viewYawOffset;
        private float m_viewPitchOffset;

        // 8:2 관성이 적용된 실제 카메라 궤도각. 원본 camera_rx/camera_ry.
        private float m_camYaw;
        private float m_camPitch;

        private float m_yaw;
        private float m_pitch;

        // 씬 전환 입력 누수 방지 — 조작권 획득 후 Fire 를 한 번 떼야 발사 허용.
        // 브리핑을 닫은 좌클릭(briefing.lua 의 fire)이 Maingame 첫 프레임 Fire 로 새는 것 차단.
        private bool m_fireReady;

        private float m_deathCamYaw;
        private float m_deathCamPitch;
        private bool m_deathCamInitialized;

        public ViewMode ViewMode => viewMode;
        public bool FirstPerson => viewMode == ViewMode.FirstPerson;

        /// <summary>
        /// 1인칭 ↔ 3인칭 시점을 토글한다 (원본 F1 키, gamemain.cpp:2293-2304).
        /// 3인칭 진입 시 시선 오프셋을 초기화(pitch = 내려다보는 각)하고 관성 카메라각을 목표로 스냅한다.
        /// </summary>
        public void ToggleViewMode()
        {
            viewMode = viewMode == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson;
            m_viewYawOffset = 0f;
            m_viewPitchOffset = viewMode == ViewMode.ThirdPerson ? k_thirdPersonInitialPitch : 0f;
            // 관성 카메라각은 오프셋을 뺀 현재 시선에서 출발 → LateUpdate 스무딩이 3인칭 시선각까지 8:2 로 이즈인.
            m_camYaw = m_yaw;
            m_camPitch = m_pitch;
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

            // 치트 F5 — F5+Enter 동시 홀드 중 강제 상승(수직 관통). 원본 gamemain.cpp:2326-2332 (CheckKeyNow AND, hold 방식).
            // 매 프레임 평가해 사망/해제 시 즉시 중지 (사망 시 false → 시체가 계속 떠오르는 것 방지).
            var kb = InputManager.Keyboard;
            bool cheatRise = m_player.Alive && kb != null && kb.f5Key.isPressed && kb.enterKey.isPressed;
            m_controller.SetCheatRise(cheatRise);

            // 치트 F8 — F8 홀드 + ←/→ 엣지로 Player Human 을 이전/다음으로 교체 (원본 gamemain.cpp:2367-2408).
            // Event/Path 는 건드리지 않고 MapLoader.Player 만 스왑. 사망 중에도 동작(다른 Human 빙의).
            // ← = 인덱스 증가, → = 감소 — 원본(←=HumanID+1) 과 동일.
            if (kb != null && kb.f8Key.isPressed)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) CyclePlayer(1);
                else if (kb.rightArrowKey.wasPressedThisFrame) CyclePlayer(-1);
            }

            // 치트 F9 — F9 홀드 + ↑/↓ 엣지로 내 복제 캐릭터 생성. ↑=따라오기, ↓=제자리 경계(기본). 원본 gamemain.cpp:2411-2455.
            // HP·탄약은 복사 안 하고 기본값(최대 HP / 기본 탄약), 종류·팀·현재 무기 종류·활성 슬롯만 복사.
            if (kb != null && kb.f9Key.isPressed)
            {
                if (kb.upArrowKey.wasPressedThisFrame) SpawnClone(Human.CloneAIMode.Follow);
                else if (kb.downArrowKey.wasPressedThisFrame) SpawnClone(Human.CloneAIMode.Guard);
            }

            // 사망 시 모든 입력 무시 (이동/회전/무기 전환/시점 사이클).
            // 카메라는 LateUpdate가 별도 분기로 ApplyDeathCamera 처리.
            if (!m_player.Alive) return;

            // 치트 F6 — F6 홀드 + Enter 엣지(누르는 순간 1회)로 현재 무기 예비탄에 장탄수만큼 추가.
            // 원본 gamemain.cpp:2337-2341 (CheckKeyNow(F6) + CheckKeyDown(Enter), HP>0). Enter 엣지라 홀드해도 반복 안 됨.
            if (kb != null && kb.f6Key.isPressed && kb.enterKey.wasPressedThisFrame)
                m_player.CurrentWeapon?.CheatAddMagazine();

            // 치트 F7 — F7 홀드 + ←/→ 엣지로 현재 슬롯 무기를 이전/다음 무기 종류로 강제 교체 (원본 gamemain.cpp:2344-2363, HP>0).
            // 현재 탄약 유지, parameter 인덱스 순환. ← = 인덱스 증가(0→1→2), → = 감소 — 원본(←=id_param+1) 과 동일.
            if (kb != null && kb.f7Key.isPressed)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) m_player.CheatCycleWeapon(1);
                else if (kb.rightArrowKey.wasPressedThisFrame) m_player.CheatCycleWeapon(-1);
            }

            var input = InputManager.Instance;

            float sensitivity = ConfigManager.Instance.MouseSensitivity; // 외부 config(0~1) 감도
            float invertY = ConfigManager.Instance.InvertY ? -1f : 1f; // 상하 반전 옵션(캐시)
            Vector2 look = input.Look.ReadValue<Vector2>();
            m_yaw += look.x * sensitivity;
            m_pitch -= look.y * sensitivity * invertY;
            m_pitch = Mathf.Clamp(m_pitch, -pitchLimit, pitchLimit);

            // 3인칭 카메라 시선 오프셋 — 넘버패드로 상하좌우 궤도 조정 (원본 gamemain.cpp:2307-2320, 홀드 시 프레임당 2°).
            if (viewMode == ViewMode.ThirdPerson)
            {
                float step = k_thirdPersonNumpadDegPerFrame * Time.deltaTime * k_referenceFps;
                if (kb != null)
                {
                    if (kb.numpad8Key.isPressed) m_viewPitchOffset += step; // 위로
                    if (kb.numpad5Key.isPressed) m_viewPitchOffset -= step; // 아래로
                    if (kb.numpad4Key.isPressed) m_viewYawOffset -= step;   // 좌 궤도
                    if (kb.numpad6Key.isPressed) m_viewYawOffset += step;   // 우 궤도
                }
            }

            Vector2 move = input.Move.ReadValue<Vector2>();
            HumanMoveFlag moveFlag = HumanMoveFlag.None;
            if (move.y > 0f) moveFlag |= HumanMoveFlag.Forward;
            if (move.y < 0f) moveFlag |= HumanMoveFlag.Back;
            if (move.x < 0f) moveFlag |= HumanMoveFlag.Left;
            if (move.x > 0f) moveFlag |= HumanMoveFlag.Right;
            if (input.Walk.IsPressed()) moveFlag |= HumanMoveFlag.Walk;
            if (input.Jump.WasPressedThisFrame()) moveFlag |= HumanMoveFlag.Jump;

            // 무기 액션 의도 — 직접 호출 대신 플래그로 모아 Human 이 소비. 슬롯선택 First→보조(0)/Second→주(1).
            HumanWeaponAction weapon = HumanWeaponAction.None;
            if (input.First.WasPressedThisFrame()) weapon |= HumanWeaponAction.SelectFirst;
            if (input.Second.WasPressedThisFrame()) weapon |= HumanWeaponAction.SelectSecond;
            if (input.Drop.WasPressedThisFrame()) weapon |= HumanWeaponAction.Drop;
            if (input.Previous.WasPressedThisFrame()) weapon |= HumanWeaponAction.SwitchPrevious;
            if (input.Next.WasPressedThisFrame()) weapon |= HumanWeaponAction.SwitchNext;
            if (input.Reload.WasPressedThisFrame()) weapon |= HumanWeaponAction.Reload;

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
                if (fire) weapon |= HumanWeaponAction.Fire;
            }

            // 한 틱 입력을 단일 구조체로 구성 — 이동/조준은 컨트롤러가, 무기 액션은 Human 이 소비.
            // 발사가 컨트롤러 조준을 읽으므로 조준 주입(SetInput)이 무기 소비(ApplyWeaponInput)보다 먼저다.
            // currentWeapon 은 전환 적용(ApplyWeaponInput) 이전 값이라 pre-switch burstMode 로 판정하지만,
            // 같은 프레임 전환은 IsChanging 으로 발사가 막히고 noneWeapon 은 fireRate<=0 no-op 이라 관측 무해.
            var frameInput = new HumanInput { moveFlag = moveFlag, yaw = m_yaw, pitch = m_pitch, weapon = weapon };
            m_controller.SetInput(in frameInput);
            m_player.ApplyWeaponInput(in frameInput);

            // 발사 시 무기가 컨트롤러 시점각에 누적한 에임 킥을 마우스 누적값으로 되읽어 영구 반영.
            // 원본 OpenXOPS gamemain.cpp:2240-2245 — ShotWeapon 후 GetRxRy 역동기화. 킥이 없으면 무변화(no-op).
            m_yaw = m_controller.Yaw;
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
                m_deathCamYaw = m_yaw;
                m_deathCamPitch = m_pitch;
                m_deathCamInitialized = true;

                // 사망 카메라는 3인칭 뷰이므로 1인칭이었더라도 body/leg를 강제 표시.
                HumanVisual visual = m_player.HumanVisual;
                if (visual != null) visual.SetBodyVisible(true);
            }

            float dt = Time.deltaTime;

            m_deathCamYaw += k_deathCamYawRate * dt;
            float blend = 1f - Mathf.Pow(k_deathCamPitchBlendBase, dt * k_deathCamFps);
            m_deathCamPitch = Mathf.Lerp(m_deathCamPitch, k_deathCamPitchTarget, blend);

            Vector3 playerPos = m_player.transform.position;
            float yawRad = m_deathCamYaw * Mathf.Deg2Rad;

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
        /// OpenXOPS gamemain.cpp:2651-2678 외부 3인칭 공식 (정중앙 뒤, 어깨 오프셋 없음).
        /// 마우스 시선 + 넘버패드 오프셋을 목표각으로 8:2 관성 접근(원본 camera_rx/ry 스무딩) 후,
        /// 원본 주시점(注視点) = 수평 sin(pitch)×3.0 앞 + 수직 base 19.5 + cos(pitch)×2.5 (전부 ×0.1) 을 잡고,
        /// 그 뒤로 시선 반대방향 dist 만큼 카메라를 배치. SphereCast(radius 0.10)로 원본 dist-1.0(=×0.1) 클리핑 재현.
        /// </summary>
        private void ApplyThirdPersonCamera()
        {
            float targetYaw = m_yaw + m_viewYawOffset;
            float targetPitch = m_pitch + m_viewPitchOffset;
            float smooth = 1f - Mathf.Pow(k_thirdPersonSmoothBase, Time.deltaTime * k_referenceFps);
            m_camYaw = Mathf.LerpAngle(m_camYaw, targetYaw, smooth);
            m_camPitch = Mathf.LerpAngle(m_camPitch, targetPitch, smooth);

            Vector3 playerPos = m_player.transform.position;
            Quaternion viewRot = Quaternion.Euler(m_camPitch, m_camYaw, 0f);
            Vector3 look = viewRot * Vector3.forward;
            Vector3 viewBack = -look;
            Vector3 horizontal = new Vector3(look.x, 0f, look.z).normalized; // 수평 시선
            float camPitchRad = m_camPitch * Mathf.Deg2Rad;

            // 원본 注視点 — 수평은 시선방향으로 sin(pitch)×0.3, 수직은 base(1.95) + cos(pitch)×0.25.
            Vector3 focus = playerPos
                          + horizontal * (Mathf.Sin(camPitchRad) * k_thirdPersonPivotBack)
                          + Vector3.up * (k_thirdPersonPivotHeight + Mathf.Cos(camPitchRad) * k_thirdPersonHeightBias);

            float dist = k_thirdPersonMaxDist;
            if (Physics.SphereCast(focus, k_thirdPersonSphereRadius, viewBack,
                                   out RaycastHit hit, k_thirdPersonMaxDist, thirdPersonCollisionMask))
            {
                dist = hit.distance;
            }

            Vector3 cameraPos = focus + viewBack * dist;
            playerCamera.transform.position = cameraPos;
            playerCamera.transform.rotation = Quaternion.LookRotation(focus - cameraPos, Vector3.up);
        }

        /// <summary>
        /// 치트(F9) — 플레이어 정면에 복제 캐릭터를 스폰한다. mode=Follow(따라오기) / Guard(제자리 경계).
        /// HP·탄약은 복사하지 않고 기본값으로 생성(원본 F9). 위치는 원본과 동일하게 정면 1.0m + 위 0.5m.
        /// </summary>
        /// <param name="mode">클론 AI 모드 (Follow=나를 추적 / Guard=제자리 경계).</param>
        private void SpawnClone(Human.CloneAIMode mode)
        {
            Vector3 pos = m_player.transform.position
                        + m_player.transform.forward * k_cloneFrontDist
                        + Vector3.up * k_cloneUpOffset;
            float yaw = m_player.transform.eulerAngles.y;
            MapLoader.SpawnHumanClone(m_player, pos, yaw, mode,
                                      mode == Human.CloneAIMode.Follow ? m_player : null);
        }

        /// <summary>
        /// 치트(F7) — Player Human 을 Humans 리스트에서 dir(-1 이전 / +1 다음) 방향으로 순환 교체한다.
        /// MapLoader.Player 만 바꾸며(Event/Path 불변), 다음 프레임 TryAcquirePlayer 가 카메라/조작을 새 Human 으로 재취득한다.
        /// </summary>
        /// <param name="dir">-1 = 이전 Human, +1 = 다음 Human.</param>
        private void CyclePlayer(int dir)
        {
            int count = MapLoader.HumanCount;
            if (count == 0) return;

            int idx = MapLoader.PlayerIndex;
            if (idx < 0) return;

            int next = ((idx + dir) % count + count) % count;
            if (next == idx) return; // Human 이 1명뿐 — 교체할 대상 없음

            // 옛 Player 는 AI(3인칭 대상)로 넘어가므로 1인칭 때 껐던 몸통/다리를 다시 켠다.
            // 안 하면 투명한 채로 남는다. 새 Player 의 표시는 TryAcquirePlayer→ApplyViewpoint 가 시점에 맞춰 처리.
            if (m_player != null && m_player.HumanVisual != null)
                m_player.HumanVisual.SetBodyVisible(true);

            m_controller.SetCheatRise(false); // 옛 Player 의 상승 치트 해제 (AI 로 넘어간 뒤 계속 떠오르는 것 방지)
            MapLoader.SetPlayer(MapLoader.GetHuman(next));
        }

        private bool TryAcquirePlayer()
        {
            Human player = MapLoader.Player;
            if (player == null) return false;

            if (player != m_player)
            {
                m_player = player;
                m_controller = player.GetComponent<HumanController>();
                if (m_controller == null) return false;

                m_yaw = player.transform.eulerAngles.y;
                // 초기 시선 pitch — 정면(0)이 아니라 AI/HumanController 와 동일한 팔 rest 각(-armAngleInitial, 살짝 아래)에서 시작.
                m_pitch = -DataManager.Instance.HumanParameterData.humanGeneralData.armAngleInitial;
                m_camYaw = m_yaw;
                m_camPitch = m_pitch;
                m_viewYawOffset = 0f;
                m_viewPitchOffset = 0f;
                m_fireReady = false; // 조작권 획득 시 비무장 — Fire 를 한 번 떼야 발사 (씬 전환 클릭 누수 차단)
                m_controller.SetInput(new HumanInput { moveFlag = HumanMoveFlag.None, yaw = m_yaw, pitch = m_pitch });

                ApplyViewpoint();
            }
            return m_controller != null;
        }
    }
}
