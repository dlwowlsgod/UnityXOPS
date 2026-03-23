using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 씬에서 여러 텍스트 항목을 생성하고 각각의 페이드 시퀀스를 실행하는 컴포넌트.
    /// </summary>
    public class OpeningTextSequence : MonoBehaviour
    {
        [SerializeField]
        private Transform root;

        [SerializeField]
        private List<XOPSSpriteTextFade> fadeTexts;

        private List<OpeningTextData> m_openingTextData;
        private float m_time;

        /// <summary>
        /// 오프닝 텍스트 데이터를 기반으로 스프라이트 텍스트를 생성하고 각 페이드 코루틴을 시작한다.
        /// </summary>
        private void Start()
        {
            m_time = Time.time;

            m_openingTextData = GetComponent<OpeningScene>().OpeningData.openingTextData;
            fadeTexts = new();
            foreach (var data in m_openingTextData)
            {
                var text = FontManager.CreateSpriteText<XOPSSpriteTextFade>(root, data.text, Vector2.zero, Vector2.one, data.position, Vector2.zero, data.size, data.color, data.alignment, data.spacing);
                text.name = data.text;
                fadeTexts.Add(text);
            }

            for (int i = 0; i < fadeTexts.Count; i++)
                StartCoroutine(TextRoutine(fadeTexts[i], m_openingTextData[i]));
        }

        /// <summary>
        /// 지정된 타이밍에 따라 텍스트를 페이드 인 후 페이드 아웃하는 코루틴.
        /// </summary>
        private IEnumerator TextRoutine(XOPSSpriteTextFade comp, OpeningTextData d)
        {
            comp.SetAlphaZero();

            yield return new WaitUntil(() => Time.time - m_time >= d.fadeInStart);
            comp.FadeIn(d.fadeInEnd - d.fadeInStart);

            yield return new WaitUntil(() => Time.time - m_time >= d.fadeOutStart);
            comp.FadeOut(d.fadeOutEnd - d.fadeOutStart);
        }
    }
}