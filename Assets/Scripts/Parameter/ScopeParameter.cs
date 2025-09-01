using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Scope 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "ScopeParameter", menuName = "UnityXOPS/ScopeParameter")]
    public class ScopeParameter : ScriptableObject
    {
        [Tooltip("Scope의 이름입니다.")]
        public string finalName;
        [Tooltip("Scope의 움직이지 않는 조준선 데이터입니다.")]
        public List<LineDraw> staticCrosshair;
        [Tooltip("Scope의 움직이는 조준선 데이터입니다.")]
        public List<DynamicLineDraw> dynamicCrosshair;
        [Tooltip("Scope의 줌 기능 유무입니다.")]
        public bool zoom;
        [Tooltip("Scope의 줌 시 FOV값입니다.")]
        public float zoomFov;
        [Tooltip("Scope의 줌 시 텍스쳐 경로입니다.")]
        public string zoomTexturePath;
        [Tooltip("Scope의 줌 시 움직이지 않는 조준선 데이터입니다.")]
        public List<LineDraw> zoomStaticCrosshair;
        [Tooltip("Scope의 줌 시 움직이는 조준선 데이터입니다.")]
        public List<DynamicLineDraw> zoomDynamicCrosshair;
    }

    /// <summary>
    /// <see cref="ScopeParameter">ScopeParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class ScopeParameterWrapper : IParameterData
    {
        public string finalName;
        public List<LineDraw> staticCrosshair;
        public List<DynamicLineDraw> dynamicCrosshair;
        public bool zoom;
        public float zoomFov;
        public string zoomTexturePath;
        public List<LineDraw> zoomStaticCrosshair;
        public List<DynamicLineDraw> zoomDynamicCrosshair;
        
        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="ScopeParameterWrapper">ScopeParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class ScopeParameterList : IParameterList<ScopeParameterWrapper>
    {
        public List<ScopeParameterWrapper> items;
        public List<ScopeParameterWrapper> Items => items;
    }

    [Serializable]
    public class LineDraw
    {
        [Tooltip("시작점입니다.")]
        public Vector2 startPos;
        [Tooltip("끝점입니다.")]
        public Vector2 endPos;
        [Tooltip("선 색상입니다.")]
        public Color color;
        [Tooltip("선 두께입니다.")]
        public float width;
    }

    [Serializable]
    public class DynamicLineDraw : LineDraw
    {
        [Tooltip("x에 가중치를 곱합니다.")]
        public float horizontalDynamic;
        [Tooltip("y에 가중치를 곱합니다.")]
        public float verticalDynamic;
    }
}