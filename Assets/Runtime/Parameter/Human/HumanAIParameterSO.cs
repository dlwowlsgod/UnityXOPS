using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanAIParameter", menuName = "XOPS Parameter/Human AI Parameter", order = 5)]
    public class HumanAIParameterSO : ParameterSO
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
        
        
        public override ParameterJSON Serialize()
        {
            return new HumanAIParameterJSON
            {
                name = name,
                aimingTime = aimingTime,
                rotationTime = rotationTime,
                detectionChance = detectionChance,
                attackChance = attackChance,
                aimingCorrection = aimingCorrection,
                horizontalAngle = horizontalAngle,
                verticalAngle = verticalAngle,
                normalViewDistance = normalViewDistance,
                alertViewDistance = alertViewDistance,
                normalHearingDistance = normalHearingDistance,
                alertHearingDistance = alertHearingDistance
            };
        }

        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanAIParameterJSON aiJson))
            {
                return null;
            }
            
            name = aiJson.name;
            aimingTime = aiJson.aimingTime;
            rotationTime = aiJson.rotationTime;
            detectionChance = aiJson.detectionChance;
            attackChance = aiJson.attackChance;
            aimingCorrection = aiJson.aimingCorrection;
            horizontalAngle = aiJson.horizontalAngle;
            verticalAngle = aiJson.verticalAngle;
            normalViewDistance = aiJson.normalViewDistance;
            alertViewDistance = aiJson.alertViewDistance;
            normalHearingDistance = aiJson.normalHearingDistance;
            alertHearingDistance = aiJson.alertHearingDistance;
            
            return this;    
        }
    }
}
