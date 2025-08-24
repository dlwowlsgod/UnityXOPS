using UnityEngine;
using Assimp;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS에서 모델 파일을 런타임에 불러오기 위한 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// <see href="https://intelligide.github.io/assimp-unity/">Assimp</see>를 사용합니다.
    /// </remarks>
    public class ModelManager : Singleton<ModelManager>
    {
        private AssimpContext _assimpContext = new();
#if UNITY_EDITOR
        private Dictionary<string, int> _meshCache = new();

        [SerializeField] 
        private List<UnityEngine.Mesh> cache = new();
#else
        private Dictionary<string, UnityEngine.Mesh> _meshCache = new();
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
        /// 모델 파일을 읽고 불러옵니다.
        /// </summary>
        /// <param name="path">불러올 모델 파일의 경로</param>
        /// <returns>Unity의 Mesh로 변환한 모델</returns>
        /// <remarks>
        /// .x 파일만 지원합니다.
        /// </remarks>
        public UnityEngine.Mesh LoadModel(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ModelManager] Empty path returned: {path}");
#endif
                return null;
            }
            
#if UNITY_EDITOR
            if (_meshCache.TryGetValue(path, out var index))
            {

                Debug.Log($"[ModelManager] Cached mesh {path} returned");

                return cache[index];           
            }
#else
            if (_meshCache.TryGetValue(path, out var mesh))
            {
                return mesh;        
            }
#endif

            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[ModelManager] File not found: {path}");
#endif
                return null;
            }
            
            var extension = Path.GetExtension(path);
            var meshName = Path.GetFileNameWithoutExtension(path);
            if (extension != ".x")
            {
#if UNITY_EDITOR
                Debug.LogError($"[ModelManager] File is not .x file: {path}");
#endif
                return null;
            }
            
            var preset = PostProcessPreset.TargetRealTimeFast;
            var scene = _assimpContext.ImportFile(path, preset);
            
            //리스트로 받는 이유는 유저가 x 파일을 익스포트 할 때,
            //가끔 있는 "여러 개의 오브젝트"를 포함하는 경우가 있음
            //이 경우 드로우콜 증가 문제도 있지만 하나의 데이터만 있다고 처리할 경우
            //제대로 불러오지 못하기 때문에 모든 mesh를 읽어오기 위함임
            //(XOPS는 텍스쳐가 하나기 때문에 모델링 작업이 끝나면
            //개별 모델 오브젝트를 전부 하나로 합치는걸 추천)
            var uMeshes = new List<UnityEngine.Mesh>();
            
            // assimp mesh를 Unity mesh로 변환
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

            if (uMeshes.Count == 0)
            {
#if UNITY_EDITOR
                Debug.Log($"[ModelManager] {name} has no meshes.");
                cache.Add(null);
                _meshCache.Add(path, cache.Count - 1);
                return null;
#else
                _meshCache.Add(path, null);
                return null;
#endif
            }
            
            if (uMeshes.Count == 1)
            {
#if UNITY_EDITOR
                cache.Add(uMeshes[0]);
                _meshCache.Add(path, cache.Count - 1);
                return uMeshes[0];
#else
                _meshCache.Add(path, uMeshes[0]);
                return uMeshes[0];
#endif
            }
            
            // 불러온 mesh 오브젝트가 두 개인 경우가 있음
            // 이 경우 mesh를 하나로 합침
            // 이 게임의 경우 mesh 하나당 texture 하나이기 때문에
            // 하나로 합치는 것이 적합함
            var combineInstance = new CombineInstance[uMeshes.Count];

            for (int i = 0; i < uMeshes.Count; i++)
            {
                combineInstance[i].mesh = uMeshes[i];
                //y축을 180도 회전
                //combineInstance[i].transform = UnityEngine.Matrix4x4.Rotate(UnityEngine.Quaternion.Euler(0f, -180f, 0f));
                combineInstance[i].transform = UnityEngine.Matrix4x4.identity;
            }
            
            var combinedMesh = new UnityEngine.Mesh();
            combinedMesh.CombineMeshes(combineInstance, true, true);

            //이후 있을 gc 문제를 위해 mesh 파괴
            foreach (var uMesh in uMeshes)
            {
                Destroy(uMesh);
            }
            
#if UNITY_EDITOR
            cache.Add(combinedMesh);
            _meshCache.Add(path, cache.Count - 1);
            return combinedMesh;
#else
            _meshCache.Add(path, combinedMesh);
            return combinedMesh;
#endif
        }
    }
}