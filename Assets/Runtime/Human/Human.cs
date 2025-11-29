using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    public class Human : MonoBehaviour
    {
        [SerializeField]
        private HumanDataParameterSO humanData;
        [SerializeField]
        private int hp;
        [SerializeField]
        private int maxHP;
        [SerializeField]
        private bool isAlive;

        [Space(20)]
        [SerializeField]
        private Transform cameraRoot;
        [SerializeField]
        private Transform orbitCameraRoot;
        [SerializeField]
        private Transform visualRoot;
        [SerializeField]
        private Transform bodyRoot;
        [SerializeField]
        private Transform leftArmRoot;
        [SerializeField]
        private Transform rightArmRoot;
        [SerializeField]
        private Transform legRoot;

        private Material[] _textures;
        private GameObject[] _leftArmObject;
        private GameObject[] _rightArmObject;
        private GameObject[] _legObject;

        public void CreateHuman(HumanDataParameterSO data)
        {
            leftArmRoot.localPosition = ParameterManager.Instance.HumanParameterSO.armRootPosition;
            leftArmRoot.localScale = ParameterManager.Instance.HumanParameterSO.armRootScale;
            rightArmRoot.localPosition = ParameterManager.Instance.HumanParameterSO.armRootPosition;
            rightArmRoot.localScale = ParameterManager.Instance.HumanParameterSO.armRootScale;
            legRoot.localPosition = ParameterManager.Instance.HumanParameterSO.legRootPosition;
            legRoot.localScale = ParameterManager.Instance.HumanParameterSO.legRootScale;
            
            humanData = data;
            hp = data.hp;
            maxHP = data.hp;
            isAlive = hp > 0;

            var visuals = ParameterManager.Instance.HumanParameterSO.humanVisualParameterSOs;
            if (data.visualIndex >= 0 && data.visualIndex < visuals.Length)
            {
                var visual = visuals[data.visualIndex];
                CreateHumanVisual(visual);
            }
        }
        
        public void ChangeHumanArm(int armIndex)
        {
            if (armIndex < 0 || armIndex >= _leftArmObject.Length)
            {
                return;
            }
            
            for (int i = 0; i < _leftArmObject.Length; i++)
            {
                if (_leftArmObject[i] != null)
                {
                    _leftArmObject[i].SetActive(i == armIndex);
                }

                if (_rightArmObject[i] != null)
                {
                    _rightArmObject[i].SetActive(i == armIndex);
                }
            }
        }
        
        public void ChangeHumanLeg(int legIndex)
        {
            if (legIndex < 0 || legIndex >= _legObject.Length)
            {
                return;
            }
            
            for (int i = 0; i < _legObject.Length; i++)
            {
                if (_legObject[i] != null)
                {
                    _legObject[i].SetActive(i == legIndex);
                }
            }
        }

        private void CreateHumanVisual(HumanVisualParameterSO visual)
        {
            //materials
            var textureList = visual.textures;
            _textures = new Material[textureList.Length];
            for (int i = 0; i < textureList.Length; i++)
            {
                var texturePath = SafeIO.Combine(Application.streamingAssetsPath, textureList[i]);
                var extension = Path.GetExtension(texturePath).ToLower();
                var texture = ImageLoader.LoadImage(texturePath);
                var material = ImageLoader.ToMaterial(texture, extension == ".png" ? ImageLoader.TransparentMaterial : ImageLoader.CutoutMaterial);
                _textures[i] = material;
            }
            
            //body
            var models = visual.models;
            for (int i = 0; i < models.Length; i++)
            {
                var modelPath = SafeIO.Combine(Application.streamingAssetsPath, models[i].path);
                var model = ModelLoader.LoadModel(modelPath);
                if (model == null)
                {
                    continue;
                }

                var obj = new GameObject($"model_{i}");
                obj.transform.SetParent(bodyRoot);
                obj.transform.localPosition = visual.models[i].position;
                obj.transform.localRotation = Quaternion.Euler(visual.models[i].position);
                obj.transform.localScale = visual.models[i].scale;

                obj.AddComponent<MeshFilter>().sharedMesh = model;
                obj.AddComponent<MeshRenderer>().sharedMaterial = _textures[visual.models[i].textureIndex];
            }
            
            var arms = ParameterManager.Instance.HumanParameterSO.humanArmParameterSOs;
            var armCount = ParameterManager.Instance.HumanParameterSO.armName.Length;
            _leftArmObject = new GameObject[armCount];
            _rightArmObject = new GameObject[armCount];
            if (visual.armIndex >= 0 && visual.armIndex < armCount)
            {
                var arm = arms[visual.armIndex];
                CreateHumanLeftArm(arm, visual.armTextureIndex, 2);
                CreateHumanRightArm(arm, visual.armTextureIndex, 2);
            }

            var legs = ParameterManager.Instance.HumanParameterSO.humanLegParameterSOs;
            var legCount = ParameterManager.Instance.HumanParameterSO.legName.Length;
            _legObject = new GameObject[legCount];
            if (visual.legIndex >= 0 && visual.legIndex < legCount)
            {
                var leg = legs[visual.legIndex];
                CreateHumanLeg(leg, visual.legTextureIndex);
            }
        }

        private void CreateHumanLeftArm(HumanArmParameterSO arm, int textureIndex, int currentArmIndex)
        {
            var human = ParameterManager.Instance.HumanParameterSO;
            
            var leftArmPath = arm.armModelsLeft;
            for (int i = 0; i < leftArmPath.Length; i++)
            {
                var leftArmModelPath = SafeIO.Combine(Application.streamingAssetsPath, leftArmPath[i]);
                var leftArmModel = ModelLoader.LoadModel(leftArmModelPath);

                if (leftArmModel == null)
                {
                    continue;
                }
                
                var leftArmObj = new GameObject($"left_arm_{i}");
                leftArmObj.transform.SetParent(leftArmRoot);
                leftArmObj.transform.localPosition = Vector3.zero;
                leftArmObj.transform.localRotation = Quaternion.identity;
                leftArmObj.transform.localScale = Vector3.one;
                leftArmObj.AddComponent<MeshFilter>().sharedMesh = leftArmModel;
                leftArmObj.AddComponent<MeshRenderer>().sharedMaterial = _textures[textureIndex];
                _leftArmObject[i] = leftArmObj;

                if (i != currentArmIndex)
                {
                    leftArmObj.SetActive(false);
                }
            }
        }

        private void CreateHumanRightArm(HumanArmParameterSO arm, int textureIndex, int currentArmIndex)
        {
            var human = ParameterManager.Instance.HumanParameterSO;
            
            var rightArmPath = arm.armModelsRight;
            for (int i = 0; i < rightArmPath.Length; i++)
            {
                var rightArmModelPath = SafeIO.Combine(Application.streamingAssetsPath, rightArmPath[i]);
                var rightArmModel = ModelLoader.LoadModel(rightArmModelPath);
                
                if (rightArmModel == null)
                {
                    continue;
                }
                
                var rightArmObj = new GameObject($"right_arm_{i}");
                rightArmObj.transform.SetParent(rightArmRoot);
                rightArmObj.transform.localPosition = Vector3.zero;
                rightArmObj.transform.localRotation = Quaternion.identity;
                rightArmObj.transform.localScale = Vector3.one;
                rightArmObj.AddComponent<MeshFilter>().sharedMesh = rightArmModel;
                rightArmObj.AddComponent<MeshRenderer>().sharedMaterial = _textures[textureIndex];
                _rightArmObject[i] = rightArmObj;
                
                if (i != currentArmIndex)
                {
                    rightArmObj.SetActive(false);
                }
            }
        } 

        private void CreateHumanLeg(HumanLegParameterSO leg, int textureIndex)
        {
            var human = ParameterManager.Instance.HumanParameterSO;
            
            var legPath = leg.legModels;
            for (int i = 0; i < legPath.Length; i++)
            {
                var legModelPath = SafeIO.Combine(Application.streamingAssetsPath, legPath[i]);
                var legModel = ModelLoader.LoadModel(legModelPath);
                
                if (legModel == null)
                {
                    Debug.Log($"{i}, {legModelPath}");
                    continue;
                }
                
                var legObj = new GameObject($"leg_{i}");
                legObj.transform.SetParent(legRoot);
                legObj.transform.localPosition = Vector3.zero;
                legObj.transform.localRotation = Quaternion.identity;
                legObj.transform.localScale = Vector3.one;
                legObj.AddComponent<MeshFilter>().sharedMesh = legModel;
                legObj.AddComponent<MeshRenderer>().sharedMaterial = _textures[textureIndex];
                _legObject[i] = legObj;
                
                legObj.SetActive(false);
            }
            _legObject[0].SetActive(true);
        }
    }
}
