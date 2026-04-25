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
        public float controllerStepClimbSpeed;
        public float controllerSlopeLimit;
        public float controllerGroundProbeRadius;

        public float gravityAcceleration;
        public float fallMinSpeed;
        public float fallMaxSpeed;
        public float deadlineY;
        public float deadBodyFallAngularSpeed;
        public bool deadBodyCollision;

        public float headHitboxHeight;
        public float headHitboxRadius;
        public float bodyHitboxHeight;
        public float bodyHitboxRadius;
        public float legHitboxHeight;
        public float legHitboxRadius;

        public FloatRange weaponPickupVerticalRange;
        public float weaponPickupRadius;

        public float armAngleNoWeapon;
        public float armAngleReloading;

        public int headHitReaction;
        public int bodyHitReaction;
        public int legHitReaction;
        public int zombieHitReaction;
        public int grenadeHitReaction;

        public List<HumanAnimation> humanAnimation;
    }
}
