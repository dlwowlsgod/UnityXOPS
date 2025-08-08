using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace UnityXOPS
{
    /// <summary>
    /// Provides functionality to read private profile settings specific to the application.
    /// </summary>
    public static class PrivateProfileReader
    {
        /*
        OpenXOPS에 있는 그 ini 시스템을 가져오는 겁니다.
        kernel32.dll에 관련 함수가 이미 작성되어 있습니다. 이걸 불러오기만 하면 됩니다.
        물론 윈도우 에서만 가능한 거고요..
        mikan님도 아마 그걸 알고 이 함수를 사용하지 않고 직접 구현하셨던데
        아마 플랫폼 이식 문제를 염두하고 그랬을 겁니다.
        이건 추후에 전처리문을 이용해서 프로필 읽는 부분을 분기 처리할 예정입니다.
        */
        public static string ImportLanguage { get; private set; }
        public static bool FixXFileError { get; private set; }
        
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder returnedString, int size, string filePath);
        private static readonly string ProfilePath = Path.Combine(Application.streamingAssetsPath, "common", "UnityXOPSProfile.ini");
#endif
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ClearProfileOnLoad()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state is not (PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode))
                {
                    return;
                }
                
                ImportLanguage = "kr";
                FixXFileError = false;
                Debug.Log("[PrivateProfileReader] Profile cleared.");
            };
        }
#endif
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadProfile()
        {
            //This only works a window platform now (written on 0.1)
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!File.Exists(ProfilePath))
            {
                SetDefaultProfile();
#if UNITY_EDITOR
                Debug.LogWarning("[PrivateProfileReader] Profile not found. Using default settings.");
#endif
                return;
            }

            var success = 0;
            var failed = 0;
            

            var sb = new StringBuilder(256);
            GetPrivateProfileString("General", "import_language", "", sb, sb.Capacity, ProfilePath);
            var importLanguage = sb.ToString().ToLower();
            switch (importLanguage)
            {
                case "jp":
                    ImportLanguage = "jp";
                    success++;
                    break;
                case "kr":
                    ImportLanguage = "kr";
                    success++;
                    break;
                default:
                    ImportLanguage = "kr";
                    failed++;
                    break;
            }

            sb.Clear();
            GetPrivateProfileString("General", "fix_x_file_error", "", sb, sb.Capacity, ProfilePath);
            var fixXFileError = sb.ToString();
            switch (fixXFileError)
            {
                case "0":
                    FixXFileError = false;
                    success++;
                    break;
                case "1":
                    FixXFileError = true;
                    success++;
                    break;
                default:
                    FixXFileError = false;
                    failed++;
                    break;
            }

            sb.Clear();
#if UNITY_EDITOR
            Debug.Log($"[PrivateProfileReader] Profile loaded: read {success} profile, use default {failed} profile");
#endif
      
#else
            SetDefaultProfile();
#endif
        }

        private static void SetDefaultProfile()
        {
            ImportLanguage = "kr";
            FixXFileError = false;
        }
    }
}