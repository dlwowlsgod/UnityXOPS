using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Human Arm 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "HumanArmParameter", menuName = "UnityXOPS/HumanArmParameter")]
    public class HumanArmParameter : ScriptableObject
    {
        [Tooltip("팔 그룹의 이름입니다.")]
        public string finalName;
        [Tooltip("팔 메시의 경로입니다.")]
        public List<string> armMeshPaths;
    }

    /// <summary>
    /// <see cref="HumanArmParameter">HumanArmParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanArmParameterWrapper : IParameterData
    {
        public string finalName;
        public List<string> armMeshPaths;
        
        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="HumanArmParameterWrapper">HumanArmParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanArmParameterList : IParameterList<HumanArmParameterWrapper>
    {
        public List<HumanArmParameterWrapper> items;
        public List<HumanArmParameterWrapper> Items => items;
    }
}