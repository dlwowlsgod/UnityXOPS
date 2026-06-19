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

        private void Start()
        {
            var height = GetComponent<OpeningScene>().OpeningData.letterBoxHeight;
            top.sizeDelta = new Vector2(0, height);
            bottom.sizeDelta = new Vector2(0, height);
        }
    }
}
