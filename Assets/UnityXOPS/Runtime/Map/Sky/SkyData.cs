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
    }
}
