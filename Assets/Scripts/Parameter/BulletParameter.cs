using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Bullet 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "BulletParameter", menuName = "UnityXOPS/BulletParameter")]
    public class BulletParameter : ScriptableObject
    {
        [Tooltip("Bullet의 이름입니다.")]
        public string finalName;
        [Tooltip("Bullet의 메시입니다.")]
        public string meshPath;
        [Tooltip("Bullet의 텍스쳐입니다.")]
        public string texturePath;
        [Tooltip("Bullet은 timer 이후 폭발합니다. 비활성화 시 사라집니다.")]
        public bool explodeAfterTimer;
        [Tooltip("Bullet은 벽에 부딫힐 시 폭발합니다.")]
        public bool explodeAfterWallHit;
        [Tooltip("Bullet은 충돌체에 맞을 시 폭발합니다.")]
        public bool explodeAfterColliderHit;
        [Tooltip("Bullet의 무게입니다.")]
        public float mass;
        [Tooltip("Bullet의 폭발 크기입니다.")]
        public float explodeSize;
        [Tooltip("Bullet의 충돌 크기입니다.")]
        public float colliderSize;
        [Tooltip("Bullet이 사라지는 시간입니다.")]
        public float timer;
        [Tooltip("Bullet이 벽에 부딫힐 때 효과입니다.")]
        public SoundEffectPack wallHitPack;
        [Tooltip("Bullet이 폭발할 때 효과입니다.")]
        public SoundEffectPack explodePack;
    }

    /// <summary>
    /// <see cref="BulletParameter">BulletParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class BulletParameterWrapper : IParameterData
    {
        public string finalName;
        public string meshPath;
        public string texturePath;
        public bool explodeAfterTimer;
        public bool explodeAfterWallHit;
        public bool explodeAfterColliderHit;
        public float mass;
        public float explodeSize;
        public float colliderSize;
        public float timer;
        public SoundEffectPack wallHitPack;
        public SoundEffectPack explodePack;
        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="BulletParameterWrapper">BulletParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class BulletParameterList : IParameterList<BulletParameterWrapper>
    {
        public List<BulletParameterWrapper> items;
        public List<BulletParameterWrapper> Items => items;
    }

    [Serializable]
    public class SoundEffectPack
    {
        [Tooltip("소리 데이터입니다.")]
        public List<SoundPack> soundPack;
        [Tooltip("소리 범위입니다.")]
        public float soundRadius;
        [Tooltip("효과 데이터입니다.")]
        public List<EffectPack> effectPack;
    }

    [Serializable]
    public class SoundPack
    {
        [Tooltip("소리 경로입니다.")]
        public string soundPath;
        [Tooltip("소리가 재생될 확률입니다. (각 확률을 더한 값에서의 비율이 확률)")]
        public float soundProbability;
    }

    [Serializable]
    public class EffectPack
    {
        [Tooltip("효과 경로입니다.")]
        public string effectPath;
        [Tooltip("효과의 시작 크기입니다.")]
        public float effectStartSize;
        [Tooltip("효과의 종료 크기입니다.")]
        public float effectEndSize;
        [Tooltip("효과의 지속시간입니다.")]
        public float effectDuration;
        [Tooltip("효과 시작의 지연시간입니다.")]
        public float effectDelay;
    }
}