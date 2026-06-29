using UnityEngine;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 모델의 메시 경로, 변환, 텍스처 인덱스를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class ModelData
    {
        public string modelPath;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public int textureIndex;
    }
}
