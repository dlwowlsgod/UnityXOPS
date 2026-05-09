using UnityEngine;

namespace UnityXOPS
{
    public class MaingameUIStaticLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform normalBottomLeft, normalBottomRight;

        private void Start()
        {
            Color32 fontColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            Vector2 fontSize = new Vector2(32, 32);
            Vector2 oneZero = new Vector2(1, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "³´´´´´´µ", Vector2.zero, Vector2.zero, new Vector2(15, 105), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "ÃÄÄÄÄÄÄÅ", Vector2.zero, Vector2.zero, new Vector2(15, 105 - 32), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "³´´¶···¸¹", Vector2.zero, Vector2.zero, new Vector2(15, 55), new Vector2(32 * 8, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomLeft, "ÃÄÄÆÇÇÇÈÉ", Vector2.zero, Vector2.zero, new Vector2(15, 55 - 32), new Vector2(32 * 8, 32), fontSize, fontColor, TextAnchor.UpperLeft, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomRight, "°±±±±±±²", oneZero, oneZero, new Vector2(0, 98), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperRight, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomRight, "ÀÁÁÁÁÁÁÂ", oneZero, oneZero, new Vector2(0, 98 - 32), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperRight, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                normalBottomRight, "ÐÑÑÑÑÑÑÒ", oneZero, oneZero, new Vector2(0, 98 - 64), new Vector2(32 * 7, 32), fontSize, fontColor, TextAnchor.UpperRight, 0);
        }
    }
}
