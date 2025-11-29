using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Object = UnityEngine.Object;

namespace UnityXOPS
{
    public static class ImageLoader
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ImageData
        {
            public int width;
            public int height;
            public IntPtr pixelData;
        }
        
        private enum ImageImportProfile : uint
        {
            IMPORT_ABORT = 0,
            IMPORT_JPG = 1,
            IMPORT_PNG = 2,
            IMPORT_BMP = 3,
            IMPORT_TGA = 4,
            IMPORT_DDS = 5
        }
        
        [DllImport("UnityXOPSNative")]
        private static extern IntPtr ImportImage(string filePath, ImageImportProfile profile);
        
        [DllImport("UnityXOPSNative")]
        private static extern void DeAllocImage(IntPtr data);

#if UNITY_EDITOR
        [DllImport("UnityXOPSNative", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetFreeImageVersion();
#endif

        private static readonly Dictionary<string, Texture2D> ImageCache = new();
        private static readonly Dictionary<Texture2D, Material> MaterialCache = new();
        
        public static Material CutoutMaterial { get; private set; }
        public static Material TransparentMaterial { get; private set; }
        public static Material NullMaterial { get; private set; }
        
        static ImageLoader()
        {
            CutoutMaterial = Resources.Load<Material>("Graphic/CutoutMaterial");
            TransparentMaterial = Resources.Load<Material>("Graphic/TransparentMaterial");
            NullMaterial = Resources.Load<Material>("Graphic/NullMaterial");
        }

        public static void Initialize()
        {
#if UNITY_EDITOR
            IntPtr versionPtr = GetFreeImageVersion();
            string versionString = Marshal.PtrToStringAnsi(versionPtr);
            Debug.Log($"FreeImage Version: {versionString}");
#endif
            Application.quitting += OnApplicationQuit;
            void OnApplicationQuit()
            {
#if UNITY_EDITOR
                ImageCache.Clear();
                Application.quitting -= OnApplicationQuit;
#endif
                MaterialCache.Clear();
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            return;

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                MaterialCache.Clear();
            }
        }
        
        public static Texture2D LoadImage(string filePath)
        {
            var path = SafeIO.Combine(Application.streamingAssetsPath, filePath);
            
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                Debug.LogError("Image path is empty.");
#endif
                return null;
            }
            
            if (ImageCache.TryGetValue(path, out var cachedTexture))
            {
                return cachedTexture;
            }

            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError("Image file does not exist.");
#endif
                return null;
            }
            
            var filename = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path).ToLower();
            
            var profile = ImageImportProfile.IMPORT_ABORT;
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    profile = ImageImportProfile.IMPORT_JPG;
                    break;
                case ".png":
                    profile = ImageImportProfile.IMPORT_PNG;
                    break;
                case ".bmp":
                    profile = ImageImportProfile.IMPORT_BMP;
                    break;
                case ".tga":
                    profile = ImageImportProfile.IMPORT_TGA;
                    break;
                case ".dds":
                    profile = ImageImportProfile.IMPORT_DDS;
                    break;
            }
            
            IntPtr imageDataPtr = ImportImage(path, profile);
            if (imageDataPtr == IntPtr.Zero)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to import image {path}");
#endif
                return null;
            }

            try
            {
                ImageData managedImageData = Marshal.PtrToStructure<ImageData>(imageDataPtr);

                int dataSize = managedImageData.width * managedImageData.height * 4;
                byte[] pixelData = new byte[dataSize];

                Marshal.Copy(managedImageData.pixelData, pixelData, 0, dataSize);

                Texture2D texture = new Texture2D(managedImageData.width, managedImageData.height, TextureFormat.RGBA32, false);
                texture.name = filename;
                texture.LoadRawTextureData(pixelData);
                texture.Compress(false);
#if !UNITY_EDITOR
                texture.Apply();
#endif
                
                return texture;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to import image {path}: {e.Message}");
#endif
                return null;
            }
            finally
            {
                DeAllocImage(imageDataPtr);
            }
        }

        public static Material ToMaterial(Texture2D texture, Material reference)
        {
            if (texture == null)
            {
                return null;
            }
            
            if (MaterialCache.TryGetValue(texture, out var cachedMaterial))
            {
                return cachedMaterial;
            }

            var material = Object.Instantiate(reference);
            material.mainTexture = texture;
            MaterialCache.Add(texture, material);
            return material;
        }
    }
}
