using JJLUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityXOPS
{
    /// <summary>
    /// BD1 파일 전체의 텍스처 경로, 원시 블록 데이터, 빌드된 블록 배열을 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class BlockData
    {
        public string[] texturePaths;
        public RawBlockData[] rawBlockData;
        public Material[] textures;
        public Block[] blocks;
    }

    public partial class MapLoader
    {
        // openxops.net/filesystem-bd1.php 기준
        private static readonly int[][] FaceVertexIndices = new int[][]
        {
            new int[] { 0, 3, 2, 1 }, // 면 0
            new int[] { 7, 4, 5, 6 }, // 면 1
            new int[] { 4, 0, 1, 5 }, // 면 2
            new int[] { 5, 1, 2, 6 }, // 면 3
            new int[] { 6, 2, 3, 7 }, // 면 4
            new int[] { 7, 3, 0, 4 }, // 면 5
        };

        /// <summary>
        /// BD1 바이너리 파일을 파싱해 BlockData 객체로 반환한다.
        /// </summary>
        /// <param name="filepath">BD1 파일 경로.</param>
        /// <returns>파싱된 BlockData. 실패 시 null.</returns>
        private static BlockData LoadBD1File(string filepath)
        {
            try
            {
                using var reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read), Encoding.ASCII);

                // 텍스쳐 경로 (10개 × 31바이트, ASCII null 종료)
                string[] texturePaths = new string[10];
                for (int i = 0; i < 10; i++)
                {
                    byte[] pathBytes = reader.ReadBytes(31);
                    int len = 0;
                    while (len < 31 && pathBytes[len] != 0) len++;
                    texturePaths[i] = Encoding.ASCII.GetString(pathBytes, 0, len);
                }

                // 블럭 개수 (uint16 리틀 엔디안)
                int blockCount = reader.ReadUInt16();

                RawBlockData[] rawBlocks = new RawBlockData[blockCount];
                for (int b = 0; b < blockCount; b++)
                {
                    // 버텍스 좌표: X[8] → Y[8] → Z[8] 순서로 분리 저장됨
                    float[] xs = new float[8];
                    float[] ys = new float[8];
                    float[] zs = new float[8];
                    for (int i = 0; i < 8; i++) xs[i] = reader.ReadSingle();
                    for (int i = 0; i < 8; i++) ys[i] = reader.ReadSingle();
                    for (int i = 0; i < 8; i++) zs[i] = reader.ReadSingle();

                    Vector3[] vertices = new Vector3[8];
                    for (int i = 0; i < 8; i++)
                        vertices[i] = new Vector3(-xs[i] * 0.1f, ys[i] * 0.1f, -zs[i] * 0.1f); // Y축 기준 180° 회전

                    // UV 좌표: U[24] → V[24] 순서 (6면 × 4개씩 분리 저장됨)
                    float[] us = new float[24];
                    float[] vs = new float[24];
                    for (int i = 0; i < 24; i++) us[i] = reader.ReadSingle();
                    for (int i = 0; i < 24; i++) vs[i] = reader.ReadSingle();

                    Vector2[] uvs = new Vector2[24];
                    for (int i = 0; i < 24; i++)
                        uvs[i] = new Vector2(us[i], 1f - vs[i]);

                    // 텍스쳐 인덱스 (6면 × int32, 유효 바이트는 최하위 1바이트)
                    int[] textureIndices = new int[6];
                    for (int i = 0; i < 6; i++)
                        textureIndices[i] = reader.ReadInt32();

                    // 활성화 플래그
                    int flag = reader.ReadInt32();

                    rawBlocks[b] = new RawBlockData
                    {
                        vertices = vertices,
                        uvs = uvs,
                        textureIndices = textureIndices,
                        flag = flag,
                    };
                }

                return new BlockData
                {
                    texturePaths = texturePaths,
                    rawBlockData = rawBlocks,
                };
            }
            catch (Exception e)
            {
                Debugger.LogError($"BD1 read failed: {filepath}\n{e.Message}", Instance, nameof(MapLoader));
                return null;
            }
        }

        /// <summary>
        /// 원시 블록 데이터 배열을 Unity Mesh가 포함된 Block 배열로 빌드한다.
        /// </summary>
        private static Block[] BuildBlocks(RawBlockData[] rawBlocks)
        {
            Block[] blocks = new Block[rawBlocks.Length];
            for (int i = 0; i < rawBlocks.Length; i++)
            {
                blocks[i] = BuildBlock(rawBlocks[i]);
                blocks[i].mesh.name = $"block_{i}";
            }
                
            return blocks;
        }

        /// <summary>
        /// 하나의 RawBlockData로부터 서브메시로 구성된 Unity Mesh와 Block 객체를 생성한다.
        /// </summary>
        private static Block BuildBlock(RawBlockData raw)
        {
            // 블럭 중심 계산
            Vector3 center = Vector3.zero;
            for (int i = 0; i < 8; i++) center += raw.vertices[i];
            center /= 8f;

            // 사용된 고유 텍스쳐 인덱스 수집 (등장 순서 유지 → 서브메시 순서)
            var uniqueTextures = new List<int>();
            for (int f = 0; f < 6; f++)
            {
                int texIdx = raw.textureIndices[f];
                if (!uniqueTextures.Contains(texIdx))
                    uniqueTextures.Add(texIdx);
            }

            int subMeshCount = uniqueTextures.Count;
            var allVertices = new List<Vector3>();
            var allUVs = new List<Vector2>();
            var subTriangles = new List<int>[subMeshCount];
            for (int s = 0; s < subMeshCount; s++)
                subTriangles[s] = new List<int>();

            for (int f = 0; f < 6; f++)
            {
                int subMeshIdx = uniqueTextures.IndexOf(raw.textureIndices[f]);
                int vertexBase = allVertices.Count;

                // 면의 4개 버텍스를 FaceVertexIndices 순서로 추가 (중심 기준 로컬 좌표)
                int[] faceVerts = FaceVertexIndices[f];
                for (int v = 0; v < 4; v++)
                {
                    allVertices.Add(raw.vertices[faceVerts[v]] - center);
                    allUVs.Add(raw.uvs[f * 4 + (v + 3) % 4]); 
                }

                // 쿼드 → 삼각형 2개 (0,1,2 / 0,2,3)
                subTriangles[subMeshIdx].Add(vertexBase + 0);
                subTriangles[subMeshIdx].Add(vertexBase + 1);
                subTriangles[subMeshIdx].Add(vertexBase + 2);
                subTriangles[subMeshIdx].Add(vertexBase + 0);
                subTriangles[subMeshIdx].Add(vertexBase + 2);
                subTriangles[subMeshIdx].Add(vertexBase + 3);
            }

            var mesh = new Mesh();
            if (allVertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(allVertices);
            mesh.SetUVs(0, allUVs);
            mesh.subMeshCount = subMeshCount;
            for (int s = 0; s < subMeshCount; s++)
                mesh.SetTriangles(subTriangles[s], s);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return new Block
            {
                mesh = mesh,
                subMeshTextureIndices = uniqueTextures.ToArray(),
                position = center,
                collider = true,
            };
        }
    }
}
