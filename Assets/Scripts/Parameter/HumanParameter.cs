using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Human 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "HumanParameter", menuName = "UnityXOPS/HumanParameter")]
    public class HumanParameter : ScriptableObject
    {
        [Tooltip("Human의 이름입니다.")]
        public string finalName;
        [Tooltip("Human의 체력입니다.")]
        public int hp;
        [Tooltip("Human의 보조 무기입니다.")]
        public int weaponIndex0;
        [Tooltip("Human의 주 무기입니다.")]
        public int weaponIndex1;
        [Tooltip("Human의 정적인 모델과 각 모델의 텍스쳐 경로들입니다.")]
        public List<HumanModel> staticModels;
        [Tooltip("Human의 팔 모델입니다.")]
        public int armModel;
        [Tooltip("Human의 다리 모델입니다.")]
        public int legModel;
        [Tooltip("Human의 AI입니다.")]
        public int aiIndex;
        [Tooltip("Human의 종류입니다.")]
        public int typeIndex;
    }
    
    /// <summary>
    /// <see cref="HumanParameter">HumanParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanParameterWrapper : IParameterData
    {
        public string finalName;
        public int hp;
        public int weaponIndex0;
        public int weaponIndex1;
        public List<HumanModel> staticModels;
        public int armModel;
        public int legModel;
        public int aiIndex;
        public int typeIndex;
        
        public string FinalName => finalName;
    }
    
    /// <summary>
    /// <see cref="HumanParameterWrapper">HumanParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanParameterList : IParameterList<HumanParameterWrapper>
    {
        public List<HumanParameterWrapper> items;
        public List<HumanParameterWrapper> Items => items;
    }
    
    [Serializable]
    public class HumanModel
    {
        [Tooltip("메시 경로입니다.")]
        public string meshPath;
        [Tooltip("텍스쳐 경로입니다.")]
        public string texturePath;
    }
}