using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// HumanAI의 Parameter 정보입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "HumanAIParameter", menuName = "UnityXOPS/Parameter/Human/HumanAIParameter")]
    public class HumanAIParameter : ScriptableObject
    {
        public string realName;
        public MinMaxData lookDistance;
        public ViewAngleData closedViewAngle;
        public ViewAngleData rangedViewAngle;
        public float aimingPointHeight;
        public float aimingSphereRadius;
        public float aimingTime;
        public float properAimingTime;
        public MinMaxData accuracyAdjust;
        public float fireChance;
    }

    /// <summary>
    /// HumanAI Parameter의 Wrapper입니다.
    /// </summary>
    [Serializable]
    public class HumanAIParameterWrapper : IParameterData
    {
        public string realName;
        
        public string Name => realName;
    }
    
    /// <summary>
    /// HumanAI Parameter의 Wrapper를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanAIParameterList : IParameterList<HumanAIParameterWrapper>
    {
        public List<HumanAIParameterWrapper> items;
        
        public List<HumanAIParameterWrapper> Items => items;
    }
}