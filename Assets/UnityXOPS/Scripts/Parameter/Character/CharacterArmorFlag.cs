using System;

/// <summary>
/// <para>캐릭터의 방탄복이 감소시키거나 막아주는 데미지 타입을 정의합니다.</para>
/// <para>일부 감소 타입은 비현실적이지만, 사용자의 창의성을 위해 구현됐습니다.</para>
/// </summary>
[Flags]
public enum CharacterArmorFlag
{
    /// <summary>
    /// 캐릭터가 가진 방탄효과가 없음을 나타냅니다.
    /// None의 경우 Flag에서 필요가 없지만 간편한 비교를 위해 구현됐습니다.
    /// <para>예시 의사코드: if a == CharacterArmorFlag.None</para>
    /// </summary>
    None = 0,

    /// <summary>
    /// 캐릭터가 받는 근접 무기 탄도 데미지를 감소시킵니다.
    /// </summary>
    ReduceMeleeBulletDamage = 1 << 0,

    /// <summary>
    /// 캐릭터가 받는 근접 무기 물리 데미지를 감소시킵니다.
    /// </summary>
    ReduceMeleePhysicalDamage = 1 << 1,

    /// <summary>
    /// 캐릭터가 받는 근접 무기 폭발 데미지를 감소시킵니다.
    /// </summary>
    ReduceMeleeExplosiveDamage = 1 << 2,

    /// <summary>
    /// 캐릭터가 받는 권총 탄도 데미지를 감소시킵니다.
    /// </summary>
    ReducePistolBulletDamage = 1 << 3,

    /// <summary>
    /// 캐릭터가 받는 권총 물리 데미지를 감소시킵니다.
    /// </summary>
    ReducePistolPhysicalDamage = 1 << 4,

    /// <summary>
    /// 캐릭터가 받는 권총 폭발 데미지를 감소시킵니다.
    /// </summary>
    ReducePistolExplosiveDamage = 1 << 5,

    /// <summary>
    /// 캐릭터가 받는 소총 탄도 데미지를 감소시킵니다.
    /// </summary>
    ReduceRifleBulletDamage = 1 << 6,

    /// <summary>
    /// 캐릭터가 받는 소총 물리 데미지를 감소시킵니다.
    /// </summary>
    ReduceRiflePhysicalDamage = 1 << 7,

    /// <summary>
    /// 캐릭터가 받는 소총 폭발 데미지를 감소시킵니다.
    /// </summary>
    ReduceRifleExplosiveDamage = 1 << 8,

    /// <summary>
    /// 캐릭터가 받는 중화기 탄도 데미지를 감소시킵니다.
    /// </summary>
    ReduceHeavyWeaponBulletDamage = 1 << 9,

    /// <summary>
    /// 캐릭터가 받는 중화기 물리 데미지를 감소시킵니다.
    /// </summary>
    ReduceHeavyWeaponPhysicalDamage = 1 << 10,

    /// <summary>
    /// 캐릭터가 받는 중화기 폭발 데미지를 감소시킵니다.
    /// </summary>
    ReduceHeavyWeaponExplosiveDamage = 1 << 11,

    /// <summary>
    /// 캐릭터가 받는 근접 무기 탄도 데미지를 막습니다.
    /// </summary>
    PreventMeleeBulletDamage = 1 << 12,

    /// <summary>
    /// 캐릭터가 받는 근접 무기 물리 데미지를 막습니다.
    /// </summary>
    PreventMeleePhysicalDamage = 1 << 13,

    /// <summary>
    /// 캐릭터가 받는 근접 무기 폭발 데미지를 막습니다.
    /// </summary>
    PreventMeleeExplosiveDamage = 1 << 14,

    /// <summary>
    /// 캐릭터가 받는 권총 탄도 데미지를 막습니다.
    /// </summary>
    PreventPistolBulletDamage = 1 << 15,

    /// <summary>
    /// 캐릭터가 받는 권총 물리 데미지를 막습니다.
    /// </summary>
    PreventPistolPhysicalDamage = 1 << 16,

    /// <summary>
    /// 캐릭터가 받는 권총 폭발 데미지를 막습니다.
    /// </summary>
    PreventPistolExplosiveDamage = 1 << 17,

    /// <summary>
    /// 캐릭터가 받는 소총 탄도 데미지를 막습니다.
    /// </summary>
    PreventRifleBulletDamage = 1 << 18,

    /// <summary>
    /// 캐릭터가 받는 소총 물리 데미지를 막습니다.
    /// </summary>
    PreventRiflePhysicalDamage = 1 << 19,

    /// <summary>
    /// 캐릭터가 받는 소총 폭발 데미지를 막습니다.
    /// </summary>
    PreventRifleExplosiveDamage = 1 << 20,

    /// <summary>
    /// 캐릭터가 받는 중화기 탄도 데미지를 막습니다.
    /// </summary>
    PreventHeavyWeaponBulletDamage = 1 << 21,

    /// <summary>
    /// 캐릭터가 받는 중화기 물리 데미지를 막습니다.
    /// </summary>
    PreventHeavyWeaponPhysicalDamage = 1 << 22,

    /// <summary>   
    /// 캐릭터가 받는 중화기 폭발 데미지를 막습니다.
    /// </summary>
    PreventHeavyWeaponExplosiveDamage = 1 << 23
}
