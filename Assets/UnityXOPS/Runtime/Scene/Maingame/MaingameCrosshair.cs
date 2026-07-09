using UnityEngine;
using UnityEngine.UI;
using JJLUtility;
using JJLUtility.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 플레이어(MapLoader.Player) 상태를 읽어 크로스헤어/스코프 UI 를 제어하는 컴포넌트.
    /// 동적 크로스헤어 간격 = GetGunsightErrorRange() 픽셀 1:1 (원본 gamemain.cpp:3195-3198, errorRange.min·각도 변환 없음).
    /// 스코프 토글 = 원본 ChangeScopeMode (Down 엣지 반전, objectmanager.cpp:2267) — 발사/이동으로는 안 풀리고 무기교체/재장전/사망으로만 해제.
    /// 스코프 켜짐 시 ScopeData.texturePath 오버레이 + ScopeData.fovDegrees FOV 전환 + ScopeData.lines 레티클(미리 빌드한 것 중 인덱스 일치분만 활성).
    /// </summary>
    public class MaingameCrosshair : MonoBehaviour
    {
        [SerializeField]
        private RectTransform crosshairRoot, crosshairErrorRoot, scopeRoot, leftBox, rightBox, upBox, downBox;
        [SerializeField]
        private RawImage scopeImage;
        [SerializeField]
        private RectTransform scopeLineRoot;   // 스코프 레티클(line) 부모. 미지정 시 scopeImage 로 폴백. 화면 중앙 정렬 권장.

        private XOPSSpriteText m_crosshairLeft, m_crosshairRight;
        private List<GameObject> m_scopeReticles;   // scopeData 인덱스별 레티클 컨테이너 (시작 시 1회 빌드).

        private Human m_player;
        private PlayerController m_playerController;

        private Camera m_camera;
        private float m_baseFov;
        private bool m_baseFovCaptured;

        public bool HasPlayer => m_player != null;

        // 3인칭 시점 여부 — 원본은 3인칭에서 크로스헤어/스코프/FOV줌을 전부 숨긴다 (gamemain.cpp:2755·2935·3169, Camera_F1mode 게이팅).
        private bool IsThirdPerson
        {
            get
            {
                if (m_playerController == null) m_playerController = FindFirstObjectByType<PlayerController>();
                return m_playerController != null && !m_playerController.FirstPerson;
            }
        }

        // 현재 무기가 크로스헤어 표시 무기인지 (WeaponData.crosshair). 죽었거나 무기 없으면 false.
        public bool ShowCrosshair
        {
            get
            {
                if (m_player == null || !m_player.Alive) return false;
                Weapon w = m_player.CurrentWeapon;
                return w != null && w.WeaponData != null && w.WeaponData.crosshair;
            }
        }

        // 현재 조준 오차 (ErrorRange 정수 단위, 상태+반동 raw). 크로스헤어 간격에 1:1 대응.
        public float ErrorRange => m_player != null ? m_player.GunsightErrorRange : 0f;

        private void Start()
        {
            // 좌우 오차 글리프를 crosshairErrorRoot 중앙에 앵커(중앙 기준 ±오프셋용). 실제 위치는 Update 에서 ±ErrorRange 로 갱신.
            // 원본 gamemain.cpp:3195-3198 — 0xBD("ｽ")=왼쪽, 0xBE("ｾ")=오른쪽, 32×32, 흰색 alpha 0.5, 수직 화면 중앙 정렬.
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 size = new Vector2(32f, 32f);
            Color col = new Color(1f, 1f, 1f, 0.5f);

            m_crosshairLeft = FontManager.CreateSpriteText<XOPSSpriteText>(
                crosshairErrorRoot, "½", center, center, Vector2.zero, size, size, col, TextAnchor.MiddleCenter, 0f);
            m_crosshairRight = FontManager.CreateSpriteText<XOPSSpriteText>(
                crosshairErrorRoot, "¾", center, center, Vector2.zero, size, size, col, TextAnchor.MiddleCenter, 0f);

            BuildScopeReticles();
        }

        private void Update()
        {
            // 스폰/리스폰/사망으로 Player 가 교체·소멸될 수 있어 매 프레임 재취득.
            m_player = MapLoader.Player;

            // 스코프 토글 입력 → Human 에 위임 (상태·자동해제는 Human 이 관리, 원본 ChangeScopeMode).
            // 3인칭에서도 스코프 상태값은 토글되지만(원본 InputPlayer 는 F1mode 무관 호출) 화면엔 아무것도 안 그려진다.
            if (m_player != null && InputManager.Instance.Zoom.WasPressedThisFrame())
                m_player.ToggleScope();

            bool thirdPerson = IsThirdPerson;
            ApplyScope(thirdPerson);

            // 크로스헤어 표시: 1인칭이고, 무기 crosshair 가 켜져 있고, (스코프 미사용 || 그 스코프가 크로스헤어를 숨기지 않음).
            ScopeData activeScope = m_player != null ? m_player.ActiveScope : null;
            bool showCross = !thirdPerson && ShowCrosshair && !(activeScope != null && activeScope.hideCrosshair);
            crosshairRoot.gameObject.SetActive(showCross);
            if (!showCross) return;

            // 두 글리프를 화면 중앙에서 좌우로 ±ErrorRange (640×480 기준 픽셀 1:1). y 오프셋은 0 (수직 중앙 정렬).
            float er = ErrorRange;
            m_crosshairLeft.rectTransform.anchoredPosition = new Vector2(-er, 0f);
            m_crosshairRight.rectTransform.anchoredPosition = new Vector2(er, 0f);
        }

        // 스코프 오버레이 텍스처/표시 + 카메라 FOV + 레티클(인덱스 일치분만)을 현재 스코프 상태에 맞춰 적용.
        // 3인칭이면 스코프 상태와 무관하게 오버레이/레티클/FOV줌 모두 끈다 (원본 3인칭 게이팅).
        private void ApplyScope(bool thirdPerson)
        {
            bool scoping = m_player != null && m_player.IsScoping && !thirdPerson;
            ScopeData scope = scoping ? m_player.ActiveScope : null;

            if (scopeImage != null)
            {
                if (scoping && scope != null && !string.IsNullOrEmpty(scope.texturePath))
                {
                    Texture2D tex = ImageLoader.LoadTexture(SafePath.Combine(Application.streamingAssetsPath, scope.texturePath));
                    if (tex != null) scopeImage.texture = tex;
                }
                scopeRoot.gameObject.SetActive(scoping && scope != null);

                float aspect = Screen.width / (float)Screen.height;

                if (aspect >= 1.3333333333f)
                {
                    // 가로가 더 긺 → 스코프는 높이 기준 4:3(폭=높이×4/3) 중앙, 남는 좌우 갭을 검정 박스로.
                    float gap = Mathf.Max(0f, (Screen.width - Screen.height * (4f / 3f)) / 2f);
                    leftBox.sizeDelta = new Vector2(gap, leftBox.sizeDelta.y);
                    rightBox.sizeDelta = new Vector2(gap, rightBox.sizeDelta.y);
                    upBox.sizeDelta = new Vector2(upBox.sizeDelta.x, 0f); // 반대 쌍 리셋 (가로↔세로 전환 잔상 방지)
                    downBox.sizeDelta = new Vector2(downBox.sizeDelta.x, 0f);
                }
                else
                {
                    // 세로가 더 긺 → 스코프는 폭 기준 4:3(높이=폭×3/4) 중앙, 남는 상하 갭을 검정 박스로.
                    float gap = Mathf.Max(0f, (Screen.height - Screen.width * (3f / 4f)) / 2f);
                    upBox.sizeDelta = new Vector2(upBox.sizeDelta.x, gap);
                    downBox.sizeDelta = new Vector2(downBox.sizeDelta.x, gap);
                    leftBox.sizeDelta = new Vector2(0f, leftBox.sizeDelta.y); // 반대 쌍 리셋
                    rightBox.sizeDelta = new Vector2(0f, rightBox.sizeDelta.y);
                }
            }

            // 레티클 — 스코프 중일 때 현재 무기 scopeIndex 와 일치하는 것만 활성, 나머지는 끔.
            int activeIdx = (scoping && scope != null) ? m_player.CurrentWeapon.WeaponData.scopeIndex : -1;
            if (m_scopeReticles != null)
            {
                for (int i = 0; i < m_scopeReticles.Count; i++)
                    m_scopeReticles[i].SetActive(i == activeIdx);
            }

            // 카메라 FOV — 비스코프 기본값을 한 번 캡처해두고, 스코프 시 fovDegrees, 해제 시 기본값으로 복원.
            if (m_camera == null) m_camera = Camera.main;
            if (m_camera != null)
            {
                if (!m_baseFovCaptured && !scoping)
                {
                    m_baseFov = m_camera.fieldOfView;
                    m_baseFovCaptured = true;
                }

                if (scoping && scope != null) m_camera.fieldOfView = scope.fovDegrees;
                else if (m_baseFovCaptured) m_camera.fieldOfView = m_baseFov;
            }
        }

        // 시작 시 1회 — 모든 ScopeData 의 레티클(line 묶음)을 컨테이너로 미리 생성해 인덱스별 리스트화. 전부 비활성으로 둠.
        private void BuildScopeReticles()
        {
            m_scopeReticles = new List<GameObject>();

            var list = DataManager.Instance.WeaponParameterData.scopeData;
            if (list == null) return;

            RectTransform parent = scopeLineRoot != null
                ? scopeLineRoot
                : (scopeImage != null ? scopeImage.rectTransform : null);
            if (parent == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var container = new GameObject($"ScopeReticle_{i}", typeof(RectTransform));
                var crt = container.GetComponent<RectTransform>();
                crt.SetParent(parent, false);
                crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = Vector2.zero;

                var lines = list[i].lines;
                if (lines != null)
                {
                    for (int j = 0; j < lines.Count; j++)
                        CreateScopeLine(crt, lines[j]);
                }

                container.SetActive(false);
                m_scopeReticles.Add(container);
            }
        }

        // ScopeLine(start→end) 1개를 회전된 RawImage(흰색 텍스처 × color) 막대로 생성. 중앙 앵커, 640×480 기준 좌표.
        private void CreateScopeLine(RectTransform parent, ScopeLine line)
        {
            Vector2 diff = line.end - line.start;
            float len = diff.magnitude;
            float ang = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            var go = new GameObject("Line", typeof(RectTransform), typeof(RawImage));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (line.start + line.end) * 0.5f;
            rt.sizeDelta = new Vector2(len, Mathf.Max(1f, line.width));
            rt.localRotation = Quaternion.Euler(0f, 0f, ang);

            var img = go.GetComponent<RawImage>();
            img.texture = Texture2D.whiteTexture;
            img.color = line.color;
            img.raycastTarget = false;
        }
    }
}
