using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 버전 문자열을 그림자와 함께 스프라이트 텍스트로 렌더링하는 컴포넌트.
    /// </summary>
    public class GameVersionText : MonoBehaviour
    {
        [SerializeField]
        private RectTransform root;
        [SerializeField]
        private Vector2 position;
        [SerializeField]
        private Color32 textColor;
        [SerializeField]
        private Color32 shadowColor;

        private void Start()
        {
            FontManager.CreateSpriteText<XOPSSpriteText>(
                root, Application.version, Vector2.zero, Vector2.one, position + new Vector2(1, -1), Vector2.zero, new Vector2(18, 22), shadowColor, TextAnchor.UpperRight, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                root, Application.version, Vector2.zero, Vector2.one, position, Vector2.zero, new Vector2(18, 22), textColor, TextAnchor.UpperRight, 0);
        }
    }
}
