using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    public class HumanVisual : MonoBehaviour
    {
        [SerializeField]
        private Transform bodyRoot;
        [SerializeField]
        private Transform leftArmRoot;
        [SerializeField]
        private Transform rightArmRoot;
        [SerializeField]
        private Transform legRoot;
        
        private MeshFilter _bodyFilter;
        private MeshFilter _leftArmFilter;
        private MeshFilter _rightArmFilter;
        private MeshFilter _legFilter;
        
        private MeshRenderer _bodyRenderer;
        private MeshRenderer _leftArmRenderer;
        private MeshRenderer _rightArmRenderer;
        private MeshRenderer _legRenderer;

        private HumanParameterSO _humanSO;
        private Material[] _textures;

        private void Awake()
        {
            _bodyFilter = bodyRoot.GetComponent<MeshFilter>();
            _leftArmFilter = leftArmRoot.GetComponent<MeshFilter>();
            _rightArmFilter = rightArmRoot.GetComponent<MeshFilter>();
            _legFilter = legRoot.GetComponent<MeshFilter>();

            _bodyRenderer = bodyRoot.GetComponent<MeshRenderer>();
            _leftArmRenderer = leftArmRoot.GetComponent<MeshRenderer>();
            _rightArmRenderer = rightArmRoot.GetComponent<MeshRenderer>();
            _legRenderer = legRoot.GetComponent<MeshRenderer>();
        }
        
        public void InitializeHumanVisual(HumanParameterSO humanSO, HumanVisualParameterSO visualSO)
        {
            _humanSO = humanSO;
            
            //initialize transform
            transform.localScale = _humanSO.humanScale;
            leftArmRoot.localPosition = _humanSO.armRootPosition;
            rightArmRoot.localPosition = _humanSO.armRootPosition;
            leftArmRoot.localScale = _humanSO.armRootScale;
            rightArmRoot.localScale = _humanSO.armRootScale;
            legRoot.localPosition = _humanSO.legRootPosition;
            legRoot.localScale = _humanSO.legRootScale;

            //load textures
            _textures = new Material[visualSO.textures.Length];
            for (var i = 0; i < visualSO.textures.Length; i++)
            {
                var fullPath = SafeIO.Combine(Application.streamingAssetsPath, visualSO.textures[i]);
                var fileName = Path.GetFileName(fullPath);
                var extension = Path.GetExtension(fileName);
                var texture = ImageLoader.LoadImage(fullPath);

                if (texture == null)
                {
                    _textures[i] = ImageLoader.CutoutMaterial;
                    continue;
                }

                _textures[i] = ImageLoader.ToMaterial(texture, extension == ".png" ? ImageLoader.TransparentMaterial : ImageLoader.CutoutMaterial);
                _textures[i].name = fileName;
            }
            
            //load body meshes
            for (int i = 0; i < visualSO.models.Length; i++)
            {
                var fullPath = SafeIO.Combine(Application.streamingAssetsPath, visualSO.models[i].path);
                var fileName = Path.GetFileName(fullPath);
                var body = ModelLoader.LoadModel(fullPath);
                var bodyObj = new GameObject(fileName, typeof(MeshFilter), typeof(MeshRenderer));
                bodyObj.transform.SetParent(bodyRoot);
                bodyObj.transform.localPosition = visualSO.models[i].position;
                bodyObj.transform.localRotation = Quaternion.Euler(visualSO.models[i].rotation);
                bodyObj.transform.localScale = visualSO.models[i].scale;
                bodyObj.GetComponent<MeshFilter>().sharedMesh = body;
                bodyObj.GetComponent<MeshRenderer>().sharedMaterial = _textures[visualSO.models[i].textureIndex];
            }

            //todo: combine meshes (not individual object)
            //todo: load arm meshes
            //todo: load leg meshes
        }
    }
}
