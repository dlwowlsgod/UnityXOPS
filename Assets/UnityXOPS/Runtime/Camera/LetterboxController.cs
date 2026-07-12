using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 카메라를 지정 화면비로 레터박스/필러박스 하는 전역 컨트롤러(persistent 싱글톤).
    /// 타깃 비율과 실제 화면(Screen) 비율을 비교해 Camera.main의 뷰포트 rect를 중앙 정렬 서브렉트로 맞춘다.
    /// 화면이 더 넓으면 좌우 띠(필러박스), 더 좁으면 상하 띠(레터박스), 같으면 꽉 채운다.
    /// rect 바깥은 전용 배경 카메라가 매 프레임 검게 클리어하며, 씬 전환 시 새 메인 카메라에 자동 재적용한다.
    /// </summary>
    public class LetterboxController : SingletonBehavior<LetterboxController>
    {
        private float m_targetAspect;        // 목표 가로/세로 비. 0 이하면 레터박스 없이 꽉 채움.
        private Camera m_backgroundCamera;   // rect 바깥을 검게 채우는 배경 카메라.
        private Camera m_appliedCamera;      // 마지막으로 rect를 적용한 메인 카메라(씬 전환/변경 감지용).
        private int m_appliedWidth = -1;
        private int m_appliedHeight = -1;
        private float m_appliedAspect = -1f;
        private Rect m_viewport = new Rect(0f, 0f, 1f, 1f);

        /// <summary>현재 적용된 정규화 뷰포트 rect(0~1). UI를 게임 영역에 정렬할 때 참고한다.</summary>
        public Rect Viewport => m_viewport;

        /// <summary>
        /// 레터박스 목표 화면비를 설정한다. 다음 LateUpdate에 뷰포트가 갱신된다.
        /// </summary>
        /// <param name="aspect">가로/세로 비(예: 4f/3f, 16f/9f). 0 이하면 레터박스 해제(꽉 채움).</param>
        public void SetTargetAspect(float aspect)
        {
            m_targetAspect = aspect;
        }

        /// <summary>
        /// width/height로 목표 화면비를 설정한다. 해상도 값에서 바로 넘길 때 쓴다.
        /// </summary>
        /// <param name="width">콘텐츠 가로(px). 0 이하면 레터박스 해제</param>
        /// <param name="height">콘텐츠 세로(px). 0 이하면 레터박스 해제</param>
        public void SetTargetResolution(int width, int height)
        {
            SetTargetAspect((width > 0 && height > 0) ? (float)width / height : 0f);
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == m_appliedCamera && Screen.width == m_appliedWidth &&
                Screen.height == m_appliedHeight && Mathf.Approximately(m_targetAspect, m_appliedAspect))
            {
                return;
            }
            Apply(cam);
        }

        /// <summary>
        /// 현재 화면 크기·타깃 비율로 뷰포트를 재계산해 메인 카메라에 적용하고 배경 카메라를 보장한다.
        /// </summary>
        /// <param name="cam">적용 대상 메인 카메라(null이면 rect 적용은 건너뛴다).</param>
        private void Apply(Camera cam)
        {
            m_viewport = ComputeViewport(m_targetAspect, Screen.width, Screen.height);
            if (cam != null)
            {
                cam.rect = m_viewport;
                // 밝기/감마 포스트 이펙트를 메인 카메라에 보장(씬마다 새 카메라에 자동 부착).
                if (cam.GetComponent<ScreenColorAdjust>() == null)
                {
                    cam.gameObject.AddComponent<ScreenColorAdjust>();
                }
            }
            EnsureBackgroundCamera();
            m_appliedCamera = cam;
            m_appliedWidth = Screen.width;
            m_appliedHeight = Screen.height;
            m_appliedAspect = m_targetAspect;
        }

        /// <summary>
        /// 타깃 비율을 화면 안에 중앙 정렬한 정규화 서브렉트를 계산한다.
        /// </summary>
        /// <param name="target">목표 가로/세로 비. 0 이하면 꽉 채운다.</param>
        /// <param name="screenW">현재 화면 가로(px)</param>
        /// <param name="screenH">현재 화면 세로(px)</param>
        /// <returns>정규화 rect(0~1). 레터박스 불필요 시 (0,0,1,1).</returns>
        private static Rect ComputeViewport(float target, int screenW, int screenH)
        {
            if (target <= 0f || screenH <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }
            float screenAspect = (float)screenW / screenH;
            if (screenAspect > target)
            {
                // 화면이 더 넓다 → 좌우 필러박스
                float w = target / screenAspect;
                return new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            // 화면이 더 좁다(또는 세로가 길다) → 상하 레터박스
            float h = screenAspect / target;
            return new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }

        /// <summary>
        /// rect 바깥을 검게 채우는 배경 카메라를 1회 생성한다(이미 있으면 유지).
        /// 아무것도 렌더하지 않고(cullingMask=0) 메인보다 먼저(depth -100) 전체를 검게 클리어한다.
        /// </summary>
        private void EnsureBackgroundCamera()
        {
            if (m_backgroundCamera != null)
            {
                return;
            }
            GameObject go = new GameObject("LetterboxBackground");
            go.transform.SetParent(transform, false);
            m_backgroundCamera = go.AddComponent<Camera>();
            m_backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            m_backgroundCamera.backgroundColor = Color.black;
            m_backgroundCamera.cullingMask = 0;
            m_backgroundCamera.depth = -100f;
            m_backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
            m_backgroundCamera.allowMSAA = false;
            m_backgroundCamera.allowHDR = false;
            m_backgroundCamera.useOcclusionCulling = false;
        }
    }
}
