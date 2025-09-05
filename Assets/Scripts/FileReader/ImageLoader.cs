using UnityEngine;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using B83.Image.BMP;
using UnityDds;
using Cysharp.Threading.Tasks;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS에서 이미지 파일을 런타임에 불러오기 위한 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// </remarks>
    public class ImageLoader : Singleton<ImageLoader>
    {
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
            //에디터에서는 Inspector 창에서 캐싱 값이 보이게
            //런타임에는 Texture2D를 직접 캐싱
#if UNITY_EDITOR
            _imageCache = new Dictionary<string, int>();
            cache = new List<Texture2D>();
#else
            _imageCache = new Dictionary<string, Texture2D>();
#endif
        }
        
        public async UniTask<Texture2D> LoadImageAsync(string path, CancellationToken cancellationToken = default)
        {
            // --- [추가] 캐시 확인 ---
#if UNITY_EDITOR
            if (_imageCache.TryGetValue(path, out var index))
            {
                Debug.Log($"[ImageLoader] Cached image returned for path (Editor): {path}");
                return cache[index];
            }
#else
            if (_imageCache.TryGetValue(path, out var cachedTexture))
            {
                return cachedTexture;
            }
#endif
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
#if UNITY_EDITOR
                    Debug.LogError($"[ImageLoader] File not found or path is null/empty: {path}");
#endif
                    return null;
                }

                var extension = Path.GetExtension(path)?.ToLower();
                if (string.IsNullOrEmpty(extension))
                {
#if UNITY_EDITOR
                    Debug.LogError($"[ImageLoader] Empty extension: {path}");
#endif
                    return null;
                }

                var imageBytes = await UniTask.RunOnThreadPool(() =>
                {
                    var bytes = File.ReadAllBytes(path);
                    cancellationToken.ThrowIfCancellationRequested();
                    return bytes;
                }, cancellationToken: cancellationToken);

                if (imageBytes == null) return null;

                await UniTask.SwitchToMainThread(cancellationToken);

                Texture2D texture = null;
                var textureName = Path.GetFileNameWithoutExtension(path);
                
                switch (extension)
                {
                    case ".bmp":
                        var bmpLoader = new BMPLoader();
                        var bmpImage = bmpLoader.LoadBMP(imageBytes);
                        if (bmpImage != null)
                        {
                            texture = bmpImage.ToTexture2D(textureName);
                        }
                        break;
                    case ".dds":
                        texture = DdsTextureLoader.LoadTexture(imageBytes, false, textureName);
                        break;
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                        texture = new Texture2D(2, 2);
                        texture.LoadImage(imageBytes);
                        texture.name = textureName;
                        texture.Apply();
                        break;
                }

                // --- [추가] 로드 성공 시 캐시에 추가 ---
                if (texture != null)
                {
#if UNITY_EDITOR
                    cache.Add(texture);
                    _imageCache.Add(path, cache.Count - 1);
                    Debug.Log($"[ImageLoader] Image loaded and cached (Editor): {path}");
#else
                    _imageCache.Add(path, texture);
#endif
                }
                
                return texture;
            }
            catch (System.OperationCanceledException)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[ImageLoader] Image loading cancelled for {path}");
#endif
                return null;
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[ImageLoader] Image loading failed for {path}: {e.Message}\n{e.StackTrace}");
#endif
                return null;
            }
        }
    }
}