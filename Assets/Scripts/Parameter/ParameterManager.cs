using UnityEngine;
using System.Collections.Generic;

namespace UnityXOPS
{
    public class ParameterManager : Singleton<ParameterManager>
    {
        [SerializeField]
        private List<HumanParameter> humanParameter;
        [SerializeField]
        private List<HumanAIParameter> humanAIParameter;
        
        public List<HumanParameter> HumanParameter => humanParameter;
        public List<HumanAIParameter> HumanAIParameter => humanAIParameter;
    }
}