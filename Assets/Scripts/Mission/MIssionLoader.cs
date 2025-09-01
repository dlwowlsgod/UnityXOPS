using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// XOPS의 임무 데이터를 불러옵니다. 
    /// </summary>
    public class MissionLoader : Singleton<MissionLoader>
    {
        [Header("Briefing")]
        public string finalName;
        public string longName;
        public bool twoImage;
        public string imagePath0;
        public string imagePath1;
        public string briefingContent;

        [Header("Legacy MapData")] 
        public string bd1Path;
        public string pd1Path;
        public int skyIndex;
        public bool adjustCollision;
        public bool darkScreen;
        public string addonObjectTxtPath;

        [Header("Expansion MapData")] 
        public List<string> bd2Path;
        public List<string> pd2Path;
        public string addonHumanPath;
        public string addonWeaponPath;
        public string addonObjectPath;
        
        /// <summary>
        /// 임무를 불러옵니다.
        /// </summary>
        /// <param name="page">메인 메뉴의 미션 페이지(0: 공식, 1~n-1:expansion, n:레거시 에드온</param>
        /// <param name="index">메인 메뉴 페이지의 선택한 임무의 인덱스</param>
        public void LoadMission(int page, int index)
        {
            var officialMissions = ParameterManager.Instance.officialMissionParameters;
            var legacyMissions = ParameterManager.Instance.legacyAddonMissionParameters;

            var hasOfficial = officialMissions is { Count: > 0 };
            var hasLegacy = legacyMissions is { Count: > 0 };

            var pageTypes = new List<string>();
            if (hasOfficial)
            {
                pageTypes.Add("Official");
            }
            //추후 구현
            if (hasLegacy)
            {
                pageTypes.Add("Legacy");
            }
            
            if (page < 0 || page >= pageTypes.Count)
            {
#if UNITY_EDITOR
                Debug.LogError("[MissionLoader] Invalid page index");
#endif
                return;
            }
            
            var selectedPageType = pageTypes[page];
            if (selectedPageType == "Official")
            {
                LoadOfficialMission(index);
            }
            else if (selectedPageType == "Legacy")
            {
                LoadLegacyMission(index);
            }
        }

        public void ClearMission()
        {
            finalName = null;
            longName = null;
            imagePath0 = null;
            imagePath1 = null;
            briefingContent = null;
            
            bd1Path = null;
            pd1Path = null;
            skyIndex = 0;
            adjustCollision = false;
            darkScreen = false;
            addonObjectTxtPath = null;

            bd2Path = null;
            pd2Path = null;
            addonHumanPath = null;
            addonObjectPath = null;
            addonWeaponPath = null;
        }

        private void LoadOfficialMission(int index)
        {
            var mission = ParameterManager.Instance.officialMissionParameters[index];
            if (!mission)
            {
                return;
            }

            finalName = mission.finalName;
            longName = mission.longName;
            var txtPath = Path.Combine(Application.streamingAssetsPath, mission.txtPath);
            var lines = File.ReadAllLines(txtPath, HelperMethod.Instance.DetectEncoding(txtPath));
            imagePath0 = Path.Combine(@"data\briefing", lines[0] + ".bmp");
            if (lines[1] != "!")
            {
                imagePath1 = Path.Combine(@"data\briefing", lines[1] + ".bmp");
                twoImage = true;
            }
            else
            {
                imagePath1 = null;
                twoImage = false;
            }
            briefingContent = string.Join("\n", lines[3..]);

            bd1Path = mission.bd1Path;
            pd1Path = mission.pd1Path;
            skyIndex = int.TryParse(lines[2], out var idx) ? idx : 0;
            adjustCollision = mission.adjustCollision;
            darkScreen = mission.darkScreen;
            addonObjectTxtPath = null;

            bd2Path = null;
            bd2Path = null;
            addonHumanPath = null;
            addonWeaponPath = null;
            addonObjectPath = null;
        }

        private void LoadExpansionMission(int index)
        {
            
        }

        private void LoadLegacyMission(int index)
        {
            var mission = ParameterManager.Instance.legacyAddonMissionParameters[index];
            if (!mission)
            {
                return;
            }
            
            finalName = mission.finalName;
            longName = mission.longName;
            imagePath0 = mission.imagePath0;
            if (mission.imagePath1 != null)
            {
                imagePath1 = mission.imagePath1;
                twoImage = true;
            }
            else
            {
                imagePath1 = null;
                twoImage = false;
            }
            briefingContent = mission.briefing;
            
            bd1Path = mission.bd1Path;
            pd1Path = mission.pd1Path;
            skyIndex = mission.skyIndex;
            adjustCollision = mission.adjustCollision;
            darkScreen = mission.darkScreen;
            addonObjectTxtPath = mission.addonObjectTxtPath;

            bd2Path = null;
            pd2Path = null;
            addonHumanPath = null;
            addonWeaponPath = null;
            addonObjectPath = null;
        }
    }
}