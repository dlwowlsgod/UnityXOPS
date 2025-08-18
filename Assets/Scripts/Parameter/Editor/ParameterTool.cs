using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS.Editor
{
    public class ParameterTool
    {
        [MenuItem("UnityXOPS/Parameter/Save Demo Parameter to JSON")]
        private static void SaveDemoParameter()
        {
            ParameterManager manager = UnityEngine.Object.FindFirstObjectByType<ParameterManager>();
            if (!manager)
            {
                Debug.LogError("[ParameterTool] Detected parameter manager instance null.");
                return;
            }

            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
                var commonPath = Path.Combine(Application.streamingAssetsPath, "common");
                Directory.CreateDirectory(commonPath);
                var parameterPath = Path.Combine(commonPath, "parameter");
                Directory.CreateDirectory(parameterPath);
            }

            var demoDataList = manager.demoParameters.Select(so => new DemoParameterWrapper
            {
                finalName = so.finalName,
                bd1Path = so.bd1Path,
                pd1Path = so.pd1Path,
                skyIndex = so.skyIndex
            }).ToList();
            var demoWrapper = new DemoParameterList { items = demoDataList };
            var demoJson = JsonUtility.ToJson(demoWrapper, true);
            var demoPath = Path.Combine(Application.streamingAssetsPath, "common", "parameter", "demo.json");
            File.WriteAllText(demoPath, demoJson);
            Debug.Log($"[ParameterTool] Demo parameter saved to {demoPath}");
            
            AssetDatabase.Refresh();
        }
        
        [MenuItem("UnityXOPS/Parameter/Save Sky Parameter to JSON")]
        private static void SaveSkyParameter()
        {
            ParameterManager manager = UnityEngine.Object.FindFirstObjectByType<ParameterManager>();
            if (!manager)
            {
                Debug.LogError("[ParameterTool] Detected parameter manager instance null.");
                return;
            }
            
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
                var commonPath = Path.Combine(Application.streamingAssetsPath, "common");
                Directory.CreateDirectory(commonPath);
                var parameterPath = Path.Combine(commonPath, "parameter");
                Directory.CreateDirectory(parameterPath);
            }

            var skyDataList = manager.skyParameters.Select(so => new SkyParameterWrapper
            {
                finalName = so.finalName,
                skyTexturePath = so.skyTexturePath,
                billboardTexturePath = so.billboardTexturePath,
                cloudTexturePath = so.cloudTexturePath,
                lightTexturePath = so.lightTexturePath,
                light = so.light,
                lightStrength = so.lightStrength,
                lightColor = so.lightColor,
                lightDirection = so.lightDirection
            }).ToList();
            var skyWrapper = new SkyParameterList { items = skyDataList };
            var skyJson = JsonUtility.ToJson(skyWrapper, true);
            var skyPath = Path.Combine(Application.streamingAssetsPath, "common", "parameter", "sky.json");
            File.WriteAllText(skyPath, skyJson);
            Debug.Log($"[ParameterTool] Sky parameter saved to {skyPath}");
            
            AssetDatabase.Refresh();
        }
        
        [MenuItem("UnityXOPS/Parameter/Save Official Mission Parameter to JSON")]
        private static void SaveOfficialMissionParameter()
        {
            ParameterManager manager = UnityEngine.Object.FindFirstObjectByType<ParameterManager>();
            if (!manager)
            {
                Debug.LogError("[ParameterTool] Detected parameter manager instance null.");
                return;
            }
            
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
                var commonPath = Path.Combine(Application.streamingAssetsPath, "common");
                Directory.CreateDirectory(commonPath);
                var parameterPath = Path.Combine(commonPath, "parameter");
                Directory.CreateDirectory(parameterPath);
            }
            
            var officialMissionDataList = manager.officialMissionParameters.Select(so => new OfficialMissionParameterWrapper
            {
                finalName = so.finalName,
                longName = so.longName,
                bd1Path = so.bd1Path,
                pd1Path = so.pd1Path,
                txtPath = so.txtPath,
                adjustCollision = so.adjustCollision,
                darkScreen = so.darkScreen
            }).ToList();
            var officialMissionWrapper = new OfficialMissionParameterList { items = officialMissionDataList };
            var officialMissionJson = JsonUtility.ToJson(officialMissionWrapper, true);
            var officialMissionPath = Path.Combine(Application.streamingAssetsPath, "common", "parameter", "officialMission.json");
            File.WriteAllText(officialMissionPath, officialMissionJson);
            Debug.Log($"[ParameterTool] Official mission parameter saved to {officialMissionPath}");
            
            AssetDatabase.Refresh();
        }
    }
}