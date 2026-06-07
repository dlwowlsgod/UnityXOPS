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

        // 랜덤 무기([7]) 스폰 시 자동 보충 탄수 배율 = 장탄수 × 배율. OpenXOPS TOTAL_WEAPON_AUTOBULLET (objectmanager.h:45).
        private const int k_weaponAutoBullet = 3;

        [SerializeField]
        private Transform blockRoot, humanRoot, weaponRoot, objectRoot, skyRoot;
        [SerializeField]
        private GameObject humanPrefab, weaponPrefab, objectPrefab;
        public  GameObject WeaponPrefab => weaponPrefab;
        public  Transform  WeaponRoot   => weaponRoot;

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
        private Human player;
        public static Human Player => Instance.player;

        // 스폰된 전체 Human 목록 — AI 타겟 탐색(SearchEnemy 랜덤 샘플링)용. 스폰 순서대로 채워지며 언로드 시 비워진다.
        private List<Human> m_humans = new List<Human>();
        public static IReadOnlyList<Human> Humans => Instance.m_humans;


        [SerializeField]
        private string missionName, missionFullname, missionBD1Path, missionPD1Path,
            missionAddonObjectPath, missionImage0, missionImage1, missionBriefing;
        [SerializeField]
        private int skyIndex;
        [SerializeField]
        private bool adjustCollision, darkScreen;

        private static int blockLayerMask = 7;
        private static int blockLayer     = 0;
        public static int BlockLayerMask => blockLayerMask;
        public static int BlockLayer     => blockLayer;

        public static IReadOnlyList<Block> BlockColliders => Instance.blockColliders;

        /// <summary>
        /// AI 경로 포인트를 식별번호(param3=원본 p4)로 조회. AIPATH(param0=3) 먼저, 없으면 RAND_AIPATH(param0=8).
        /// 원본 SearchPointdata(pmask=0x08) 대응 — 중복 시 첫 매치. 못 찾으면 false (경로 끝/막다른 길).
        /// </summary>
        public static bool TryGetPathPoint(int id, out RawPointData point)
        {
            point = null;
            var sorted = Instance.m_sortedRawPointData;
            if (sorted == null) return false;

            if (sorted[3].TryGetValue(id, out var aipath) && aipath.Count > 0) { point = aipath[0]; return true; }
            if (sorted[8].TryGetValue(id, out var rand)   && rand.Count   > 0) { point = rand[0];   return true; }
            return false;
        }

        /// <summary>
        /// 이벤트 노드를 식별번호(param3=원본 P4)로 조회. 이벤트 타입(param0=10~19) 전체에서 검색.
        /// 원본 SearchPointdata(pmask=0x08, P4만) 대응 — 중복 시 첫 매치. 못 찾으면 false (라인 끝).
        /// </summary>
        public static bool TryGetEventPoint(int id, out RawPointData point)
        {
            point = null;
            var sorted = Instance.m_sortedRawPointData;
            if (sorted == null) return false;

            for (int type = 10; type <= 19; type++)
            {
                if (sorted[type].TryGetValue(id, out var list) && list.Count > 0) { point = list[0]; return true; }
            }
            return false;
        }

        /// <summary>메시지 ID(.msg 파일 0-base 행번호)로 텍스트 조회. 범위 밖이면 빈 문자열 (원본 GetMessageText).</summary>
        public static string GetMessageText(int id)
        {
            var msgs = Instance.messages;
            if (msgs == null || id < 0 || id >= msgs.Count) return string.Empty;
            return msgs[id];
        }

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
            blockLayer     = LayerMask.NameToLayer("Block");
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

        // OpenXOPS ObjectManager::SearchHuman (objectmanager.cpp:1763-1780) 대응.
        // identifier 일치하는 첫 번째 Human 을 반환 — 중복 시 첫 매치만, 미발견 시 null.
        // 호출 예: 미션 이벤트 (CheckDead/CheckArrival/CheckHaveWeapon), AI 추적 대상 지정.
        public static Human SearchHuman(int identifier)
        {
            Transform root = Instance.humanRoot;
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Human human = root.GetChild(i).GetComponent<Human>();
                if (human != null && human.Identifier == identifier) return human;
            }
            return null;
        }

        // OpenXOPS ObjectManager::SearchSmallobject (objectmanager.cpp:1786-1804) 대응.
        // identifier 일치하는 첫 번째 SmallObject 를 반환 — 중복 시 첫 매치만, 미발견 시 null.
        // 호출 예: 미션 이벤트 (CheckBreakSmallObject 의 파괴 조건 판정).
        public static SmallObject SearchSmallObject(int identifier)
        {
            Transform root = Instance.objectRoot;
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                SmallObject so = root.GetChild(i).GetComponent<SmallObject>();
                if (so != null && so.Identifier == identifier) return so;
            }
            return null;
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

        // 공유 머티리얼(MaterialManager 원본)은 건드리지 않고 런타임 생성분만 파괴
        private static void DestroyIfRuntimeMaterial(Material material)
        {
            if (material == null) return;
            var mm = MaterialManager.Instance;
            if (material == mm.MainMaterial || material == mm.BlockMaterial ||
                material == mm.TransparentMaterial || material == mm.EffectMaterial ||
                material == mm.SkyMaterial)
            {
                return;
            }
            Destroy(material);
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
            Instance.player = null;
            Instance.m_humans.Clear();
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

            // 원본 LoadPointData는 파일 순서대로 한 번 순회하며 HUMAN/HUMAN2 스폰.
            // m_sortedRawPointData는 HUMANINFO 등 O(1) 조회용으로 유지하되, 스폰 순서는 파일 순서를 따른다.
            for (int i = 0; i < pointData.rawPointData.Length; i++)
            {
                var raw = pointData.rawPointData[i];
                if (raw.param0 != 1 && raw.param0 != 6) continue;

                Instance.m_sortedRawPointData[4].TryGetValue(raw.param1, out var infoList);
                if (infoList == null || infoList.Count == 0) continue;

                var humanObj = Instantiate(Instance.humanPrefab, Instance.humanRoot);
                humanObj.transform.SetLocalPositionAndRotation(raw.position, Quaternion.Euler(0, raw.look, 0));
                var human = humanObj.GetComponent<Human>();
                // 원본 SearchPointdata는 첫 매치를 반환
                human.CreateHuman(raw, infoList[0]);
                Instance.m_humans.Add(human);

                // AddHumanIndex: p4==0인 HUMAN/HUMAN2는 Player_HumanID를 무조건 덮어씀 → 파일 내 마지막이 승
                if (raw.param3 == 0) Instance.player = human;
            }

            // fallback: p4==0 포인트가 전혀 없으면 원본 초기값 Player_HumanID=0 → 첫 번째 생성된 human
            if (Instance.player == null && Instance.humanRoot.childCount > 0)
                Instance.player = Instance.humanRoot.GetChild(0).GetComponent<Human>();

            // PD1 weapon 포인트 스폰. OpenXOPS objectmanager.cpp:367-414 AddWeaponIndex 대응 (param0=2, 7 둘 다 동일 함수 처리).
            // weaponRoot 는 localScale=1 유지 (스케일 박으면 자식 position 도 같이 곱해져 옹기종기 발생).
            // 스케일은 Weapon.CreateWeapon(dropped:true) 가 자기 localScale 에 weaponScale * size 로 적용한다.
            // [2] 일반 무기: param1 = 무기 인덱스, param2 = 총탄수.
            // [7] 랜덤 무기: param1(a)·param2(b) = 무기 인덱스 후보 2개 → GetRand(2)로 50:50 선택, param3(c)는 식별번호(스폰 미사용).
            //               탄수는 PD1 값이 아니라 선택된 무기의 magazineSize × k_weaponAutoBullet(3) 로 자동 산출.
            // OpenXOPS object.cpp:2383-2408 RunReload 분배 환산: magazine = min(total, magSize), reserve = max(0, total - magSize).
            Instance.weaponCount = 0;
            var wp = DataManager.Instance.WeaponParameterData;
            for (int i = 0; i < pointData.rawPointData.Length; i++)
            {
                var raw = pointData.rawPointData[i];
                if (raw.param0 != 2 && raw.param0 != 7) continue;

                int weaponIndex;
                int totalBullets;
                if (raw.param0 == 7)
                {
                    weaponIndex = Random.Range(0, 2) == 0 ? raw.param1 : raw.param2;
                    // 원본은 GetWeapon 실패 시 그 포인트를 스폰하지 않음 (return -1). 탄수도 무기 데이터에서 산출하므로 범위 밖이면 skip.
                    if (weaponIndex < 0 || weaponIndex >= wp.weaponData.Count) continue;
                    totalBullets = wp.weaponData[weaponIndex].magazineSize * k_weaponAutoBullet;
                }
                else
                {
                    weaponIndex  = raw.param1;
                    totalBullets = raw.param2;
                }

                int magazine = -1;
                int reserve  = -1;
                if (weaponIndex >= 0 && weaponIndex < wp.weaponData.Count)
                {
                    int magSize = wp.weaponData[weaponIndex].magazineSize;
                    magazine = System.Math.Min(totalBullets, magSize);
                    reserve  = System.Math.Max(0, totalBullets - magSize);
                }

                var weaponObj = Instantiate(Instance.weaponPrefab, Instance.weaponRoot);
                weaponObj.transform.localPosition = raw.position;

                var weapon = weaponObj.GetComponent<Weapon>();
                weapon.CreateWeapon(weaponIndex, magazine, reserve, dropped: true);
                weapon.OnDrop(raw.look, Vector3.zero);

                Instance.weaponCount++;
            }

            // PD1 small object 포인트(param0=5) 스폰. OpenXOPS objectmanager.cpp:459-484 AddSmallObjectIndex 대응.
            // param1=ObjectData 인덱스 (범위 초과 시 silently skip), param2=바닥 스냅 플래그 (1이면 SnapToGround), param3=식별 ID (이벤트 시스템용).
            Instance.objectCount = 0;
            var op = DataManager.Instance.ObjectParameterData;

            // .mif 미션 전용 추가 사물(ADDON-OBJECT, index = addonObjectIndex) 슬롯을 채운다.
            // 소물 스폰 루프가 objectData[addonObjectIndex] 를 읽기 전에 호출돼야 함. addon 없는 미션이면 슬롯을 비워 잔존 방지.
            InitializeAddonObject();

            for (int i = 0; i < pointData.rawPointData.Length; i++)
            {
                var raw = pointData.rawPointData[i];
                if (raw.param0 != 5) continue;

                int objectIndex = raw.param1;
                if (objectIndex < 0 || objectIndex >= op.objectData.Count) continue;

                var objectObj = Instantiate(Instance.objectPrefab, Instance.objectRoot);
                objectObj.transform.SetLocalPositionAndRotation(raw.position, Quaternion.Euler(0f, raw.look, 0f));

                var smallObj = objectObj.GetComponent<SmallObject>();
                smallObj.CreateObject(objectIndex, raw.param3);

                if (raw.param2 != 0)
                {
                    smallObj.SnapToGround();
                }

                Instance.objectCount++;
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
            Instance.weaponCount = 0;
            Instance.objectCount = 0;
            Instance.player = null;
            Instance.m_humans.Clear();

            foreach (Transform child in Instance.humanRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in Instance.weaponRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in Instance.objectRoot)
            {
                Destroy(child.gameObject);
            }

            Instance.messages.Clear();
            
            //DestroyMaterialCache(Instance.m_humanMaterialCache);
            //DestroyMaterialCache(Instance.m_weaponMaterialCache);
            //DestroyMaterialCache(Instance.m_objectMaterialCache);
            Instance.m_sortedRawPointData = null; //GC 이슈있을듯. (260409)
        }

        private static void DestroyMaterialCache(Dictionary<string, Material> cache)
        {
            if (cache == null) return;
            foreach (var material in cache.Values)
            {
                DestroyIfRuntimeMaterial(material);
            }
            cache.Clear();
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

            // Skybox를 MapLoader 하위(skyRoot) 아래에 붙여 씬 전환 시에도 유지.
            // 쉐이더는 _WorldSpaceCameraPos 기준 렌더라 렌더링은 트랜스폼 무관하지만,
            // Unity 프러스텀 컬링은 transform 위치를 사용 → skyRoot는 별도 스크립트로 카메라 추적.
            GameObject skyObject = new GameObject("Skybox");
            skyObject.transform.SetParent(Instance.skyRoot, false);
            skyObject.AddComponent<MeshFilter>().sharedMesh = skyMesh;
            skyObject.AddComponent<MeshRenderer>().sharedMaterial = skyMaterial;

            // fog(RenderSettings)는 씬별 자산이라 여기서 걸면 씬 전환에 씻겨나간다.
            // fog 가 보여야 할 씬의 카메라(CameraClippingApplier)가 Awake 에서 ApplySkyFog 를 직접 호출한다.
        }

        /// <summary>
        /// 메인 카메라 하위의 스카이박스 오브젝트를 모두 제거한다.
        /// </summary>
        public static void UnloadSkyData()
        {
            if (Instance.skyRoot == null) return;
            foreach (Transform child in Instance.skyRoot)
            {
                var renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null) DestroyIfRuntimeMaterial(renderer.sharedMaterial);
                Destroy(child.gameObject);
            }

            ClearFog();
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
                    Instance.missionImage1 = SafePath.Combine(
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
