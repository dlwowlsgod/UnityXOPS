using UnityEngine;
using TMPro;

namespace UnityXOPS
{
    /// <summary>
    /// 이벤트 메시지(Msg 트리거) 화면 표시. EventManager 의 현재 메시지 텍스트/페이드 알파를 매 프레임 폴링해
    /// TMP 텍스트에 반영한다. 출력 타이밍·5초 수명·페이드는 EventManager 가 33.333fps 로 관리(원본 충실), 이 클래스는 표시만.
    /// </summary>
    public class Message : MonoBehaviour
    {
        private TMP_Text m_text;

        private void Start()
        {
            m_text = GetComponent<TMP_Text>();
            if (m_text != null)
            {
                SetVisible(false);
            }

            m_text.font = FontManager.OSFont;
        }

        private void Update()
        {
            if (m_text == null) return;

            // EventManager 가 없으면(메뉴/브리핑 등) 표시 안 함. SingletonBehavior 라 maingame 에선 항상 존재.
            EventManager ev = EventManager.Instance;
            if (ev == null || ev.CurrentMessageId < 0)
            {
                SetVisible(false);
                return;
            }

            m_text.text = ev.CurrentMessageText;

            // 페이드 — EventManager 가 계산한 알파(0~1)를 TMP 색 알파에 반영.
            Color c = m_text.color;
            c.a = ev.CurrentMessageAlpha;
            m_text.color = c;
        }

        private void SetVisible(bool visible)
        {
            Color c = m_text.color;
            c.a = visible ? 1f : 0f;
            m_text.color = c;
        }
    }
}
