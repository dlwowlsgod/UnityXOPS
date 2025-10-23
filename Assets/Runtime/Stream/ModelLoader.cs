using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace UnityXOPS
{
    /// <summary>
    /// Utility class for loading 3D models into Unity's runtime environment.
    /// Provides functionality to import models from specified file paths and cache their instances for reuse.
    /// </summary>
    public static class ModelLoader
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MeshData
        {
            public IntPtr vertices;
            public int vertexCount;
            public IntPtr uvs;
            public int uvCount;
            public IntPtr indices;
            public int indexCount;
        }

        private enum ModelImportProfile : uint
        {
            IMPORT_ABORT = 0,
            IMPORT_XFILE = 1,
            IMPORT_XFILE_FIXTOKEN = 2
        }
        
        [DllImport("UnityXOPSNative")]
        private static extern IntPtr ImportModel(string filePath, ModelImportProfile profile);
        
        [DllImport("UnityXOPSNative")]
        private static extern void DeAllocModel(IntPtr data);
        
#if UNITY_EDITOR
        [DllImport("UnityXOPSNative")] 
        private static extern void GetAssimpVersion(out uint major, out uint minor, out uint patch, out uint revision);
#endif

        private static readonly Dictionary<string, Mesh> MeshCache = new();
        
        public static void Initialize()
        {
#if UNITY_EDITOR
            GetAssimpVersion(out var major, out var minor, out var patch, out var revision);
            Debug.Log($"Assimp version: {major}.{minor}.{patch}.{revision}");
#endif
        }

        /// <summary>
        /// Loads a 3D model from the specified file path and returns it as a Mesh object.
        /// If the model has been previously loaded, it retrieves the cached instance.
        /// </summary>
        /// <param name="filePath">The file path of the 3D model to be loaded.
        /// The file must exist and have a supported extension.</param>
        /// <returns>Returns the loaded Mesh object or null if the model file is not found, invalid,
        /// or an error occurs during the import process.</returns>
        public static Mesh LoadModel(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
#if UNITY_EDITOR
                Debug.LogError("Model path is empty.");
#endif
                return null;
            }
            
            if (MeshCache.TryGetValue(filePath, out var cachedMesh))
            {
                return cachedMesh;
            }
            
            if (!File.Exists(filePath))
            {
#if UNITY_EDITOR
                Debug.LogError("Model file does not exist.");
#endif
                return null;
            }
            
            var filename = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath).ToLower();

            var profile = ModelImportProfile.IMPORT_ABORT;
            switch (extension)
            {
                case ".x":
                    var fixXFileToken = ProfileLoader.GetProfileValue("Model", "FixXFileToken", "true") == "true";
                    profile = fixXFileToken ? ModelImportProfile.IMPORT_XFILE_FIXTOKEN : ModelImportProfile.IMPORT_XFILE;
                    break;
            }

            IntPtr meshDataPtr = ImportModel(filePath, profile);
            if (meshDataPtr == IntPtr.Zero)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to import model {filePath}");
#endif
                return null;
            }

            try
            {
                MeshData managedMeshData = Marshal.PtrToStructure<MeshData>(meshDataPtr);

                Vector3[] vertices = new Vector3[managedMeshData.vertexCount];
                int vertexSize = Marshal.SizeOf(typeof(Vector3));
                for (int i = 0; i < vertices.Length; i++)
                {
                    IntPtr currentPtr = new IntPtr(managedMeshData.vertices.ToInt64() + i * vertexSize);
                    vertices[i] = Marshal.PtrToStructure<Vector3>(currentPtr);
                }

                Vector2[] uvs = new Vector2[managedMeshData.uvCount];
                int uvSize = Marshal.SizeOf(typeof(Vector2));
                for (int i = 0; i < uvs.Length; i++)
                {
                    IntPtr currentPtr = new IntPtr(managedMeshData.uvs.ToInt64() + i * uvSize);
                    uvs[i] = Marshal.PtrToStructure<Vector2>(currentPtr);
                }

                int[] indices = new int[managedMeshData.indexCount];
                Marshal.Copy(managedMeshData.indices, indices, 0, managedMeshData.indexCount);

                Mesh mesh = new Mesh
                {
                    name = filename,
                    vertices = vertices,
                    uv = uvs,
                    triangles = indices
                };
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                
#if !UNITY_EDITOR
                mesh.UploadMeshData(true);
#endif

                // Add the newly created mesh to the cache before returning.
                MeshCache[filePath] = mesh;
                return mesh;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to import model {filePath}: {e.Message}");
#endif
                return null;
            }
            finally
            {
                DeAllocModel(meshDataPtr);
            }
        }
    }
}
