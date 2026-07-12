using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    public class MaingameUIStaticLayout : MonoBehaviour
    {
        [SerializeField] 
        private RectTransform normalBottomLeft, normalBottomRight;
        [SerializeField]
        private CanvasScaler scaler;

        private void Start()
        {
            Color32 fontColor = new Color(1.0f, 1.0f, 1.0f, 0.75f);
            Vector2 fontSize = new Vector2(32, 32);
            Vector2 oneZero = new Vector2(1, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "\u00B3\u00B4\u00B4\u00B4\u00B4\u00B4\u00B4\u00B5", Vector2.zero, Vector2.zero, new Vector2(15, 105), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "\u00C3\u00C4\u00C4\u00C4\u00C4\u00C4\u00C4\u00C5", Vector2.zero, Vector2.zero, new Vector2(15, 105 - 32), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "\u00B3\u00B4\u00B4\u00B6\u00B7\u00B7\u00B7\u00B8\u00B9", Vector2.zero, Vector2.zero, new Vector2(15, 55), new Vector2(32 * 8, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "\u00C3\u00C4\u00C4\u00C6\u00C7\u00C7\u00C7\u00C8\u00C9", Vector2.zero, Vector2.zero, new Vector2(15, 55 - 32), new Vector2(32 * 8, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomRight, "\u00B0\u00B1\u00B1\u00B1\u00B1\u00B1\u00B1\u00B2", oneZero, oneZero, new Vector2(0, 98), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperRight, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomRight, "\u00C0\u00C1\u00C1\u00C1\u00C1\u00C1\u00C1\u00C2", oneZero, oneZero, new Vector2(0, 98 - 32), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperRight, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomRight, "\u00D0\u00D1\u00D1\u00D1\u00D1\u00D1\u00D1\u00D2", oneZero, oneZero, new Vector2(0, 98 - 64), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperRight, 0);

            // UI 배수 — General.UIScale. 해상도별 상한은 ConfigManager가 이미 클램프해 두므로 그대로 쓴다.
            float uiScale = ConfigManager.Loaded
                ? ConfigManager.Instance.GetFloat(ConfigManager.SectionGeneral, ConfigManager.KeyUIScale, 1f)
                : 1f;
            scaler.scaleFactor = uiScale;
        }
    }
}
