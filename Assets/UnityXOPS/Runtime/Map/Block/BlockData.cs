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
        /// <param name="darkFlag">true면 다크 모드(미션 screenflag bit1) — 면 명도 오프셋 0.3, false면 0.5.</param>
        private static Block[] BuildBlocks(RawBlockData[] rawBlocks, bool darkFlag)
        {
            Block[] blocks = new Block[rawBlocks.Length];
            for (int i = 0; i < rawBlocks.Length; i++)
            {
                blocks[i] = BuildBlock(rawBlocks[i], darkFlag);
                blocks[i].mesh.name = $"block_{i}";
            }

            return blocks;
        }

        // 원본 datafile.cpp:181-268 fake lighting 광원 방향.
        // OpenXOPS: L = (cos(190°), sin(120°), sin(190°)) — 정규화되지 않음(‖L‖≈1.312), 원본 그대로 사용.
        // BD1 로드 시 정점이 (-xs, ys, -zs)로 X/Z 반전됐으므로 광원도 동일하게 반전.
        private const float k_lightX =  0.98480775f;  // -cos(190°)
        private const float k_lightY =  0.86602540f;  //  sin(120°)
        private const float k_lightZ =  0.17364818f;  // -sin(190°)

        private static Block BuildBlock(RawBlockData raw, bool darkFlag)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < 8; i++) center += raw.vertices[i];
            center /= 8f;

            // --- 면 법선 먼저 계산 (셰이딩 + 충돌 공용) ---
            // 원본 OpenXOPS의 COLLISION_ADDSIZE는 cbdata.min/max AABB fast-reject에만 쓰이고
            // polygon_center_x/y/z (= 여기 faceCenters)는 실제 면 중심을 그대로 사용. 확장 금지.
            Vector3[] faceNormals = new Vector3[6];
            Vector3[] faceCenters = new Vector3[6];
            for (int f = 0; f < 6; f++)
            {
                int[]   fi = FaceVertexIndices[f];
                Vector3 v0 = raw.vertices[fi[0]];
                Vector3 v1 = raw.vertices[fi[1]];
                Vector3 v2 = raw.vertices[fi[2]];
                Vector3 v3 = raw.vertices[fi[3]];

                // 원본 datafile.cpp:222-245: 두 삼각형 각각의 법선 계산 후 더 긴 쪽(비퇴화) 선택.
                // 경사 블록의 면이 비평면 quad여도 유효한 법선을 얻을 수 있다.
                Vector3 c1 = Vector3.Cross(v3 - v2, v0 - v2);
                Vector3 c2 = Vector3.Cross(v1 - v0, v2 - v0);
                Vector3 n  = (c1.sqrMagnitude > c2.sqrMagnitude ? c1 : c2).normalized;

                Vector3 fc = (v0 + v1 + v2 + v3) * 0.25f;
                // 법선이 중심에서 바깥쪽을 향하도록 보정
                if (Vector3.Dot(n, fc - center) < 0f) n = -n;

                faceCenters[f] = fc;
                faceNormals[f] = n;
            }

            // --- 면별 셰이딩 스칼라: shadow = ‖N + L‖ / 6 + offset (원본 datafile.cpp:253-265) ---
            float offset = darkFlag ? 0.3f : 0.5f;
            float[] faceShadows = new float[6];
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = faceNormals[f];
                float rx = n.x + k_lightX;
                float ry = n.y + k_lightY;
                float rz = n.z + k_lightZ;
                faceShadows[f] = Mathf.Sqrt(rx * rx + ry * ry + rz * rz) / 6f + offset;
            }

            // --- 메시 빌드 (면당 정점 분리로 flat shading 보장) ---
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
            var allColors = new List<Color>();
            var subTriangles = new List<int>[subMeshCount];
            for (int s = 0; s < subMeshCount; s++)
                subTriangles[s] = new List<int>();

            for (int f = 0; f < 6; f++)
            {
                int subMeshIdx = uniqueTextures.IndexOf(raw.textureIndices[f]);
                int vertexBase = allVertices.Count;

                int[] faceVerts = FaceVertexIndices[f];
                float s = faceShadows[f];
                Color faceColor = new Color(s, s, s, 1f);
                for (int v = 0; v < 4; v++)
                {
                    allVertices.Add(raw.vertices[faceVerts[v]] - center);
                    allUVs.Add(raw.uvs[f * 4 + (v + 3) % 4]);
                    allColors.Add(faceColor);
                }

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
            mesh.SetColors(allColors);
            mesh.subMeshCount = subMeshCount;
            for (int s = 0; s < subMeshCount; s++)
                mesh.SetTriangles(subTriangles[s], s);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Vector3[] expanded = ExpandVertices(raw.vertices);
            bool isBoardBlock  = HasDuplicateExpandedVertices(expanded)
                              || IsCenterVisibleFromAnyFace(center, faceNormals, faceCenters);

            return new Block
            {
                mesh = mesh,
                subMeshTextureIndices = uniqueTextures.ToArray(),
                position = center,
                collider = !isBoardBlock,
                faceNormals = faceNormals,
                faceCenters = faceCenters,
            };
        }

        private static Vector3[] ExpandVertices(Vector3[] verts)
        {
            Vector3[] result = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float r = verts[i].magnitude + 0.01f;
                result[i] = verts[i].normalized * r;
            }
            return result;
        }

        private static bool HasDuplicateExpandedVertices(Vector3[] ev)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = i + 1; j < 8; j++)
                {
                    if (ev[i] == ev[j]) return true;
                }
            }
            return false;
        }

        private static bool IsCenterVisibleFromAnyFace(Vector3 center, Vector3[] normals, Vector3[] centers)
        {
            for (int i = 0; i < 6; i++)
            {
                float d = Vector3.Dot(normals[i], centers[i] - center);
                if (d <= 0f) return true;
            }
            return false;
        }
    }
}
