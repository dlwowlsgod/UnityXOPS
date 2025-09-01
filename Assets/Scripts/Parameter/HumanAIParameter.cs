using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Human AI 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "HumanAIParameter", menuName = "UnityXOPS/HumanAIParameter")]
    public class HumanAIParameter : ScriptableObject
    {
        [Tooltip("AI 값의 이름입니다.")]
        public string finalName;
        [Tooltip("AI의 발사 빈도입니다.")]
        public Vector2Int fireFrequency;
        [Tooltip("AI의 수색 빈도입니다")]
        public Vector2Int searchFrequency;
        [Tooltip("일반 상태에서의 수색 변수값입니다.")]
        public SearchView normalSearchView;
        [Tooltip("경계, 전투 상태에서의 수색 변수값입니다.")]
        public SearchView cautionSearchView;
        [Tooltip("조준 보정과 조준 오차 값입니다.")]
        public AIAiming aimFire;
    }

    /// <summary>
    /// <see cref="HumanAIParameter">HumanAIParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanAIParameterWrapper : IParameterData
    {
        public string finalName;
        public Vector2Int fireFrequency;
        public Vector2Int searchFrequency;
        public SearchView normalSearchView;
        public SearchView cautionSearchView;
        public AIAiming aimFire;
        
        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="HumanAIParameterWrapper">HumanAIParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanAIParameterList : IParameterList<HumanAIParameterWrapper>
    {
        public List<HumanAIParameterWrapper> items;
        public List<HumanAIParameterWrapper> Items => items;
    }

    [Serializable]
    public class SearchView
    {
        [Tooltip("상하 시야각입니다.")]
        public Vector2 horizontal;
        [Tooltip("좌우 시야각입니다.")]
        public Vector2 vertical;
        [Tooltip("시야 거리입니다.")]
        public float distance;
    }

    [Serializable]
    public class AIAiming
    {  
        [Tooltip("적을 조준하는 시간입니다.")]
        public float aimTime;
        [Tooltip("적을 향한 조준 오차값입니다.")]
        public float aimError;
        [Tooltip("총기 정확도 오차값입니다.")]
        public float accuracyError;
    }
}