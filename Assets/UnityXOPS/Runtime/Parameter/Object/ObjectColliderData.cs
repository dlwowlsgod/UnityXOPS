using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 오브젝트의 충돌 판정 형상 목록을 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class ObjectColliderData
    {
        public List<ColliderShape> shapes;
    }

    /// <summary>
    /// 개별 충돌 형상 (구/박스/캡슐) 정의.
    /// size 해석: Box = (x, y, z) 전체 크기 / Sphere = x (반지름) / Capsule = x (반지름), y (높이), z (방향 0=X 1=Y 2=Z)
    /// </summary>
    [Serializable]
    public class ColliderShape
    {
        public ColliderShapeType type;
        public Vector3 center;
        public Vector3 size;
    }

    /// <summary>
    /// 충돌 형상 타입 열거형.
    /// </summary>
    public enum ColliderShapeType
    {
        Sphere,
        Box,
        Capsule
    }
}
