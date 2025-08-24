using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    /// <summary>
    /// BD1Reader는 bd1 파일의 데이터를 읽는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// UnityXOPS에서 bd1 파일을 읽기 위해 구현된 <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// DirectX의 좌표계를 Unity가 사용하는 OpenGL로 변환하여 적용합니다.
    /// </remarks>
    //resharper disable once InconsistentNaming
    public class BD1Reader : Singleton<BD1Reader>
    {
        public List<TextureData> texturePaths = new();
        
        public List<BlockData> blockData = new();
        
        public bool Read { get; private set; }
        
        /// <summary>
        /// bd1 파일의 데이터를 읽습니다.
        /// </summary>
        /// <param name="path">읽을 bd1 파일의 경로</param>
        //Resharper disable once InconsistentNaming
        public void ReadBD1(string path)
        {
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[BD1Reader] File not found: {path}");
#endif
                return;
            }

            // 파일스트림을 생성하고 BinaryReader로 더 빠르게 읽습니다.
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new(fs);
            
            var fileName = Path.GetFileNameWithoutExtension(path);
            
            // 텍스쳐 읽기 (10개, 1개당 최대 31바이트)
            var sb = new StringBuilder(32); 
            for (var i = 0; i < 10; i++)
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
            
            // 블록의 개수 (2바이트)
            var blockCount = (int)br.ReadInt16();
            
            // 블록의 데이터 (316바이트 * 블록개수)
            for (var i = 0; i < blockCount; i++)
            {
                var floats = new List<float>();
                var ints = new List<int>();

                //4바이트 float 데이터를 처리 (288 바이트)
                for (var j = 0; j < 72; j++)
                {
                    floats.Add(BitConverter.ToSingle(br.ReadBytes(4), 0));
                }

                //4바이트 int 데이터를 처리 (24 바이트)
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
                
                //버텍스 읽기
                //버텍스는 x, x, ..., x, x, y, y, ..., y, y, z, z, ..., z, z
                //하나의 좌표쌍끼리 묶는게 상식이지만 xops는 그렇지 않음에 주의
                for (var j = 0; j < 8; j++)
                {
                    block.vertices.Add(new Vector3(floats[j], floats[j + 8], floats[j + 16]) * 0.1f);
                }
                
                //uv 읽기
                //uv는 u, u, u, u, ..., u, u, v, v, ..., v, v
                //하나의 uv쌍끼리 묶는게 상식이지만 xops는 그렇지 않음에 주의
                //v좌표를 뒤집는 (1 - 좌표) 이유는 OpenGL의 좌표계는 DirectX대비 위아래가 뒤집어졌기 때문
                for (var j = 0; j < 6; j++) 
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

                //블럭의 각 면마다 어떤 텍스쳐 번호를 쓰는지
                for (var j = 0; j < 6; j++) 
                {
                    block.textureIndices.Add(ints[j]); 
                }

                //인게임에서 사용되지 않는 4바이트 데이터.
                block.bitMaskingData = br.ReadInt32();
                
                blockData.Add(block);
            }
            
#if UNITY_EDITOR
                Debug.Log($"[BD1Reader] .bd1 file Read: {fileName}");
#endif
            Read = true;
            br.Close();
            fs.Close();
        }
        
        /// <summary>
        /// bd1 파일의 데이터를 초기화합니다.
        /// </summary>
        //Resharper disable once InconsistentNaming
        public void ClearBD1()
        {
            texturePaths.Clear();
            blockData.Clear();
            Read = false;
            
#if UNITY_EDITOR
            Debug.Log($"[BD1Reader] Block data cleared.");
#endif
        }
    }

    /// <summary>
    /// 텍스쳐 경로와 텍스쳐를 불러온 머티리얼 값을 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class TextureData
    {
        public string path;
        public Material material;
    }

    /// <summary>
    /// 블록 데이터입니다.
    /// </summary>
    [Serializable]
    public class BlockData
    {
        public List<Vector3> vertices;
        public List<FaceData> faces;
        public List<int> textureIndices;
        public int bitMaskingData;
        public List<Mesh> meshes;
    }

    /// <summary>
    /// 블록의 면이 가지고 있는 UV 데이터입니다.
    /// </summary>
    [Serializable]
    public class FaceData
    {
        public List<Vector2> uv;
    }
}