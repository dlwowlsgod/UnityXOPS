using JJLUtility;
using JJLUtility.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace UnityXOPS
{
    /// <summary>
    /// Human 캐릭터의 신체, 팔, 다리 메시와 애니메이션을 렌더링하고 제어하는 시각 표현 컴포넌트.
    /// </summary>
    public class HumanVisual : MonoBehaviour
    {
        [SerializeField]
        private Transform bodyRoot, armRoot, leftArmRoot, rightArmRoot, legRoot;

        private MeshFilter m_leftArmMeshFilter, m_rightArmMeshFilter, m_legMeshFilter;
        private MeshRenderer m_leftArmMeshRenderer, m_rightArmMeshRenderer, m_legMeshRenderer;

        private List<Material> m_humanMaterials;
        private List<Mesh> m_leftArmMeshes, m_rightArmMeshes, m_legMeshes;

        private HumanModelData m_humanModelData;
        private HumanArmModelData m_humanArmModelData;
        private HumanLegModelData m_humanLegModelData;

        private List<HumanAnimation> m_legAnimation;
        private HumanAnimation m_idleAnimation;
        private HumanAnimation m_walkAnimation;
        private HumanAnimation m_runAnimation;

        private bool m_fixLeft, m_fixRight;

        private const float k_fixedArmPitch = -70f;

        /// <summary>
        /// 인간 데이터로부터 신체, 팔, 다리 메시와 머티리얼을 로드해 초기화한다.
        /// </summary>
        /// <param name="data">인간 파라미터 데이터.</param>
        public void CreateHumanVisual(HumanData data)
        {
            m_humanMaterials = new List<Material>();
            m_leftArmMeshes = new List<Mesh>();
            m_rightArmMeshes = new List<Mesh>();
            m_legMeshes = new List<Mesh>();

            m_leftArmMeshFilter = leftArmRoot.gameObject.GetComponent<MeshFilter>();
            m_rightArmMeshFilter = rightArmRoot.gameObject.GetComponent<MeshFilter>();
            m_legMeshFilter = legRoot.gameObject.GetComponent<MeshFilter>();
            m_leftArmMeshRenderer = leftArmRoot.gameObject.GetComponent<MeshRenderer>();
            m_rightArmMeshRenderer = rightArmRoot.gameObject.GetComponent<MeshRenderer>();
            m_legMeshRenderer = legRoot.gameObject.GetComponent<MeshRenderer>();

            var modelParameter = DataManager.Instance.HumanParameterData.humanModelData;
            var armModelParameter = DataManager.Instance.HumanParameterData.humanArmModelData;
            var legModelParameter = DataManager.Instance.HumanParameterData.humanLegModelData;

            int modelIndex = data.modelIndex;
            if (modelIndex < 0 || modelIndex >= modelParameter.Count)
            {
                return;
            }

            float bodyScale = DataManager.Instance.HumanParameterData.humanGeneralData.humanBodyScale;
            float bodyHeight = DataManager.Instance.HumanParameterData.humanGeneralData.humanBodyHeight;
            m_humanModelData = modelParameter[modelIndex];

            //texture
            foreach (var path in m_humanModelData.textures.Select((value, index) => (value, index)))
            {
                var fullPath = SafePath.Combine(Application.streamingAssetsPath, path.value);

                if (MapLoader.Instance.HumanMaterialCache.ContainsKey(fullPath))
                {
                    m_humanMaterials.Add(MapLoader.Instance.HumanMaterialCache[fullPath]);
                    continue;
                }

                var texture = ImageLoader.LoadTexture(fullPath);
                texture.name = Path.GetFileName(fullPath);

                var material = new Material(MaterialManager.Instance.MainMaterial);
                material.mainTexture = texture;
                material.name = texture.name;

                MapLoader.Instance.HumanMaterialCache[fullPath] = material;
                m_humanMaterials.Add(material);
            }

            //body
            var bodyDataList = m_humanModelData.modelData;
            bodyRoot.localPosition = new Vector3(0, bodyHeight, 0);
            bodyRoot.localScale *= bodyScale;
            foreach (var bodyData in bodyDataList.Select((value, index) => (value, index)))
            {
                var bodyObj = new GameObject($"Body_{bodyData.index}");
                bodyObj.transform.SetParent(bodyRoot);
                bodyObj.transform.SetLocalPositionAndRotation(bodyData.value.position, Quaternion.Euler(bodyData.value.rotation));
                bodyObj.transform.localScale = bodyData.value.scale;

                var bodyMeshFilter = bodyObj.AddComponent<MeshFilter>();
                var bodyMeshPath = SafePath.Combine(Application.streamingAssetsPath, bodyData.value.modelPath);
                bodyMeshFilter.sharedMesh = ModelLoader.LoadMesh(bodyMeshPath);

                var bodyMeshRenderer = bodyObj.AddComponent<MeshRenderer>();
                if (bodyData.value.textureIndex < 0 || bodyData.value.textureIndex >= m_humanMaterials.Count)
                {
                    bodyMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    bodyMeshRenderer.sharedMaterial = m_humanMaterials[bodyData.value.textureIndex];
                }
            }

            //arms
            int armModelIndex = m_humanModelData.armIndex;
            int armTextureIndex = m_humanModelData.armTextureIndex;
            float armHeight = DataManager.Instance.HumanParameterData.humanGeneralData.humanArmHeight;
            armRoot.localPosition = new Vector3(0, armHeight, 0);
            armRoot.localScale *= DataManager.Instance.HumanParameterData.humanGeneralData.humanArmScale;

            if (armModelIndex >= 0 && armModelIndex < armModelParameter.Count)
            {
                m_humanArmModelData = armModelParameter[armModelIndex];

                var leftArmList = m_humanArmModelData.leftArms;
                var rightArmList = m_humanArmModelData.rightArms;

                foreach (var leftArmData in leftArmList)
                {
                    var leftArmMeshPath = SafePath.Combine(Application.streamingAssetsPath, leftArmData);
                    var leftArmMesh = ModelLoader.LoadMesh(leftArmMeshPath);
                    m_leftArmMeshes.Add(leftArmMesh);
                }
                foreach (var rightArmData in rightArmList)
                {
                    var rightArmMeshPath = SafePath.Combine(Application.streamingAssetsPath, rightArmData);
                    var rightArmMesh = ModelLoader.LoadMesh(rightArmMeshPath);
                    m_rightArmMeshes.Add(rightArmMesh);
                }

                if (armTextureIndex < 0 || armTextureIndex >= m_humanMaterials.Count)
                {
                    m_leftArmMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                    m_rightArmMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    m_leftArmMeshRenderer.sharedMaterial = m_humanMaterials[armTextureIndex];
                    m_rightArmMeshRenderer.sharedMaterial = m_humanMaterials[armTextureIndex];
                }

                SetArmModel(2, 2, true, true); //임시. 무기 손에 맞게 수정해야 함
            }

            //legs
            int legModelIndex = m_humanModelData.legIndex;
            int legTextureIndex = m_humanModelData.legTextureIndex;
            float legHeight = DataManager.Instance.HumanParameterData.humanGeneralData.humanLegHeight;
            legRoot.localPosition = new Vector3(0, legHeight, 0);
            legRoot.localScale *= DataManager.Instance.HumanParameterData.humanGeneralData.humanLegScale;
            m_legAnimation = DataManager.Instance.HumanParameterData.humanGeneralData.humanAnimation;

            if (m_legAnimation != null && legModelIndex >= 0 && legModelIndex < legModelParameter.Count)
            {
                m_idleAnimation = m_legAnimation.FirstOrDefault(a => a.name == "Idle");
                m_walkAnimation = m_legAnimation.FirstOrDefault(a => a.name == "Walk");
                m_runAnimation = m_legAnimation.FirstOrDefault(a => a.name == "Run");

                m_humanLegModelData = legModelParameter[legModelIndex];

                foreach (var legData in m_humanLegModelData.legs)
                {
                    var legPath = SafePath.Combine(Application.streamingAssetsPath, legData);
                    var legMesh = ModelLoader.LoadMesh(legPath);
                    m_legMeshes.Add(legMesh);
                }

                if (legTextureIndex < 0 || legTextureIndex >= m_humanMaterials.Count)
                {
                    m_legMeshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    m_legMeshRenderer.sharedMaterial = m_humanMaterials[legTextureIndex];
                }
            }
            
            if (m_idleAnimation != null)
            {
                SetLegModel(m_idleAnimation.index[0]);
            }
            else
            {
                SetLegModel(0);
            }
        }

        public void SetArmModel(int leftIndex, int rightIndex, bool fixLeft, bool fixRight)
        {
            if (m_humanArmModelData == null)
            {
                m_leftArmMeshFilter.sharedMesh = null;
                m_rightArmMeshFilter.sharedMesh = null;
                return;
            }

            m_fixLeft = fixLeft;
            m_fixRight = fixRight;

            // fix된 팔은 즉시 고정 각도로 설정 
            if (m_fixLeft)
                leftArmRoot.localEulerAngles = new Vector3(k_fixedArmPitch, 0f, 0f);
            if (m_fixRight)
                rightArmRoot.localEulerAngles = new Vector3(k_fixedArmPitch, 0f, 0f);

            if (leftIndex >= 0 && leftIndex < m_leftArmMeshes.Count)
            {
                m_leftArmMeshFilter.sharedMesh = m_leftArmMeshes[leftIndex];
            }
            else
            {
                m_leftArmMeshFilter.sharedMesh = null;
            }

            if (rightIndex >= 0 && rightIndex < m_rightArmMeshes.Count)
            {
                m_rightArmMeshFilter.sharedMesh = m_rightArmMeshes[rightIndex];
            }
            else
            {
                m_rightArmMeshFilter.sharedMesh = null;
            }
        }

        public void RotateArmModel(float pitch)
        {
            if (!m_fixLeft)
            {
                leftArmRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }
            if (!m_fixRight)
            {
                rightArmRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }
        }

        public void SetLegModel(int legIndex)
        {
            if (m_legAnimation == null)
            {
                m_legMeshFilter.sharedMesh = null;
                return;
            }

            if (legIndex >= 0 && legIndex < m_legMeshes.Count)
            {
                m_legMeshFilter.sharedMesh = m_legMeshes[legIndex];
            }
            else
            {
                m_legMeshFilter.sharedMesh = null;
            }
        }
    }
}
