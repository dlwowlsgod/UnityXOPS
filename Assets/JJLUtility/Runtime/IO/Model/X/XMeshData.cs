using UnityEngine;
using System.Collections.Generic;

namespace JJLUtility.IO
{
    /// <summary>
    /// .x 파일에서 파싱된 단일 메시의 정점, 인덱스, UV 데이터를 담는 클래스.
    /// </summary>
    public class XMeshData
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<int> Indices = new List<int>();
        public List<Vector2> UVs = new List<Vector2>();
    }
}
