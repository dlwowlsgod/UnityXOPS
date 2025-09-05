using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Human의 Parameter 정보입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "HumanParameter", menuName = "UnityXOPS/Parameter/Human/HumanParameter")]
    public class HumanParameter : ScriptableObject
    {
        public string realName;
        public int hp;
        public int weapon0;
        public int weapon1;
        public int ai;
        public int type;
        public List<VisualData> visualData;
    }

    /// <summary>
    /// Human Parameter의 Wrapper입니다.
    /// </summary>
    [Serializable]
    public class HumanParameterWrapper : IParameterData
    {
        public string realName;
        public int hp;
        public int weapon0;
        public int weapon1;
        public int ai;
        public int type;
        public List<VisualData> visualData;
        
        public string Name => realName;
    }
    
    /// <summary>
    /// Human Parameter의 Wrapper를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanParameterList : IParameterList<HumanParameterWrapper>
    {
        public List<HumanParameterWrapper> items;
        
        public List<HumanParameterWrapper> Items => items;
    }
}