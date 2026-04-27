using JJLUtility;
using JJLUtility.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace UnityXOPS
{
    /// <summary>
    /// Human 캐릭터의 신체, 팔, 다리 메시와 애니메이션을 렌더링하고 제어하는 시각 표현 컴포넌트.
    /// </summary>
    public class HumanVisual : MonoBehaviour
    {
        [SerializeField]
        private Transform bodyRoot, fixedArmRoot, dynamicArmRoot, leftArmRoot, rightArmRoot, legRoot;
        [SerializeField]
        private Transform fixedWeaponAttachRoot, dynamicWeaponAttachRoot;
        public  Transform FixedWeaponAttachRoot   => fixedWeaponAttachRoot;
        public  Transform DynamicWeaponAttachRoot => dynamicWeaponAttachRoot;

        private MeshFilter m_leftArmMeshFilter, m_rightArmMeshFilter, m_legMeshFilter;
        private MeshRenderer m_leftArmMeshRenderer, m_rightArmMeshRenderer, m_legMeshRenderer;

        private List<Material> m_humanMaterials;
        private List<Mesh> m_leftArmMeshes, m_rightArmMeshes, m_legMeshes;

        private HumanModelData m_humanModelData;
        private HumanArmModelData m_humanArmModelData;
        private HumanLegModelData m_humanLegModelData;

        private List<HumanAnimation> m_legAnimation;
        private HumanAnimation m_idleAnimation;
        private HumanAnimation m_walkAnimation;
        private HumanAnimation m_runAnimation;

        // 원본 HumanMotionControl 상태
        private float  m_legAnimationTime;
        private string m_legAnimationName;
        private float  m_legRotationX;
        private bool   m_legRotationInitialized;

        // 원본 reaction_y/slowarm — dynamicArmRoot 의 X 축 추가 offset (도).
        // 두 모드: (1) slowarm 보간 — duration 동안 0 으로 선형 복원 (슬롯 持ち替え/픽업).
        //         (2) hold 모드 — duration 동안 holdDeg 유지, 종료 시 즉시 0 으로 스냅 (Switch ID/재장전).
        private float m_armReactionDeg;
        private float m_armReactionRecoverySpeed;
        private float m_armReactionHoldTimer;
        private float m_armPitchDeg;

        /// <summary>
        /// 인간 데이터로부터 신체, 팔, 다리 메시와 머티리얼을 로드해 초기화한다.
        /// </summary>
        /// <param name="data">인간 파라미터 데이터.</param>
        public void CreateHumanVisual(HumanData data)
        {
            m_humanMaterials = new List<Material>();
            m_leftArmMeshes = new List<Mesh>();
            m_rightArmMeshes = new List<Mesh>();
            m_legMeshes = new List<Mesh>();

            m_leftArmMeshFilter = leftArmRoot.gameObject.GetComponent<MeshFilter>();
            m_rightArmMeshFilter = rightArmRoot.gameObject.GetComponent<MeshFilter>();
            m_legMeshFilter = legRoot.gameObject.GetComponent<MeshFilter>();
            m_leftArmMeshRenderer = leftArmRoot.gameObject.GetComponent<MeshRenderer>();
            m_rightArmMeshRenderer = rightArmRoot.gameObject.GetComponent<MeshRenderer>();
            m_legMeshRenderer = legRoot.gameObject.GetComponent<MeshRenderer>();

            var modelParameter = DataManager.Instance.HumanParameterData.humanModelData;
            var armModelParameter = DataManager.Instance.HumanParameterData.humanArmModelData;
            var legModelParameter = DataManager.Instance.HumanParameterData.humanLegModelData;

            int modelIndex = data.modelIndex;
            if (modelIndex < 0 || modelIndex >= modelParameter.Count)
            {
                return;
            }

            float bodyScale = DataManager.Instance.HumanParameterData.humanGeneralData.humanBodyScale;
            float bodyHeight = DataManager.Instance.HumanParameterData.humanGeneralData.humanBodyHeight;
            m_humanModelData = modelParameter[modelIndex];

            //texture
            foreach (var path in m_humanModelData.textures.Select((value, index) => (value, index)))
            {
                var fullPath = SafePath.Combine(Application.streamingAssetsPath, path.value);

                if (MapLoader.Instance.HumanMaterialCache.TryGetValue(fullPath, out var cached))
                {
                    m_humanMaterials.Add(cached);
                    continue;
                }

                var texture = ImageLoader.LoadTexture(fullPath);
                if (texture == null)
                {
                    // 텍스처 로드 실패 — 공유 MainMaterial로 폴백(캐시에는 등록하지 않음)
                    m_humanMaterials.Add(MaterialManager.Instance.MainMaterial);
                    continue;
                }
                texture.name = Path.GetFileName(fullPath);

                var material = new Material(MaterialManager.Instance.MainMaterial);
                material.mainTexture = texture;
                material.name = texture.name;

                MapLoader.Instance.HumanMaterialCache[fullPath] = material;
                m_humanMaterials.Add(material);
            }

            //body
            var bodyDataList = m_humanModelData.modelData;
            bodyRoot.localPosition = new Vector3(0, bodyHeight, 0);
            bodyRoot.localScale *= bodyScale;
            foreach (var bodyData in bodyDataList.Select((value, index) => (value, index)))
            {
                var bodyObj = new GameObject($"Body_{bodyData.index}");
                bodyObj.transform.SetParent(bodyRoot);
                bodyObj.transform.SetLocalPositionAndRotation(bodyData.value.position, Quaternion.Euler(bodyData.value.rotation));
                bodyObj.transform.localScale = bodyData.value.scale;

                var bodyMeshFilter = bodyObj.AddComponent<MeshFilter>();
                var bodyMeshPath = SafePath.Combine(Application.streamingAssetsPath, bodyData.value.modelPath);
                bodyMeshFilter.sharedMesh = ModelLoader.LoadMesh(bodyMeshPath);

                var bodyMeshRenderer = bodyObj.AddComponent<MeshRenderer>();
                if (bodyData.value.textureIndex < 0 || bodyData.value.textureIndex >= m_humanMaterials.Count)
                {
                    bodyMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    bodyMeshRenderer.sharedMaterial = m_humanMaterials[bodyData.value.textureIndex];
                }
            }

            //arms
            int armModelIndex = m_humanModelData.armIndex;
            int armTextureIndex = m_humanModelData.armTextureIndex;
            float armHeight = DataManager.Instance.HumanParameterData.humanGeneralData.humanArmHeight;
            float armAngleNoWeapon = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleNoWeapon;
            float armScale = DataManager.Instance.HumanParameterData.humanGeneralData.humanArmScale;
            dynamicArmRoot.localPosition = new Vector3(0, armHeight, 0);
            dynamicArmRoot.localScale *= armScale;
            fixedArmRoot.localPosition = new Vector3(0, armHeight, 0);
            fixedArmRoot.localRotation = Quaternion.Euler(armAngleNoWeapon, 0, 0);
            fixedArmRoot.localScale *= armScale;

            if (armModelIndex >= 0 && armModelIndex < armModelParameter.Count)
            {
                m_humanArmModelData = armModelParameter[armModelIndex];

                var leftArmList = m_humanArmModelData.leftArms;
                var rightArmList = m_humanArmModelData.rightArms;

                foreach (var leftArmData in leftArmList)
                {
                    var leftArmMeshPath = SafePath.Combine(Application.streamingAssetsPath, leftArmData);
                    var leftArmMesh = ModelLoader.LoadMesh(leftArmMeshPath);
                    m_leftArmMeshes.Add(leftArmMesh);
                }
                foreach (var rightArmData in rightArmList)
                {
                    var rightArmMeshPath = SafePath.Combine(Application.streamingAssetsPath, rightArmData);
                    var rightArmMesh = ModelLoader.LoadMesh(rightArmMeshPath);
                    m_rightArmMeshes.Add(rightArmMesh);
                }

                if (armTextureIndex < 0 || armTextureIndex >= m_humanMaterials.Count)
                {
                    m_leftArmMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                    m_rightArmMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    m_leftArmMeshRenderer.sharedMaterial = m_humanMaterials[armTextureIndex];
                    m_rightArmMeshRenderer.sharedMaterial = m_humanMaterials[armTextureIndex];
                }

                // 팔 모델은 EquipInitialWeapons 직후 ApplyWeaponVisual 에서 무기에 맞게 결정됨.
            }

            //legs
            int legModelIndex = m_humanModelData.legIndex;
            int legTextureIndex = m_humanModelData.legTextureIndex;
            float legHeight = DataManager.Instance.HumanParameterData.humanGeneralData.humanLegHeight;
            legRoot.localPosition = new Vector3(0, legHeight, 0);
            legRoot.localScale *= DataManager.Instance.HumanParameterData.humanGeneralData.humanLegScale;
            m_legAnimation = DataManager.Instance.HumanParameterData.humanGeneralData.humanAnimation;

            if (m_legAnimation != null && legModelIndex >= 0 && legModelIndex < legModelParameter.Count)
            {
                m_idleAnimation = m_legAnimation.FirstOrDefault(a => a.name == "Idle");
                m_walkAnimation = m_legAnimation.FirstOrDefault(a => a.name == "Walk");
                m_runAnimation = m_legAnimation.FirstOrDefault(a => a.name == "Run");

                m_humanLegModelData = legModelParameter[legModelIndex];

                foreach (var legData in m_humanLegModelData.legs)
                {
                    var legPath = SafePath.Combine(Application.streamingAssetsPath, legData);
                    var legMesh = ModelLoader.LoadMesh(legPath);
                    m_legMeshes.Add(legMesh);
                }

                if (legTextureIndex < 0 || legTextureIndex >= m_humanMaterials.Count)
                {
                    m_legMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    m_legMeshRenderer.sharedMaterial = m_humanMaterials[legTextureIndex];
                }
            }
            
            if (m_idleAnimation != null)
            {
                SetLegModel(m_idleAnimation.index[0]);
            }
            else
            {
                SetLegModel(0);
            }
        }

        /// <summary>
        /// fixed/dynamic weaponAttachRoot 양쪽의 world scale 을 weaponScale 로 강제한다.
        /// 부모(fixedArmRoot/dynamicArmRoot) 에 누적된 armScale 영향을 상쇄해 무기가 OpenXOPS 원본 스케일(=weaponScale) 로 보이게 한다.
        /// </summary>
        /// <param name="weaponScale">WeaponGeneralData.weaponScale (OpenXOPS → Unity 스케일 비율).</param>
        public void ApplyWeaponAttachScale(float weaponScale)
        {
            ApplyAttachScale(fixedWeaponAttachRoot,   weaponScale);
            ApplyAttachScale(dynamicWeaponAttachRoot, weaponScale);
        }

        private static void ApplyAttachScale(Transform target, float weaponScale)
        {
            if (target == null) return;
            Transform parent = target.parent;
            Vector3   pl     = parent != null ? parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(weaponScale / pl.x, weaponScale / pl.y, weaponScale / pl.z);
        }

        /// <summary>
        /// 활성 weapon 의 WeaponModelData 에서 팔 모델 인덱스/고정 여부를 읽어 SetArmModel 을 호출한다.
        /// Human.SetSelectWeapon 또는 EquipInitialWeapons 직후 호출됨.
        /// </summary>
        /// <param name="active">현재 활성 슬롯의 weapon. WeaponModelData 가 null 이면 폴백 인덱스 적용.</param>
        public void ApplyWeaponVisual(Weapon active)
        {
            if (active == null || active.WeaponModelData == null)
            {
                SetArmModel(0, 0, false, false);
                return;
            }
            WeaponModelData m = active.WeaponModelData;
            SetArmModel(m.leftArmIndex, m.rightArmIndex, m.fixLeftArm, m.fixRightArm);
        }

        public void SetArmModel(int leftIndex, int rightIndex, bool fixLeft, bool fixRight)
        {
            if (m_humanArmModelData == null)
            {
                m_leftArmMeshFilter.sharedMesh = null;
                m_rightArmMeshFilter.sharedMesh = null;
                return;
            }

            if (fixLeft)
            {
                leftArmRoot.SetParent(fixedArmRoot);
                leftArmRoot.localRotation = Quaternion.identity;
            }
            else
            {
                leftArmRoot.SetParent(dynamicArmRoot);
                leftArmRoot.localRotation = Quaternion.identity;
            }
            if (fixRight)
            {
                rightArmRoot.SetParent(fixedArmRoot);
                rightArmRoot.localRotation = Quaternion.identity;
            }
            else
            {
                rightArmRoot.SetParent(dynamicArmRoot);
                rightArmRoot.localRotation = Quaternion.identity;
            }

            if (leftIndex >= 0 && leftIndex < m_leftArmMeshes.Count)
            {
                m_leftArmMeshFilter.sharedMesh = m_leftArmMeshes[leftIndex];
            }
            else
            {
                m_leftArmMeshFilter.sharedMesh = null;
            }

            if (rightIndex >= 0 && rightIndex < m_rightArmMeshes.Count)
            {
                m_rightArmMeshFilter.sharedMesh = m_rightArmMeshes[rightIndex];
            }
            else
            {
                m_rightArmMeshFilter.sharedMesh = null;
            }
        }

        public void SetBodyVisible(bool visible)
        {
            bodyRoot.gameObject.SetActive(visible);
            legRoot.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 카메라 피치(상하 조준각)를 dynamicArmRoot의 X축 회전으로 반영.
        /// 원본 OpenXOPS: 무기 소지 상태에서 armmodel_rotation_y = armrotation_y + reaction_y (object.cpp:3450-3455).
        /// Human.prefab의 Visual Transform이 Y축 180° 플립(OpenXOPS와 pitch가 180° 틀어진 상태 보정용) 되어 있어
        /// 하위 dynamicArmRoot의 로컬 X축 회전이 월드에서 부호 반전됨 → 카메라 피치와 같은 월드 방향을 얻으려면 부호 반전 필요.
        /// reaction (슬롯 전환 등) 은 카메라 pitch 와 별개의 offset 이므로 합쳐 적용 — 카메라 방향과 항상 m_armReactionDeg 만큼 차이 유지.
        /// </summary>
        /// <param name="pitchDeg">피치 각도 (도 단위, 위를 볼수록 음수).</param>
        public void SetArmPitch(float pitchDeg)
        {
            if (dynamicArmRoot == null) return;
            m_armPitchDeg = pitchDeg;
            ApplyArmRotation();
        }

        private void ApplyArmRotation()
        {
            dynamicArmRoot.localRotation = Quaternion.Euler(-m_armPitchDeg + m_armReactionDeg, 0f, 0f);
        }

        /// <summary>
        /// slowarm 보간 모드: reaction 을 startDeg 로 즉시 세팅하고 duration 동안 0 으로 선형 복원.
        /// 슬롯 持ち替え/픽업 시 사용. 원본 OpenXOPS slowarm=true (object.cpp:3413-3418, ±2°/frame).
        /// </summary>
        /// <param name="startDeg">시작 offset (도). OpenXOPS 의 ARMRAD_RELOADWEAPON 등.</param>
        /// <param name="duration">0 으로 복원되는 시간 (초). slotChangeTime 등.</param>
        public void BeginArmReaction(float startDeg, float duration)
        {
            if (duration <= 0f)
            {
                m_armReactionDeg           = 0f;
                m_armReactionRecoverySpeed = 0f;
                m_armReactionHoldTimer     = 0f;
                ApplyArmRotation();
                return;
            }
            m_armReactionDeg           = startDeg;
            m_armReactionRecoverySpeed = Mathf.Abs(startDeg) / duration;
            m_armReactionHoldTimer     = 0f;
            ApplyArmRotation();
        }

        /// <summary>
        /// hold 락 모드: reaction 을 holdDeg 로 즉시 세팅하고 duration 동안 그 값을 유지.
        /// 타이머 종료 시 즉시 0 으로 스냅 (보간 없음). Switch ID / 재장전 시 사용.
        /// 원본 OpenXOPS ChangeWeaponIDCnt > 0 동안 reaction_y = ARMRAD_RELOADWEAPON 강제 (object.cpp:3427-3429).
        /// </summary>
        /// <param name="holdDeg">유지할 offset (도).</param>
        /// <param name="duration">유지 시간 (초). switchTime / reloadTime 등.</param>
        public void BeginArmReactionHold(float holdDeg, float duration)
        {
            if (duration <= 0f)
            {
                // duration 이 0 이면 hold 효과 자체 없음. 이전 reaction 상태가 남아 slowarm 으로 fallback 되지 않도록 0 으로 클리어.
                m_armReactionDeg       = 0f;
                m_armReactionHoldTimer = 0f;
                ApplyArmRotation();
                return;
            }
            m_armReactionDeg       = holdDeg;
            m_armReactionHoldTimer = duration;
            ApplyArmRotation();
        }

        /// <summary>
        /// 매 프레임 호출. hold 타이머 > 0 면 그 값 유지 + 타이머 감소(끝나면 0 스냅).
        /// 그 외엔 slowarm 선형 복원.
        /// </summary>
        public void TickArmReaction(float dt)
        {
            if (m_armReactionHoldTimer > 0f)
            {
                m_armReactionHoldTimer -= dt;
                if (m_armReactionHoldTimer <= 0f)
                {
                    m_armReactionHoldTimer = 0f;
                    m_armReactionDeg       = 0f;
                    ApplyArmRotation();
                }
                return;
            }

            if (m_armReactionDeg == 0f) return;
            m_armReactionDeg = Mathf.MoveTowards(m_armReactionDeg, 0f, m_armReactionRecoverySpeed * dt);
            ApplyArmRotation();
        }

        public void SetLegModel(int legIndex)
        {
            if (!legRoot.gameObject.activeSelf)
            {
                return;
            }

            if (m_legAnimation == null)
            {
                m_legMeshFilter.sharedMesh = null;
                return;
            }

            if (legIndex >= 0 && legIndex < m_legMeshes.Count)
            {
                m_legMeshFilter.sharedMesh = m_legMeshes[legIndex];
            }
            else
            {
                m_legMeshFilter.sharedMesh = null;
            }
        }

        /// <summary>
        /// 매 FixedUpdate마다 HumanController가 호출. 이동 플래그와 바디 Yaw에 따라 다리 모델 프레임과 다리 루트 회전을 갱신한다.
        /// 원본 HumanMotionControl::ProcessObject (object.cpp:3396-3540) 포팅.
        /// </summary>
        /// <param name="dt">프레임 간격 (초).</param>
        /// <param name="moveFlag">이전 프레임 기준 이동 플래그 (원본 MoveFlag_lt).</param>
        /// <param name="bodyYaw">바디 월드 Yaw (도 단위).</param>
        /// <param name="alive">캐릭터 생존 여부.</param>
        public void TickLeg(float dt, HumanMoveFlag moveFlag, float bodyYaw, bool alive)
        {
            if (m_legAnimation == null) return;

            // 1. 애니메이션 선택 (원본 object.cpp:3519-3538)
            //    Walk 플래그는 방향 플래그 유무와 상관없이 전진 이동 → Walk 애니메이션 우선.
            bool moving = (moveFlag & (HumanMoveFlag.Forward | HumanMoveFlag.Back |
                                       HumanMoveFlag.Left    | HumanMoveFlag.Right)) != 0;
            HumanAnimation anim;
            if (!alive)                                         anim = m_idleAnimation;
            else if ((moveFlag & HumanMoveFlag.Walk) != 0)      anim = m_walkAnimation;
            else if (moving)                                    anim = m_runAnimation;
            else                                                anim = m_idleAnimation;
            if (anim == null) anim = m_idleAnimation;
            if (anim == null || anim.index == null || anim.index.Count == 0) return;

            // 2. 애니메이션 전환 시 타이머 리셋
            if (anim.name != m_legAnimationName)
            {
                m_legAnimationTime = 0f;
                m_legAnimationName = anim.name;
            }

            // 3. 방향별 사이클 시간 선택 (JSON forward/strafe/backwardSpeed = 초 단위 한 사이클 길이).
            //    Walk는 방향 플래그 무시하고 항상 전진 속도 사용 (원본 3465).
            float cycle;
            if      ((moveFlag & HumanMoveFlag.Walk) != 0)                                    cycle = anim.forwardSpeed;
            else if ((moveFlag & HumanMoveFlag.Back) != 0)                                    cycle = anim.backwardSpeed;
            else if ((moveFlag & (HumanMoveFlag.Left | HumanMoveFlag.Right)) != 0)            cycle = anim.strafeSpeed;
            else                                                                              cycle = anim.forwardSpeed;

            int frameCount = anim.index.Count;
            int frameIdx;
            if (cycle > 1e-6f && frameCount > 1)
            {
                m_legAnimationTime += dt;
                m_legAnimationTime %= cycle;
                frameIdx = Mathf.FloorToInt(m_legAnimationTime / cycle * frameCount) % frameCount;
            }
            else
            {
                m_legAnimationTime = 0f;
                frameIdx = 0;
            }
            SetLegModel(anim.index[frameIdx]);

            // 4. 다리 회전각 계산 (원본 object.cpp:3463-3513)
            float moveRxDeg = 0f;
            if (alive && (moveFlag & HumanMoveFlag.Walk) == 0)
            {
                HumanMoveFlag dir = moveFlag & (HumanMoveFlag.Forward | HumanMoveFlag.Back |
                                                HumanMoveFlag.Left    | HumanMoveFlag.Right);
                switch (dir)
                {
                    case HumanMoveFlag.Forward:                        moveRxDeg =    0f; break;
                    case HumanMoveFlag.Back:                           moveRxDeg =  180f; break;
                    case HumanMoveFlag.Left:                           moveRxDeg =   90f; break;
                    case HumanMoveFlag.Right:                          moveRxDeg =  -90f; break;
                    case HumanMoveFlag.Forward | HumanMoveFlag.Left:   moveRxDeg =   45f; break;
                    case HumanMoveFlag.Back    | HumanMoveFlag.Left:   moveRxDeg =  135f; break;
                    case HumanMoveFlag.Back    | HumanMoveFlag.Right:  moveRxDeg = -135f; break;
                    case HumanMoveFlag.Forward | HumanMoveFlag.Right:  moveRxDeg =  -45f; break;
                }
            }

            // 후진 성분이면 180도 뒤집기 (원본 3503-3509) → 다리 자체는 정면을 향한 채 애니메이션만 역방향
            float moveRx2Deg = (Mathf.Abs(moveRxDeg) > 90f) ? moveRxDeg + 180f : moveRxDeg;
            float target     = bodyYaw - moveRx2Deg;

            if (!alive || !m_legRotationInitialized)
            {
                m_legRotationX           = alive ? target : bodyYaw;
                m_legRotationInitialized = true;
            }
            else
            {
                // 원본: leg = leg*0.85 + target*0.15 (per 33.33Hz frame) → Unity 연속시간 변환
                float blend    = 1f - Mathf.Pow(0.85f, dt * 33.3333f);
                m_legRotationX = Mathf.LerpAngle(m_legRotationX, target, blend);
            }

            legRoot.localRotation = Quaternion.Euler(0f, m_legRotationX - bodyYaw, 0f);
        }
    }
}
