using UnityEngine;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// Unity 씬에서 사용하기 위해 빌드된 블록의 메시, 텍스처 인덱스, 위치 정보를 담는 클래스.
    /// </summary>
    [Serializable]
    public class Block
    {
        public Mesh mesh;
        public int[] subMeshTextureIndices;
        public Vector3 position;
        public bool collider;
    }
}