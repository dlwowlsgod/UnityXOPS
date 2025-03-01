using System;

/// <summary>
/// 캐릭터의 의심심 상태를 결정하는 플래그입니다.
/// </summary>
[Flags]
public enum CharacterSuspiciousFlag : ulong
{
    /// <summary>
    /// 캐릭터가 어떤 것도 의심하지 않습니다.
    /// None의 경우 Flag에서 필요가 없지만 간편한 비교를 위해 구현됐습니다.
    /// <para>예시 의사코드: if a == CharacterSuspiciousFlag.None</para>
    /// </summary>
    None = 0UL,

    /// <summary>
    /// 중립 캐릭터가 내는 걸음 소리를 공공 장소에서 들었습니다.
    /// </summary>
    HearingNeutralCharacterFootstepInPublicArea = 1UL << 0,

    /// <summary>
    /// 중립 캐릭터가 내는 걸음 소리를 개인 장소에서 들었습니다.
    /// </summary>
    HearingNeutralCharacterFootstepInPrivateArea = 1UL << 1,

    /// <summary>
    /// 중립 캐릭터가 내는 걸음 소리를 보안 장소에서 들었습니다.
    /// </summary>
    HearingNeutralCharacterFootstepInSecuredArea = 1UL << 2,

    /// <summary>
    /// 적대 캐릭터가 내는 걸음 소리를 공공 장소에서 들었습니다.
    /// </summary>
    HearingHostileCharacterFootstepInPublicArea = 1UL << 3,

    /// <summary>
    /// 적대 캐릭터가 내는 걸음 소리를 개인 장소에서 들었습니다.
    /// </summary>
    HearingHostileCharacterFootstepInPrivateArea = 1UL << 4,

    /// <summary>
    /// 적대 캐릭터가 내는 걸음 소리를 보안 장소에서 들었습니다.
    /// </summary>
    HearingHostileCharacterFootstepInSecuredArea = 1UL << 5,

    /// <summary>
    /// 중립 캐릭터가 내는 총소리를 공공 장소에서 들었습니다.
    /// </summary>
    HearingNeutralCharacterGunshotInPublicArea = 1UL << 6,

    /// <summary>
    /// 중립 캐릭터가 내는 총소리를 개인 장소에서 들었습니다.
    /// </summary>
    HearingNeutralCharacterGunshotInPrivateArea = 1UL << 7,

    /// <summary>
    /// 중립 캐릭터가 내는 총소리를 보안 장소에서 들었습니다.
    /// </summary>
    HearingNeutralCharacterGunshotInSecuredArea = 1UL << 8,

    /// <summary>
    /// 적대 캐릭터가 내는 총소리를 공공 장소에서 들었습니다.
    /// </summary>
    HearingHostileCharacterGunshotInPublicArea = 1UL << 9,

    /// <summary>
    /// 적대 캐릭터가 내는 총소리를 개인 장소에서 들었습니다.
    /// </summary>
    HearingHostileCharacterGunshotInPrivateArea = 1UL << 10,

    /// <summary>
    /// 적대 캐릭터가 내는 총소리를 보안 장소에서 들었습니다.
    /// </summary>
    HearingHostileCharacterGunshotInSecuredArea = 1UL << 11,

    /// <summary>
    /// 중립 캐릭터가 총을 쏘는 장면을 공공 장소에서 발견했습니다.
    /// </summary>
    FoundNeutralCharacterShootingInPublicArea = 1UL << 12,

    /// <summary>
    /// 중립 캐릭터가 총을 쏘는 장면을 개인 장소에서 발견했습니다.
    /// </summary>
    FoundNeutralCharacterShootingInPrivateArea = 1UL << 13,

    /// <summary>
    /// 중립 캐릭터가 총을 쏘는 장면을 보안 장소에서 발견했습니다.
    /// </summary>
    FoundNeutralCharacterShootingInSecuredArea = 1UL << 14,

    /// <summary>
    /// 적대 캐릭터가 총을 쏘는 장면을 공공 장소에서 발견했습니다.
    /// </summary>
    FoundHostileCharacterShootingInPublicArea = 1UL << 15,

    /// <summary>
    /// 적대 캐릭터가 총을 쏘는 장면을 개인 장소에서 발견했습니다.
    /// </summary>
    FoundHostileCharacterShootingInPrivateArea = 1UL << 16,

    /// <summary>
    /// 적대 캐릭터가 총을 쏘는 장면을 보안 장소에서 발견했습니다.
    /// </summary>
    FoundHostileCharacterShootingInSecuredArea = 1UL << 17,

    /// <summary>
    /// 중립 캐릭터를 공공 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundNeutralCharacterForMomentsInPublicArea = 1UL << 18,

    /// <summary>
    /// 중립 캐릭터를 개인 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundNeutralCharacterForMomentsInPrivateArea = 1UL << 19,

    /// <summary>
    /// 중립 캐릭터를 보안 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundNeutralCharacterForMomentsInSecuredArea = 1UL << 20,

    /// <summary>
    /// 적대 캐릭터를 공공 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundHostileCharacterForMomentsInPublicArea = 1UL << 21,

    /// <summary>
    /// 적대 캐릭터를 개인 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundHostileCharacterForMomentsInPrivateArea = 1UL << 22,

    /// <summary>
    /// 적대 캐릭터를 보안 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundHostileCharacterForMomentsInSecuredArea = 1UL << 23,

    /// <summary>
    /// 중립 캐릭터가 공공 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundNeutralCharacterCarryingVisibleWeaponForMomentsInPublicArea = 1UL << 24,

    /// <summary>
    /// 중립 캐릭터가 개인 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundNeutralCharacterCarryingVisibleWeaponForMomentsInPrivateArea = 1UL << 25,

    /// <summary>
    /// 중립 캐릭터가 보안 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundNeutralCharacterCarryingVisibleWeaponForMomentsInSecuredArea = 1UL << 26,

    /// <summary>
    /// 적대 캐릭터가 공공 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundHostileCharacterCarryingVisibleWeaponForMomentsInPublicArea = 1UL << 27,

    /// <summary>
    /// 적대 캐릭터가 개인 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundHostileCharacterCarryingVisibleWeaponForMomentsInPrivateArea = 1UL << 28,

    /// <summary>
    /// 적대 캐릭터가 보안 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단하지 못했습니다.
    /// </summary>
    FoundHostileCharacterCarryingVisibleWeaponForMomentsInSecuredArea = 1UL << 29,

    /// <summary>
    /// 중립 캐릭터를 공공 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundNeutralCharacterSurelyInPublicArea = 1UL << 30,

    /// <summary>
    /// 중립 캐릭터를 개인 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundNeutralCharacterSurelyInPrivateArea = 1UL << 31,

    /// <summary>
    /// 중립 캐릭터를 보안 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundNeutralCharacterSurelyInSecuredArea = 1UL << 32,

    /// <summary>
    /// 적대 캐릭터를 공공 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundHostileCharacterSurelyInPublicArea = 1UL << 33,

    /// <summary>
    /// 적대 캐릭터를 개인 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundHostileCharacterSurelyInPrivateArea = 1UL << 34,

    /// <summary>
    /// 적대 캐릭터를 보안 장소에서 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundHostileCharacterSurelyInSecuredArea = 1UL << 35,

    /// <summary>
    /// 중립 캐릭터가 공공 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundNeutralCharacterSurelyCarryingVisibleWeaponInPublicArea = 1UL << 36,

    /// <summary>
    /// 중립 캐릭터가 개인 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundNeutralCharacterSurelyCarryingVisibleWeaponInPrivateArea = 1UL << 37,

    /// <summary>
    /// 중립 캐릭터가 보안 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundNeutralCharacterSurelyCarryingVisibleWeaponInSecuredArea = 1UL << 38,

    /// <summary>
    /// 적대 캐릭터가 공공 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundHostileCharacterSurelyCarryingVisibleWeaponInPublicArea = 1UL << 39,

    /// <summary>
    /// 적대 캐릭터가 개인 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundHostileCharacterSurelyCarryingVisibleWeaponInPrivateArea = 1UL << 40,

    /// <summary>
    /// 적대 캐릭터가 보안 장소에서 무기를 들고 있는 것을 발견했지만 의심, 적대 캐릭터라고 확실히 판단했습니다.
    /// </summary>
    FoundHostileCharacterSurelyCarryingVisibleWeaponInSecuredArea = 1UL << 41,

    /// <summary>
    /// 무기를 공공 장소에서 발견했습니다.
    /// </summary>
    FoundWeaponInPublicArea = 1UL << 42,

    /// <summary>
    /// 무기를 개인 장소에서 발견했습니다.
    /// </summary>
    FoundWeaponInPrivateArea = 1UL << 43,

    /// <summary>
    /// 무기를 보안 장소에서 발견했습니다.
    /// </summary>
    FoundWeaponInSecuredArea = 1UL << 44,

    /// <summary>
    /// 시체를 공공 장소에서 발견했습니다.
    /// </summary>
    FoundDeadBodyInPublicArea = 1UL << 45,

    /// <summary>
    /// 시체를 개인 장소에서 발견했습니다.
    /// </summary>
    FoundDeadBodyInPrivateArea = 1UL << 46,

    /// <summary>
    /// 시체를 보안 장소에서 발견했습니다.
    /// </summary>
    FoundDeadBodyInSecuredArea = 1UL << 47
}
