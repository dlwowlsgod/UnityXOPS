using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace UnityXOPS
{
    public class ImageLoader
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

        public static void Initialize()
        {
#if UNITY_EDITOR
            IntPtr versionPtr = GetFreeImageVersion();
            string versionString = Marshal.PtrToStringAnsi(versionPtr);
            Debug.Log($"FreeImage Version: {versionString}");
#endif
        }

        public static Texture2D LoadImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
#if UNITY_EDITOR
                Debug.LogError("Image path is empty.");
#endif
                return null;
            }
            
            if (ImageCache.TryGetValue(filePath, out var cachedTexture))
            {
                return cachedTexture;
            }
            
            var useInternalCharDDS = ProfileLoader.GetProfileValue("Image", "UseInternalCharDDS", "false") == "true";
            var combinePath = Path.Combine(Application.streamingAssetsPath, @"data\char.dds");
            if (useInternalCharDDS && filePath == combinePath)
            {
                var texture2D = Resources.Load<Texture2D>("char");
                ImageCache[filePath] = texture2D;
                return texture2D;           
            }

            if (!File.Exists(filePath))
            {
#if UNITY_EDITOR
                Debug.LogError("Image file does not exist.");
#endif
                return null;
            }
            
            var filename = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath).ToLower();
            
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
            
            IntPtr imageDataPtr = ImportImage(filePath, profile);
            if (imageDataPtr == IntPtr.Zero)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to import image {filePath}");
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
                Debug.LogError($"Failed to import image {filePath}: {e.Message}");
#endif
                return null;
            }
            finally
            {
                DeAllocImage(imageDataPtr);
            }
        }
    }
}
