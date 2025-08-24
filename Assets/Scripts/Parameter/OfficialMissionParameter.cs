using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 공식 임무를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "OfficialMissionParameter", menuName = "UnityXOPS/OfficialMissionParameter")]
    public class OfficialMissionParameter : ScriptableObject
    {
        [Tooltip("미션의 요약된 이름(메인메뉴에 사용되는 이름)입니다.")]
        public string finalName;
        [Tooltip("미션의 이름(브리핑에 사용되는 이름)입니다.")]
        public string longName;
        [Tooltip("미션의 bd1 파일 경로입니다.")]
        public string bd1Path;
        [Tooltip("미션의 pd1 파일 경로입니다.")]
        public string pd1Path;
        [Tooltip("미션의 txt 파일 경로입니다.")]
        public string txtPath;
        [Tooltip("캐릭터와 블록의 충돌 판정을 더 크게 할 것인지 여부입니다.")]
        public bool adjustCollision;
        [Tooltip("화면을 더 어둡게 할 것인지 여부입니다.")]
        public bool darkScreen;
    }

    /// <summary>
    /// <see cref="OfficialMissionParameter">OfficialMissionParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class OfficialMissionParameterWrapper : IParameterData
    {
        public string finalName;
        public string longName;
        public string bd1Path;
        public string pd1Path;
        public string txtPath;
        public bool adjustCollision;
        public bool darkScreen;
        
        public string FinalName => finalName;
    }
    
    /// <summary>
    /// <see cref="OfficialMissionParameterWrapper">OfficialMissionParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class OfficialMissionParameterList : IParameterList<OfficialMissionParameterWrapper>
    {
        public List<OfficialMissionParameterWrapper> items;
        public List<OfficialMissionParameterWrapper> Items => items;
    }
}