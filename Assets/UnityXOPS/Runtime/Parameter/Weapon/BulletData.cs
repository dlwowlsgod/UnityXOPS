using System.Collections.Generic;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 탄환의 메시, 텍스처, 중력, 폭발, 음향 파라미터를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class BulletData
    {
        public string name;
        public string texturePath;
        public string modelPath;
        public float modelScale;
        public bool useGravity;
        public float gravityScale;
        public bool hasExplosion;
        public float explosionRadius;
        public float explosionDamageMax;
        public float explosionknockbackMax;
        public string explosionSound;
        public List<string> wallHitSounds;
    }
}
