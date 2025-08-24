using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    /// <summary>
    /// PD1Reader는 pd1 파일의 데이터를 읽는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// UnityXOPS에서 pd1 파일을 읽기 위해 구현된 <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// DirectX의 좌표계를 Unity가 사용하는 OpenGL로 변환하여 적용합니다.
    /// </remarks>
    //resharper disable once InconsistentNaming
    public class PD1Reader : Singleton<PD1Reader>
    {
        public List<PointData> pointData = new();
        
        public bool Read { get; private set; }
        
        /// <summary>
        /// pd1 파일의 데이터를 읽습니다.
        /// </summary>
        /// <param name="path">읽을 pd1 파일의 경로</param>
        //Resharper disable once InconsistentNaming
        public void ReadPD1(string path)
        {
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[PD1Reader] File not found: {path}");
#endif
                return;
            }
            
            // 파일스트림을 생성하고 BinaryReader로 더 빠르게 읽습니다.
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new(fs);
            
            var fileName = Path.GetFileNameWithoutExtension(path);
            
            //포인트의 개수를 읽기 (2바이트)
            var pointCount = (int)br.ReadInt16();
            
            //포인트의 개수만큼 데이터를 읽기 (개당 20바이트)
            for (var i = 0; i < pointCount; i++)
            {
                var point = new PointData
                {
                    //포인트의 위치 정보
                    position = new Vector3
                    {
                        x = BitConverter.ToSingle(br.ReadBytes(4), 0) * 0.1f,
                        y = BitConverter.ToSingle(br.ReadBytes(4), 0) * 0.1f,
                        z = BitConverter.ToSingle(br.ReadBytes(4), 0) * 0.1f
                    },
                    //포인트의 회전 정보는 4바이트 단독 라디안 값이지만
                    //유니티에서 적용하기 위해 Quaternion으로 변환하여 적용합니다.
                    rotation = Quaternion.Euler(0, BitConverter.ToSingle(br.ReadBytes(4), 0) * Mathf.Rad2Deg, 0),
                    
                    //아래는 파라미터 값입니다. ([A][B][C][D] 할때 그거)
                    key = br.ReadByte(),
                    value0 = br.ReadByte(),
                    value1 = br.ReadByte(),
                    value2 = br.ReadByte()
                };
                
                pointData.Add(point);
            }
            
#if UNITY_EDITOR
                Debug.Log($"[PD1Reader] .pd1 legacy file loaded: {fileName}");
#endif
            Read = true;
            br.Close();
            fs.Close();
        }
        
        /// <summary>
        /// pd1 파일의 데이터를 초기화합니다.
        /// </summary>
        //Resharper disable once InconsistentNaming
        public void ClearPD1()
        {
            pointData.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            Read = false;
#if UNITY_EDITOR
            Debug.Log("[PD1Reader] Block data cleared.");
#endif
        }
    }

    /// <summary>
    /// 포인트 데이터입니다.
    /// </summary>
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
