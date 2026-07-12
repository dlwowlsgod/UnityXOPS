using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 씬 전체 구성 데이터를 담는 직렬화 가능 클래스.
    /// </summary>
    [Serializable]
    public class OpeningData
    {
        public string openingBD1Path;
        public string openingPD1Path;
        public int openingSkyIndex;
        public float letterBoxHeight;
        public OpeningFadeData openingFadeData;
        public List<OpeningTextData> openingTextData;
        public OpeningCameraData openingCameraData;
    }

    /// <summary>
    /// 오프닝 페이드 인/아웃 타이밍 데이터.
    /// </summary>
    [Serializable]
    public class OpeningFadeData
    {
        public float fadeInStart;
        public float fadeInEnd;
        public float fadeOutStart;
        public float fadeOutEnd;
    }

    /// <summary>
    /// 오프닝 중 표시되는 개별 텍스트 항목의 레이아웃 및 페이드 타이밍 데이터.
    /// </summary>
    [Serializable]
    public class OpeningTextData
    {
        public string text;
        public Vector2 position;
        public Vector2 size;
        public Color color;
        public TextAnchor alignment;
        public float spacing;
        public float fadeInStart;
        public float fadeInEnd;
        public float fadeOutStart;
        public float fadeOutEnd;
    }

    /// <summary>
    /// 오프닝 카메라의 초기 위치/회전과 이동/회전 애니메이션 데이터.
    /// </summary>
    [Serializable]
    public class OpeningCameraData
    {
        public Vector3 initialPosition;   
        public Vector3 initialEuler;      
        public OpeningCameraAnimation posAnim;
        public OpeningCameraAnimation rotAnim;
    }

    /// <summary>
    /// 가속·등속·감쇠 구간으로 구성된 카메라 단일 축 애니메이션 파라미터.
    /// </summary>
    [Serializable]
    public class OpeningCameraAnimation
    {
        public float accelStart;      
        public float accelEnd;        
        public float constantEnd;     
        public Vector3 targetAdd;     
        public float smoothFactor;    
    }
}