using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// HUD(우측 하단) 무기 3D 모델 표시. 전용 레이어 카메라 → RenderTexture → RawImage 로 합성된다.
    /// 원본 OpenXOPS gamemain.cpp:3259-3279 — 장착 무기는 크게+Y축 회전(프레임당 2°), 보관 무기는 작게+180° 고정.
    /// mainAnchor/subAnchor 의 위치·스케일로 "크게/작게, 우하단 배치" 를 조절하고, 두 앵커는 HUDWeapon 레이어에 둔다.
    /// </summary>
    public class HUDWeaponDisplay : MonoBehaviour
    {
        [SerializeField] private Camera    hudCamera;     // HUDWeapon 레이어만 RenderTexture 로 그리는 전용 카메라
        [SerializeField] private Transform mainAnchor;   // 장착 무기 부모 (회전)
        [SerializeField] private Transform subAnchor;    // 보관 무기 부모 (정지)

        // 원본: 프레임당 +2° (33.3333fps) → 초당 66.6667°. Y축 yaw 스핀.
        private const float k_mainSpinDegPerSec = 2f * 33.3333f;
        // 원본 슬롯1: Y 180° 고정.
        private const float k_subFixedYaw = 180f;

        private Human  m_player;
        private Weapon m_builtMain;
        private Weapon m_builtSub;
        private GameObject m_mainModel;
        private GameObject m_subModel;
        private float  m_mainYaw;

        private void Start() => m_player = MapLoader.Player;

        // HUD 표시가 켜진 동안에만 전용 카메라가 RenderTexture 를 갱신하도록 lifecycle 에 묶는다.
        private void OnEnable()  { if (hudCamera != null) hudCamera.enabled = true; }
        private void OnDisable() { if (hudCamera != null) hudCamera.enabled = false; }

        private void Update()
        {
            if (m_player == null)
            {
                m_player = MapLoader.Player;
                if (m_player == null) return;
            }

            int selected = m_player.SelectWeapon;
            Weapon mainWeapon = m_player.GetWeapon(selected);
            Weapon subWeapon  = m_player.GetWeapon(1 - selected);

            if (!ReferenceEquals(mainWeapon, m_builtMain)) { m_builtMain = mainWeapon; Rebuild(ref m_mainModel, mainAnchor, mainWeapon); }
            if (!ReferenceEquals(subWeapon,  m_builtSub))  { m_builtSub  = subWeapon;  Rebuild(ref m_subModel,  subAnchor,  subWeapon); }

            m_mainYaw += k_mainSpinDegPerSec * Time.deltaTime;
            if (m_mainModel != null) m_mainModel.transform.localRotation = Quaternion.Euler(0f, m_mainYaw, 0f);
            if (m_subModel  != null) m_subModel.transform.localRotation  = Quaternion.Euler(0f, k_subFixedYaw, 0f);
        }

        private void Rebuild(ref GameObject model, Transform anchor, Weapon weapon)
        {
            if (model != null) Destroy(model);
            model = null;

            if (weapon == null || weapon.WeaponModelData == null) return;

            model = new GameObject("Model");
            model.transform.SetParent(anchor, false);
            // 무기별 描画倍率(WeaponData.size). 앵커 스케일(메인 크게/서브 작게)에 무기별 상대 크기를 곱한다.
            model.transform.localScale = Vector3.one * weapon.WeaponData.size;
            WeaponVisual.BuildModelParts(model.transform, weapon.WeaponModelData);
            SetLayerRecursive(model, anchor.gameObject.layer);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
