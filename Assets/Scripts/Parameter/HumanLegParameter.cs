using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Human Leg 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "HumanLegParameter", menuName = "UnityXOPS/HumanLegParameter")]
    public class HumanLegParameter : ScriptableObject
    {
        [Tooltip("다리 그룹의 이름입니다.")]
        public string finalName;
        [Tooltip("일반 다리 메시의 경로입니다.")] 
        public string idleLegPath;
        [Tooltip("걷는 다리 메시의 경로입니다.")] 
        public List<string> walkLegPath;
        [Tooltip("뛰는 다리 메시의 경로입니다.")] 
        public List<string> runLegPath;
    }

    /// <summary>
    /// <see cref="HumanLegParameter">HumanLegParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanLegParameterWrapper : IParameterData
    {
        public string finalName;
        public string idleLegPath;
        public List<string> walkLegPath;
        public List<string> runLegPath;
        
        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="HumanLegParameterWrapper">HumanLegParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanLegParameterList : IParameterList<HumanLegParameterWrapper>
    {
        public List<HumanLegParameterWrapper> items;
        public List<HumanLegParameterWrapper> Items => items;
    }
}