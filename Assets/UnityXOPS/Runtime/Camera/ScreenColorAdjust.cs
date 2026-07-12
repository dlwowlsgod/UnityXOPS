using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 카메라의 최종 이미지에 전역 밝기/감마를 적용하는 포스트 이펙트(Built-in RP OnRenderImage).
    /// 값은 ConfigManager 캐시(Brightness/Gamma)에서 매 프레임 읽어 라이브로 반영된다.
    /// 밝기 1.0·감마 1.0(중립)이면 블릿만 하고 셰이더를 건너뛴다. 카메라 뷰포트 rect(레터박스)와 함께 동작한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ScreenColorAdjust : MonoBehaviour
    {
        private Material m_material;

        /// <summary>
        /// 렌더된 화면(src)에 밝기/감마를 적용해 dst에 출력한다. Built-in RP가 카메라 렌더 직후 호출한다.
        /// </summary>
        /// <param name="src">카메라가 렌더한 원본.</param>
        /// <param name="dst">출력 대상.</param>
        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (!ConfigManager.Loaded)
            {
                Graphics.Blit(src, dst);
                return;
            }

            float brightness = ConfigManager.Instance.Brightness;
            float gamma = ConfigManager.Instance.Gamma;
            if (Mathf.Approximately(brightness, 1f) && Mathf.Approximately(gamma, 1f))
            {
                Graphics.Blit(src, dst);
                return;
            }

            if (m_material == null)
            {
                Shader shader = Shader.Find("UnityXOPS/ScreenColorAdjust");
                if (shader == null)
                {
                    Graphics.Blit(src, dst);
                    return;
                }
                m_material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            m_material.SetFloat("_Brightness", brightness);
            m_material.SetFloat("_Gamma", Mathf.Max(0.001f, gamma));
            Graphics.Blit(src, dst, m_material);
        }

        private void OnDestroy()
        {
            if (m_material != null)
            {
                Destroy(m_material);
            }
        }
    }
}
