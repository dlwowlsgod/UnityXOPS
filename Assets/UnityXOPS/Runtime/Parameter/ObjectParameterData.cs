using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 오브젝트 파라미터(공용, 데이터, 모델, 콜라이더)를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class ObjectParameterData
    {
        public ObjectGeneralData objectGeneralData;
        public List<ObjectData> objectData;
        public List<ObjectModelData> objectModelData;
        public List<ObjectColliderData> objectColliderData;
    }
}
