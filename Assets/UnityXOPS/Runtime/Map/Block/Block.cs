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

        /// <summary>
        /// 블록 6면 중 보이는 앞면과 레이의 교차를 판정한다. (원본 Collision::CheckBlockIntersectRay 대응)
        /// </summary>
        /// <param name="origin">레이 시작점.</param>
        /// <param name="direction">레이 방향 (정규화 권장).</param>
        /// <param name="maxDist">최대 거리. 0 이하이면 무한.</param>
        /// <param name="hitFace">맞은 면 인덱스 (0~5).</param>
        /// <param name="hitDist">맞은 거리.</param>
        /// <returns>맞았으면 true.</returns>
        public bool IntersectRay(Vector3 origin, Vector3 direction, float maxDist,
                                 out int hitFace, out float hitDist)
        {
            hitFace = -1;
            hitDist = 0f;
            if (!collider) return false;

            float minT      = (maxDist > 0f) ? maxDist : float.MaxValue;
            int   foundFace = -1;

            for (int i = 0; i < 6; i++)
            {
                Vector3 n = faceNormals[i];
                Vector3 c = faceCenters[i];

                // 원점이 면 앞쪽이어야 함 (면 뒤쪽에서는 무시)
                float ndc = Vector3.Dot(n, c - origin);
                if (ndc >= 0f) continue;

                float ndd = Vector3.Dot(n, direction);
                if (ndd >= -1e-6f) continue;

                float t = ndc / ndd;
                if (t < 0f || t > minT) continue;

                // p가 블록 내부(나머지 면들의 안쪽)인가?
                Vector3 p      = origin + direction * t;
                bool    inside = true;
                for (int j = 0; j < 6; j++)
                {
                    if (j == i) continue;
                    if (Vector3.Dot(faceNormals[j], faceCenters[j] - p) < -1e-4f)
                    {
                        inside = false;
                        break;
                    }
                }
                if (!inside) continue;

                minT      = t;
                foundFace = i;
            }

            if (foundFace == -1) return false;
            hitFace = foundFace;
            hitDist = minT;
            return true;
        }
    }
}