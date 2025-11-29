using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanTypeParameter", menuName = "XOPS Parameter/Human Type Parameter", order = 4)]
    public class HumanTypeParameterSO : ParameterSO
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
        
        public override ParameterJSON Serialize()
        {
            return new HumanTypeParameterJSON
            {
                name = name,
                walkSpeed = walkSpeed,
                runProgressSpeed = runProgressSpeed,
                runSidewaySpeed = runSidewaySpeed,
                runRegressSpeed = runRegressSpeed,
                jumpHeight = jumpHeight,
                headDamageMultiplier = headDamageMultiplier,
                bodyDamageMultiplier = bodyDamageMultiplier,
                legDamageMultiplier = legDamageMultiplier,
                maxFallDamage = maxFallDamage,
                onHitEffect = onHitEffect,
                onDeathEffect = onDeathEffect,
                onLandEffect = onLandEffect
            };
        }

        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanTypeParameterJSON typeJson))
            {
                return null;
            }
            
            name = typeJson.name;
            walkSpeed = typeJson.walkSpeed;
            runProgressSpeed = typeJson.runProgressSpeed;
            runSidewaySpeed = typeJson.runSidewaySpeed;
            runRegressSpeed = typeJson.runRegressSpeed;
            jumpHeight = typeJson.jumpHeight;
            headDamageMultiplier = typeJson.headDamageMultiplier;
            bodyDamageMultiplier = typeJson.bodyDamageMultiplier;
            legDamageMultiplier = typeJson.legDamageMultiplier;
            maxFallDamage = typeJson.maxFallDamage;
            onHitEffect = typeJson.onHitEffect;
            onDeathEffect = typeJson.onDeathEffect;
            onLandEffect = typeJson.onLandEffect;
            
            return this;
        }
    }
}
