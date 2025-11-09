using UnityEngine;

namespace UnityXOPS
{
    public class ParameterManager : Singleton<ParameterManager>
    {
        [SerializeField]
        private SkyParameter skyParameter;
        
        public SkyParameter SkyParameter => skyParameter;
        
        public static void Initialize()
        {
            
        }
    }
}
