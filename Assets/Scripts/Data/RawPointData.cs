using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class RawPointData
    {
        public Vector3 position;
        public Quaternion rotation;
        public PointType type;
        public int param0;
        public int param1;
        public int param2;
    }
}