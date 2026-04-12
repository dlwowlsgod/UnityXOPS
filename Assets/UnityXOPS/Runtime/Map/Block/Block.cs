using UnityEngine;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 블록 메시, 텍스처, 위치, 충돌 정보를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class Block
    {
        public Mesh mesh;
        public int[] subMeshTextureIndices;
        public Vector3 position;
        public bool collider;
        public Vector3[] faceNormals;
        public Vector3[] faceCenters;

        /// <summary>
        /// 주어진 월드 좌표가 블록 내부에 있는지 판정한다.
        /// </summary>
        /// <param name="worldPoint">판정할 월드 좌표.</param>
        /// <returns>내부이면 true, 외부이면 false.</returns>
        public bool Contains(Vector3 worldPoint)
        {
            if (!collider) return false;

            for (int i = 0; i < 6; i++)
            {
                float d = Vector3.Dot(faceNormals[i], faceCenters[i] - worldPoint);
                if (d <= 0f) return false;
            }
            return true;
        }
    }
}