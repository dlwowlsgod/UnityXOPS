using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanTypeParameterJSON : ParameterJSON
    {
        public float speed;
        public float runProgressSpeedMultiplier;
        public float runSidewaySpeedMultiplier;
        public float runRegressSpeedMultiplier;
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