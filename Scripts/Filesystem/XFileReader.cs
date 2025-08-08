using UnityEngine;
using Assimp;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityXOPS
{
    /// <summary>
    /// Provides methods for loading and processing meshes from .x files into Unity-compatible Mesh objects.
    /// </summary>
    public static class XFileReader
    {
        /*
        .x 파일을 불러오는 기능입니다.
        Assimp라고 하는 외부 모듈을 이용하고요. 실제로 파일을 직접 파싱하지는 않습니다.
        원본 엑옵은 하나의 모델 당 하나의 텍스쳐를 절대 원칙으로 하기 때문에
        그걸 감안해서 메시가 여러 개일 경우 하나로 합쳐버립니다.
        왜냐하면 메시가 많아지면 드로우콜 영향도 있을 수 있거든요
        애초에 텍스쳐 여러개를 쓸게 아니면 나눌 필요도 없습니다.
        그리고 기존 엑옵에서 grenade.x, mac10.x와 같은 경우 파일 자체에 오류가 좀 있습니다.
        이 경우는 ini 설정에 따라 파일 자체를 수정해 버리게 처리합니다.
         */
        
        private static readonly AssimpContext AssimpContext;
        private static readonly Dictionary<string, UnityEngine.Mesh> MeshCache = new();

        static XFileReader()
        {
            AssimpContext = new AssimpContext();
        }

        /// <summary>
        /// Loads a mesh from a specified .x file path and returns it as a Unity mesh.
        /// </summary>
        /// <param name="path">The full file path to the .x file to be loaded.</param>
        /// <returns>
        /// A UnityEngine.Mesh object if the mesh is successfully loaded and processed, or null if
        /// the provided path is invalid, the file does not exist, is not an .x file, or the file does not contain meshes.
        /// </returns>
        public static UnityEngine.Mesh LoadMesh(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[XFileLoader] Empty path returned: {path}");
#endif
                return null;
            }

            if (MeshCache.TryGetValue(path, out var mesh))
            {
#if UNITY_EDITOR
                Debug.Log($"[XFileLoader] Cached mesh {path} returned");
#endif
                return mesh;           
            }

            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogError($"[XFileLoader] File not found: {path}");
#endif
                return null;
            }
            
            var extension = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);
            
            if (PrivateProfileReader.FixXFileError)
            {
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    
                    var isTextXFile = bytes.Length > 16 &&
                                      bytes[8] == 't' && bytes[9] == 'x' && bytes[10] == 't' && bytes[11] == ' ';

                    if (isTextXFile)
                    {
                        if (bytes[16] != '\r' && bytes[16] != '\n')
                        {
                            var fixedBytes = new List<byte>(bytes.Length + 2);
                            fixedBytes.AddRange(bytes.Take(16));
                            fixedBytes.Add((byte)'\r');
                            fixedBytes.Add((byte)'\n');
                            fixedBytes.AddRange(bytes.Skip(16));

                            File.WriteAllBytes(path, fixedBytes.ToArray());

#if UNITY_EDITOR
                            Debug.Log($"[XFileLoader] Model file {name}{extension} fixed.");
#endif
                        }
                    }
                }
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogError($"[XFileLoader] Failed to fix .x file {path}. Error: {e.Message}");
#endif
                    return null;
                }

            }

            if (extension != ".x")
            {
#if UNITY_EDITOR
                Debug.LogError($"[XFileLoader] File is not .x file: {path}");
#endif
                return null;
            }

            var preset = PostProcessPreset.TargetRealTimeFast;
            var scene = AssimpContext.ImportFile(path, preset);
            
            var uMeshes = new List<UnityEngine.Mesh>();

            foreach (var aMesh in scene.Meshes)
            {
                var uMesh = new UnityEngine.Mesh
                {
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
                Debug.Log($"[XFileLoader] {name} has no meshes.");
#endif
                MeshCache.Add(path, null);
                return null;
            }
            
            var combineInstance = new CombineInstance[uMeshes.Count];

            for (int i = 0; i < uMeshes.Count; i++)
            {
                combineInstance[i].mesh = uMeshes[i];
                combineInstance[i].transform = UnityEngine.Matrix4x4.Rotate(UnityEngine.Quaternion.Euler(0f, -180f, 0f));
            }
            
            var combinedMesh = new UnityEngine.Mesh();
            combinedMesh.CombineMeshes(combineInstance, true, true);

            foreach (var uMesh in uMeshes)
            {
                Object.Destroy(uMesh);
            }
            
#if UNITY_EDITOR
            Debug.Log(uMeshes.Count > 1
                ? $"[XFileLoader] multiple mesh instance {name} combined and returned"
                : $"[XFileLoader] single mesh instance {name} returned");
#endif
            MeshCache.Add(path, combinedMesh);
            return combinedMesh;
        }
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ClearCacheOnLoad()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state is not (PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode))
                {
                    return;
                }
                MeshCache.Clear();
                Debug.Log("[XFileLoader] Mesh cache cleared.");
            };
        }
#endif

    }
}