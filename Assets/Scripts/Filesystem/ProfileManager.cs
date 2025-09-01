using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// profile.ini를 불러오는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// </remarks>
    public class ProfileManager : Singleton<ProfileManager>
    {
        private Dictionary<string, Dictionary<string, string>> _profileData;
#if UNITY_EDITOR
        [SerializeField]
        private List<SerializedProfileData> serializedProfileData;
#endif

        protected override void Awake()
        {
            base.Awake();
            _profileData = new Dictionary<string, Dictionary<string, string>>();
#if UNITY_EDITOR
            serializedProfileData = new List<SerializedProfileData>();
#endif
        }

        public void LoadProfile()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "common", "profile.ini");

            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ProfileManager] Profile file not found at path: {path}");
#endif
                return;
            }

            _profileData.Clear();
#if UNITY_EDITOR
            serializedProfileData.Clear();
#endif
            
            try
            {
                var lines = File.ReadAllLines(path);
                var currentSection = string.Empty;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                    {
                        continue;
                    }

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                        if (!_profileData.ContainsKey(currentSection))
                        {
                            _profileData[currentSection] = new Dictionary<string, string>();
                        }
                        continue;
                    }
                
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length < 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                
                    if (!_profileData.ContainsKey(currentSection))
                    {
                        _profileData[currentSection] = new Dictionary<string, string>();
                    }
                
                    _profileData[currentSection][key] = value;
                }
            
#if UNITY_EDITOR
                foreach (var tk in _profileData)
                {
                    foreach (var kv in tk.Value)
                    {
                        serializedProfileData.Add(new SerializedProfileData
                        {
                            section = tk.Key,
                            key = kv.Key,
                            value = kv.Value
                        });
                    }
                }
                Debug.Log($"[ProfileManager] {serializedProfileData.Count} profile data loaded.");
#endif
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[ProfileManager] Failed to load profile.ini. Error: {e.Message}");
#endif
            }

        }

        public string GetProfileValue(string section, string key, string defaultValue)
        {
            if (!_profileData.TryGetValue(section, out var sectionData))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[ProfileManager] Section {section} not found.");   
#endif
                return defaultValue;
            }
            
            if (sectionData.TryGetValue(key, out var value))
            {
#if UNITY_EDITOR
                Debug.Log($"[ProfileManager] Key {key} found and returned.");   
#endif
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[ProfileManager] Key {key} not found.");   
#endif
            return defaultValue;
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 유니티 에디터에서 Profile을 직렬화하여 Inspector로 확인할 수 있게 하는 직렬화 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class SerializedProfileData
    {
        public string section;
        public string key;
        public string value;
    }
#endif
}