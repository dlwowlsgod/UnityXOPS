using UnityEngine;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// JSON에서 로드되는 공식 미션의 경로 및 설정 데이터를 담는 클래스.
    /// </summary>
    [Serializable]
    public class OfficialMissionData
    {
        public string name;
        public string fullname;
        public string bd1Path;
        public string pd1Path;
        public string txtPath;
        public bool adjustCollision;
        public bool darkScreen;
    }
}