using JJLUtility;
using JJLUtility.IO;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// BD1 맵 블록, 스카이, 미션 데이터를 로드/언로드하고 씬에 오브젝트를 생성하는 싱글톤 매니저.
    /// </summary>
    public partial class MapLoader : SingletonBehavior<MapLoader>
    {
        private const int k_maxParameterCount = 20;

        [SerializeField]
        private Transform blockRoot, humanRoot;
        [SerializeField]
        private GameObject humanPrefab;

        [SerializeField]
        private int blockCount;
        [SerializeField]
        private List<Material> blockMaterials;
        public List<Material> BlockMaterials => blockMaterials;
        [SerializeField]
        private List<Block> blockColliders;

        [SerializeField]
        private int pointCount, humanCount, weaponCount, objectCount;
        [SerializeField]
        private List<string> messages;
        private Dictionary<string, Material> m_humanMaterialCache, m_weaponMaterialCache, m_objectMaterialCache;
        public Dictionary<string, Material> HumanMaterialCache => m_humanMaterialCache;
        public Dictionary<string, Material> WeaponMaterialCache => m_weaponMaterialCache;
        public Dictionary<string, Material> ObjectMaterialCache => m_objectMaterialCache;


        [SerializeField]
        private string missionName, missionFullname, missionBD1Path, missionPD1Path,
            missionAddonObjectPath, missionImage0, missionImage1, missionBriefing;
        [SerializeField]
        private int skyIndex;
        [SerializeField]
        private bool adjustCollision, darkScreen;

        private static int blockLayerMask = 7;

        public static IReadOnlyList<Block> BlockColliders => Instance.blockColliders;

        public string MissionName => missionName;
        public string MissionFullname => missionFullname;
        public string MissionBD1Path => missionBD1Path;
        public string MissionPD1Path => missionPD1Path;
        public string MissionAddonObjectPath => missionAddonObjectPath;
        public string MissionImage0 => missionImage0;
        public string MissionImage1 => missionImage1;
        public int SkyIndex => skyIndex;
        public string MissionBriefing => missionBriefing;
        public bool AdjusterCollision => adjustCollision;
        public bool DarkScreen => darkScreen;

        private List<Dictionary<int, List<RawPointData>>> m_sortedRawPointData;

        private void Start()
        {
            blockLayerMask = LayerMask.GetMask("Block");
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
            blockData.blocks = BuildBlocks(blockData.rawBlockData);

            Instance.blockColliders = new List<Block>();
            Instance.blockMaterials = new List<Material>();
            string bd1Dir = Path.GetDirectoryName(filepath);
            for (int i = 0; i < blockData.texturePaths.Length; i++)
            {
                string texturePath = blockData.texturePaths[i];

                if (string.IsNullOrEmpty(texturePath))
                {
                    Instance.blockMaterials.Add(MaterialManager.Instance.MainMaterial);
                    continue;
                }

                string extension = Path.GetExtension(texturePath).ToLower();
                Material baseMaterial = MaterialManager.Instance.MainMaterial;

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
            Instance.blockMaterials.Clear();
            Instance.blockColliders.Clear();
        }

        public static void LoadPointData(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Debugger.LogError("PD1 path is empty.", Instance, nameof(MapLoader));
                return;
            }

            if (!File.Exists(filepath))
            {
                Debugger.LogError($"PD1 file not exists: {filepath}", Instance, nameof(MapLoader));
                return;
            }

            PointData pointData = LoadPD1File(filepath);
            if (pointData == null)
            {
                return;
            }
            
            Instance.pointCount = pointData.rawPointData.Length;
            Instance.m_humanMaterialCache = new Dictionary<string, Material>();
            Instance.m_weaponMaterialCache = new Dictionary<string, Material>();
            Instance.m_objectMaterialCache = new Dictionary<string, Material>();

            // 파라미터 정리
            Instance.m_sortedRawPointData = new List<Dictionary<int, List<RawPointData>>>();
            for (int i = 0; i < k_maxParameterCount; i++)
            {
                Instance.m_sortedRawPointData.Add(new Dictionary<int, List<RawPointData>>());
            }
            for (int i = 0; i < pointData.rawPointData.Length; i++)
            {
                var raw = pointData.rawPointData[i];
                if (raw.param0 >= 0 && raw.param0 < k_maxParameterCount)
                {
                    var dict = Instance.m_sortedRawPointData[raw.param0];
                    if (!dict.ContainsKey(raw.param3))
                    {
                        dict[raw.param3] = new List<RawPointData>();
                    }

                    dict[raw.param3].Add(raw);
                }
            }

            foreach (var humanRaw in Instance.m_sortedRawPointData[1].Values)
            {
                foreach (var raw in humanRaw)
                {
                    var humanObj = Instantiate(Instance.humanPrefab, Instance.humanRoot);
                    humanObj.transform.SetLocalPositionAndRotation(raw.position, Quaternion.Euler(0, raw.look, 0));
                    Instance.m_sortedRawPointData[4].TryGetValue(raw.param1, out var list);
                    var human = humanObj.GetComponent<Human>();

                    if (list.Count <= 0)
                    {
                        continue;
                    }

                    human.CreateHuman(raw, list[^1]);
                }
            }

            Instance.pointCount = pointData.rawPointData.Length;
            if (pointData.msg != null)
            {
                Instance.messages = new List<string>(pointData.msg);
            }
        }

        public static void UnloadPointData()
        {
            Instance.pointCount = 0;

            foreach (Transform child in Instance.humanRoot)
            {
                Destroy(child.gameObject);
            }

            Instance.messages.Clear();  
            Instance.m_humanMaterialCache.Clear();
            Instance.m_weaponMaterialCache.Clear();
            Instance.m_objectMaterialCache.Clear();
            Instance.m_sortedRawPointData = null; //GC 이슈있을듯. (260409)
        }

        /// <summary>
        /// 스카이 메시와 텍스처를 로드해 메인 카메라 하위에 스카이박스 오브젝트를 생성한다.
        /// </summary>
        /// <param name="textureIndex">SkyData 텍스처 경로 목록의 인덱스.</param>
        public static void LoadSkyData(int textureIndex)
        {
            var skyData = DataManager.Instance.SkyData;

            if (skyData == null)
            {
                Debugger.LogError("SkyData is null.", Instance, nameof(MapLoader));
                return;
            }

            string streamingPath = Application.streamingAssetsPath;
            string fullMeshPath = SafePath.Combine(streamingPath, skyData.skyMeshPath);

            Mesh skyMesh = ModelLoader.LoadMesh(fullMeshPath);
            if (skyMesh == null)
            {
                Debugger.LogError($"Failed to load sky mesh: {fullMeshPath}", Instance, nameof(MapLoader));
                return;
            }

            // textureIndex가 유효하고 경로가 비어있지 않으면 텍스처 적용, 아니면 검은색
            Material skyMaterial = new Material(MaterialManager.Instance.SkyMaterial);
            skyMaterial.name = "SkyMaterial";

            if (textureIndex > 0 && textureIndex < skyData.skyTexturePath.Count)
            {
                string texPath = skyData.skyTexturePath[textureIndex];
                if (!string.IsNullOrEmpty(texPath))
                {
                    string fullTexPath = SafePath.Combine(streamingPath, texPath);
                    Texture2D tex = ImageLoader.LoadTexture(fullTexPath);
                    if (tex != null)
                        skyMaterial.mainTexture = tex;
                }
            }

            GameObject skyObject = new GameObject("Skybox");
            skyObject.transform.parent = Camera.main.transform;
            skyObject.AddComponent<MeshFilter>().sharedMesh = skyMesh;
            skyObject.AddComponent<MeshRenderer>().sharedMaterial = skyMaterial;
        }

        /// <summary>
        /// 메인 카메라 하위의 스카이박스 오브젝트를 제거한다.
        /// </summary>
        public static void UnloadSkyData()
        {
            var skyObject = Camera.main.transform.Find("Skybox");
            if (skyObject != null)
            {
                Destroy(skyObject.gameObject);
            }
        }

        /// <summary>
        /// 지정된 미션 데이터를 읽어 MapLoader 인스턴스 필드에 세팅한다.
        /// </summary>
        /// <param name="index">미션 목록의 인덱스.</param>
        /// <param name="mif">true면 어드온 .mif 파일, false면 공식 미션 데이터를 로드한다.</param>
        public static void LoadMissionData(int index, bool mif)
        {
            if (mif)
            {
                string addonMissionPath = DataManager.Instance.MissionData.addonMissions[index].mifPath;
                string[] mifLines = File.ReadAllLines(addonMissionPath, EncodingHelper.GetEncoding());

                Instance.missionName = mifLines[0];
                Instance.missionFullname = mifLines[1];
                Instance.missionBD1Path = SafePath.Combine(
                    Application.streamingAssetsPath, mifLines[2].TrimStart('.').TrimStart('\\').Replace('\\', '/'));
                Instance.missionPD1Path = SafePath.Combine(
                    Application.streamingAssetsPath, mifLines[3].TrimStart('.').TrimStart('\\').Replace('\\', '/'));
                Instance.skyIndex = int.TryParse(mifLines[4], out var si) ? si : 0;
                
                if (int.TryParse(mifLines[5], out var bit))
                {
                    Instance.adjustCollision = (bit & 1) != 0;
                    Instance.darkScreen = (bit & 2) != 0;
                }
                else
                {
                    Instance.adjustCollision = Instance.darkScreen = false;
                }

                if (string.IsNullOrEmpty(mifLines[6]) || mifLines[6] != "!")
                {
                    Instance.missionAddonObjectPath = SafePath.Combine(
                        Application.streamingAssetsPath, mifLines[6].TrimStart('.').TrimStart('\\').Replace('\\', '/'));
                }
                Instance.missionImage0 = SafePath.Combine(
                    Application.streamingAssetsPath, mifLines[7].TrimStart('.').TrimStart('\\').Replace('\\', '/'));
                if (string.IsNullOrEmpty(mifLines[8]) || mifLines[8] != "!")
                {
                    Instance.missionAddonObjectPath = SafePath.Combine(
                        Application.streamingAssetsPath, mifLines[8].TrimStart('.').TrimStart('\\').Replace('\\', '/'));
                }
                Instance.missionBriefing = string.Join('\n', mifLines[9..]);
            }
            else
            {
                var officialMission = DataManager.Instance.MissionData.officialMissions[index];
                Instance.missionName = officialMission.name;
                Instance.missionFullname = officialMission.fullname;
                Instance.missionBD1Path = SafePath.Combine(Application.streamingAssetsPath, officialMission.bd1Path);
                Instance.missionPD1Path = SafePath.Combine(Application.streamingAssetsPath, officialMission.pd1Path);
                Instance.adjustCollision = officialMission.adjustCollision;
                Instance.darkScreen = officialMission.darkScreen;

                var txtPath = SafePath.Combine(Application.streamingAssetsPath, officialMission.txtPath);
                if (File.Exists(txtPath))
                {
                    var txt = File.ReadAllLines(txtPath, EncodingHelper.GetEncoding());
                    if (txt.Length > 2)
                    {
                        var briefingPath = Path.Combine(Application.streamingAssetsPath, "data/briefing");
                        if (!string.IsNullOrEmpty(txt[0]) && txt[0] != "!")
                        {
                            Instance.missionImage0 = SafePath.Combine(briefingPath, txt[0]) + ".bmp";
                        }
                        if (!string.IsNullOrEmpty(txt[1]) && txt[1] != "!")
                        {
                            Instance.missionImage1 = SafePath.Combine(briefingPath, txt[1]) + ".bmp";
                        }
                        if (int.TryParse(txt[2], out int skyIndex))
                        {
                            Instance.skyIndex = skyIndex;
                        }
                        Instance.missionBriefing = string.Join("\n", txt, 3, txt.Length - 3);
                    }
                }
            }
        }

        /// <summary>
        /// MapLoader 인스턴스에 저장된 모든 미션 데이터 필드를 초기화한다.
        /// </summary>
        public static void UnloadMissionData()
        {
            Instance.missionName = string.Empty;
            Instance.missionFullname = string.Empty;
            Instance.missionBD1Path = string.Empty;
            Instance.missionPD1Path = string.Empty;
            Instance.missionAddonObjectPath = string.Empty;
            Instance.missionImage0 = string.Empty;
            Instance.missionImage1 = string.Empty;
            Instance.skyIndex = 0;
            Instance.missionBriefing = string.Empty;
            Instance.adjustCollision = false;
            Instance.darkScreen = false;
        }
    }
}
