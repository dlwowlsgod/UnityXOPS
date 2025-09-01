using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Human Type 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "HumanTypeParameter", menuName = "UnityXOPS/HumanTypeParameter")]
    public class HumanTypeParameter : ScriptableObject
    {
        [Tooltip("HumanType의 이름입니다.")]
        public string finalName;
        [Tooltip("좀비 AI로 시작할지 여부입니다.")]
        public bool startZombie;
        [Tooltip("좀비에 감염됐는지 여부입니다.")]
        public bool infected;
        [Tooltip("좀비로 소생할 확률입니다.")]
        public float resurrectionChance;
        [Tooltip("어느 정도 시간 이후 좀비로 소생할지 여부입니다.")]
        public float resurrectionTime;
        [Tooltip("무기를 주울 수 있는지 여부입니다.")]
        public bool pickupWeapon;
        [Tooltip("머리 부위 데미지 증감 배율입니다.")]
        public float headDamageMult;
        [Tooltip("몸 부위 데미지 증감 배율입니다.")]
        public float bodyDamageMult;
        [Tooltip("다리 부위 데미지 증감 배율입니다..")]
        public float legDamageMult;
        [Tooltip("이동속도 증감 배율입니다..")]
        public float speedMult;
        [Tooltip("죽을 때 시체에 연기가 나는지 여부입니다.")]
        public bool smokeOnDeath;
        [Tooltip("이 체력이 될때까지 피가 나오지 않습니다.")]
        public float clampNoBloodHp;
        [Tooltip("낙하 데미지는 이 값으로 제한됩니다.")]
        public float clampFallDamage;
    }

    /// <summary>
    /// <see cref="HumanTypeParameter">HumanTypeParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanTypeParameterWrapper : IParameterData
    {
        public string finalName;
        public bool startZombie;
        public bool infected;
        public float resurrectionChance;
        public float resurrectionTime;
        public bool pickupWeapon;
        public float headDamageMult;
        public float bodyDamageMult;
        public float legDamageMult;
        public float speedMult;
        public bool smokeOnDeath;
        public float clampNoBloodHp;
        public float clampFallDamage;

        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="HumanTypeParameterWrapper">HumanTypeParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class HumanTypeParameterList : IParameterList<HumanTypeParameterWrapper>
    {
        public List<HumanTypeParameterWrapper> items = new List<HumanTypeParameterWrapper>();
        public List<HumanTypeParameterWrapper> Items => items;
    }
}