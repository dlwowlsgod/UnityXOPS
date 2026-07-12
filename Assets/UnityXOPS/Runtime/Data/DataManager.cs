using UnityEngine;
using JJLUtility;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 데이터(스카이, 미션)를 JSON에서 로드하고 전역 접근을 제공하는 싱글톤 매니저.
    /// </summary>
    public partial class DataManager : SingletonBehavior<DataManager>
    {
        [SerializeField]
        private HumanParameterData humanParameterData;
        public HumanParameterData HumanParameterData => humanParameterData;

        [SerializeField]
        private WeaponParameterData weaponParameterData;
        public WeaponParameterData WeaponParameterData => weaponParameterData;

        [SerializeField]
        private ObjectParameterData objectParameterData;
        public ObjectParameterData ObjectParameterData => objectParameterData;

        [SerializeField]
        private EffectParameterData effectParameterData;
        public EffectParameterData EffectParameterData => effectParameterData;

        [SerializeField]
        private SkyData skyData;
        public SkyData SkyData => skyData;

        [SerializeField]
        private MissionData missionData;
        public MissionData MissionData => missionData;

        [SerializeField]
        private GlobalData globalData;
        public GlobalData GlobalData => globalData;

        private void Start()
        {
            LoadHumanParameterData();
            LoadWeaponParameterData();
            LoadObjectParameterData();
            LoadEffectParameterData();
            LoadSkyData();
            LoadMissionData();
            LoadGlobalData();
        }

        private void LoadHumanParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_humanParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            humanParameterData = JsonUtility.FromJson<HumanParameterData>(json);
        }

        private void LoadWeaponParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_weaponParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            weaponParameterData = JsonUtility.FromJson<WeaponParameterData>(json);
        }

        private void LoadObjectParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_objectParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            objectParameterData = JsonUtility.FromJson<ObjectParameterData>(json);
        }

        private void LoadEffectParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_effectParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            effectParameterData = JsonUtility.FromJson<EffectParameterData>(json);
        }

        private void LoadSkyData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_skyDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            skyData = JsonUtility.FromJson<SkyData>(json);
        }

        private void LoadMissionData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_missionDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            missionData = JsonUtility.FromJson<MissionData>(json);

            LoadAddonMissions();
        }

        /// <summary>
        /// 어드온 미션을 페이지 단위로 로드한다. 페이지 0은 항상 기본 OpenXOPS 어드온 폴더("addon") 폴백(이름 없음).
        /// 페이지 1~은 addon.json의 addonPath 목록(유저 추가 경로)으로, 파일이 없거나 목록이 없으면 폴백만 남는다.
        /// 경로는 항상 StreamingAssets 기준이며 SafePath로 결합해 상위 디렉터리 탈출을 차단한다.
        /// 경로가 감지되면 폴더가 없거나 .mif가 없어도 빈 페이지로 추가한다. 페이지 이름은 같은 인덱스의 addonName(없으면 빈 문자열).
        /// </summary>
        private void LoadAddonMissions()
        {
            missionData.addonMissions = new List<List<AddonMissionData>>();
            missionData.addonPageNames = new List<string>();

            // 페이지 0: 항상 존재하는 폴백 = 기본 OpenXOPS 어드온 폴더. 페이지 이름 없음.
            string fallbackDirectory = Path.Combine(Application.streamingAssetsPath, k_fallbackAddonFolder);
            missionData.addonMissions.Add(ScanAddonMifs(fallbackDirectory));
            missionData.addonPageNames.Add(string.Empty);

            // 페이지 1~: addon.json이 지정한 유저 추가 경로. 파일이 없거나 경로 목록이 없으면 유저 페이지 없음(폴백만).
            string addonJsonPath = Path.Combine(Application.streamingAssetsPath, k_addonPathDataPath);
            if (!File.Exists(addonJsonPath))
            {
                return;
            }

            AddonPathData addonPathData = JsonUtility.FromJson<AddonPathData>(EncodingHelper.ReadAllText(addonJsonPath));
            if (addonPathData?.addonPath == null)
            {
                return;
            }

            for (int i = 0; i < addonPathData.addonPath.Count; i++)
            {
                string packDirectory = SafePath.Combine(Application.streamingAssetsPath, addonPathData.addonPath[i]);
                if (packDirectory == null)
                {
                    continue;   // 상위 디렉터리 탈출 시도 무시
                }

                // 경로가 감지되면 폴더가 없거나 .mif가 없어도 빈 페이지로 추가한다.
                missionData.addonMissions.Add(ScanAddonMifs(packDirectory));
                string pageName = (addonPathData.addonName != null && i < addonPathData.addonName.Count)
                    ? addonPathData.addonName[i]
                    : string.Empty;
                missionData.addonPageNames.Add(pageName ?? string.Empty);
            }
        }

        /// <summary>
        /// 지정한 디렉터리에서 .mif 파일을 스캔해 어드온 미션 목록을 만든다. 파일 순서는 파일명 자연 정렬(숫자 값 비교).
        /// </summary>
        /// <param name="directory">스캔할 맵 팩 디렉터리(전체 경로).</param>
        /// <returns>해당 디렉터리의 어드온 미션 목록. 디렉터리가 없으면 빈 목록.</returns>
        private List<AddonMissionData> ScanAddonMifs(string directory)
        {
            var page = new List<AddonMissionData>();
            if (!Directory.Exists(directory))
            {
                return page;
            }

            // Directory.GetFiles는 순서를 보장하지 않으므로 파일명 기준으로 자연 정렬한다(gates2 < gates10처럼 숫자를 값으로 비교).
            string[] mifPaths = Directory.GetFiles(directory, "*.mif");
            System.Array.Sort(mifPaths, (a, b) => CompareNatural(Path.GetFileName(a), Path.GetFileName(b)));
            foreach (var path in mifPaths)
            {
                var addonData = new AddonMissionData();
                addonData.mifPath = path;
                var lines = EncodingHelper.ReadAllLines(addonData.mifPath);
                var name = lines.Length > 0 ? lines[0] : string.Empty;
                addonData.name = string.IsNullOrEmpty(name) ? string.Empty : name;
                page.Add(addonData);
            }
            return page;
        }

        /// <summary>
        /// 두 문자열을 자연 정렬 순서로 비교한다. 숫자 구간은 값으로 비교해 gates2가 gates10보다 앞서게 하고,
        /// 그 외 문자는 대소문자 무시 비교한다. 선행 0(zero-padding)은 값에 영향을 주지 않는다.
        /// </summary>
        /// <param name="a">비교 대상 문자열 A.</param>
        /// <param name="b">비교 대상 문자열 B.</param>
        /// <returns>A가 앞서면 음수, 같으면 0, 뒤면 양수.</returns>
        private static int CompareNatural(string a, string b)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    int startA = i, startB = j;
                    while (i < a.Length && char.IsDigit(a[i])) i++;
                    while (j < b.Length && char.IsDigit(b[j])) j++;

                    // 선행 0 제거 후 자릿수 → 값 비교. 값이 같으면 자릿수 적은(패딩 없는) 쪽을 앞에 둔다.
                    string na = a.Substring(startA, i - startA).TrimStart('0');
                    string nb = b.Substring(startB, j - startB).TrimStart('0');
                    if (na.Length != nb.Length) return na.Length - nb.Length;
                    int digitCompare = string.CompareOrdinal(na, nb);
                    if (digitCompare != 0) return digitCompare;
                    if ((i - startA) != (j - startB)) return (i - startA) - (j - startB);
                }
                else
                {
                    int charCompare = char.ToLowerInvariant(a[i]).CompareTo(char.ToLowerInvariant(b[j]));
                    if (charCompare != 0) return charCompare;
                    i++;
                    j++;
                }
            }
            return (a.Length - i) - (b.Length - j);
        }

        /// <summary>
        /// 전역 데이터를 JSON에서 로드한다. 파일이 없으면 Player Settings 값으로 새 JSON 파일을 생성한 뒤 로드한다.
        /// </summary>
        private void LoadGlobalData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_globalDataPath);
            if (File.Exists(fullPath))
            {
                string json = EncodingHelper.ReadAllText(fullPath);
                globalData = JsonUtility.FromJson<GlobalData>(json);
                return;
            }

            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            globalData = BuildGlobalDataFromPlayerSettings();
            File.WriteAllText(fullPath, JsonUtility.ToJson(globalData, true));
        }

        /// <summary>
        /// Player Settings(회사명·제품명·버전)로 기본 전역 데이터를 만든다. 라이선스 항목은 비워 둔다.
        /// </summary>
        /// <returns>Player Settings 값이 채워진 GlobalData.</returns>
        private static GlobalData BuildGlobalDataFromPlayerSettings()
        {
            var data = new GlobalData();
            data.productName = Application.productName;
            data.companyName = Application.companyName;
            data.licenseType = string.Empty;
            data.licenseName = string.Empty;
            data.licenseLines = new string[0];

            // 버전은 '.'로 나눠 앞의 둘을 major/minor로 쓰고, 남는 조각은 다시 '.'로 이어 patch로 둔다(예: "1.2.3.4" → patch "3.4").
            string version = Application.version;
            string[] parts = string.IsNullOrEmpty(version) ? new string[0] : version.Split('.');
            data.versionMajor = parts.Length > 0 ? parts[0] : "0";
            data.versionMinor = parts.Length > 1 ? parts[1] : "0";
            data.versionPatch = parts.Length > 2 ? string.Join(".", parts, 2, parts.Length - 2) : "0";
            return data;
        }
    }
}