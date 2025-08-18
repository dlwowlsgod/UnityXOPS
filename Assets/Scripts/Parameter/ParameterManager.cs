using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// Manages and provides access to various parameters, including demo-specific
    /// and sky-related parameters, for the UnityXOPS framework.
    /// </summary>
    public class ParameterManager : Singleton<ParameterManager>
    {
        public List<DemoParameter> demoParameters;
        public List<SkyParameter> skyParameters;
        public List<OfficialMissionParameter> officialMissionParameters;

        public void LoadParameters()
        {
            LoadParameter<DemoParameter, DemoParameterList, DemoParameterWrapper>(demoParameters, "common/parameter/demo.json");
            LoadParameter<SkyParameter, SkyParameterList, SkyParameterWrapper>(skyParameters, "common/parameter/sky.json");

        }

        private void LoadParameter<TSo, TList, TData>(List<TSo> target, string path) 
            where TSo : ScriptableObject
            where TList : IParameterList<TData>
            where TData : IParameterData
        {
            var finalPath = Path.Combine(Application.streamingAssetsPath, path);
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.Log("[ParameterManager] No Json file detected. Use default parameters.");
#endif
                return;
            }

            var json = File.ReadAllText(finalPath);

            var loaded = JsonUtility.FromJson<TList>(json);

            target.Clear();
            foreach (var itemData in loaded.Items)
            {
                var newSo = ScriptableObject.CreateInstance<TSo>();
                newSo.name = itemData.FinalName;
                var itemJson = JsonUtility.ToJson(itemData);
                JsonUtility.FromJsonOverwrite(itemJson, newSo);
                target.Add(newSo);
            }
#if UNITY_EDITOR
            Debug.Log($"[ParameterManager] {target.Count} {typeof(TSo).Name} parameter completely loaded.");
#endif
        }
    }

    public interface IParameterData
    {
        string FinalName { get; }
    }
    
    public interface IParameterList<T> where T : IParameterData
    {
        List<T> Items { get; }
    }
}