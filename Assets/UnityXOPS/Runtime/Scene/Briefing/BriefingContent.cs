using JJLUtility.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnityXOPS
{
    /// <summary>
    /// 미션 이미지·전체 이름·브리핑 텍스트를 로드하여 브리핑 화면을 구성하는 컴포넌트.
    /// </summary>
    public class BriefingContent : MonoBehaviour
    {
        [SerializeField]
        private RawImage singleImage, doubleFirstImage, doubleSecondImage;
        [SerializeField]
        private GameObject singleObject, doubleObject;
        [SerializeField]
        private Transform fullnameRoot;
        [SerializeField]
        private Vector2 fullnameFontSize;
        [SerializeField]
        private Color32 fullnameColor;
        [SerializeField]
        private TMP_Text textArea;

        /// <summary>
        /// 미션 이미지 수에 따라 단일/이중 레이아웃을 선택하고 이름·브리핑 텍스트를 설정한다.
        /// </summary>
        private void Start()
        {
            if (string.IsNullOrEmpty(MapLoader.Instance.MissionImage1))
            {
                singleObject.SetActive(true);
                doubleObject.SetActive(false);

                singleImage.texture = ImageLoader.LoadTexture(MapLoader.Instance.MissionImage0);
            }
            else
            {
                singleObject.SetActive(false);
                doubleObject.SetActive(true);

                doubleFirstImage.texture = ImageLoader.LoadTexture(MapLoader.Instance.MissionImage0);
                doubleSecondImage.texture = ImageLoader.LoadTexture(MapLoader.Instance.MissionImage1);
            }

            FontManager.CreateSpriteText<XOPSSpriteText>(
                fullnameRoot, MapLoader.Instance.MissionFullname, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                fullnameFontSize, fullnameColor, TextAnchor.UpperCenter, 0);

            textArea.font = FontManager.OSFont;
            textArea.text = MapLoader.Instance.MissionBriefing;
        }
    }
}
