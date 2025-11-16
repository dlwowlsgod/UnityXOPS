using UnityEngine;
using System.Linq;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanParameter", menuName = "XOPS Parameter/Human Parameter", order = 1)]
    public class HumanParameterSO : ParameterSO
    {
        public string[] armName;
        public string[] legName;
        public int[] walkAnimationIndices;
        public int[] runAnimationIndices;
        public Vector3 armRootPosition;
        public Vector3 armRootScale;
        public Vector3 legRootPosition;
        public Vector3 legRootScale;
        public HumanDataParameterSO[] humanDataParameterSOs;
        public HumanVisualParameterSO[] humanVisualParameterSOs;
        public HumanArmParameterSO[] humanArmParameterSOs;
        public HumanLegParameterSO[] humanLegParameterSOs;
        
        public override ParameterJSON Serialize()
        {
            return new HumanParameterJSON
            {
                name = name,
                armName = armName,
                legName = legName, 
                walkAnimationIndices = walkAnimationIndices, 
                runAnimationIndices = runAnimationIndices, 
                armRootPosition = armRootPosition, 
                armRootScale = armRootScale,
                legRootPosition = legRootPosition,
                legRootScale = legRootScale,
                humanDataParameterJSONs = humanDataParameterSOs?.Select(so => (HumanDataParameterJSON)so.Serialize()).ToArray(),
                humanVisualParameterJSONs = humanVisualParameterSOs?.Select(so => (HumanVisualParameterJSON)so.Serialize()).ToArray(),
                humanArmParameterJSONs = humanArmParameterSOs?.Select(so => (HumanArmParameterJSON)so.Serialize()).ToArray(),
                humanLegParameterJSONs = humanLegParameterSOs?.Select(so => (HumanLegParameterJSON)so.Serialize()).ToArray()
            };
        }
        
        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanParameterJSON humanJson))
            {
                return null;
            }
            
            name = humanJson.name;
            armName = humanJson.armName;
            legName = humanJson.legName; 
            walkAnimationIndices = humanJson.walkAnimationIndices; 
            runAnimationIndices = humanJson.runAnimationIndices; 
            armRootPosition = humanJson.armRootPosition; 
            armRootScale = humanJson.armRootScale;
            legRootPosition = humanJson.legRootPosition;
            legRootScale = humanJson.legRootScale;
            humanDataParameterSOs = humanJson.humanDataParameterJSONs
                .Select(j => (HumanDataParameterSO)CreateInstance<HumanDataParameterSO>().Deserialize(j))
                .ToArray();
            humanVisualParameterSOs = humanJson.humanVisualParameterJSONs
                .Select(j => (HumanVisualParameterSO)CreateInstance<HumanVisualParameterSO>().Deserialize(j))
                .ToArray();
            humanArmParameterSOs = humanJson.humanArmParameterJSONs
                .Select(j => (HumanArmParameterSO)CreateInstance<HumanArmParameterSO>().Deserialize(j))
                .ToArray();
            humanLegParameterSOs = humanJson.humanLegParameterJSONs
                .Select(j => (HumanLegParameterSO)CreateInstance<HumanLegParameterSO>().Deserialize(j))
                .ToArray();

            return this;
        }
    }
}