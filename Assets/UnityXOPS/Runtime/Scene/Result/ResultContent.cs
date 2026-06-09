using UnityEngine;

namespace UnityXOPS
{
    public class ResultContent : MonoBehaviour
    {
        [SerializeField]
        private RectTransform fullnameRoot, resultRoot, timeRoot, firedRoot, hitRoot, accuracyRoot, killHeadshotRoot;
        [SerializeField]
        private Vector2 fullnameFontSize, resultFontSize, infoFontSize;
        [SerializeField]
        private Color32 fullnameColor, completeColor, failedColor, infoColor;

        private void Start()
        {
            var result = EventManager.Instance.Result == MissionResult.Complete ? "mission successful" : "mission failure";
            var color = EventManager.Instance.Result == MissionResult.Complete ? completeColor : failedColor;

            FontManager.CreateSpriteText<XOPSSpriteText>(
                fullnameRoot, MapLoader.Instance.MissionFullname, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                fullnameFontSize, fullnameColor, TextAnchor.UpperCenter, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                resultRoot, result, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                resultFontSize, color, TextAnchor.UpperCenter, 0);

            int min = (int)MapLoader.Stats.PlayTime / 60;
            int sec = (int)MapLoader.Stats.PlayTime % 60;
            FontManager.CreateSpriteText<XOPSSpriteText>(
                timeRoot, $"Time  {min}min {sec}sec", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                infoFontSize, infoColor, TextAnchor.UpperCenter, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                firedRoot, $"Rounds fired  {MapLoader.Stats.Fire}", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                infoFontSize, infoColor, TextAnchor.UpperCenter, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                hitRoot, $"Rounds on target  {MapLoader.Stats.OnTargetInt}", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                infoFontSize, infoColor, TextAnchor.UpperCenter, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                accuracyRoot, $"Accuracy rate  {MapLoader.Stats.AccuracyPercent:F1}%", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                infoFontSize, infoColor, TextAnchor.UpperCenter, 0);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                killHeadshotRoot, $"Kill  {MapLoader.Stats.Kill} / HeadShot  {MapLoader.Stats.Headshot}", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                infoFontSize, infoColor, TextAnchor.UpperCenter, 0);
        }
    }
}
