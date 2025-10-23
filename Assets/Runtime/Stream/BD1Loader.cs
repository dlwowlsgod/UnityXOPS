using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Object = UnityEngine.Object;

namespace UnityXOPS
{
    public static class BD1Loader
    {
        private static bool _textureCountHeader;
        private static Material _defaultMaterial;
        private static Material _transparentMaterial;

        private static readonly int[] FaceVertexIndices = new[]
        {
            0, 3, 2, 1, // face 0
            7, 4, 5, 6, // face 1
            4, 0, 1, 5, // face 2
            5, 1, 2, 6, // face 3
            6, 2, 3, 7, // face 4
            7, 3, 0, 4  // face 5
        };
        private static readonly int[] UVIndexOrder = new[] { 3, 0, 1, 2 };

        public static void Initialize()
        {
            _textureCountHeader = 
                ProfileLoader.GetProfileValue("Stream", "UseBlockDataTextureCountHeader", "false") == "true";
            
            // _defaultMaterial
            _defaultMaterial = new Material(Shader.Find("Standard"));
            _defaultMaterial.name = "defaultBlockMaterial";
            _defaultMaterial.SetFloat("_Mode", 1f);
            _defaultMaterial.SetFloat("_Glossiness", 0.0f);
            _defaultMaterial.SetOverrideTag("RenderType", "TransparentCutout");
            _defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            _defaultMaterial.SetInt("_ZWrite", 1);
            _defaultMaterial.EnableKeyword("_ALPHATEST_ON");
            _defaultMaterial.DisableKeyword("_ALPHABLEND_ON");
            _defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _defaultMaterial.renderQueue = 2450;
            _defaultMaterial.color = Color.white;
            
            // _transparentMaterial
            // maybe cause the unity engine debug log, but 3rd line caused that I think
            _transparentMaterial = Object.Instantiate(_defaultMaterial);
            _transparentMaterial.name = "transparentBlockMaterial";
            _transparentMaterial.SetFloat("_Mode", 3f); // this
            _transparentMaterial.SetOverrideTag("RenderType", "Transparent");
            _transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _transparentMaterial.SetInt("_ZWrite", 0);
            _transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            _transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            _transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _transparentMaterial.renderQueue = 3000;
            _transparentMaterial.color = new Color(1f, 1f, 1f, 0f);
        }


        public static BlockData LoadBD1(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"BD1 data open failed: {path}.");
#endif
                return null;
            }
            
            var directory = Path.GetDirectoryName(path);
            
            using FileStream fs = new(path, FileMode.Open);
            using BinaryReader br = new(fs);
            var blockData = new BlockData();
            
            var textureCount = _textureCountHeader ? br.ReadInt16() : 10;
            blockData.textures = new Material[textureCount];

            for (int i = 0; i < textureCount; i++)
            {
                var textureChars = br.ReadChars(31);
                var nullPtr = Array.IndexOf(textureChars, '\0');
                var texturePath = nullPtr switch
                {
                    -1 => new string(textureChars),
                    _ => new string(textureChars, 0, nullPtr)
                };

                if (string.IsNullOrEmpty(texturePath))
                {
                    blockData.textures[i] = _defaultMaterial;
                    continue;
                }
                
                var texture = ImageLoader.LoadImage(Path.Combine(directory, texturePath));
                var material = Object.Instantiate(_defaultMaterial);
                material.name = texturePath;
                material.mainTexture = texture;
                blockData.textures[i] = material;
            }
            
            var blockCount = br.ReadInt16();
            blockData.rawBlockData = new RawBlockData[blockCount];
            blockData.blocks = new Mesh[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                float[] floatData = new float[72];
                for (int j = 0; j < 72; j++)
                {
                    floatData[j] = br.ReadSingle();
                }

                int[] intData = new int[7];
                for (int j = 0; j < 7; j++)
                {
                    intData[j] = br.ReadInt32();
                }
                
                var uniqueVertices = Enumerable.Range(0, 8)
                    .Select(j => new Vector3(floatData[j], floatData[j + 8], floatData[j + 16]))
                    .ToArray();

                var rawBlockData = new RawBlockData
                {
                    vertices = FaceVertexIndices.Select(index => uniqueVertices[index]).ToArray(),
                    uvs = Enumerable.Range(0, 6)
                        .SelectMany(faceIndex => UVIndexOrder.Select(uvOrderIndex => new Vector2(
                            floatData[24 + faceIndex * 4 + uvOrderIndex], 
                            -floatData[48 + faceIndex * 4 + uvOrderIndex])))
                        .ToArray(),
                    textureIndices = new []
                    {
                        intData[0], intData[1], intData[2], intData[3],
                        intData[4], intData[5]
                    },
                    flag = intData[6]
                };

                Mesh mesh = new();
                mesh.name = $"block_{i}";
                mesh.vertices = rawBlockData.vertices.Select(v => new Vector3(v.x, v.y, v.z) * 0.1f).ToArray();
                mesh.uv = rawBlockData.uvs;

                var subMeshGroups = Enumerable.Range(0, 6)
                    .Where(faceIndex => rawBlockData.textureIndices[faceIndex] >= 0)
                    .GroupBy(faceIndex => rawBlockData.textureIndices[faceIndex])
                    .OrderBy(group => group.Key)
                    .ToList();

                rawBlockData.subMeshTextureIndices = subMeshGroups.Select(g => g.Key).ToArray();
                
                var materials = new Material[rawBlockData.subMeshTextureIndices.Length];
                for (int j = 0; j < rawBlockData.subMeshTextureIndices.Length; j++)
                {
                    var textureIndex = rawBlockData.subMeshTextureIndices[j];
                    materials[j] = blockData.textures[textureIndex];
                }
                rawBlockData.subMeshMaterials = materials;

                var subMeshData = subMeshGroups.Select(group =>
                    group.SelectMany(faceIndex => new[]
                    {
                        faceIndex * 4, faceIndex * 4 + 1, faceIndex * 4 + 2,
                        faceIndex * 4, faceIndex * 4 + 2, faceIndex * 4 + 3
                    }).ToArray()).ToList();
                
                mesh.subMeshCount = subMeshData.Count;
                for (var submeshIndex = 0; submeshIndex < subMeshData.Count; submeshIndex++)
                {
                    mesh.SetTriangles(subMeshData[submeshIndex], submeshIndex);
                }
                
                mesh.RecalculateBounds();
                
                var center = mesh.bounds.center;
                rawBlockData.center = center;
                mesh.vertices = mesh.vertices.Select(v => v - center).ToArray();

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                blockData.blocks[i] = mesh;
                blockData.rawBlockData[i] = rawBlockData;
            }

            return blockData;
        }
    }
}
