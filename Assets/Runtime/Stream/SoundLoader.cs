using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityXOPS
{
    public static class SoundLoader
    {
        private static readonly Dictionary<string, AudioClip> SoundCache = new();
        
        public static void Initialize()
        {
#if UNITY_EDITOR
            Application.quitting += OnApplicationQuit;
            void OnApplicationQuit()
            {
                SoundCache.Clear();
                
                Application.quitting -= OnApplicationQuit;
            }           
#endif
        }

        public static AudioClip LoadSound(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
#if UNITY_EDITOR
                Debug.LogError("Sound path is empty.");
#endif
                return null;
            }
            
            if (SoundCache.TryGetValue(filePath, out var cachedSound))
            {
                return cachedSound;
            }

            if (!File.Exists(filePath))
            {
#if UNITY_EDITOR
                Debug.LogError("Sound file does not exist.");
#endif
                return null;
            }
            
            var filename = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath).ToLower();

            if (extension != ".wav")
            {
#if UNITY_EDITOR
                Debug.LogError($"Unsupported sound file extension: {extension}");
#endif
                return null;
            }
            

            var url = "file://" + filePath;
            // www is obsolete because only support sync loading (like original xops)
            // if you want to change async loading, use UnityWebRequest
            // with async/await method or coroutine
#pragma warning disable CS0618
            using (var www = new WWW(url))
#pragma warning restore CS0618
            {
                while (!www.isDone) { }

                if (!string.IsNullOrEmpty(www.error))
                {
#if UNITY_EDITOR
                    Debug.LogError($"WWW Error: {www.error}");
#endif
                    return null;
                }

                var audioClip = www.GetAudioClip(false, false, AudioType.WAV);
                if (audioClip != null)
                {
                    audioClip.name = filename;
                    SoundCache.Add(filePath, audioClip);
                    return audioClip;
                }
#if UNITY_EDITOR
                Debug.LogError("Failed to create AudioClip from WWW data.");
#endif
                return null;
            }
        }
    }
}
