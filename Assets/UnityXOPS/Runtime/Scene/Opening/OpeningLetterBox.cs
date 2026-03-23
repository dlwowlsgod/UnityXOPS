using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 씬의 상하 레터박스 UI 높이를 데이터 기반으로 설정하는 컴포넌트.
    /// </summary>
    public class OpeningLetterBox : MonoBehaviour
    {
        [SerializeField]
        private RectTransform top;
        [SerializeField]
        private RectTransform bottom;

        /// <summary>
        /// 오프닝 데이터에서 레터박스 높이를 읽어 상단·하단 RectTransform에 적용한다.
        /// </summary>
        private void Start()
        {
            var height = GetComponent<OpeningScene>().OpeningData.letterBoxHeight;
            top.sizeDelta = new Vector2(0, height);
            bottom.sizeDelta = new Vector2(0, height);
        }
    }
}