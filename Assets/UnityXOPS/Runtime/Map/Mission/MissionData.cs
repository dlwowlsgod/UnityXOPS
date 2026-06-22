using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// 데모, 공식 미션, 어드온 미션 목록을 담는 최상위 미션 데이터 클래스.
    /// </summary>
    [Serializable]
    public class MissionData
    {
        public List<DemoData> demoData;
        public List<OfficialMissionData> officialMissions;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public List<AddonMissionData> addonMissions;
    }

    public partial class MapLoader
    {
        // 추가 사물 콜라이더 변환: OpenXOPS decide × SMALLOBJECT_COLLISIONSCALE(0.13) × 0.1(unity) = decide × 0.013 = Sphere 반지름.
        private const float k_addonDecideToRadius = 0.013f;

        [SerializeField]
        private string missionName, missionFullname, missionBD1Path, missionPD1Path,
            missionAddonObjectPath, missionImage0, missionImage1, missionBriefing;
        [SerializeField]
        private int skyIndex;
        [SerializeField]
        private bool adjustCollision, darkScreen;

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

        // 플레이어 미션 통계(플레이타임/발사/명중/킬/헤드샷). Briefing→Maingame→Result 씬 전환(맵 1개 재사용)을 넘어 유지.
        // 맵 로드(LoadPointData)에서만 리셋 — 씬 전환만으로는 유지되므로 Result 까지 값이 살아있다.
        private readonly MissionStats m_stats = new MissionStats();
        public static MissionStats Stats => Instance.m_stats;

        /// <summary>
        /// 플레이어 발사 1회 기록(+1). 산탄(펠릿 수 무관)/수류탄 제외는 호출자(Weapon.Shoot)가 게이트. 원본 gamemain.cpp:2240.
        /// shooter==Player 일 때만 누적 — 원본의 "플레이어 슬롯만 픽업" 구조를 기록 시점 게이트로 등가 처리.
        /// </summary>
        public static void RecordFire(Human shooter)
        {
            if (shooter == null || shooter != Instance.player) return;
            Instance.m_stats.Fire += 1;
        }

        /// <summary>
        /// 플레이어 탄 명중 기록 — onTargetWeight 가산(산탄 가중), 머리 명중이면 헤드샷 +1. 원본 objectmanager.cpp:975-976.
        /// 수류탄은 명중/헤드샷 미집계(원본 비대칭) — Bullet.Explode 가 호출하지 않으므로 자연히 제외.
        /// </summary>
        public static void RecordHit(Human shooter, bool headshot, float onTargetWeight)
        {
            if (shooter == null || shooter != Instance.player) return;
            Instance.m_stats.OnTarget += onTargetWeight;
            if (headshot) Instance.m_stats.Headshot += 1;
        }

        /// <summary>
        /// 플레이어 킬 기록(+1) — 총알/수류탄 공통. "이전 HP>0 && 현재 HP<=0" 판정은 호출자 책임. 원본 objectmanager.cpp:978,1115.
        /// </summary>
        public static void RecordKill(Human killer)
        {
            if (killer == null || killer != Instance.player) return;
            Instance.m_stats.Kill += 1;
        }

        /// <summary>미션 경과 시간 누적(초). 원본 framecnt(gamemain.cpp:2705)의 실시간 대응 — 진행 중에만 호출(MaingameScene).</summary>
        public static void AddPlayTime(float deltaSeconds)
        {
            Instance.m_stats.PlayTime += deltaSeconds;
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

        /// <summary>
        /// .mif 미션 전용 추가 사물(ADDON-OBJECT)을 초기화한다. mif line6 이 가리키는 txt(missionAddonObjectPath)를 파싱해
        /// ObjectParameterData 의 예약 슬롯(objectGeneralData.addonObjectIndex)에 모델/콜라이더/내구력/소리/점프를 채운다.
        /// 추가 사물이 없는(경로 비었거나 파싱 실패) 미션이면 슬롯을 빈 상태로 되돌린다 → 미션마다 안전하게 호출 가능.
        /// </summary>
        public static void InitializeAddonObject()
        {
            var op = DataManager.Instance.ObjectParameterData;
            int index = op.objectGeneralData.addonObjectIndex;
            if (index < 0 || index >= op.objectData.Count) return;

            ObjectData data = op.objectData[index];

            AddonObjectFileData addon = ParseAddonObjectFile(Instance.missionAddonObjectPath);
            if (addon == null)
            {
                // 추가 사물 없음 — 예약 슬롯을 비워 이전 미션 데이터 잔존을 막는다.
                op.objectModelData[data.modelIndex] = new ObjectModelData { textures = new List<string>(), modelData = new List<ModelData>() };
                op.objectColliderData[data.colliderIndex] = new ObjectColliderData { shapes = new List<ColliderShape>() };
                data.hp = 0f;
                data.jump = 0;
                data.soundPath = string.Empty;
                return;
            }

            // 모델: 텍스처 1장 + 단일 메시. 변환은 기본값(원점/무회전/스케일 1).
            op.objectModelData[data.modelIndex] = new ObjectModelData
            {
                textures = new List<string> { addon.texturePath },
                modelData = new List<ModelData>
                {
                    new ModelData
                    {
                        modelPath = addon.modelPath,
                        position = Vector3.zero,
                        rotation = Vector3.zero,
                        scale = Vector3.one,
                        textureIndex = 0
                    }
                }
            };

            // 콜라이더: decide → Sphere 반지름. 기존 소물과 동일하게 단일 구체.
            op.objectColliderData[data.colliderIndex] = new ObjectColliderData
            {
                shapes = new List<ColliderShape>
                {
                    new ColliderShape
                    {
                        type = ColliderShapeType.Sphere,
                        center = Vector3.zero,
                        size = new Vector3(addon.decide * k_addonDecideToRadius, 0f, 0f)
                    }
                }
            };

            // jump 은 raw 정수 그대로 저장 — 파괴 점프 변환은 런타임 SmallObject 의 k_destroy* 상수가 이미 처리한다.
            data.hp = addon.hp;
            data.jump = addon.jump;
            data.soundPath = addon.soundPath;
        }

        /// <summary>
        /// 추가 사물 정보 txt(6줄)를 파싱한다. LoadMissionData 의 mif 파싱과 동일하게 File.ReadAllLines + EncodingHelper 사용.
        /// line0 모델경로 / line1 텍스처경로 / line2 decide(콜라이더 크기) / line3 hp / line4 피격음 경로 / line5 jump(파괴 점프력).
        /// MAX_ADDSMALLOBJECT == 1 이라 항목 사이 더미 줄은 없다. 경로는 OpenXOPS 형식(".\data\..")을 상대경로("data/..")로 정규화한다.
        /// </summary>
        /// <returns>파싱 성공 시 데이터, 경로가 없거나 6줄 미만이면 null.</returns>
        private static AddonObjectFileData ParseAddonObjectFile(string txtPath)
        {
            if (string.IsNullOrEmpty(txtPath) || !File.Exists(txtPath)) return null;

            string[] lines = File.ReadAllLines(txtPath, EncodingHelper.GetEncoding());
            if (lines.Length < 6) return null;

            // 원본 ENABLE_ADDOBJ_PARAM8BIT(main.h:73, 기본 ON) — 원조 XOPS 호환을 위해 char 8비트 마스킹.
            // 클램프가 아닌 비트 AND 라 범위 초과값은 wrap 된다 (decide/hp 0~127, jump 0~255).
            return new AddonObjectFileData
            {
                modelPath = NormalizeAddonPath(lines[0]),
                texturePath = NormalizeAddonPath(lines[1]),
                decide = (int.TryParse(lines[2].Trim(), out int d) ? d : 0) & 0x7F,
                hp = (int.TryParse(lines[3].Trim(), out int h) ? h : 0) & 0x7F,
                soundPath = NormalizeAddonPath(lines[4]),
                jump = (int.TryParse(lines[5].Trim(), out int j) ? j : 0) & 0xFF
            };
        }

        /// <summary>
        /// OpenXOPS 경로(".\data\.." 또는 "./data/..")를 streamingAssets 상대경로("data/..")로 정규화. "!"/빈 줄은 빈 문자열.
        /// </summary>
        private static string NormalizeAddonPath(string raw)
        {
            string s = raw.Trim();
            if (string.IsNullOrEmpty(s) || s == "!") return string.Empty;
            return s.TrimStart('.', '\\', '/').Replace('\\', '/');
        }

        /// <summary>추가 사물 txt 1개 분량의 파싱 결과.</summary>
        private class AddonObjectFileData
        {
            public string modelPath;
            public string texturePath;
            public int decide;
            public float hp;
            public string soundPath;
            public int jump;
        }
    }
}
