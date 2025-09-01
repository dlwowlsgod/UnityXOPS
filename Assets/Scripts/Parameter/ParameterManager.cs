using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    /// <summary>
    /// XOPS의 모든 Parameter화 가능한 데이터를 저장하는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
    /// </remarks>
    public class ParameterManager : Singleton<ParameterManager>
    {
        /*
         * 아래 직렬화한 리스트는 에디터 상에서 SO를 직접 생성하여 추가할 수 있습니다.
         * 그렇기 때문에 코드로 초기화해선 안됩니다.
         */
        public List<HumanParameter> humanParameters;
        public List<HumanTypeParameter> humanTypeParameters;
        public List<HumanAIParameter> humanAIParameters;
        public List<HumanArmParameter> humanArmParameters;
        public List<HumanLegParameter> humanLegParameters;
        public List<WeaponParameter> weaponParameters;
        public List<BulletParameter> bulletParameters;
        public List<ScopeParameter> scopeParameters;
        public List<ObjectParameter> objectParameters;
        public List<DemoParameter> demoParameters;
        public List<SkyParameter> skyParameters;
        public List<OfficialMissionParameter> officialMissionParameters;
        
        //이 아래의 경우 json이 아닌 파일들을 읽기 때문에 코드로 직접 초기화합니다.
        public List<LegacyAddonMissionParameter> legacyAddonMissionParameters;
        
        /// <summary>
        /// 파라미터 데이터를 전부 불러옵니다.
        /// </summary>
        public void LoadParameters()
        {
            LoadParameter<HumanParameter, HumanParameterList, HumanParameterWrapper>(humanParameters, @"common\parameter\human.json");
            LoadParameter<HumanTypeParameter, HumanTypeParameterList, HumanTypeParameterWrapper>(humanTypeParameters, @"common\parameter\human_type.json");
            LoadParameter<HumanAIParameter, HumanAIParameterList, HumanAIParameterWrapper>(humanAIParameters, @"common\parameter\human_ai.json");
            LoadParameter<HumanArmParameter, HumanArmParameterList, HumanArmParameterWrapper>(humanArmParameters, @"common\parameter\human_arm.json");
            LoadParameter<HumanLegParameter, HumanLegParameterList, HumanLegParameterWrapper>(humanLegParameters, @"common\parameter\human_leg.json");
            LoadParameter<WeaponParameter, WeaponParameterList, WeaponParameterWrapper>(weaponParameters, @"common\parameter\weapon.json");
            LoadParameter<BulletParameter, BulletParameterList, BulletParameterWrapper>(bulletParameters, @"common\parameter\bullet.json");
            LoadParameter<ScopeParameter, ScopeParameterList, ScopeParameterWrapper>(scopeParameters, @"common\parameter\scope.json");;
            LoadParameter<ObjectParameter, ObjectParameterList, ObjectParameterWrapper>(objectParameters, @"common\parameter\object.json");
            LoadParameter<DemoParameter, DemoParameterList, DemoParameterWrapper>(demoParameters, @"common\parameter\demo.json");
            LoadParameter<SkyParameter, SkyParameterList, SkyParameterWrapper>(skyParameters, @"common\parameter\sky.json");
            LoadParameter<OfficialMissionParameter, OfficialMissionParameterList, OfficialMissionParameterWrapper>(officialMissionParameters, @"common\parameter\official_mission.json");
            LoadAddonParameters();
        }

        /// <summary>
        /// 특정 Parameter 데이터를 불러옵니다.
        /// </summary>
        /// <param name="target">Parameter를 저장할 리스트</param>
        /// <param name="path">Parameter JSON의 경로</param>
        /// <typeparam name="TSo"><see cref="ScriptableObject">ScriptableObject</see>를 상속받는 Parameter 데이터</typeparam>
        /// <typeparam name="TList"><see cref="IParameterList{T}">IParameterList</see> 데이터를 담는 리스트</typeparam>
        /// <typeparam name="TData"><see cref="IParameterData">IParameterData</see>를 상속받는 데이터</typeparam>
        private void LoadParameter<TSo, TList, TData>(List<TSo> target, string path) 
            where TSo : ScriptableObject
            where TList : IParameterList<TData>
            where TData : IParameterData
        {
            var finalPath = Path.Combine(Application.streamingAssetsPath, path);
            if (!File.Exists(finalPath))
            {
#if UNITY_EDITOR
                Debug.Log("[ParameterManager] No Json file detected. Use default parameters.");
#endif
                return;
            }

            var json = File.ReadAllText(finalPath);

            var loaded = JsonUtility.FromJson<TList>(json);

            target.Clear();
            foreach (var itemData in loaded.Items)
            {
                var newSo = ScriptableObject.CreateInstance<TSo>();
                newSo.name = itemData.FinalName;
                var itemJson = JsonUtility.ToJson(itemData);
                JsonUtility.FromJsonOverwrite(itemJson, newSo);
                target.Add(newSo);
            }
#if UNITY_EDITOR
            Debug.Log($"[ParameterManager] {target.Count} {typeof(TSo).Name} parameter completely loaded.");
#endif
        }

        /// <summary>
        /// UnityXOPS에서 레거시 .mif 파일을 불러오는 함수입니다.
        /// </summary>
        private void LoadAddonParameters()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "addon");
            if (!Directory.Exists(path))
            {
                return;
            }
            var mifList = Directory.GetFiles(path, "*.mif");
            foreach (var mifPath in mifList)
            {
                var lines = File.ReadAllLines(mifPath, HelperMethod.Instance.DetectEncoding(mifPath));
                var fileName = Path.GetFileNameWithoutExtension(mifPath);
                var newSo = ScriptableObject.CreateInstance<LegacyAddonMissionParameter>();
                newSo.name = fileName;
                newSo.finalName = lines[0];
                newSo.longName = lines[1];
                newSo.bd1Path = lines[2].TrimStart('.').TrimStart('\\');
                newSo.pd1Path = lines[3].TrimStart('.').TrimStart('\\');
                newSo.skyIndex = int.TryParse(lines[4], out var skyIndex) ? skyIndex : 0;
                var flag = int.TryParse(lines[5], out var flagValue) ? flagValue : 0;
                newSo.adjustCollision = flag is 1 or 3;
                newSo.darkScreen = flag is 2 or 3;
                newSo.addonObjectTxtPath = lines[6] == "!" ? null : lines[6].TrimStart('.').TrimStart('\\');
                newSo.imagePath0 = lines[7].TrimStart('.').TrimStart('\\');
                newSo.imagePath1 = lines[8] == "!" ? null : lines[8].TrimStart('.').TrimStart('\\');
                newSo.briefing = string.Join("\n", lines[9..]);
                
                legacyAddonMissionParameters.Add(newSo);
            }
            
            //사전 순으로 정렬
            legacyAddonMissionParameters = legacyAddonMissionParameters.OrderBy(x => x.finalName).ToList();
            
#if UNITY_EDITOR
            Debug.Log($"[ParameterManager] {legacyAddonMissionParameters.Count} legacy addon completely loaded.");
#endif
        }
    }

    /// <summary>
    /// Parameter 데이터를 직렬화하기 위한 Wrapper 클래스의 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// FinalName은 파라미터의 이름을 덮어씌우는게 권장됩니다.
    /// </remarks>
    public interface IParameterData
    {
        string FinalName { get; }
    }
    
    /// <summary>
    /// 직렬화된 Parameter 데이터를 담는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T"><see cref="IParameterData">IParameterData</see>를 상속받는 클래스</typeparam>
    public interface IParameterList<T> where T : IParameterData
    {
        List<T> Items { get; }
    }
}