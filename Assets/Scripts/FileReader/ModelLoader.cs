using UnityEngine;
using Assimp;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS에서 모델 파일을 런타임에 불러오기 위한 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// <see href="https://intelligide.github.io/assimp-unity/">Assimp</see>를 사용합니다.
    /// </remarks>
    public class ModelLoader : Singleton<ModelLoader>
    {
        private AssimpContext _assimpContext;
#if UNITY_EDITOR
        private Dictionary<string, int> _meshCache;

        [SerializeField] 
        private List<UnityEngine.Mesh> cache;
#else
        private Dictionary<string, UnityEngine.Mesh> _meshCache;
#endif

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            _meshCache = new Dictionary<string, int>();
            cache = new List<UnityEngine.Mesh>();
#else
            _meshCache = new Dictionary<string, UnityEngine.Mesh>();   
#endif
            _assimpContext = new AssimpContext();
        }

        /// <summary>
        /// 모델 파일을 비동기적으로 읽고 불러옵니다.
        /// </summary>
        /// <param name="path">불러올 모델 파일의 경로</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>Unity의 Mesh로 변환한 모델</returns>
        /// <remarks>
        /// .x 파일만 지원합니다.
        /// </remarks>
        public async UniTask<UnityEngine.Mesh> LoadModelAsync(string path, CancellationToken cancellationToken = default)
        {
#if UNITY_EDITOR
            if (_meshCache.TryGetValue(path, out var index))
            {
                Debug.Log($"[ModelLoader] Cached mesh returned for path (Editor): {path}");
                return cache[index];
            }
#else
            if (_meshCache.TryGetValue(path, out var cachedMesh))
            {
                return cachedMesh;
            }
#endif

            try
            {
                if (string.IsNullOrEmpty(path))
                {
#if UNITY_EDITOR
                    Debug.LogError($"[ModelLoader] Empty path provided.");
#endif
                    return null;
                }

                var extension = Path.GetExtension(path);
                if (extension != ".x")
                {
#if UNITY_EDITOR
                    Debug.LogError($"[ModelLoader] File is not a .x file: {path}");
#endif
                    return null;
                }

                // 파일 I/O와 Assimp 파싱은 백그라운드 스레드에서 처리
                var scene = await UniTask.RunOnThreadPool(() =>
                {
                    if (!File.Exists(path))
                    {
#if UNITY_EDITOR
                        Debug.LogError($"[ModelLoader] File not found: {path}");
#endif
                        return null;
                    }
                    
                    var bytes = File.ReadAllBytes(path);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var preset = PostProcessPreset.TargetRealTimeFast;
                    return _assimpContext.ImportFileFromStream(new MemoryStream(bytes), preset, Path.GetExtension(path));

                }, cancellationToken: cancellationToken);

                if (scene == null) return null;

                // Unity API를 사용하는 작업은 메인 스레드에서 처리
                await UniTask.SwitchToMainThread(cancellationToken);

                var uMeshes = new List<UnityEngine.Mesh>();
                var meshName = Path.GetFileNameWithoutExtension(path);

                foreach (var aMesh in scene.Meshes)
                {
                    var uMesh = new UnityEngine.Mesh
                    {
                        name = meshName,
                        vertices = aMesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray(),
                        triangles = aMesh.Faces.SelectMany(f => f.Indices).ToArray(),
                        uv = aMesh.HasTextureCoords(0) ? aMesh.TextureCoordinateChannels[0].Select(t => new Vector2(t.X, t.Y)).ToArray() : null,
                        normals = aMesh.HasNormals ? aMesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToArray() : null,
                    };

                    if (!aMesh.HasNormals)
                    {
                        uMesh.RecalculateNormals();
                    }
                    uMesh.RecalculateBounds();
                    uMesh.RecalculateTangents();
                    uMeshes.Add(uMesh);
                }
                
                scene.Clear();

                UnityEngine.Mesh finalMesh;

                if (uMeshes.Count == 0)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[ModelLoader] {path} has no meshes.");
#endif
                    finalMesh = null;
                }
                else if (uMeshes.Count == 1)
                {
                    finalMesh = uMeshes[0];
                }
                else
                {
                    var combineInstance = new CombineInstance[uMeshes.Count];
                    for (int i = 0; i < uMeshes.Count; i++)
                    {
                        combineInstance[i].mesh = uMeshes[i];
                        combineInstance[i].transform = UnityEngine.Matrix4x4.identity;
                    }
                    
                    var combinedMesh = new UnityEngine.Mesh();
                    combinedMesh.CombineMeshes(combineInstance, true, true);
                    finalMesh = combinedMesh;

                    foreach (var uMesh in uMeshes)
                    {
                        Destroy(uMesh);
                    }
                }

                // 캐시에 추가
#if UNITY_EDITOR
                cache.Add(finalMesh);
                _meshCache.Add(path, cache.Count - 1);
                Debug.Log($"[ModelLoader] Model loaded and cached (Editor): {path}");
#else
                _meshCache.Add(path, finalMesh);
#endif
                return finalMesh;
            }
            catch (System.OperationCanceledException)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[ModelLoader] Model loading cancelled for {path}");
#endif
                return null;
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[ModelLoader] Model loading failed for {path}: {e.Message}\n{e.StackTrace}");
#endif
                return null;
            }
        }
    }
}