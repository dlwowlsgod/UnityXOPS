using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 오브젝트 모델의 메시와 텍스처 인덱스를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class ObjectModelData
    {
        public List<string> textures;
        public List<ModelData> modelData;
    }
}
