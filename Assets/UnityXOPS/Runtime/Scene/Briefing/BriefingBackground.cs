using JJLUtility.IO;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 브리핑 씬의 배경 타이틀 이미지를 로드하여 표시하는 컴포넌트.
    /// </summary>
    public class BriefingBackground : MonoBehaviour
    {
        [SerializeField]
        private RawImage titleImage;

        /// <summary>
        /// 타이틀 DDS 이미지를 로드하여 RawImage 텍스처로 설정한다.
        /// </summary>
        private void Start()
        {
            titleImage.texture = ImageLoader.LoadTexture(Path.Combine(Application.streamingAssetsPath, "data/title.dds"));
        }
    }
}
