using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace UnityXOPS
{
    public static class PD1Loader
    {
        public static void Initialize()
        {
            
        }
        
        public static PointData LoadPD1(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"PD1 data open failed: {path}.");
#endif
                return null;
            }
            
            var directory = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);
            
            var pointData = new PointData();
            var msgPath = Path.Combine(directory, filename + ".msg");
            pointData.msgData = EncodingHelper.ReadAllLinesWithEncoding(msgPath);

            using FileStream fs = new(path, FileMode.Open);
            using BinaryReader br = new(fs);
            
            var pointCount = br.ReadInt16();
            pointData.rawPointData = new RawPointData[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                var rawPointData = new RawPointData();
                
                var x = br.ReadSingle();
                var y = br.ReadSingle();
                var z = br.ReadSingle();
                rawPointData.position = new Vector3(-x * 0.1f, y * 0.1f, -z * 0.1f);

                var r = br.ReadSingle();
                rawPointData.rotation = Quaternion.Euler(0, -r, 0);

                var extended = ProfileLoader.GetProfileValue("Stream", "UseExtendedPointParameter", "false") == "true";
                if (extended)
                {
                    var type = br.ReadInt32();
                    rawPointData.type = (PointType)type;
                
                    rawPointData.param0 = br.ReadInt32();
                    rawPointData.param1 = br.ReadInt32();
                    rawPointData.param2 = br.ReadInt32();
                }
                else
                {
                    var type = br.ReadByte();
                    rawPointData.type = (PointType)type;
                
                    rawPointData.param0 = br.ReadByte();
                    rawPointData.param1 = br.ReadByte();
                    rawPointData.param2 = br.ReadByte();
                }
                
                pointData.rawPointData[i] = rawPointData;
            }

            return pointData;
        }
    }
}