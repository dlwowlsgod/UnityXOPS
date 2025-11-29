using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    public static class SafeIO
    {
        public static string Combine(string safePath, params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = paths[i].Replace(".\\", "/").Replace("\\", "/");
            }
            
            try
            {
                string normalizedSafePath = Path.GetFullPath(safePath);
                
                string combinedPath = Path.Combine(safePath, Path.Combine(paths));
                string fullPath = Path.GetFullPath(combinedPath);
                
                if (!fullPath.StartsWith(normalizedSafePath, System.StringComparison.OrdinalIgnoreCase))
                {
#if UNITY_EDITOR
                    Debug.LogError($"Possible directory traversal detected: {fullPath}");
#endif
                    return null;
                }
                
                return fullPath;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"Path combination failed: {ex.Message}");
#endif
                return null;
            }
        }

        public static string Combine(string safePath, string path)
        {
            return Combine(safePath, new string[] { path });
        }

        public static string Combine(string safePath, string path, string path2)
        {
            return Combine(safePath, new string[] { path, path2 });
        }
        
        public static string Combine(string safePath, string path, string path2, string path3)
        {
            return Combine(safePath, new string[] { path, path2, path3 });
        }
        
        public static string Combine(string safePath, string path, string path2, string path3, string path4)
        {
            return Combine(safePath, new string[] { path, path2, path3, path4 });
        }
    }
}