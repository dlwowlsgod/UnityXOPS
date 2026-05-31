using JJLUtility;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    public partial class MapLoader
    {
        // 추가 사물 콜라이더 변환: OpenXOPS decide × SMALLOBJECT_COLLISIONSCALE(0.13) × 0.1(unity) = decide × 0.013 = Sphere 반지름.
        private const float k_addonDecideToRadius = 0.013f;

        /// <summary>
        /// .mif 미션 전용 추가 사물(ADDON-OBJECT)을 초기화한다. mif line6 이 가리키는 txt(missionAddonObjectPath)를 파싱해
        /// ObjectParameterData 의 예약 슬롯(objectGeneralData.addonObjectIndex)에 모델/콜라이더/내구력/소리/점프를 채운다.
        /// 추가 사물이 없는(경로 비었거나 파싱 실패) 미션이면 슬롯을 빈 상태로 되돌린다 → 미션마다 안전하게 호출 가능.
        /// </summary>
        public static void InitializeAddonObject()
        {
            var op    = DataManager.Instance.ObjectParameterData;
            int index = op.objectGeneralData.addonObjectIndex;
            if (index < 0 || index >= op.objectData.Count) return;

            ObjectData data = op.objectData[index];

            AddonObjectFileData addon = ParseAddonObjectFile(Instance.missionAddonObjectPath);
            if (addon == null)
            {
                // 추가 사물 없음 — 예약 슬롯을 비워 이전 미션 데이터 잔존을 막는다.
                op.objectModelData[data.modelIndex]       = new ObjectModelData    { textures = new List<string>(), modelData = new List<ModelData>() };
                op.objectColliderData[data.colliderIndex] = new ObjectColliderData { shapes   = new List<ColliderShape>() };
                data.hp        = 0f;
                data.jump      = 0;
                data.soundPath = string.Empty;
                return;
            }

            // 모델: 텍스처 1장 + 단일 메시. 변환은 기본값(원점/무회전/스케일 1).
            op.objectModelData[data.modelIndex] = new ObjectModelData
            {
                textures  = new List<string> { addon.texturePath },
                modelData = new List<ModelData>
                {
                    new ModelData
                    {
                        modelPath    = addon.modelPath,
                        position     = Vector3.zero,
                        rotation     = Vector3.zero,
                        scale        = Vector3.one,
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
                        type   = ColliderShapeType.Sphere,
                        center = Vector3.zero,
                        size   = new Vector3(addon.decide * k_addonDecideToRadius, 0f, 0f)
                    }
                }
            };

            // jump 은 raw 정수 그대로 저장 — 파괴 점프 변환은 런타임 SmallObject 의 k_destroy* 상수가 이미 처리한다.
            data.hp        = addon.hp;
            data.jump      = addon.jump;
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
                modelPath   = NormalizeAddonPath(lines[0]),
                texturePath = NormalizeAddonPath(lines[1]),
                decide      = (int.TryParse(lines[2].Trim(), out int d) ? d : 0) & 0x7F,
                hp          = (int.TryParse(lines[3].Trim(), out int h) ? h : 0) & 0x7F,
                soundPath   = NormalizeAddonPath(lines[4]),
                jump        = (int.TryParse(lines[5].Trim(), out int j) ? j : 0) & 0xFF
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
            public int    decide;
            public float  hp;
            public string soundPath;
            public int    jump;
        }
    }
}
