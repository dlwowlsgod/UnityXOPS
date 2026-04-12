using UnityEngine;
using UnityEngine.UI;
using System.IO;
using JJLUtility.IO;

namespace UnityXOPS
{
    /// <summary>
    /// 타이틀 DDS 이미지를 로드하여 RawImage에 적용하고 크기·위치를 설정하는 컴포넌트.
    /// </summary>
    public class GameTitleImage : MonoBehaviour
    {
        [SerializeField]
        private RawImage titleImage;
        [SerializeField]
        private Vector2 position;
        [SerializeField]
        private Vector2 size;

        private const string k_titlePath = "data/title.dds";

        /// <summary>
        /// 타이틀 이미지를 로드하고 RectTransform에 크기·위치를 적용한다.
        /// </summary>
        private void Start()
        {
            var path = Path.Combine(Application.streamingAssetsPath, k_titlePath);
            var texture = ImageLoader.LoadTexture(path);
            titleImage.texture = texture;
            
            var rect = titleImage.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
