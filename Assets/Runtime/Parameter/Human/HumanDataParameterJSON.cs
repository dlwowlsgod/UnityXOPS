using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanDataParameterJSON : ParameterJSON
    {
        public int hp;
        public int weapon0Index;
        public int weapon1Index;
        public int visualIndex;
        public string typeClass;
        public string aiClass;
    }
}