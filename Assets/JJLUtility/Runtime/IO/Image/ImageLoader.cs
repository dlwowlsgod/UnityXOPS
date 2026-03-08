using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace JJLUtility.IO
{
    public partial class ImageLoader : SingletonBehavior<ImageLoader>
    {
#if UNITY_EDITOR
        private Dictionary<string, int> m_textureCache = new Dictionary<string, int>();
        [SerializeField]
        private List<Texture2D> textureCacheList = new List<Texture2D>();
#else
        private Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
#endif //UNITY_EDITOR

        public static Texture2D LoadTexture(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Debugger.LogError($"Image path is empty: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"Image file not found: {filepath}", Instance, nameof(ImageLoader));
                return null;
            }

            if (Instance.m_textureCache.ContainsKey(filepath))
            {
#if UNITY_EDITOR
                return Instance.textureCacheList[Instance.m_textureCache[filepath]];
#else
        return _textureCache[filepath];
#endif //UNITY_EDITOR
            }

            
            string filename = Path.GetFileNameWithoutExtension(filepath);
            string extension = Path.GetExtension(filepath).ToLower();

            Texture2D texture = null;
            switch (extension)
            {
                case ".jpg" or ".jpeg" or ".png":
                    byte[] imageData = File.ReadAllBytes(filepath);
                    texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData);
                    break;
                case ".bmp":
                    BMPFile bmpFile = LoadBMPFile(filepath);
                    if (bmpFile == null)
                    {
                        Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                        return null;
                    }
                    texture = new Texture2D(bmpFile.InfoHeader.Width, bmpFile.InfoHeader.Height);
                    texture.SetPixels32(bmpFile.Pixels);
                    texture.Apply();
                    break;
                case ".tga":
                    TGAFile tgaFile = LoadTGAFile(filepath);
                    if (tgaFile == null)
                    {
                        Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                        return null;
                    }
                    texture = new Texture2D(tgaFile.Header.Width, tgaFile.Header.Height);
                    texture.SetPixels32(tgaFile.Pixels);
                    texture.Apply();
                    break;
                case ".dds":
                    DDSFile ddsFile = LoadDDSFile(filepath);
                    if (ddsFile == null)
                    {
                        Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                        return null;
                    }
                    texture = new Texture2D((int)ddsFile.Header.Width, (int)ddsFile.Header.Height);
                    texture.SetPixels32(ddsFile.Pixels);
                    texture.Apply();
                    break;
                default:
                    Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                    return null;
            }
            
            texture.name = filename;
            
#if UNITY_EDITOR
            Instance.textureCacheList.Add(texture);
            Instance.m_textureCache.Add(filepath, Instance.textureCacheList.Count - 1);
#else
            Instance._textureCache.Add(filepath, texture);
#endif //UNITY_EDITOR
            
            return texture;
        }
    }
}