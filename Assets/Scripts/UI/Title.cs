using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    public class Title : MonoBehaviour
    {
        [SerializeField]
        private string titlePath;

        private RawImage _image;
        
        private void Start()
        {
            var path = Path.Combine(Application.streamingAssetsPath, titlePath);
            var texture = ImageManager.Instance.LoadImage(path);
            _image = GetComponent<RawImage>();
            _image.texture = texture;
        }
    }
}