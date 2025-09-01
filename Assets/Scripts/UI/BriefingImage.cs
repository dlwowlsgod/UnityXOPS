using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace UnityXOPS
{
    public class BriefingImage : MonoBehaviour
    {
        [SerializeField]
        private RawImage single;
        [SerializeField]
        private RawImage double0;
        [SerializeField]
        private RawImage double1;

        private void Start()
        {
            if (MissionLoader.Instance.twoImage)
            {
                single.gameObject.SetActive(false);
                double0.gameObject.SetActive(true);
                double1.gameObject.SetActive(true);
                
                var path0 = Path.Combine(Application.streamingAssetsPath, MissionLoader.Instance.imagePath0);
                var path1 = Path.Combine(Application.streamingAssetsPath, MissionLoader.Instance.imagePath1);
                var texture0 = ImageManager.Instance.LoadImage(path0);
                var texture1 = ImageManager.Instance.LoadImage(path1);
                double0.texture = texture0;
                double1.texture = texture1;
            }
            else
            {
                single.gameObject.SetActive(true);
                double0.gameObject.SetActive(false);
                double1.gameObject.SetActive(false);

                var path = Path.Combine(Application.streamingAssetsPath, MissionLoader.Instance.imagePath0);
                var texture = ImageManager.Instance.LoadImage(path);
                single.texture = texture;
            }
        }
    }
}