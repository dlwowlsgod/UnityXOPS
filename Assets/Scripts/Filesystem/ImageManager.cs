using UnityEngine;
using System.IO;
using System.Collections.Generic;
using B83.Image.BMP;
using UnityDds;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS에서 이미지 파일을 런타임에 불러오기 위한 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// </remarks>
    public class ImageManager : Singleton<ImageManager>
    {
        private BMPLoader _bmpLoader;
#if UNITY_EDITOR
        private Dictionary<string, int> _imageCache;

        [SerializeField] 
        private List<Texture2D> cache;
#else
        private Dictionary<string, Texture2D> _imageCache;
#endif
        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            _imageCache = new Dictionary<string, int>();
            cache = new List<Texture2D>();
#else
            _imageCache = new Dictionary<string, Texture2D>();
#endif
            _bmpLoader = new BMPLoader();
        }

        /// <summary>
        /// 이미지 파일을 읽고 불러옵니다.
        /// </summary>
        /// <param name="path">불러올 이미지 파일의 경로</param>
        /// <returns><see cref="Texture2D">Texture2D</see>로 변환한 이미지</returns>
        /// <remarks>
        /// DDS의 경우 DXT1, DXT5만 지원합니다. 추후 추가 예정입니다.
        /// </remarks>
        public Texture2D LoadImage(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ImageManager] Empty path returned: {path}");
#endif
                return null;
            }
            
#if UNITY_EDITOR
            if (_imageCache.TryGetValue(path, out var index))
            {

                Debug.Log($"[ImageManager] Cached image {path} returned");

                return cache[index];           
            }
#else
            if (_imageCache.TryGetValue(path, out var image))
            {
                return image;        
            }
#endif
            
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ImageManager] File not found: {path}");
#endif
                return null;
            }
            
            var extension = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);

            try
            {
                // 가끔 보이는 대문자 확장자 예외 처리
                switch (extension.ToLower())
                {
                    // jpg, png의 경우 유니티에서 바이트 데이터를 이용한 런타임 로드를 지원
                    case ".jpg" or ".jpeg" or ".png":
                    {
                        var bytes = File.ReadAllBytes(path);
                        var texture2D = new Texture2D(2, 2);
                        texture2D.LoadImage(bytes);
                        texture2D.name = name;
                        texture2D.Apply();
                    
#if UNITY_EDITOR
                        cache.Add(texture2D);
                        _imageCache.Add(path, cache.Count - 1);
                        Debug.Log($"[ImageManager] texture {name} returned");
#else
                    _imageCache.Add(path, texture2D);
#endif
                        return texture2D;
                    }
                    // bmp의 경우 외부의 bmp loader를 이용
                    case ".bmp":
                    {
                        var bmpImage = _bmpLoader.LoadBMP(path);
                        var texture2D = bmpImage.ToTexture2D(name);
#if UNITY_EDITOR
                        cache.Add(texture2D);
                        _imageCache.Add(path, cache.Count - 1);
                        Debug.Log($"[ImageManager] texture {name} returned");
#else
                    _imageCache.Add(path, texture2D);
#endif
                        return texture2D;
                    }
                    // dds는 dds loader를 이용
                    case ".dds":
                    {
                        var texture2D = DdsTextureLoader.LoadTexture(path, false, name);
#if UNITY_EDITOR
                        cache.Add(texture2D);
                        _imageCache.Add(path, cache.Count - 1);
                        Debug.Log($"[ImageManager] texture {name} returned");
#else
                    _imageCache.Add(path, texture2D);
#endif
                        return texture2D;   
                    }
                    default:
#if UNITY_EDITOR
                        Debug.LogError($"[ImageManager] File is not supported format {extension}: {path}");
#endif
                        return null;
                }
            }
            catch
            {
#if UNITY_EDITOR
                Debug.LogError($"[ImageManager] File is not supported format {extension}: {path}");
#endif
                return null;           
            }
            
        }
    }
}