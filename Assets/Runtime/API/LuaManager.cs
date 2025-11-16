using System;
using System.IO;
using UnityEngine;
using XLua;

namespace UnityXOPS
{
    public class LuaManager : Singleton<LuaManager>
    {
        public LuaEnv LuaEnv { get; private set; }
        
        private string _luaPath = Path.Combine(Application.streamingAssetsPath, "common/scripts");

        protected override void Awake()
        {
            base.Awake();

            if (LuaEnv == null)
            {
                LuaEnv = new LuaEnv();
                LuaEnv.AddLoader(CustomLoader);
            }
        }

        private void Update()
        {
            LuaEnv?.Tick();
        }
        
        private void OnDestroy()
        {
            LuaEnv?.Dispose();
            LuaEnv = null;
        }

        private byte[] CustomLoader(ref string fileName)
        {
            try
            {
                var scriptFileName = fileName.EndsWith(".lua") ? fileName : fileName + ".lua";

                var fullPath = Path.Combine(_luaPath, scriptFileName);

                var normalizedBasePath = Path.GetFullPath(_luaPath);
                var normalizedFullPath = Path.GetFullPath(fullPath);

                if (!normalizedFullPath.StartsWith(normalizedBasePath))
                {
#if UNITY_EDITOR
                    Debug.LogError($"Path not allowed due to security: {fileName}");
#endif
                    return null;
                }

                if (!File.Exists(normalizedFullPath))
                {
#if UNITY_EDITOR
                    Debug.LogError($"File not exists: {normalizedFullPath}");
#endif
                    return null;
                }

                var luaScript = File.ReadAllText(normalizedFullPath, System.Text.Encoding.UTF8);

                return System.Text.Encoding.UTF8.GetBytes(luaScript);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"Error loading script: {fileName}\n{e.Message}");
#endif
                return null;
            }
        }
    }
}
