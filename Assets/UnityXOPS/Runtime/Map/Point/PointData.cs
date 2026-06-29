using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// PD1 파일 전체의 포인트 데이터와 메시지를 담는 컨테이너 클래스.
    /// </summary>
    public class PointData
    {
        public RawPointData[] rawPointData;
        public string[] msg;
    }

    public partial class MapLoader
    {
        private const int k_maxParameterCount = 20;

        // 랜덤 무기([7]) 스폰 시 자동 보충 탄수 배율 = 장탄수 × 배율. OpenXOPS TOTAL_WEAPON_AUTOBULLET (objectmanager.h:45).
        private const int k_weaponAutoBullet = 3;

        [SerializeField]
        private Transform humanRoot, weaponRoot, objectRoot;
        public Transform WeaponRoot => weaponRoot;
        [SerializeField]
        private GameObject humanPrefab, weaponPrefab, objectPrefab;
        public GameObject WeaponPrefab => weaponPrefab;

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

        private List<Dictionary<int, List<RawPointData>>> m_sortedRawPointData;

        /// <summary>
        /// PD1 바이너리 파일을 파싱해 PointData 객체로 반환한다.
        /// </summary>
        /// <param name="filepath">PD1 파일 경로.</param>
        /// <returns>파싱된 PointData. 실패 시 null.</returns>
        private static PointData LoadPD1File(string filepath)
        {
            try
            {
                using var reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read), Encoding.ASCII);

                var pointData = new PointData();

                int pointCount = reader.ReadInt16();
                pointData.rawPointData = new RawPointData[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    var rawPointData = new RawPointData
                    {
                        position = new Vector3(-reader.ReadSingle() * 0.1f, reader.ReadSingle() * 0.1f, -reader.ReadSingle() * 0.1f),
                        look = reader.ReadSingle() * Mathf.Rad2Deg + 180f,
                        param0 = reader.ReadByte(),
                        param1 = reader.ReadByte(),
                        param2 = reader.ReadByte(),
                        param3 = reader.ReadByte()
                    };
                    
                    pointData.rawPointData[i] = rawPointData;
                }

                string msgPath = Path.ChangeExtension(filepath, ".msg");
                if (File.Exists(msgPath))
                {
                    pointData.msg = EncodingHelper.ReadAllLines(msgPath);
                }

                return pointData;
            }
            catch (Exception e)
            {
                Debugger.LogError($"PD1 read failed: {filepath}\n{e.Message}", Instance, nameof(MapLoader));
                return null;
            }
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
            Instance.m_stats.Reset(); // 맵 로드(Briefing 진입/F12 재시작) 시 통계 초기화. 씬 전환(→Maingame→Result)엔 미호출이라 Result 까지 유지.
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
                    weaponIndex = UnityEngine.Random.Range(0, 2) == 0 ? raw.param1 : raw.param2;
                    // 원본은 GetWeapon 실패 시 그 포인트를 스폰하지 않음 (return -1). 탄수도 무기 데이터에서 산출하므로 범위 밖이면 skip.
                    if (weaponIndex < 0 || weaponIndex >= wp.weaponData.Count) continue;
                    totalBullets = wp.weaponData[weaponIndex].magazineSize * k_weaponAutoBullet;
                }
                else
                {
                    weaponIndex = raw.param1;
                    totalBullets = raw.param2;
                }

                int magazine = -1;
                int reserve = -1;
                if (weaponIndex >= 0 && weaponIndex < wp.weaponData.Count)
                {
                    int magSize = wp.weaponData[weaponIndex].magazineSize;
                    magazine = System.Math.Min(totalBullets, magSize);
                    reserve = System.Math.Max(0, totalBullets - magSize);
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

            // 머티리얼 캐시 파괴(DestroyMaterialCache)는 GC 부담으로 보류 — 캐시 참조만 해제한다. (260409)
            //DestroyMaterialCache(Instance.m_humanMaterialCache);
            //DestroyMaterialCache(Instance.m_weaponMaterialCache);
            //DestroyMaterialCache(Instance.m_objectMaterialCache);
            Instance.m_sortedRawPointData = null;
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
        /// AI 경로 포인트를 식별번호(param3=원본 p4)로 조회. AIPATH(param0=3) 먼저, 없으면 RAND_AIPATH(param0=8).
        /// 원본 SearchPointdata(pmask=0x08) 대응 — 중복 시 첫 매치. 못 찾으면 false (경로 끝/막다른 길).
        /// </summary>
        public static bool TryGetPathPoint(int id, out RawPointData point)
        {
            point = null;
            var sorted = Instance.m_sortedRawPointData;
            if (sorted == null) return false;

            if (sorted[3].TryGetValue(id, out var aipath) && aipath.Count > 0) { point = aipath[0]; return true; }
            if (sorted[8].TryGetValue(id, out var rand) && rand.Count > 0) { point = rand[0]; return true; }
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
    }
}
