using JJLUtility;
using JJLUtility.IO;
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
        [SerializeField]
        private Transform blockRoot;
        [SerializeField]
        private int blockCount;
        [SerializeField]
        private List<Material> blockMaterials;
        public List<Material> BlockMaterials => blockMaterials;
        [SerializeField]
        private List<Block> blockColliders;
        public static IReadOnlyList<Block> BlockColliders => Instance.blockColliders;

        private static int blockLayerMask = 7;
        private static int blockLayer = 0;
        public static int BlockLayerMask => blockLayerMask;
        public static int BlockLayer => blockLayer;

        private void Start()
        {
            blockLayerMask = LayerMask.GetMask("Block");
            blockLayer = LayerMask.NameToLayer("Block");
        }

        /// <summary>
        /// BD1 파일을 파싱해 블록 메시와 머티리얼을 생성하고 씬에 배치한다.
        /// </summary>
        /// <param name="filepath">BD1 파일 경로.</param>
        public static void LoadBlockData(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Debugger.LogError("BD1 path is empty.", Instance, nameof(MapLoader));
                return;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"BD1 file not exists: {filepath}", Instance, nameof(MapLoader));
                return;
            }

            BlockData blockData = LoadBD1File(filepath);
            if (blockData == null)
            {
                return;
            }

            Instance.blockCount = blockData.rawBlockData.Length;
            blockData.blocks = BuildBlocks(blockData.rawBlockData, Instance.darkScreen);

            Instance.blockColliders = new List<Block>();
            Instance.blockMaterials = new List<Material>();
            string bd1Dir = Path.GetDirectoryName(filepath);
            for (int i = 0; i < blockData.texturePaths.Length; i++)
            {
                string texturePath = blockData.texturePaths[i];

                if (string.IsNullOrEmpty(texturePath))
                {
                    Instance.blockMaterials.Add(MaterialManager.Instance.BlockMaterial);
                    continue;
                }

                string extension = Path.GetExtension(texturePath).ToLower();
                Material baseMaterial = MaterialManager.Instance.BlockMaterial;

                string fullTexturePath = SafePath.Combine(bd1Dir, texturePath);
                Texture2D blockTexture = ImageLoader.LoadTexture(fullTexturePath);

                if (blockTexture == null)
                {
                    Instance.blockMaterials.Add(baseMaterial);
                    continue;
                }

                Material blockMaterial = new Material(baseMaterial);
                blockMaterial.name = Path.GetFileName(texturePath);
                blockMaterial.mainTexture = blockTexture;

                Instance.blockMaterials.Add(blockMaterial);
            }

            // 원본 OpenXOPS 는 맵 블록을 먼저(RenderMapdata), 소물/오브젝트를 나중(ObjMgr.Render)에 그린다.
            // 블록과 addon(소물)이 같은 셰이더·같은 queue(2450)면 Unity 가 카메라 거리순으로 정렬해 순서가 뒤집히며
            // 동일 평면에서 Z-fighting(회전 시 깜빡임)이 난다. 블록 queue 를 2449 로 내려 addon(2450) 보다 항상 먼저
            // 그려지게 하면, ZTest LEqual 상 동일 깊이서 나중에 그린 addon 이 이겨 블록을 덮는다 = 원본 그리기 순서와 동일.
            // BlockMaterial 은 MainMaterial 과 별도 에셋이고 여기선 블록용 런타임 인스턴스만 바꾸므로 오브젝트 렌더엔 영향 없음.
            const int k_blockRenderQueue = 2449;
            for (int i = 0; i < Instance.blockMaterials.Count; i++)
                Instance.blockMaterials[i].renderQueue = k_blockRenderQueue;

            for (int i = 0; i < blockData.blocks.Length; i++)
            {
                Block block = blockData.blocks[i];
                GameObject blockObj = new GameObject($"Block_{i}");
                blockObj.transform.SetParent(Instance.blockRoot, false);

                MeshFilter meshFilter = blockObj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = block.mesh;

                MeshRenderer meshRenderer = blockObj.AddComponent<MeshRenderer>();
                Material[] materials = new Material[block.subMeshTextureIndices.Length];
                for (int j = 0; j < block.subMeshTextureIndices.Length; j++)
                {
                    int textureIndex = block.subMeshTextureIndices[j];
                    if (textureIndex >= 0 && textureIndex < Instance.blockMaterials.Count)
                    {
                        materials[j] = Instance.blockMaterials[textureIndex];
                    }
                    else
                    {
                        //투명벽 처리
                        materials[j] = MaterialManager.Instance.TransparentMaterial;
                    }
                }
                meshRenderer.sharedMaterials = materials;

                blockObj.transform.localPosition = block.position;

                if (block.collider)
                {
                    blockObj.layer = LayerMask.NameToLayer("Block");
                    MeshCollider mc = blockObj.AddComponent<MeshCollider>();
                    mc.sharedMesh = block.mesh;
                    Instance.blockColliders.Add(block);
                }
            }

            Physics.SyncTransforms();
        }

        // CheckALLBlockIntersectRay 대응 — 두꺼운 블록과 레이 교차 여부 반환
        public static bool RaycastBlock(Vector3 origin, Vector3 direction, float maxDist, out float dist)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist, blockLayerMask))
            {
                dist = hit.distance;
                return true;
            }
            dist = 0f;
            return false;
        }

        // CheckALLBlockInside 대응 — point가 두꺼운 블록 내부이면 true 반환
        public static bool IsInsideBlock(Vector3 point)
        {
            var colliders = Instance.blockColliders;
            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i].Contains(point)) return true;
            }
            return false;
        }

        /// <summary>
        /// 씬에 생성된 모든 블록 오브젝트와 머티리얼을 제거한다.
        /// </summary>
        public static void UnloadBlockData()
        {
            foreach (Transform child in Instance.blockRoot)
            {
                Destroy(child.gameObject);
            }
            // blockMaterials는 공유 머티리얼(BlockMaterial)과 런타임 복제본이 섞여 있음
            for (int i = 0; i < Instance.blockMaterials.Count; i++)
            {
                DestroyIfRuntimeMaterial(Instance.blockMaterials[i]);
            }
            Instance.blockMaterials.Clear();
            Instance.blockColliders.Clear();
        }

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
        private const float k_lightX = 0.98480775f; // -cos(190°)
        private const float k_lightY = 0.86602540f; // sin(120°)
        private const float k_lightZ = 0.17364818f; // -sin(190°)

        // 평평한 블록의 degenerate(부피 0) bounds 로 인한 프러스텀 컬링 오작동 방지용 최소 두께 (Unity 단위, = 1 OpenXOPS 단위).
        private const float k_minBoundsThickness = 0.1f;

        // 충돌 AABB 여유 (원본 COLLISION_ADDSIZE × 0.1). 브로드페이즈 경계 케이스 포함용.
        private const float k_collisionAddSize = 0.001f;

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
                int[] fi = FaceVertexIndices[f];
                Vector3 v0 = raw.vertices[fi[0]];
                Vector3 v1 = raw.vertices[fi[1]];
                Vector3 v2 = raw.vertices[fi[2]];
                Vector3 v3 = raw.vertices[fi[3]];

                // 원본 datafile.cpp:222-245: 두 삼각형 각각의 법선 계산 후 더 긴 쪽(비퇴화) 선택.
                // 경사 블록의 면이 비평면 quad여도 유효한 법선을 얻을 수 있다.
                Vector3 c1 = Vector3.Cross(v3 - v2, v0 - v2);
                Vector3 c2 = Vector3.Cross(v1 - v0, v2 - v0);
                Vector3 n = (c1.sqrMagnitude > c2.sqrMagnitude ? c1 : c2).normalized;

                Vector3 fc = (v0 + v1 + v2 + v3) * 0.25f;
                // 원본 CalculationBlockdata(datafile.cpp:222-250) 와 동일하게 winding 으로 구한 법선을 그대로 쓴다.
                // 중심 바깥쪽으로 강제 보정하면 IsCenterVisibleFromAnyFace(판자블록 조건2: 중심이 한 면이라도 앞쪽)가
                // 항상 거짓이 돼, 함몰/뒤집힌 형태의 통과벽(원본 BoardBlock)이 콜라이더를 갖게 되어 통과 불가가 된다.

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

            // 평평한 블록(바닥/천장 등 한 축 두께 0)은 degenerate AABB(부피 0)가 되어, Unity 프러스텀 컬링이
            // 특정 카메라 각도에서 "프러스텀 밖"으로 잘못 판정해 통째로 사라진다. 두께 0 축에 최소 두께를 부여해 막는다.
            // 컬링 자체는 유지되고, 메시 지오메트리·MeshCollider(삼각형 기반)·렌더링에는 영향이 없다.
            Bounds mb = mesh.bounds;
            Vector3 size = mb.size;
            size.x = Mathf.Max(size.x, k_minBoundsThickness);
            size.y = Mathf.Max(size.y, k_minBoundsThickness);
            size.z = Mathf.Max(size.z, k_minBoundsThickness);
            mb.size = size;
            mesh.bounds = mb;

            Vector3[] expanded = ExpandVertices(raw.vertices);
            bool isBoardBlock = HasDuplicateExpandedVertices(expanded)
                              || IsCenterVisibleFromAnyFace(center, faceNormals, faceCenters);

            // 8정점 월드 AABB — 충돌 브로드페이즈 fast-reject 용. 원본 COLLISION_ADDSIZE 여유를 반영해 살짝 확장.
            Vector3 boundsMin = raw.vertices[0];
            Vector3 boundsMax = raw.vertices[0];
            for (int i = 1; i < 8; i++)
            {
                boundsMin = Vector3.Min(boundsMin, raw.vertices[i]);
                boundsMax = Vector3.Max(boundsMax, raw.vertices[i]);
            }
            Vector3 boundsMargin = Vector3.one * k_collisionAddSize;
            boundsMin -= boundsMargin;
            boundsMax += boundsMargin;

            return new Block
            {
                mesh = mesh,
                subMeshTextureIndices = uniqueTextures.ToArray(),
                position = center,
                collider = !isBoardBlock,
                faceNormals = faceNormals,
                faceCenters = faceCenters,
                boundsMin = boundsMin,
                boundsMax = boundsMax,
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
