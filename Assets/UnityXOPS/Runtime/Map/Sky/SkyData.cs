using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 스카이박스 메시 경로와 텍스처 경로 목록을 담는 데이터 클래스.
    /// </summary>
    [Serializable]
    public class SkyData
    {
        public string skyMeshPath;
        public List<string> skyTexturePath;

        // sky 번호별 fog 색 (skyTexturePath 와 동일 인덱스). 원본 SetFog skycolor switch (d3dgraphics-directx.cpp:1287-1292).
        public List<Color32> skyColor;

        // 메인 카메라 클리핑 면 (원본 CLIPPINGPLANE_NEAR/FAR ×0.1). CameraClippingApplier 가 카메라에 적용.
        public float nearClippingPlane;
        public float farClippingPlane;

        // 안개 적용 여부 (false 면 fog 미적용). fogStart~fogEnd = Linear 안개 거리 구간.
        public bool  fog;
        public float fogStart;
        public float fogEnd;
    }
}
