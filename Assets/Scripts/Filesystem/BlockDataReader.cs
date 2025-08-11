using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    /// <summary>
    /// Handles the reading and processing of block data (BD1 files) in UnityXOPS.
    /// </summary>
    /// <remarks>
    /// This class inherits from the Singleton class to enforce a single instance pattern.
    /// It includes methods to load and verify BD1 files, which are specific to the UnityXOPS framework.
    /// </remarks>
    public class BlockDataReader : Singleton<BlockDataReader>
    {
        /*
        .bd1 파일 리더입니다.
        파일 자체는 걍 그 안에서 파싱을 하는거 뿐입니다.
        단지 원본 엑옵을 그대로 구현하겠다는 했지만
        yosi_mikan concept를 보고 삘을 받은게 너무 많기 때문에
        같은 bd1이라도 제가 만든 UnityXOPS에서는 여러 다이나믹한 블록을 쓸 수 있게
        헤더 부분을 집어넣으려고 합니다.
        그걸 기반으로 레거시 파일과 제가 처리하려는 파일을 구분할 수 있게 됐고요.
        레거시 파일이면 그냥 원본처럼 불러오면 되는거고 제가 만든 거면 제 방식대로 불러오면 되는겁니다.
        어쨌든 차이점은 움직이는 블록을 구현할 수 있다 이정도? 이긴 합니다.
         */
        
        private static readonly byte[] BlockDataHeader =
        {
            0x42, 0x44, 0x31, 0x21, //BD1!
            0x1A, 0x00, 0xFF, 0x01  //(rest 4 bytes are unwritable with keyboard)
        };

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

        [SerializeField]
        private List<TextureData> texturePaths = new();
        
        [SerializeField]
        private List<BlockData> blockData = new();
        
        public bool Loaded { get; private set; }

        //Resharper disable once InconsistentNaming
        /// <summary>
        /// Loads a BD1 file and processes it, determining its type (legacy or expansion).
        /// </summary>
        /// <param name="path">The file path of the BD1 file to be loaded.</param>
        public void LoadBD1(string path)
        {
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[BlockDataReader] File not found: {path}");
#endif
                return;
            }

            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            if (fs.Length < 8)
            {
#if UNITY_EDITOR
                Debug.LogError($"[BlockDataReader] File is corrupted: {path}");
#endif
            }
                
            var fileName = Path.GetFileNameWithoutExtension(path);
            var directory = Path.GetDirectoryName(path);
                
            var header = new byte[8];
            _ = fs.Read(header, 0, 8);
            if (header.SequenceEqual(BlockDataHeader))
            {
                LoadBD1Expansion(fs);
#if UNITY_EDITOR
                Debug.Log($"[BlockDataReader] .bd1 expansion file loaded: {fileName}");
#endif
            }
            else
            {
                fs.Seek(0, SeekOrigin.Begin);
                LoadBD1Legacy(fs);
#if UNITY_EDITOR
                Debug.Log($"[BlockDataReader] .bd1 legacy file loaded: {fileName}");
#endif
            }

            fs.Close();
            
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //아래 시퀀스 독립 스크립트로 복붙해서 분리 필요 (단일 책임 위반)
            //아래 코드는 테스트를 위해 임시로 작성해둠
            //(지우고 다른 스크립트 파일에 다시 작성하기에는 너무 고된 작업이라서)
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            
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
                        if (File.Exists(texturePath))
                        {
                            var texture = ImageReader.LoadTexture(texturePath);
                            if (texture)
                            {
                                material.mainTexture = texture;
                                
                            }
                        }
                    }
                }
                texturePaths[i].material = material;
                
#if UNITY_EDITOR
                Debug.Log($"[BlockDataReader][{fileName}] Texture {i} built");
#endif
            }

            //Build block into meshes
            for (var i = 0; i < blockData.Count; i++)
            {
                blockData[i].meshes = new List<Mesh>();
                var parent = new GameObject($"block {i}");
                parent.transform.SetParent(transform);
                parent.transform.localPosition = Vector3.zero;
                parent.transform.localRotation = Quaternion.identity;
                parent.transform.localScale = Vector3.one;
                
                for (var j = 0; j < 6; j++)
                {
                    var mesh = new Mesh
                    {
                        name = $"Block {i} Face {j}",
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
                    
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();
                    mesh.RecalculateBounds();
                    mesh.Optimize();
                    
                    blockData[i].meshes.Add(mesh);
                    
                    var child = new GameObject($"block {i}_{j}");
                    child.transform.SetParent(parent.transform);
                    child.transform.localPosition = Vector3.zero;
                    child.transform.localRotation = Quaternion.identity;
                    child.transform.localScale = Vector3.one;
                    
                    child.AddComponent<MeshFilter>().mesh = mesh;
                    child.AddComponent<MeshRenderer>().material = texturePaths[blockData[i].textureIndices[j]].material;
                }
                
#if UNITY_EDITOR
                Debug.Log($"[BlockDataReader][{fileName}] Block {i} built");
#endif
            }
        }

        //Resharper disable once InconsistentNaming
        /// <summary>
        /// Clears all loaded BD1 block data, including textures and child objects in the scene.
        /// </summary>
        public void ClearBD1()
        {
            texturePaths.Clear();
            blockData.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
#if UNITY_EDITOR
            Debug.Log("[BlockDataReader] Block data cleared.");
#endif
        }
        
        //Resharper disable once InconsistentNaming
        private void LoadBD1Legacy(FileStream fs)
        {
            //Do not close the file stream here, as it is used in the expansion file loading method
            using BinaryReader br = new(fs);

            //Import texturePaths (31 bytes * 10)
            var sb = new StringBuilder(32);
            for (int i = 0; i < 10; i++)
            {
                var chars = br.ReadChars(31);
                foreach (var c in chars)
                {
                    if (c == '\0')
                    {
                        break;
                    }
                    sb.Append(c);
                }
                texturePaths.Add(new TextureData
                {
                    path = sb.ToString()
                });
                
                sb.Clear();
            }
            
            //Read block count (2 bytes)
            var blockCount = (int)br.ReadInt16();

            //Read block data (316 bytes * blockCount)
            for (var i = 0; i < blockCount; i++)
            {
                var floats = new List<float>();
                var ints = new List<int>();

                //Read float data (288 bytes)
                for (var j = 0; j < 72; j++)
                {
                    floats.Add(BitConverter.ToSingle(br.ReadBytes(4), 0));
                }

                //Read int data (24 bytes)
                for (var j = 0; j < 6; j++)
                {
                    ints.Add(BitConverter.ToInt32(br.ReadBytes(4), 0));
                }

                var block = new BlockData
                {
                    vertices = new List<Vector3>(),
                    faces = new List<FaceData>(),
                    textureIndices = new List<int>()
                };
                
                //Read vertex data 
                for (var j = 0; j < 8; j++) // (xxxxxxxxyyyyyyyyzzzzzzzz)
                {
                    block.vertices.Add(new Vector3(floats[j], floats[j + 8], floats[j + 16]) * 0.1f);
                }

                for (var j = 0; j < 6; j++) // (uuuuuu.....uuuuuuvvvvvv.....vvvvvv) 
                {
                    block.faces.Add(new FaceData
                    {
                        uv = new List<Vector2>
                        {
                            new Vector2(floats[j * 4 + 27], 1.0f - floats[j * 4 + 51]), //0, 1
                            new Vector2(floats[j * 4 + 24], 1.0f - floats[j * 4 + 48]), //0, 0
                            new Vector2(floats[j * 4 + 25], 1.0f - floats[j * 4 + 49]), //1, 0
                            new Vector2(floats[j * 4 + 26], 1.0f - floats[j * 4 + 50]) //1, 1
                        }
                    });
                }

                for (var j = 0; j < 6; j++) // (iiiiii)
                {
                    block.textureIndices.Add(ints[j]); 
                }

                block.bitMaskingData = br.ReadInt32();
                
                blockData.Add(block);
            }
        }
        
        //Resharper disable once InconsistentNaming
        private void LoadBD1Expansion(FileStream fs)
        {
            //Do not close the file stream here, as it is used in the legacy file loading method
        }
    }

    [Serializable]
    public class TextureData
    {
        public string path;
        public Material material;
    }

    [Serializable]
    public class BlockData
    {
        public List<Vector3> vertices;
        public List<FaceData> faces;
        public List<int> textureIndices;
        public int bitMaskingData;
        public List<Mesh> meshes;
    }

    [Serializable]
    public class FaceData
    {
        public List<Vector2> uv;
    }
}