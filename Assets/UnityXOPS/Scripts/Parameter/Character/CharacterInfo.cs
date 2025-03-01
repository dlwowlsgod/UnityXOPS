using System;

/// <summary>
/// <para>캐릭터의 정보를 저장하기 위한 구조체입니다. 외부 정보 파일에서 받은 정보를 저장합니다.</para>
/// <para>캐릭터 정보는 로드된 이후, 이 값이 직접적으로 사용되진 않습니다.</para>
/// </summary>
[Serializable]
public struct CharacterInfo
{
    /// <summary>
    /// 캐릭터의 이름입니다. 게임 상에서 실제로 사용되지는 않습니다. (에디터 전용)
    /// </summary>
    public string Name;

    /// <summary>
    /// 캐릭터의 성별이 남성인지 여성인지를 알리는 불리언 값입니다.
    /// </summary>
    public bool isFemale;

    /// <summary>
    /// 캐릭터의 최대 체력 값입니다.
    /// </summary>
    public int HealthPoint;
    
    /// <summary>
    /// 캐릭터의 방탄복의 흡수 값입니다.
    /// </summary>
    public int ArmorPoint;

    /// <summary>
    /// 캐릭터 헬멧이 데미지 감소를 몇 회까지 할 수 있는지의 값입니다.
    /// </summary>
    public int HelmetDamagePreventionCount;

    /// <summary>
    /// 캐릭터의 방탄복이 감소시키거나 막아주는 부위를 정의합니다.
    /// </summary>
    public CharacterArmorPart ArmorParts;

    /// <summary>
    /// 캐릭터의 방탄복이 감소시키거나 막아주는 데미지 타입을 정의합니다.
    /// </summary>
    public CharacterArmorFlag ArmorFlag;

    /// <summary>
    /// 캐릭터의 헬멧이 감소시키거나 막아주는 데미지 타입을 정의합니다.
    /// </summary>
    public CharacterArmorFlag HelmetFlag;

    /// <summary>
    /// 캐릭터 정보가 없을 때 사용되는 기본 값입니다.
    /// </summary>
    public static CharacterInfo Null => new()
    {
        Name = "NullCharacter",
        isFemale = false,
        HealthPoint = 100,
        ArmorPoint = 0,
        ArmorParts = CharacterArmorPart.None,
        ArmorFlag = CharacterArmorFlag.None,
        HelmetFlag = CharacterArmorFlag.None
    };
}