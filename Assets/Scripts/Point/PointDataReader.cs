using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    public class PointDataReader : Singleton<PointDataReader>
    {
        /*
        .pd1을 불러오는 파일입니다.
        뭐 특이점은 없습니다. bd1과 같이 나만의 pd1을 만들고 싶어서 헤더를 넣었다 그 정도?
        OpenXOPS와 다른 점은 param이 1 byte로 강제되지 않기 때문에, 127 종류(?) 이상의 사람을 넣을 수 있다
        뭐 그정도 뿐입니다.
         */
        
        private static readonly byte[] PointDataHeader =
        {
            0x50, 0x44, 0x31, 0x21, //PD1!
            0x1A, 0x00, 0xFF, 0x01 //(rest 4 bytes are unwritable with keyboard)
        };
        
        public List<PointData> pointData = new();
        
        public bool Read { get; private set; }
        
        //Resharper disable once InconsistentNaming
        public void ReadPD1(string path)
        {
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[PointDataReader] File not found: {path}");
#endif
                return;
            }
            
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            if (fs.Length < 8)
            {
#if UNITY_EDITOR
                Debug.LogError($"[PointDataReader] File is corrupted: {path}");
#endif
                return;
            }
            
            var fileName = Path.GetFileNameWithoutExtension(path);
            
            var header = new byte[8];
            _ = fs.Read(header, 0, 8);
            
            if (header.SequenceEqual(PointDataHeader))
            {
                ReadPD1Expansion(fs);
#if UNITY_EDITOR
                Debug.Log($"[PointDataReader] .pd1 expansion file loaded: {fileName}");
#endif
            }
            else
            {
                fs.Seek(0, SeekOrigin.Begin);
                ReadPD1Legacy(fs);
#if UNITY_EDITOR
                Debug.Log($"[PointDataReader] .pd1 legacy file loaded: {fileName}");
#endif
            }

            Read = true;
            fs.Close();
        }
        
        //Resharper disable once InconsistentNaming
        /// <summary>
        /// Clears all loaded PD1 point data, including child objects in the scene.
        /// </summary>
        public void ClearPD1()
        {
            pointData.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            Read = false;
#if UNITY_EDITOR
            Debug.Log("[PointDataReader] Block data cleared.");
#endif
        }
        
        //Resharper disable once InconsistentNaming
        private void ReadPD1Legacy(FileStream fs)
        {
            using BinaryReader br = new(fs);

            var pointCount = (int)br.ReadInt16();
            for (var i = 0; i < pointCount; i++)
            {
                var point = new PointData
                {
                    position = new Vector3
                    {
                        x = BitConverter.ToSingle(br.ReadBytes(4), 0) * 0.1f,
                        y = BitConverter.ToSingle(br.ReadBytes(4), 0) * 0.1f,
                        z = BitConverter.ToSingle(br.ReadBytes(4), 0) * 0.1f
                    },
                    rotation = Quaternion.Euler(0, BitConverter.ToSingle(br.ReadBytes(4), 0) * Mathf.Rad2Deg, 0),
                    key = br.ReadByte(),
                    value0 = br.ReadByte(),
                    value1 = br.ReadByte(),
                    value2 = br.ReadByte()
                };
                
                pointData.Add(point);
            }
        }
        
        //Resharper disable once InconsistentNaming
        private void ReadPD1Expansion(FileStream fs)
        {
            
        }
    }

    [Serializable]
    public class PointData
    {
        public Vector3 position;
        public Quaternion rotation;
        public int key;
        public int value0;
        public int value1;
        public int value2;
    }
}
