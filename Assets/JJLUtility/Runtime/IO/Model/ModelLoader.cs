using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

namespace JJLUtility.IO
{
    /// <summary>
    /// 다양한 3D 모델 파일(.x)을 로드하고 메시 캐시를 관리하는 싱글톤 클래스.
    /// </summary>
    public partial class ModelLoader : SingletonBehavior<ModelLoader>
    {
#if UNITY_EDITOR
        private Dictionary<string, int> m_meshCache = new Dictionary<string, int>();
        [SerializeField]
        private List<Mesh> meshCacheList = new List<Mesh>();
#else
        private Dictionary<string, Mesh> m_meshCache = new Dictionary<string, Mesh>();
#endif

        /// <summary>
        /// 지정 경로의 3D 메시 파일을 로드해 Mesh를 반환한다. 캐시된 메시는 재사용한다.
        /// </summary>
        /// <param name="filepath">메시 파일 경로.</param>
        /// <returns>로드된 Mesh. 실패 시 null.</returns>
        public static Mesh LoadMesh(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Debugger.LogError($"Mesh path is empty.", Instance, nameof(ModelLoader));
                return null;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"Mesh file not found: {filepath}", Instance, nameof(ModelLoader));
                return null;
            }

            if (Instance.m_meshCache.ContainsKey(filepath))
            {
#if UNITY_EDITOR
                return Instance.meshCacheList[Instance.m_meshCache[filepath]];
#else
                return Instance.m_meshCache[filepath];
#endif
            }

            string extension = Path.GetExtension(filepath).ToLower();
            string filename = Path.GetFileNameWithoutExtension(filepath);

            Mesh mesh = null;
            switch (extension)
            {
                case ".x":
                    XFile xFile = LoadXFile(filepath);
                    if (xFile == null) return null;
                    mesh = BuildMeshFromXFile(xFile, filename);
                    break;

                default:
                    Debugger.LogError($"Unsupported mesh extension: {filepath}", Instance, nameof(ModelLoader));
                    return null;
            }

            if (mesh == null) return null;

#if UNITY_EDITOR
            Instance.meshCacheList.Add(mesh);
            Instance.m_meshCache.Add(filepath, Instance.meshCacheList.Count - 1);
#else
            Instance.m_meshCache.Add(filepath, mesh);
#endif

            return mesh;
        }

        /// <summary>
        /// XFile의 모든 메시 데이터를 하나의 Unity Mesh로 합쳐 반환한다.
        /// </summary>
        private static Mesh BuildMeshFromXFile(XFile xFile, string meshName)
        {
            if (xFile.Meshes.Count == 0)
            {
                Debugger.LogError($"No mesh data found in .x file: {meshName}", Instance, nameof(ModelLoader));
                return null;
            }

            var allVertices = new List<Vector3>();
            var allIndices = new List<int>();
            var allUVs = new List<Vector2>();

            foreach (var meshData in xFile.Meshes)
            {
                int offset = allVertices.Count;
                allVertices.AddRange(meshData.Vertices);
                foreach (int idx in meshData.Indices)
                    allIndices.Add(idx + offset);
                allUVs.AddRange(meshData.UVs);
            }

            var mesh = new Mesh();
            mesh.name = meshName;

            if (allVertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(allVertices);
            mesh.SetTriangles(allIndices, 0);

            if (allUVs.Count == allVertices.Count)
                mesh.SetUVs(0, allUVs);

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
