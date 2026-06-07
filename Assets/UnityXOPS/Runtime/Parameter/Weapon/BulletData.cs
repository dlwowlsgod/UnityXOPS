using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 탄환의 폭발 트리거 조건. Flags 비트 조합으로 데이터에서 자유 설정.
    /// 0=None(폭발 안 함), 1=Lifetime(시한), 2=Block(맵 명중), 4=Human(사람 명중), 8=Object(소품 명중).
    /// 예: 수류탄=1(Lifetime), RPG=15(전부).
    /// </summary>
    [Flags]
    public enum ExplosionTrigger
    {
        None     = 0,
        Lifetime = 1 << 0,
        Block    = 1 << 1,
        Human    = 1 << 2,
        Object   = 1 << 3,
    }

    /// <summary>
    /// 탄환의 메시, 텍스처, 중력, 폭발, 음향 파라미터를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class BulletData
    {
        public string           name;
        public string           texturePath;
        public string           modelPath;
        public Vector3          modelPosition;
        public Vector3          modelRotation;
        public Vector3          modelScale;
        public float            bulletBoundAdjust;
        public bool             useGravity;
        public float            gravityScale;
        public ExplosionTrigger explosionTrigger;
        public float            armingDelay;
        public float            explosionRadius;
        public float            humanExplosiveHeadDamageMax;
        public float            humanExplosiveLegDamageMax;
        public float            objectExplosiveDamageMax;
        public float            explosionknockbackMax;
        public string           explosionSound;
        public int              explosionEffectIndex;
        public int              wallHitEffectIndex;
        public int              humanHitEffectIndex;
        public int              objectHitEffectIndex;
        public List<string>     wallHitSounds;
        public List<string>     humanHitSounds;       // 사람 피격음 — 리스트에서 균등 랜덤 선택 (wallHitSounds 와 동일 방식). 하나만 넣으면 그것만 재생.
        public List<string>     bulletPassingSounds;  // 총알이 카메라 근처 통과 시 hyu 음 — 리스트 랜덤 선택. 비어있으면(GRENADE 등) 재생 안 함.
        public float            lifetime;
    }
}
