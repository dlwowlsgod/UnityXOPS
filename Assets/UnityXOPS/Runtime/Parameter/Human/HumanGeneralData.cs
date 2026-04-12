using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 인간 캐릭터에 적용되는 공통 스케일, 높이, 컨트롤러 파라미터를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class HumanGeneralData
    {
        public float humanBodyScale;
        public float humanArmScale;
        public float humanLegScale;
        public float controllerHeight;
        public float humanBodyHeight;
        public float humanArmHeight;
        public float humanLegHeight;
        public float cameraAttachPosition;
        public float controllerRadiusControllerToMap;
        public float controllerRadiusControllerToController;
        public float controllerStepOffset;
        public float controllerClimbSpeed;
        public float controllerSlopeLimit;
        public List<HumanAnimation> humanAnimation;
    }
}