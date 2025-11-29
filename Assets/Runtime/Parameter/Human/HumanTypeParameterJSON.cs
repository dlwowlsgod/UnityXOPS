using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanTypeParameterJSON : ParameterJSON
    {
        public float walkSpeed;
        public float runProgressSpeed;
        public float runSidewaySpeed;
        public float runRegressSpeed;
        public float jumpHeight;

        public float headDamageMultiplier;
        public float bodyDamageMultiplier;
        public float legDamageMultiplier;
        public int maxFallDamage;

        public int onHitEffect;
        public int onDeathEffect;
        public int onLandEffect;
    }
}