using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 인간 모델의 신체, 팔, 다리 메시와 텍스처 인덱스를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class HumanModelData
    {
        public string name;
        public List<string> textures;
        public List<ModelData> modelData;
        public int armIndex;
        public int armTextureIndex;
        public int legIndex;
        public int legTextureIndex;
    }
}
