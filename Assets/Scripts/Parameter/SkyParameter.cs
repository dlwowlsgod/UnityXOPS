using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 하늘 데이터를 담는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "SkyParameter", menuName = "UnityXOPS/SkyParameter")]
    public class SkyParameter : ScriptableObject
    {
        [Tooltip("Sky의 이름입니다.")]
        public string finalName;
        [Tooltip("Sky의 스카이박스 텍스쳐 경로입니다.")]
        public string skyTexturePath;
        [Tooltip("Sky의 빌보드(도시 등을 표현하는) 텍스쳐 경로입니다.")]
        public string billboardTexturePath;
        [Tooltip("Sky의 구름 텍스쳐 경로입니다.")]
        public string cloudTexturePath;
        [Tooltip("Sky의 하늘에 떠 있는 광원체(태양,달 등) 텍스쳐 경로입니다.")]
        public string lightTexturePath;
        [Tooltip("이 Sky를 사용할 경우 광원을 추가할지 여부입니다.")]
        public bool light;
        [Tooltip("광원이 켜진 경우 광원의 세기입니다.")]
        public float lightStrength;
        [Tooltip("광원이 켜진 경우 광원의 색상입니다.")]
        public Color lightColor;
        [Tooltip("광원이 켜진 경우 광원의 방향입니다. lightTexture는 이 방향에 영향을 받습니다.")]
        public Vector2 lightDirection;
    }
    
    /// <summary>
    /// <see cref="SkyParameter">SkyParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class SkyParameterWrapper : IParameterData
    {
        public string finalName;
        public string skyTexturePath;
        public string billboardTexturePath;
        public string cloudTexturePath;
        public string lightTexturePath;
        public bool light;
        public float lightStrength;
        public Color lightColor;
        public Vector2 lightDirection;
        
        public string FinalName => finalName;
    }
    
    /// <summary>
    /// <see cref="SkyParameterWrapper">SkyParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class SkyParameterList : IParameterList<SkyParameterWrapper>
    {
        public List<SkyParameterWrapper> items;
        public List<SkyParameterWrapper> Items => items;
    }
}