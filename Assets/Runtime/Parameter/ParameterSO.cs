using UnityEngine;

namespace UnityXOPS
{
    public abstract class ParameterSO : ScriptableObject
    {
        public abstract ParameterJSON Serialize();
        public abstract ParameterSO Deserialize(ParameterJSON json);
    }
}