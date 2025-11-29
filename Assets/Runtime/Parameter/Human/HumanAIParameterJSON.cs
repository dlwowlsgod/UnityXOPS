using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanAIParameterJSON : ParameterJSON
    {
        public float aimingTime;
        public float rotationTime;
        public float detectionChance;
        public float attackChance;
        public float aimingCorrection;

        public float horizontalAngle;
        public float verticalAngle;
        public float normalViewDistance;
        public float alertViewDistance;
        public float normalHearingDistance;
        public float alertHearingDistance;
    }
}