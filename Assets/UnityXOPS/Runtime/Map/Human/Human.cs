using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 사망 상태머신. 원본 OpenXOPS human::deadstate (object.cpp:1208-1389) 정수값과 동일.
    /// 0 Alive 정상 / 1 Falling 쓰러지기 시작 / 2 HeadStuck 머리 박힘+자유낙하 /
    /// 3 LegSliding 다리 미끄러뜨리기 / 4 Settling 1프레임 정지 / 5 Done 완전 고정.
    /// </summary>
    public enum HumanDeadState
    {
        Alive = 0,
        Falling = 1,
        HeadStuck = 2,
        LegSliding = 3,
        Settling = 4,
        Done = 5,
    }

    /// <summary>
    /// 게임 맵에 배치된 인간 캐릭터의 데이터와 시각 표현을 관리하는 컴포넌트.
    /// </summary>
    public partial class Human : MonoBehaviour
    {
        [SerializeField]
        private float hp;
        public float HP => hp;

        [SerializeField]
        private int team;
        public int Team => team;

        // 미션 이벤트 19(チーム変更) — 대상 인물의 팀번호를 0(아군측)으로 변경. 원본 EventControl::SetTeamID.
        public void SetTeam(int value) => team = value;

        [SerializeField]
        private HumanDeadState deadState = HumanDeadState.Alive;
        public HumanDeadState DeadState => deadState;
        public bool Alive => deadState == HumanDeadState.Alive;

        /// <summary>
        /// 사망 상태를 설정. 전이 로직은 HumanController.Tick에서 호출 예정.
        /// </summary>
        public void SetDeadState(HumanDeadState value)
        {
            bool aliveChanged = (deadState == HumanDeadState.Alive) != (value == HumanDeadState.Alive);
            deadState = value;
            if (value != HumanDeadState.Alive) m_scoping = false; // 사망 시 스코프 해제 (원본 object.cpp:1240 SetDisableScope)

            // 생사 전환 시 팔 비주얼 재적용 — 사망하면 비무장 동적 팔이 fixedArm 으로 복귀(시체 팔 늘어뜨림). 부활 시 재평가.
            if (aliveChanged && humanVisual != null && m_weapons != null)
                humanVisual.ApplyWeaponVisual(m_weapons[m_selectWeapon]);
        }

        // 비무장 팔을 dynamicArmRoot(AI 조준 pitch 추종)에 둘지 여부. fixed/dynamic 자체는 무기 데이터가 결정하지만,
        // 좀비 공격·항복처럼 비무장인데 팔을 "조준 방향으로 움직여야" 하는 ACTION 포즈 중에만 AIBrain 이 true 로 세팅한다.
        // 평상시(NORMAL/CAUTION)·사망·플레이어는 false → 무기 데이터 그대로(none=fixed).
        private bool m_unarmedArmDynamic;
        public bool UnarmedArmDynamic => m_unarmedArmDynamic;
        public void SetUnarmedArmDynamic(bool value)
        {
            if (m_unarmedArmDynamic == value) return; // 변경 시에만 재적용 (re-parent 비용 절약)
            m_unarmedArmDynamic = value;
            if (humanVisual != null && m_weapons != null) humanVisual.ApplyWeaponVisual(m_weapons[m_selectWeapon]);
        }

        /// <summary>
        /// 데미지 적용. HP 만 차감. 사망 진입(Falling) + 무기 드롭은 HumanController.EnterDeadState 가 처리한다
        /// (시체 회전 초기화 m_deadDirection/m_deadAddRy 와 함께 한 곳에서 처리하기 위함).
        /// HP ≤ 0 인 채 다음 FixedUpdate 까지의 짧은 갭은 Human.Update 의 Alive 게이트로 보호.
        /// 원본 OpenXOPS human::SubHP (object.cpp:1060-1080) 단순화 버전.
        /// </summary>
        public void ApplyDamage(float damage)
        {
            if (!Alive || damage <= 0f) return;

            hp -= damage;
            if (hp < 0f) hp = 0f;
        }

        // 원본 OpenXOPS human::Hit_rx (object.cpp:1084-1088 SetHitFlag) — 마지막 피격 yaw (월드 deg).
        // 사망 진입 시 HumanController.EnterDeadState 가 이 값과 본인 Yaw 차이로 앞/뒤 쓰러짐 분기.
        // Hit_rx 는 클리어 안 됨: 살아있는 동안 여러 번 맞으면 마지막 hit 방향이 사망 시 사용 (원본 동작 그대로).
        private float m_hitYaw;
        public float HitYaw => m_hitYaw;

        /// <summary>
        /// 마지막 피격 방향 (월드 yaw, deg). Bullet 측이 명중 시 호출. 사망 분기에만 사용, 살아있는 동안의 동작에는 영향 없음.
        /// </summary>
        public void SetHitYaw(float yawDeg)
        {
            m_hitYaw = yawDeg;
            m_hitPending = true; // AI 피격 반응(FaceCaution)용 — 다음 AI 틱이 ConsumeHit 로 소비.
        }

        // 이번 구간 피격 여부 (원본 human::HitFlag). AIBrain 이 매 틱 소비해 경계+공격자 방향 조준(FaceCaution).
        private bool m_hitPending;
        /// <summary>피격 소비. 맞았으면 true + 공격자를 바라보는 월드 yaw(=총알 진행방향 HitYaw + 180°). 원본 CheckHit + SetHitFlag(공격자 방향).</summary>
        public bool ConsumeHit(out float faceYawDeg)
        {
            faceYawDeg = m_hitYaw + 180f; // SetHitYaw 는 총알 진행방향(피격자→탄착) → 공격자는 반대편
            bool v = m_hitPending;
            m_hitPending = false;
            return v;
        }

        // 적 총성·총알 통과·폭발 등 위협 소리를 들었다는 신호 (원본 월드 사운드 GetWorldSound>0).
        // 음원(WorldSound/Bullet)이 청취 범위 안에서 set, AIBrain 이 매 틱 ConsumeThreatHeard 로 소비해 경계 전환. 방향은 쓰지 않음(원본도 카운트만).
        private bool m_threatHeard;
        public void NotifyThreatHeard() => m_threatHeard = true;
        public bool ConsumeThreatHeard()
        {
            bool v = m_threatHeard;
            m_threatHeard = false;
            return v;
        }

        [SerializeField]
        private Transform cameraRoot;
        public Transform CameraRoot => cameraRoot;

        [SerializeField]
        private HumanVisual humanVisual;
        public HumanVisual HumanVisual => humanVisual;

        [SerializeField] private HumanHitbox headHitbox;
        [SerializeField] private HumanHitbox bodyHitbox;
        [SerializeField] private HumanHitbox legHitbox;

        private HumanData m_humanData;
        public HumanData HumanData => m_humanData;
        // AI 레벨 = HumanData.aiIndex (원본 HumanParameter.AIlevel). humanAIData 리스트 인덱스. 데이터 없으면 0.
        public int AILevel => m_humanData != null ? m_humanData.aiIndex : 0;
        private HumanTypeData m_humanTypeData;
        public HumanTypeData HumanTypeData => m_humanTypeData;

        private HumanController m_controller;

        // 조준 오차 중 연사 반동 누적분. 원본 OpenXOPS human::ReactionGunsightErrorRange (object.cpp).
        // 발사 시 WeaponData.recoil 가산, 매 프레임 회복(감소) + 현재 무기 errorRange.max 클램프.
        private float m_reactionErrorRange;

        private RawPointData m_humanParam, m_humanDataParam;

        private int m_identifier;
        public int Identifier => m_identifier;

        // AI 경로 시작 웨이포인트 식별번호 — HUMAN 포인트의 param2(=원본 p3). AIMoveNavi 가 첫 포인트로 사용. (원본 점프: human point → p3 → 첫 AIPATH p4)
        public int PathStartId => m_humanParam != null ? m_humanParam.param2 : 0;

        // 스코프(ADS) 상태 — 원본 OpenXOPS human::scopemode (object.cpp:918-944). 발사/이동으론 안 풀리고 무기교체·재장전·사망·비스코프무기면 자동 해제 (Update 에서 CanScope 체크).
        private bool m_scoping;
        public bool IsScoping => m_scoping;

        // 현재 무기의 스코프 데이터. 스코프 무기가 아니거나 인덱스가 잘못되면 null.
        public ScopeData CurrentScopeData
        {
            get
            {
                Weapon w = CurrentWeapon;
                if (w == null || w.WeaponData == null || !w.WeaponData.scope) return null;
                var list = DataManager.Instance.WeaponParameterData.scopeData;
                int idx = w.WeaponData.scopeIndex;
                return (list != null && idx >= 0 && idx < list.Count) ? list[idx] : null;
            }
        }

        // 스코프 조준 중일 때만 non-null. 발사 반동(ScopeData.recoilAim*Adjust) 등 게임플레이가 읽는 단일 소스.
        public ScopeData ActiveScope => m_scoping ? CurrentScopeData : null;

        // 스코프 켜기/유지 가능 조건. 깨지면 자동 해제 (원본: 무기교체/재장전/사망 시 SetDisableScope).
        public bool CanScope => Alive && !IsChanging && CurrentScopeData != null;

        /// <summary>
        /// 스코프 토글 (원본 ChangeScopeMode). 입력 측(PlayerController/UI)이 호출. 조건 불충족이면 무시.
        /// </summary>
        public void ToggleScope()
        {
            if (CanScope) m_scoping = !m_scoping;
        }

        /// <summary>
        /// 포인트 데이터와 파라미터로부터 인간 캐릭터를 생성 및 초기화한다.
        /// </summary>
        /// <param name="humanParam">인간 배치 포인트 데이터.</param>
        /// <param name="humanDataParam">인간 파라미터 포인트 데이터.</param>
        public void CreateHuman(RawPointData humanParam, RawPointData humanDataParam)
        {
            m_humanParam = humanParam;
            m_humanDataParam = humanDataParam;
            m_identifier = humanParam.param3;
            m_controller = GetComponent<HumanController>();

            var humanParamData = DataManager.Instance.HumanParameterData;
            int humanIndex = m_humanDataParam.param1;
            if (humanIndex >= 0 && humanIndex < humanParamData.humanData.Count)
            {
                m_humanData = humanParamData.humanData[humanIndex];

                int typeIndex = m_humanData.typeIndex;
                if (typeIndex >= 0 && typeIndex < humanParamData.humanTypeData.Count)
                {
                    m_humanTypeData = humanParamData.humanTypeData[typeIndex];
                }
            }

            humanVisual.CreateHumanVisual(m_humanData);

            var general = humanParamData.humanGeneralData;
            if (headHitbox != null) headHitbox.ApplySize(general);
            if (bodyHitbox != null) bodyHitbox.ApplySize(general);
            if (legHitbox != null) legHitbox.ApplySize(general);

            float cameraAttachPosition = general.cameraAttachPosition;
            cameraRoot.localPosition = new Vector3(0, cameraAttachPosition, 0);

            EquipInitialWeapons();

            hp = m_humanData.hp;
            team = m_humanDataParam.param2;
            deadState = hp > 0 ? HumanDeadState.Alive : HumanDeadState.Done;
        }

        // 조준 표적점(월드). 플레이어가 카메라 중앙 ray 로 매 발사 전 주입 → SpawnBullets 가 총구→표적 방향으로 발사 (3인칭 어깨 오프셋 parallax 보정).
        // null 이면 controller.Yaw/Pitch 사용 (AI). 원본엔 없는 UnityXOPS 3인칭 연출 보조.
        private Vector3? m_aimPoint;
        public Vector3? AimPoint => m_aimPoint;
        public void SetAimPoint(Vector3 worldPoint) => m_aimPoint = worldPoint;

        private void Update()
        {
            // 사망 시 카운터/팔 reaction/픽업 모두 정지. HP ≤ 0 직후 다음 FixedUpdate 의 EnterDeadState 가
            // 호출되기 전 짧은 갭 (Update 가 FixedUpdate 보다 먼저 도는 경우) 도 함께 보호.
            if (!Alive || hp <= 0f) return;

            float dt = Time.deltaTime;
            if (m_selectWeaponCnt > 0f) m_selectWeaponCnt -= dt;

            TickReactionRecovery(dt);

            // m_reloadingCnt 는 SwitchID(SEMI↔FULL) 와 Reload 둘 다 사용. 0 도달 시 활성 무기가 reloading 중이면 매거진 보충.
            if (m_reloadingCnt > 0f)
            {
                m_reloadingCnt -= dt;
                if (m_reloadingCnt <= 0f)
                {
                    Weapon current = m_weapons[m_selectWeapon];
                    if (current != null && current.IsReloading) current.RunReload();
                }
            }

            humanVisual.TickArmReaction(dt);
            TryPickupWeapon();

            // 스코프 자동 해제 — 무기교체/재장전/비스코프무기 전환 시 (원본 SetDisableScope 트리거).
            if (m_scoping && !CanScope) m_scoping = false;
        }

        /// <summary>
        /// 발사 시점의 실효 조준 오차 (단위: ErrorRange 정수 스케일). 상태 오차 + 반동 누적분의 합.
        /// Weapon.SpawnBullets 가 errorRange.min 하한을 적용한 뒤 0.15° 단위로 환산해 탄도에 더한다.
        /// 원본 OpenXOPS human::GetGunsightErrorRange (object.cpp:2113-2116).
        /// </summary>
        public float GunsightErrorRange => StateErrorRange() + m_reactionErrorRange;

        /// <summary>
        /// 이동/점프/저체력 상태에 따른 조준 오차. 원본 OpenXOPS human::GunsightErrorRange (object.cpp:1130-1152).
        /// 이동 페널티는 대입(=)이라 마지막으로 평가된 조건이 우선 — Walk→Forward→Back→Strafe→airborne 순서. 저체력만 가산(+=).
        /// </summary>
        private float StateErrorRange()
        {
            var wgen = DataManager.Instance.WeaponParameterData.weaponGeneralData;
            int state = 0;

            HumanMoveFlag flag = m_controller != null ? m_controller.MoveFlagLt : HumanMoveFlag.None;
            if ((flag & HumanMoveFlag.Walk) != 0) state = wgen.walkAccuracyPenalty;
            if ((flag & HumanMoveFlag.Forward) != 0) state = wgen.forwardAccuracyPenalty;
            if ((flag & HumanMoveFlag.Back) != 0) state = wgen.backAccuracyPenalty;
            if ((flag & (HumanMoveFlag.Left | HumanMoveFlag.Right)) != 0) state = wgen.strafeAccuracyPenalty;
            if (m_controller != null && !m_controller.Grounded) state = wgen.airborneAccuracyPenalty;

            if (hp < wgen.injuryHpThreshold) state += wgen.injuryAccuracyPenalty;

            return state;
        }

        /// <summary>
        /// 발사 후 반동 누적. 원본 OpenXOPS human::ShotWeapon (object.cpp:707) ReactionGunsightErrorRange += reaction.
        /// 상한 클램프는 TickReactionRecovery 가 매 프레임 처리하므로 여기선 가산만.
        /// </summary>
        public void AddShotReaction(float recoil)
        {
            m_reactionErrorRange += recoil;
        }

        /// <summary>
        /// 피격 시 조준 반동 오차를 부위별 값으로 덮어쓴다(원본 = 대입). 다음 프레임부터 TickReactionRecovery 가 회복·클램프.
        /// 원본 OpenXOPS HitBulletHead/Up/Leg·HitGrenadeExplosion (object.cpp:1039/1049/1059/1079) ReactionGunsightErrorRange = 15/12/8/10.
        /// </summary>
        public void SetHitReaction(float value)
        {
            // 원본의 경우 무조건 대입 -> 살짝 이상함
            // Max 함수를 써서 정확도 에러가 "피격 시 에임 흐트러짐" 보다 더 높을 때 정확도가 더 낮은 수치로 보정.
            m_reactionErrorRange = Mathf.Max(m_reactionErrorRange, value);
        }

        /// <summary>
        /// 발사 시 에임 킥(실제 시점 이동) 을 컨트롤러 시점각에 누적. 플레이어는 PlayerController 가 다음 프레임 마우스 누적값으로 되읽어 영구 반영(자동 복원 없음).
        /// 원본 OpenXOPS human::ShotWeapon 의 rotation_x/armrotation_y 가산 (object.cpp:725-726). AI 는 매 틱 재조준으로 덮어써짐.
        /// </summary>
        /// <param name="yawDeg">좌우 킥 (deg). 대칭 분포.</param>
        /// <param name="pitchDeg">상하 킥 (deg). Unity 부호: 음수 = 위로.</param>
        public void AddViewRecoil(float yawDeg, float pitchDeg)
        {
            if (m_controller != null) m_controller.AddYawPitch(yawDeg, pitchDeg);
        }

        /// <summary>
        /// 반동 누적분 회복 — 매 프레임 reactionRecoveryPerSecond 만큼 감소, [0, 현재 무기 errorRange.max] 클램프.
        /// 원본 OpenXOPS object.cpp:1153-1157 (프레임당 -1, 33.333fps).
        /// </summary>
        private void TickReactionRecovery(float dt)
        {
            if (m_reactionErrorRange <= 0f) { m_reactionErrorRange = 0f; return; }

            var wgen = DataManager.Instance.WeaponParameterData.weaponGeneralData;
            m_reactionErrorRange -= wgen.reactionRecoveryPerSecond * dt;
            if (m_reactionErrorRange < 0f) m_reactionErrorRange = 0f;

            Weapon current = m_weapons[m_selectWeapon];
            if (current != null)
            {
                float maxErr = current.WeaponData.errorRange.max;
                if (m_reactionErrorRange > maxErr) m_reactionErrorRange = maxErr;
            }
        }

    }
}
