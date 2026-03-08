using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace JJLUtility.IO
{
    public partial class ImageLoader : SingletonBehavior<ImageLoader>
    {
        public static int MaxTextureSize = 4096;

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
                    if (texture.width > MaxTextureSize || texture.height > MaxTextureSize)
                    {
                        var (newW, newH, resized) = GetScaledPixels(texture.width, texture.height, texture.GetPixels32());
                        Object.Destroy(texture);
                        texture = new Texture2D(newW, newH);
                        texture.SetPixels32(resized);
                        texture.Apply();
                    }
                    break;
                case ".bmp":
                    BMPFile bmpFile = LoadBMPFile(filepath);
                    if (bmpFile == null)
                    {
                        Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                        return null;
                    }
                    var (bmpW, bmpH, bmpPixels) = GetScaledPixels(bmpFile.InfoHeader.Width, bmpFile.InfoHeader.Height, bmpFile.Pixels);
                    texture = new Texture2D(bmpW, bmpH);
                    texture.SetPixels32(bmpPixels);
                    texture.Apply();
                    break;
                case ".tga":
                    TGAFile tgaFile = LoadTGAFile(filepath);
                    if (tgaFile == null)
                    {
                        Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                        return null;
                    }
                    var (tgaW, tgaH, tgaPixels) = GetScaledPixels(tgaFile.Header.Width, tgaFile.Header.Height, tgaFile.Pixels);
                    texture = new Texture2D(tgaW, tgaH);
                    texture.SetPixels32(tgaPixels);
                    texture.Apply();
                    break;
                case ".dds":
                    DDSFile ddsFile = LoadDDSFile(filepath);
                    if (ddsFile == null)
                    {
                        Debugger.LogError($"Unsupported image extension: {filepath}", Instance, nameof(ImageLoader));
                        return null;
                    }
                    var (ddsW, ddsH, ddsPixels) = GetScaledPixels((int)ddsFile.Header.Width, (int)ddsFile.Header.Height, ddsFile.Pixels);
                    texture = new Texture2D(ddsW, ddsH);
                    texture.SetPixels32(ddsPixels);
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

        private static (int width, int height, Color32[] pixels) GetScaledPixels(int width, int height, Color32[] pixels)
        {
            if (width <= MaxTextureSize && height <= MaxTextureSize)
                return (width, height, pixels);

            float scale = (float)MaxTextureSize / Mathf.Max(width, height);
            int newW = Mathf.Max(1, Mathf.FloorToInt(width  * scale));
            int newH = Mathf.Max(1, Mathf.FloorToInt(height * scale));
            return (newW, newH, ResizePixels(pixels, width, height, newW, newH));
        }

        private static Color32[] ResizePixels(Color32[] src, int srcW, int srcH, int dstW, int dstH)
        {
            Color32[] dst = new Color32[dstW * dstH];
            float scaleX = (float)srcW / dstW;
            float scaleY = (float)srcH / dstH;

            for (int y = 0; y < dstH; y++)
            {
                float srcY = y * scaleY;
                int y0 = (int)srcY;
                int y1 = Mathf.Min(y0 + 1, srcH - 1);
                float fy  = srcY - y0;
                float fy0 = 1f - fy;

                for (int x = 0; x < dstW; x++)
                {
                    float srcX = x * scaleX;
                    int x0 = (int)srcX;
                    int x1 = Mathf.Min(x0 + 1, srcW - 1);
                    float fx  = srcX - x0;
                    float fx0 = 1f - fx;

                    Color32 c00 = src[y0 * srcW + x0];
                    Color32 c10 = src[y0 * srcW + x1];
                    Color32 c01 = src[y1 * srcW + x0];
                    Color32 c11 = src[y1 * srcW + x1];

                    float w00 = fy0 * fx0;
                    float w10 = fy0 * fx;
                    float w01 = fy  * fx0;
                    float w11 = fy  * fx;

                    dst[y * dstW + x] = new Color32(
                        (byte)(c00.r * w00 + c10.r * w10 + c01.r * w01 + c11.r * w11),
                        (byte)(c00.g * w00 + c10.g * w10 + c01.g * w01 + c11.g * w11),
                        (byte)(c00.b * w00 + c10.b * w10 + c01.b * w01 + c11.b * w11),
                        (byte)(c00.a * w00 + c10.a * w10 + c01.a * w01 + c11.a * w11)
                    );
                }
            }

            return dst;
        }
    }
}
