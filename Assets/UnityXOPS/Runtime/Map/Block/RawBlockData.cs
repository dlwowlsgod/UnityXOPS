using System;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// BD1 파일에서 읽어낸 블록의 원시 데이터(정점, UV, 텍스처 인덱스, 플래그)를 담는 구조체.
    /// </summary>
    [Serializable]
    public struct RawBlockData
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] textureIndices;
        public int flag;
    }
}