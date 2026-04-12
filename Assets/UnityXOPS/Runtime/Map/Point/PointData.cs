using System;
using System.IO;
using System.Text;
using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// PD1 파일 전체의 포인트 데이터와 메시지를 담는 컨테이너 클래스.
    /// </summary>
    public class PointData
    {
        public RawPointData[] rawPointData;
        public string[] msg;
    }

    public partial class MapLoader
    {
        /// <summary>
        /// PD1 바이너리 파일을 파싱해 PointData 객체로 반환한다.
        /// </summary>
        /// <param name="filepath">PD1 파일 경로.</param>
        /// <returns>파싱된 PointData. 실패 시 null.</returns>
        private static PointData LoadPD1File(string filepath)
        {
            try
            {
                using var reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read), Encoding.ASCII);


                var pointData = new PointData();
                
                int pointCount = reader.ReadInt16();
                pointData.rawPointData = new RawPointData[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    var rawPointData = new RawPointData
                    {
                        position = new Vector3(-reader.ReadSingle() * 0.1f, reader.ReadSingle() * 0.1f, -reader.ReadSingle() * 0.1f),
                        look = reader.ReadSingle() * Mathf.Rad2Deg + 180f,
                        param0 = reader.ReadByte(),
                        param1 = reader.ReadByte(),
                        param2 = reader.ReadByte(),
                        param3 = reader.ReadByte()
                    };
                    
                    pointData.rawPointData[i] = rawPointData;
                }

                string msgPath = Path.ChangeExtension(filepath, ".msg");
                if (File.Exists(msgPath))
                {
                    pointData.msg = File.ReadAllLines(msgPath, EncodingHelper.GetEncoding());
                }

                return pointData;
            }
            catch (Exception e)
            {
                Debugger.LogError($"PD1 read failed: {filepath}\n{e.Message}", Instance, nameof(MapLoader));
                return null;
            }
        }
    }
}
