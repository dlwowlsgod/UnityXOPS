using UnityEngine;
using System;
using System.IO;

/// <summary>
/// PNG 파일을 불러오는 클래스입니다.
/// </summary>
public static class PNGLoader
{
    /// <summary>
    /// PNG 파일을 불러옵니다.
    /// </summary>
    /// <param name="path">PNG 파일의 경로입니다.</param>
    /// <param name="textureFormat">텍스쳐의 포맷입니다.</param>
    /// <param name="filterMode">텍스쳐의 필터 모드입니다.</param>
    /// <param name="mipmap">텍스쳐의 밉맵 여부입니다.</param>
    /// <returns>불러온 텍스쳐를 반환합니다.</returns>
    public static Texture2D ImportPNG(string path, TextureFormat textureFormat = TextureFormat.RGBA32, FilterMode filterMode = FilterMode.Bilinear, bool mipmap = true)
    {
        Texture2D texture = null;
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("File path is empty.");
                return null;
            }

            if (!File.Exists(path))
            {
                Debug.LogWarning($"Can't find file in {path}.");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, textureFormat, mipmap);
            texture.filterMode = filterMode;
            
            if (!texture.LoadImage(bytes))
            {
                Debug.LogError($"PNG image load failed.");
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            
            return texture;
        }
        catch (Exception e)
        {
            Debug.LogError($"PNG file load failed: {e.Message}\n{e.StackTrace}");
            
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
            
            return null;
        }
    }

    /// <summary>
    /// 텍스쳐를 해제합니다.
    /// </summary>
    /// <param name="texture">해제할 텍스쳐입니다.</param>
    /// <param name="unloadUnusedAssets">사용하지 않는 에셋을 같이 해제할지 여부입니다.</param>
    public static void DisposePNG(Texture2D texture, bool unloadUnusedAssets = false)
    {
        if (texture != null)
        {
            UnityEngine.Object.Destroy(texture);
        }

        if (unloadUnusedAssets)
        {
            Resources.UnloadUnusedAssets();
        }
    }
} 