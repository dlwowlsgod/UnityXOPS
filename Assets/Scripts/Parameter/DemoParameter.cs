using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS에서 메인 메뉴의 Demo(메인 메뉴에서의 맵 쇼케이스)를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "DemoParameter", menuName = "UnityXOPS/DemoParameter")]
    public class DemoParameter : ScriptableObject
    {
        [Tooltip("Demo의 이름입니다.")]
        public string finalName;
        [Tooltip("bd1 파일의 경로입니다.")]
        public string bd1Path;
        [Tooltip("pd1 파일의 경로입니다.")]
        public string pd1Path;
        [Tooltip("Demo의 Sky Index입니다.")]
        public int skyIndex;
    }
    
    /// <summary>
    /// <see cref="DemoParameter">DemoParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class DemoParameterWrapper : IParameterData
    {
        public string finalName;
        public string bd1Path;
        public string pd1Path;
        public int skyIndex;
        
        public string FinalName => finalName;
    }
    
    /// <summary>
    /// <see cref="DemoParameterWrapper">DemoParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class DemoParameterList : IParameterList<DemoParameterWrapper>
    {
        public List<DemoParameterWrapper> items;
        public List<DemoParameterWrapper> Items => items;
    }
}