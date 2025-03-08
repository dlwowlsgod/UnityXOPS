using System;

/// <summary>
/// 무기 파츠의 타입입니다.
/// </summary>
[Serializable]
public enum WeaponPartType
{
    /// <summary>
    /// 무기의 몸체입니다.
    /// </summary>
    Body,

    /// <summary>
    /// 무기의 상단 부분입니다.
    /// </summary>
    Upper,

    /// <summary>
    /// 무기의 하단 부분입니다.
    /// </summary>
    Lower,

    /// <summary>
    /// 무기의 슬라이드입니다.
    /// </summary>
    Slide,

    /// <summary>
    /// 무기의 탄창입니다.
    /// </summary>
    Magazine,

    /// <summary>
    /// 무기의 전면 손잡이입니다.
    /// </summary>
    Handguard,

    /// <summary>
    /// 무기의 총신입니다.
    /// </summary>
    Barrel,
    
    /// <summary>
    /// 무기의 개머리판입니다.
    /// </summary>
    Stock,

    /// <summary>
    /// 무기의 상단 부속입니다.
    /// </summary>
    UpperAttachment,

    /// <summary>
    /// 무기의 하단 부속입니다.
    /// </summary>
    LowerAttachment,

    /// <summary>
    /// 무기의 전면 손잡이 부속입니다.
    /// </summary>
    HandguardAttachment,

    /// <summary>
    /// 무기의 총신 부속입니다.
    /// </summary>
    BarrelAttachment
}