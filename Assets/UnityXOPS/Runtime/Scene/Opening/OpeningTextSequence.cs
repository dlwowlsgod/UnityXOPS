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

        private List<XOPSSpriteTextFade> m_fadeTexts;

        private List<OpeningTextData> m_openingTextData;
        private float m_time;

        private void Start()
        {
            m_time = Time.time;

            m_openingTextData = GetComponent<OpeningScene>().OpeningData.openingTextData;
            m_fadeTexts = new();
            foreach (var data in m_openingTextData)
            {
                var text = FontManager.CreateSpriteText<XOPSSpriteTextFade>(root, data.text, Vector2.zero, Vector2.one, data.position, Vector2.zero, data.size, data.color, data.alignment, data.spacing);
                text.name = data.text;
                m_fadeTexts.Add(text);
            }

            for (int i = 0; i < m_fadeTexts.Count; i++)
                StartCoroutine(TextRoutine(m_fadeTexts[i], m_openingTextData[i]));
        }

        /// <summary>
        /// 인자 comp 텍스트를 데이터 d의 타이밍에 맞춰 페이드 인 후 페이드 아웃하는 코루틴.
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