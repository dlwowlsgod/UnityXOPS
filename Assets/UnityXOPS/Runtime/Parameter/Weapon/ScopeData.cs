using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 스코프의 시야 배율, 정확도, 반동 조정, 텍스처, 조준선 정보를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class ScopeData
    {
        public float fovDegrees;
        public FloatRange errorRangeAdjust;
        public FloatRange recoilAimVerticalAdjust;
        public FloatRange recoilAimHorizontalAdjust;
        public string texturePath;
        public float textureAspect;
        public List<ScopeLine> lines;
    }

    /// <summary>
    /// 스코프의 조준선을 정의하는 선분 정보를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class ScopeLine
    {
        public Vector2 start;
        public Vector2 end;
        public Color color;
        public float width;
    }
}
