using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanTypeData
    {
        // 인간 타입 (0: Human, 1: Robot, 2: Zombie)

        public float progressRunAcceleration;
        public float sidewaysRunAcceleration;
        public float regressRunAcceleration;
        public float progressWalkAcceleration;
        public float attenuation;
        public float jumpSpeed;

        public float headDamageMultiplier;
        public float bodyDamageMultiplier;
        public float legDamageMultiplier;
        public int maxFallDamage;
        public IntRange headRandomAddDamage;
        public IntRange bodyRandomAddDamage;
        public IntRange legRandomAddDamage;

        public int bloodEffectIndex;
        public float bloodEffectThreshold;
        public int hitEffectIndex;
        public int deathEffectIndex;
        public bool bloodAttachesToMap;

        public bool canPickupWeapon;

        public bool zombie;
        public IntRange zombieMeleeDamageRange;

        public int autoBulletMultiplier;
    }
}
