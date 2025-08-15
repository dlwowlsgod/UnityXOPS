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
        
        public List<TextureData> texturePaths = new();
        
        public List<BlockData> blockData = new();
        
        public bool Read { get; private set; }

        //Resharper disable once InconsistentNaming
        /// <summary>
        /// Loads a BD1 file and processes it, determining its type (legacy or expansion).
        /// </summary>
        /// <param name="path">The file path of the BD1 file to be read.</param>
        public void ReadBD1(string path)
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
                
            var header = new byte[8];
            _ = fs.Read(header, 0, 8);
            if (header.SequenceEqual(BlockDataHeader))
            {
                ReadBD1Expansion(fs);
#if UNITY_EDITOR
                Debug.Log($"[BlockDataReader] .bd1 expansion file Read: {fileName}");
#endif
            }
            else
            {
                fs.Seek(0, SeekOrigin.Begin);
                ReadBD1Legacy(fs);
#if UNITY_EDITOR
                Debug.Log($"[BlockDataReader] .bd1 legacy file Read: {fileName}");
#endif
            }

            Read = true;
            fs.Close();
        }
        
        //Resharper disable once InconsistentNaming
        private void ReadBD1Legacy(FileStream fs)
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
        private void ReadBD1Expansion(FileStream fs)
        {
            //Do not close the file stream here, as it is used in the legacy file loading method
        }

        //Resharper disable once InconsistentNaming
        public void ClearBD1()
        {
            texturePaths.Clear();
            blockData.Clear();
            Read = false;
            
#if UNITY_EDITOR
            Debug.Log($"[BlockDataReader] Block data cleared.");
#endif
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