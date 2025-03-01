/// <summary>
/// 캐릭터의 기본 행동을 정의하는 열거형입니다.
/// </summary>
public enum CharacterClassType
{
    /// <summary>
    /// 시민 클래스입니다. 규칙을 준수하고 궁지에 몰리지 않는 한 공격하지 않습니다.
    /// </summary>
    Citizen,

    /// <summary>
    /// 반시민 클래스입니다. 시민과 같지만 조금 더 호전적이고 반항적입니다.
    /// </summary>
    Anticitizen,

    /// <summary>
    /// 갱스터 클래스입니다. 시민과 같지만 공격받을 시 무조건 반격합니다. 
    /// </summary>
    Gangster,

    /// <summary>
    /// 테러리스트 클래스입니다. 호전적이고 누구도 좋아하지 않습니다.
    /// </summary>
    Terrorist,

    /// <summary>
    /// 경찰 클래스입니다. 시민을 우선적으로 보호합니다.
    /// </summary>
    Police,

    /// <summary>
    /// 군인 클래스입니다. 적대적 대상에 한해 매우 호전적이고 공격적입니다.
    /// </summary>
    Solider
}
