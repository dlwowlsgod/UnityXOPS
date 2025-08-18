using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UnityXOPS
{
    /// <summary>
    /// The BlockDataLoader class is responsible for managing and loading block data.
    /// </summary>
    /// <remarks>
    /// This class inherits from the Singleton class to ensure that only a single instance of
    /// BlockDataLoader exists throughout the application lifecycle. It facilitates centralized management
    /// and retrieval of block-related data.
    /// </remarks>
    public class BlockDataLoader : Singleton<BlockDataLoader>
    {
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

        //Resharper disable once InconsistentNaming
        /// <summary>
        /// Loads block data from the specified file path and processes textures and blocks.
        /// </summary>
        /// <param name="path">The file path to the block data to be loaded.</param>
        public void LoadBD1(string path)
        {
            if (!BlockDataReader.Instance.Read)
            {
#if UNITY_EDITOR
                Debug.Log("[BlockDataLoader] Block data not loaded.");
#endif
                return;
            }
            
            var fileName = Path.GetFileNameWithoutExtension(path);
            var directory = Path.GetDirectoryName(path);

            var texturePaths = BlockDataReader.Instance.texturePaths;
            var blockData = BlockDataReader.Instance.blockData;

            //Build texture into material
            for (var i = 0; i < texturePaths.Count; i++)
            {
                var material = new Material(Shader.Find("Standard"));
                material.SetFloat(Mode, 1f);
                material.SetFloat(Glossiness, 0.0f);
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt(ZWrite, 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                
                if (!string.IsNullOrEmpty(texturePaths[i].path))
                {
                    if (directory != null)
                    {
                        var texturePath = Path.Combine(directory, texturePaths[i].path);
                        var textureName = Path.GetFileName(texturePath);
                        if (File.Exists(texturePath))
                        {
                            var texture = ImageReader.LoadTexture(texturePath);
                            if (texture)
                            {
                                material.name = textureName;
                                material.mainTexture = texture;
                            }
                        }
                    }
                }
                else
                {
                    material.name = $"null_id{i}";
                }

                texturePaths[i].material = material;

#if UNITY_EDITOR
                Debug.Log($"[BlockDataLoader][{fileName}] Texture {i} built");
#endif
            }

            var combineInstancesByMaterial = new Dictionary<Material, List<CombineInstance>>();
            
            //Build block into meshes
            for (var i = 0; i < blockData.Count; i++)
            {
                for (var j = 0; j < 6; j++)
                {
                    var material = texturePaths[blockData[i].textureIndices[j]].material;
                    if (material == null) continue;

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
                    
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    var combineInstance = new CombineInstance
                    {
                        mesh = mesh,
                        transform = Matrix4x4.identity
                    };

                    if (!combineInstancesByMaterial.ContainsKey(material))
                    {
                        combineInstancesByMaterial[material] = new List<CombineInstance>();
                    }
                    combineInstancesByMaterial[material].Add(combineInstance);
                }
#if UNITY_EDITOR
                Debug.Log($"[BlockDataLoader][{fileName}] Block {i} built");
#endif
            }

            // Create a single GameObject for each material
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
                Debug.Log($"[BlockDataLoader][{fileName}] Combined mesh created for material: {material.name}");
#endif
            }
            
#if UNITY_EDITOR
            Debug.Log($"[BlockDataLoader][{fileName}] Block data completely loaded");
#endif

            Load = true;
        }
        //Resharper disable once InconsistentNaming
        public void DestroyBD1()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
#if UNITY_EDITOR
            Debug.Log("[BlockDataLoader] Block data destroyed.");
#endif
        }
    }
}