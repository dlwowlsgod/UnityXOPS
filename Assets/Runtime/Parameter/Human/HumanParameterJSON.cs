using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class HumanParameterJSON : ParameterJSON
    {
        public string[] armName;
        public string[] legName;
        public int[] walkAnimationIndices;
        public int[] runAnimationIndices;
        public Vector3 humanScale;
        public Vector3 armRootPosition;
        public Vector3 armRootScale;
        public Vector3 legRootPosition;
        public Vector3 legRootScale;
        public Vector3 headColliderCenter;
        public Vector3 headColliderSize;
        public Vector3 bodyColliderCenter;
        public Vector3 bodyColliderSize;
        public Vector3 legColliderCenter;
        public Vector3 legColliderSize;
        public float controllerHeight;
        public float controllerRadius;
        public float slopeAngle;
        public float stepHeight;
        public float viewportHeight;
        public float tpsCameraDistance;
        public HumanDataParameterJSON[] humanDataParameterJSONs;
        public HumanVisualParameterJSON[] humanVisualParameterJSONs;
        public HumanTypeParameterJSON[] humanTypeParameterJSONs;
        public HumanAIParameterJSON[] humanAIParameterJSONs;
        public HumanArmParameterJSON[] humanArmParameterJSONs;
        public HumanLegParameterJSON[] humanLegParameterJSONs;
    }
}