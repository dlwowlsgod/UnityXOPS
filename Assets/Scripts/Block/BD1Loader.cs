using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UnityXOPS
{
    /// <summary>
    /// BD1Loader 클래스는 <see cref="BD1Reader">BD1Reader</see>가 읽는 bd1 데이터를 실제 <see cref="GameObject">GameObject</see>로 변환하는데 사용합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// </remarks>
    //resharper disable once InconsistentNaming
    public class BD1Loader : Singleton<BD1Loader>
    {
        // http://openxops.net/filesystem-bd1.php
        // 를 Unity에 맞게 변환
        private static readonly int[][] VertexPosition =
        {
            new[] { 0, 3, 2, 1 },
            new[] { 7, 4, 5, 6 },
            new[] { 4, 0, 1, 5 },
            new[] { 5, 1, 2, 6 },
            new[] { 6, 2, 3, 7 },
            new[] { 7, 3, 0, 4 }
        };
        
        private static readonly int Mode = Shader.PropertyToID("_Mode");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        
        public bool Load { get; private set; }
        
        /// <summary>
        /// <see cref="BD1Reader">BD1Reader</see>에서 읽은 값을 게임오브젝트로 불러옵니다.
        /// </summary>
        /// <param name="path">불러올 bd1 파일의 경로</param>
        //Resharper disable once InconsistentNaming
        public void LoadBD1(string path)
        {
            if (!BD1Reader.Instance.Read)
            {
#if UNITY_EDITOR
                Debug.Log("[BD1Loader] Block data not loaded.");
#endif
                return;
            }
            
            var fileName = Path.GetFileNameWithoutExtension(path);
            var directory = Path.GetDirectoryName(path);

            var texturePaths = BD1Reader.Instance.texturePaths;
            var blockData = BD1Reader.Instance.blockData;

            //텍스쳐를 머티리얼로 변환
            for (var i = 0; i < texturePaths.Count; i++)
            {
                var material = new Material(Shader.Find("Standard")); //Unity의 지원 종료된 레거시 셰이더.
                material.SetFloat(Mode, 1f); //Cutout 모드.
                material.SetFloat(Glossiness, 0.0f); //PBR Metallic/Roughness의 경우 광택이 심해 이를 제거
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.One); //불투명 처리
                material.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero); //불투명 처리
                material.SetInt(ZWrite, 1); //깊이 값 처리
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON"); //사용하지 않기에 off
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON"); //사용하지 않기에 off
                material.renderQueue = 2450; //그려지는 순서
                
                //만약 텍스쳐 경로가 있다면 bd1 위치와 경로를 합쳐 텍스쳐를 머티리얼로 불러옴
                if (!string.IsNullOrEmpty(texturePaths[i].path))
                {
                    if (directory != null)
                    {
                        var texturePath = Path.Combine(directory, texturePaths[i].path);
                        var textureName = Path.GetFileName(texturePath);
                        if (File.Exists(texturePath))
                        {
                            var texture = ImageManager.Instance.LoadImage(texturePath);
                            if (texture)
                            {
                                material.name = textureName;
                                material.mainTexture = texture;
                            }
                        }
                    }
                }
                //아닐 경우 흰색 기본 텍스쳐
                else
                {
                    material.name = $"null_id{i}";
                }

                texturePaths[i].material = material;

#if UNITY_EDITOR
                Debug.Log($"[BD1Loader][{fileName}] Texture {i} built");
#endif
            }
            
            //텍스쳐별로 그룹을 만들어 블록을 하나의 메시로 합치는 작업
            //드로우콜을 2000에서 20 단위로 줄일 수 있음
            //컬링이 안되지만 블럭당 24폴리곤 * 160이니까 충분히 감당 가능
            var combineInstancesByMaterial = new Dictionary<Material, List<CombineInstance>>();
            
            //블록 데이터를 메시로 변환
            for (var i = 0; i < blockData.Count; i++)
            {
                for (var j = 0; j < 6; j++)
                {
                    var material = texturePaths[blockData[i].textureIndices[j]].material;

                    var mesh = new Mesh
                    {
                        name = $"Block_{i}_Face_{j}",
                        vertices = new[]
                        {
                            blockData[i].vertices[VertexPosition[j][0]],
                            blockData[i].vertices[VertexPosition[j][1]],
                            blockData[i].vertices[VertexPosition[j][2]],
                            blockData[i].vertices[VertexPosition[j][3]]
                        },
                        triangles = new[]
                        {
                            0, 1, 2, 2, 3, 0
                        },
                        uv = new[]
                        {
                            blockData[i].faces[j].uv[0],
                            blockData[i].faces[j].uv[1],
                            blockData[i].faces[j].uv[2],
                            blockData[i].faces[j].uv[3]
                        }
                    };
                    
                    //Bound를 설정해야 잘못된 프러스텀 컬링을 예방함
                    mesh.RecalculateBounds();
                    //Normal을 재계산해야 더 자연스러움
                    mesh.RecalculateNormals();

                    
                    var combineInstance = new CombineInstance
                    {
                        mesh = mesh,
                        transform = Matrix4x4.identity
                    };

                    // 같은 텍스쳐끼리 CombineInstance를 그룹화
                    if (!combineInstancesByMaterial.ContainsKey(material))
                    {
                        combineInstancesByMaterial[material] = new List<CombineInstance>();
                    }
                    combineInstancesByMaterial[material].Add(combineInstance);
                }
#if UNITY_EDITOR
                Debug.Log($"[BD1Loader][{fileName}] Block {i} built");
#endif
            }

            //방금 그룹화한 CombineInstance Mesh를 합치는 과정
            foreach (var kvp in combineInstancesByMaterial)
            {
                var material = kvp.Key;
                var combineInstances = kvp.Value;
                
                var combinedGo = new GameObject($"CombinedMesh_{material.name}");
                combinedGo.transform.SetParent(transform);
                combinedGo.transform.localPosition = Vector3.zero;
                combinedGo.transform.localRotation = Quaternion.identity;
                combinedGo.transform.localScale = Vector3.one;

                var meshFilter = combinedGo.AddComponent<MeshFilter>();
                var meshRenderer = combinedGo.AddComponent<MeshRenderer>();
                
                var combinedMesh = new Mesh();
                combinedMesh.name = $"combined_mesh_{material.name}";
                combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
                
                meshFilter.mesh = combinedMesh;
                meshRenderer.material = material;
#if UNITY_EDITOR
                Debug.Log($"[BD1Loader][{fileName}] Combined mesh created for material: {material.name}");
#endif
            }
            
#if UNITY_EDITOR
            Debug.Log($"[BD1Loader][{fileName}] Block data completely loaded");
#endif

            Load = true;
        }
        
        /// <summary>
        /// 로드한 블록 데이터를 전부 파괴합니다.
        /// </summary>
        //Resharper disable once InconsistentNaming
        public void DestroyBD1()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
#if UNITY_EDITOR
            Debug.Log("[BD1Loader] Block data destroyed.");
#endif
        }
    }
}