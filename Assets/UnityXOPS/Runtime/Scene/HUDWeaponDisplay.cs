using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 무기 3D 모델을 전용 레이어 카메라로 RenderTexture 에 그리는 엔진 컴포넌트.
    /// 들고 있는 무기와 메고 있는 무기를 각각 자리(슬롯) 하나에 세워두고, 무기가 바뀌면 모델을 다시 만든다.
    /// 자리의 위치·크기·회전(도는 연출 포함), 카메라 구도, 화면 어디에 띄울지는 HUD 를 그리는 쪽이 슬롯/카메라를 받아 결정한다.
    /// 여기서는 그쪽이 아무것도 정해주지 않아도 무기가 보이도록 기본 구도만 세워둔다.
    /// 자리는 월드 원점에 만들며(전용 카메라가 그곳을 비춘다), 슬롯 좌표는 그 원점 기준이다.
    /// </summary>
    public class HUDWeaponDisplay : MonoBehaviour
    {
        [SerializeField] private RenderTexture viewportTexture; // 전용 카메라가 무기 모델을 그려 넣는 텍스처

        // 무기 모델이 놓이는 레이어. 이 레이어만 전용 카메라에 잡히고 메인 카메라에서는 제외된다.
        private const string k_slotLayerName = "HUDWeapon";
        // 전용 카메라 잘림 거리 — 무기 모델은 카메라 코앞의 작은 범위에만 있다.
        private const float k_cameraNear = 0.01f;
        private const float k_cameraFar = 100f;
        // 무기 자리(원점)를 정면에서 담는 기본 구도. HUD 가 다시 정하지 않아도 이대로 제대로 보인다.
        private const float k_cameraDefaultFov = 65f;
        private static readonly Vector3 k_cameraDefaultPosition = new Vector3(0f, 0f, -10f);

        private Camera m_camera;
        private Transform m_slotRoot;
        private Human m_player;
        private Weapon m_builtMain;
        private Weapon m_builtSub;
        private GameObject m_mainModel;
        private GameObject m_subModel;

        /// <summary>
        /// 현재 화면의 무기 뷰 엔진. 무기 뷰가 없는 화면(메뉴 등)에서는 null 이다.
        /// </summary>
        public static HUDWeaponDisplay Current { get; private set; }

        /// <summary>
        /// 전용 카메라가 무기 모델을 그려 넣는 텍스처. 이 결과를 화면 어디에 얼마만큼 띄울지는 받아가는 쪽(HUD)이 정한다.
        /// </summary>
        public RenderTexture ViewportTexture => viewportTexture;

        /// <summary>무기 뷰를 비추는 카메라. 구도(위치/회전/시야각/배경색)는 HUD 를 그리는 쪽이 정한다.</summary>
        public Camera ViewCamera => m_camera;

        /// <summary>들고 있는 무기가 놓이는 자리.</summary>
        public Transform MainSlot { get; private set; }

        /// <summary>메고 있는 무기가 놓이는 자리.</summary>
        public Transform SubSlot { get; private set; }

        private void Awake()
        {
            Current = this;

            int layer = LayerMask.NameToLayer(k_slotLayerName);
            if (layer < 0)
            {
                // 이 레이어가 없으면 무기 모델이 전용 카메라에 안 잡혀 무기 뷰가 조용히 빈 화면이 된다.
                Debugger.LogWarning($"'{k_slotLayerName}' 레이어가 없어 무기 뷰가 표시되지 않습니다. Project Settings > Tags and Layers 확인 필요.");
                layer = 0;
            }

            // 자리는 원점에 만든다. 어디에 어떻게 놓을지는 HUD 가 슬롯을 받아 정한다.
            m_slotRoot = new GameObject("WeaponViewSlots").transform;
            MainSlot = CreateSlot(m_slotRoot, "Main", layer);
            SubSlot = CreateSlot(m_slotRoot, "Sub", layer);

            CreateViewCamera(layer);
        }

        private void Start() => m_player = MapLoader.Player;

        private void OnDestroy()
        {
            if (Current == this) Current = null;

            // 런타임에 만든 것들은 씬 오브젝트의 자식이 아니라 스스로 치워야 한다.
            if (m_slotRoot != null) Destroy(m_slotRoot.gameObject);
            if (m_camera != null) Destroy(m_camera.gameObject);
        }

        private void OnEnable() { if (m_camera != null) m_camera.enabled = true; }
        private void OnDisable() { if (m_camera != null) m_camera.enabled = false; }

        /// <summary>
        /// 무기 자리만 비춰 텍스처에 그리는 전용 카메라를 만든다. 무엇을 어디에 그릴지(레이어/출력 텍스처/잘림 거리)는
        /// 여기서 고정하고, 구도(위치/회전/시야각/배경색)는 HUD 를 그리는 쪽이 다시 정할 수 있다.
        /// </summary>
        /// <param name="layer">무기 모델이 놓인 레이어(이 레이어만 비춘다).</param>
        private void CreateViewCamera(int layer)
        {
            if (viewportTexture == null)
            {
                Debugger.LogWarning("무기 뷰포트 텍스처가 지정되지 않아 무기 뷰가 표시되지 않습니다.");
                return;
            }

            m_camera = new GameObject("WeaponViewCamera").AddComponent<Camera>();
            m_camera.cullingMask = 1 << layer;
            m_camera.targetTexture = viewportTexture;
            m_camera.clearFlags = CameraClearFlags.SolidColor;
            m_camera.backgroundColor = new Color(0f, 0f, 0f, 0f); // 투명 — 무기 뷰 뒤의 UI 가 비치도록
            m_camera.nearClipPlane = k_cameraNear;
            m_camera.farClipPlane = k_cameraFar;
            // HUD 가 구도를 정해주지 않아도(스크립트 누락 등) 무기가 제대로 보이도록 기본 구도를 세워둔다.
            m_camera.fieldOfView = k_cameraDefaultFov;
            m_camera.transform.localPosition = k_cameraDefaultPosition;
            m_camera.enabled = isActiveAndEnabled;
        }

        private void Update()
        {
            if (m_player == null)
            {
                m_player = MapLoader.Player;
                if (m_player == null) return;
            }

            int selected = m_player.SelectWeapon;
            Weapon mainWeapon = m_player.GetWeapon(selected);
            Weapon subWeapon = m_player.GetWeapon(1 - selected);

            if (!ReferenceEquals(mainWeapon, m_builtMain)) { m_builtMain = mainWeapon; Rebuild(ref m_mainModel, MainSlot, mainWeapon); }
            if (!ReferenceEquals(subWeapon, m_builtSub)) { m_builtSub = subWeapon; Rebuild(ref m_subModel, SubSlot, subWeapon); }
        }

        /// <summary>
        /// 무기 모델이 놓일 빈 자리를 만든다.
        /// </summary>
        /// <param name="parent">부모 Transform.</param>
        /// <param name="name">자리 이름.</param>
        /// <param name="layer">무기 모델용 레이어.</param>
        /// <returns>만들어진 자리 Transform.</returns>
        private static Transform CreateSlot(Transform parent, string name, int layer)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            return go.transform;
        }

        /// <summary>
        /// 자리에 놓인 무기 모델을 새 무기로 다시 만든다.
        /// </summary>
        /// <param name="model">교체할 모델 참조.</param>
        /// <param name="slot">모델을 놓을 자리.</param>
        /// <param name="weapon">새 무기. null 이면 자리를 비운다.</param>
        private static void Rebuild(ref GameObject model, Transform slot, Weapon weapon)
        {
            if (model != null) Destroy(model);
            model = null;

            if (weapon == null || weapon.WeaponModelData == null) return;

            model = new GameObject("Model");
            model.transform.SetParent(slot, false);
            // 무기별 그리기 배율(WeaponData.size). 자리 배율에 무기별 상대 크기를 곱한다.
            model.transform.localScale = Vector3.one * weapon.WeaponData.size;
            WeaponVisual.BuildModelParts(model.transform, weapon.WeaponModelData);
            SetLayerRecursive(model, slot.gameObject.layer);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
