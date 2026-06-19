using JJLUtility;
using JJLUtility.IO;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 오브젝트 prefab 의 Visual GameObject 에 부착되어, ObjectModelData 의 모델/머티리얼을 자식으로 빌드하는 시각 표현 컴포넌트.
    /// </summary>
    public class ObjectVisual : MonoBehaviour
    {
        [SerializeField]
        private Transform visualRoot;
        private List<Material> m_objectMaterials;

        /// <summary>
        /// ObjectModelData 의 textures, modelData 를 순회해 머티리얼/메시를 자식 Part_{i} 로 빌드한다.
        /// prefab 의 Visual GameObject 에 좌표계 보정용 180° 회전이 이미 적용돼 있으므로, ModelData 값에 추가 보정을 가하지 않는다.
        /// </summary>
        /// <param name="modelData">오브젝트 모델 파라미터 데이터.</param>
        public void CreateObjectVisual(ObjectModelData modelData)
        {
            m_objectMaterials = new List<Material>();

            // textures: WeaponVisual.CreateWeaponVisual 의 텍스처 블록 미러. ObjectMaterialCache 로 맵 단위 공유.
            for (int i = 0; i < modelData.textures.Count; i++)
            {
                string texturePath = modelData.textures[i];
                string fullPath = SafePath.Combine(Application.streamingAssetsPath, texturePath);

                if (MapLoader.Instance.ObjectMaterialCache.TryGetValue(fullPath, out var cached))
                {
                    m_objectMaterials.Add(cached);
                    continue;
                }

                var texture = ImageLoader.LoadTexture(fullPath);
                if (texture == null)
                {
                    m_objectMaterials.Add(MaterialManager.Instance.MainMaterial);
                    continue;
                }
                texture.name = Path.GetFileName(fullPath);

                var material = new Material(MaterialManager.Instance.MainMaterial);
                material.mainTexture = texture;
                material.name = texture.name;

                MapLoader.Instance.ObjectMaterialCache[fullPath] = material;
                m_objectMaterials.Add(material);
            }

            // modelData: 각 ModelData 마다 Part_{i} 자식 GameObject 생성.
            for (int i = 0; i < modelData.modelData.Count; i++)
            {
                ModelData md = modelData.modelData[i];

                var partObj = new GameObject($"Part_{i}");
                partObj.transform.SetParent(visualRoot, false);
                partObj.transform.SetLocalPositionAndRotation(md.position, Quaternion.Euler(md.rotation));
                partObj.transform.localScale = md.scale;

                var meshFilter = partObj.AddComponent<MeshFilter>();
                string meshPath = SafePath.Combine(Application.streamingAssetsPath, md.modelPath);
                meshFilter.sharedMesh = ModelLoader.LoadMesh(meshPath);

                var meshRenderer = partObj.AddComponent<MeshRenderer>();
                if (md.textureIndex < 0 || md.textureIndex >= m_objectMaterials.Count)
                {
                    meshRenderer.sharedMaterial = MaterialManager.Instance.MainMaterial;
                }
                else
                {
                    meshRenderer.sharedMaterial = m_objectMaterials[md.textureIndex];
                }
            }
        }

        /// <summary>
        /// visualRoot 의 localScale 을 균등 scale 로 설정. ObjectGeneralData.modelScale (5.0 → 0.5 환산값) 을 받는다.
        /// 시각 영역만 영향. Object Root 와 colliderRoot 는 건드리지 않는다.
        /// </summary>
        /// <param name="scale">visualRoot 에 적용할 균등 localScale 값.</param>
        public void SetVisualScale(float scale)
        {
            visualRoot.localScale = Vector3.one * scale;
        }
    }
}
