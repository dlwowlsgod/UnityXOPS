using System;

/// <summary>
/// <para>캐릭터의 데미지 감소가 적용될 부위를 나타냅니다.</para>
/// <para>실제 게임에서 사용되지 않을 부분도 포함되어 있습니다.</para>
/// </summary>
[Flags]
public enum CharacterArmorPart
{
    /// <summary>
    /// 캐릭터가 어느 부위에도 방탄이 적용되지 않음을 나타냅니다.
    /// None의 경우 Flag에서 필요가 없지만 간편한 비교를 위해 구현됐습니다.
    /// <para>예시 의사코드: if a == CharacterArmorFlag.None</para>
    /// </summary>
    None = 0,

    /// <summary>
    /// 캐릭터의 머리, 목 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    Head = 1 << 0,

    /// <summary>
    /// 캐릭터의 상체, 허리 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    Body = 1 << 1,

    /// <summary>
    /// 캐릭터의 왼쪽 어깨 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    LeftShoulder = 1 << 2,

    /// <summary>   
    /// 캐릭터의 오른쪽 어깨 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    RightShoulder = 1 << 3,

    /// <summary>
    /// 캐릭터의 왼쪽 팔 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    LeftArm = 1 << 4,

    /// <summary>
    /// 캐릭터의 오른쪽 팔 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    RightArm = 1 << 5,

    /// <summary>
    /// 캐릭터의 왼쪽 전완, 손손 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    LeftForearm = 1 << 6,

    /// <summary>
    /// 캐릭터의 오른쪽 전완, 손손 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    RightForearm = 1 << 7,

    /// <summary>
    /// 캐릭터의 왼쪽 허벅지 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    LeftThigh = 1 << 8,

    /// <summary>
    /// 캐릭터의 오른쪽 허벅지 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    RightThigh = 1 << 9,

    /// <summary>
    /// 캐릭터의 왼쪽 정강이 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    LeftCalf = 1 << 10,

    /// <summary>
    /// 캐릭터의 오른쪽 정강이 부위에 방탄이 적용됨을 나타냅니다.
    /// </summary>
    RightCalf = 1 << 11,
}
