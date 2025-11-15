using UnityEngine;
using System;
using System.Linq;
using System.IO;
using Object = UnityEngine.Object;

namespace UnityXOPS
{
    /// <summary>
    /// Provides functionality for loading and processing block data files in Unity.
    /// </summary>
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
            
            _defaultMaterial = Resources.Load<Material>("DefaultBlock");
            _transparentMaterial = Resources.Load<Material>("TransparentBlock");
        }

        /// <summary>
        /// Loads a BD1 file from the specified path and returns the corresponding BlockData object.
        /// </summary>
        /// <param name="path">The file path of the BD1 file to load.</param>
        /// <returns>A BlockData object containing the loaded block and texture data, or null if the file load fails.</returns>
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
                    .Select(j => new Vector3(-floatData[j] * 0.1f, floatData[j + 8] * 0.1f, -floatData[j + 16] * 0.1f)) // -180 rot
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
                mesh.vertices = rawBlockData.vertices.Select(v => new Vector3(v.x, v.y, v.z)).ToArray();
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
                    if (textureIndex >= 0 && textureIndex < blockData.textures.Length)
                    {
                        materials[j] = blockData.textures[textureIndex];
                    }
                    else
                    {
                        materials[j] = _transparentMaterial;
                    }
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
                rawBlockData.position = center;
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
