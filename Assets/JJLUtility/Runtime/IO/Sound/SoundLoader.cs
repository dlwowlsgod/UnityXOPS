using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace JJLUtility.IO
{
    /// <summary>
    /// WAV 오디오 파일을 로드하고 캐싱하는 싱글톤 매니저.
    /// </summary>
    public partial class SoundLoader : SingletonBehavior<SoundLoader>
    {
#if UNITY_EDITOR
        private Dictionary<string, int> m_audioCache  = new Dictionary<string, int>();
        [SerializeField]
        private List<AudioClip>         audioCacheList = new List<AudioClip>();
#else
        private Dictionary<string, AudioClip> m_audioCache = new Dictionary<string, AudioClip>();
#endif

        /// <summary>
        /// 지정된 경로의 오디오 파일을 로드해 AudioClip으로 반환한다. 이미 로드된 오디오는 캐시에서 반환한다.
        /// </summary>
        /// <param name="filepath">오디오 파일 경로.</param>
        /// <returns>로드된 AudioClip. 실패 시 null.</returns>
        public static AudioClip LoadAudio(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Debugger.LogError($"Audio path is empty.", Instance, nameof(SoundLoader));
                return null;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"Audio file not found: {filepath}", Instance, nameof(SoundLoader));
                return null;
            }

            if (Instance.m_audioCache.ContainsKey(filepath))
            {
#if UNITY_EDITOR
                return Instance.audioCacheList[Instance.m_audioCache[filepath]];
#else
                return Instance.m_audioCache[filepath];
#endif
            }

            string extension = Path.GetExtension(filepath).ToLower();

            AudioClip clip = null;
            switch (extension)
            {
                case ".wav":
                    clip = LoadWAVFile(filepath);
                    break;

                default:
                    Debugger.LogError($"Unsupported audio extension: {filepath}", Instance, nameof(SoundLoader));
                    return null;
            }

            if (clip == null) return null;

#if UNITY_EDITOR
            Instance.audioCacheList.Add(clip);
            Instance.m_audioCache.Add(filepath, Instance.audioCacheList.Count - 1);
#else
            Instance.m_audioCache.Add(filepath, clip);
#endif

            return clip;
        }
    }
}
