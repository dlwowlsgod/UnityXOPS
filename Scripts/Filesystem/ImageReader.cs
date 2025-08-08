using UnityEngine;
using System.IO;
using System.Collections.Generic;
using B83.Image.BMP;
using UnityDds;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityXOPS
{
    /// <summary>
    /// Provides functionality to load and manage textures from various image file formats
    /// including BMP, JPEG, PNG, and DDS formats. The class maintains a cache to store
    /// already loaded textures for efficient reuse.
    /// </summary>
    public static class ImageReader
    {
        /*
        그냥 런타임에 이미지를 불러오는 기능입니다.
        꽤 강력하게 코딩을 해놨고요.
        유니티 자체에서 런타임에 jpg, png를 불러올 수 있게는 해줍니다.
        문제는 bmp와 dds인데, 두개 다 전부 깃헙 어딘가에 굴러다니는 걸 파쿠리 해왔습니다.
        그래서 아마 잘 작동할 겁니다.
        dds의 경우 포멧 문제로 원본은 불러오지 못하는 것 같더라고요. 왜인지 모르겠는데
        이건 추후에 비압축 포멧도 처리하게 수정해볼 예정입니다.
         */
        
        private static readonly BMPLoader BmpLoader;
        private static readonly Dictionary<string, Texture2D> TextureCache = new();

        static ImageReader()
        {
            BmpLoader = new BMPLoader();
        }
        /// <summary>
        /// Loads a texture from a file path in various supported formats such as BMP, JPEG, PNG, or DDS.
        /// </summary>
        /// <param name="path">The file path of the texture to load.</param>
        /// <returns>
        /// A <c>Texture2D</c> object containing the loaded texture if successful; otherwise, null if the file
        /// does not exist, the path is invalid, or the file format is unsupported.
        /// </returns>
        public static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ImageLoader] Empty path returned: {path}");
#endif
                return null;
            }
            
            if (TextureCache.TryGetValue(path, out var texture))
            {
#if UNITY_EDITOR
                Debug.Log($"[ImageLoader] Cached texture {path} returned");
#endif
                return texture;           
            }

            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ImageLoader] File not found: {path}");
#endif
                return null;
            }
            
            var extension = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);

            switch (extension)
            {
                case ".jpg" or ".jpeg" or ".png":
                {
                    var bytes = File.ReadAllBytes(path);
                    var texture2D = new Texture2D(2, 2);
                    texture2D.LoadImage(bytes);
                    TextureCache.Add(path, texture2D);
#if UNITY_EDITOR
                    Debug.Log($"[ImageLoader] texture {name} returned");
#endif
                    return texture2D;
                }
                case ".bmp":
                {
                    var bmpImage = BmpLoader.LoadBMP(path);
                    var texture2D = bmpImage.ToTexture2D();
                    TextureCache.Add(path, texture2D);
#if UNITY_EDITOR
                    Debug.Log($"[ImageLoader] texture {name} returned");
#endif
                    return texture2D;
                }
                case ".dds":
                {
                    var texture2D = DdsTextureLoader.LoadTexture(path);
                    TextureCache.Add(path, texture2D);
#if UNITY_EDITOR
                    Debug.Log($"[ImageLoader] texture {name} returned");
#endif
                    return texture2D;   
                }
                default:
#if UNITY_EDITOR
                    Debug.LogError($"[ImageLoader] File is not supported format {extension}: {path}");
#endif
                    return null;
            }
        }
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ClearCacheOnLoad()
        {
            
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                {
                    TextureCache.Clear();
                    Debug.Log("[ImageLoader] Texture cache cleared.");
                }
            };
        }
#endif

    }
}
