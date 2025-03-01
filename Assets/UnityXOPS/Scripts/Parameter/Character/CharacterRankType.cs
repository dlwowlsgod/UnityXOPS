/// <summary>
/// 캐릭터의 등급을 정의합니다. 등급이 낮을수록 높은 등급을 따르거나 지킵니다.
/// <para> 열거형 값이 높을수록 높은 서열을 가집니다. AI의 계급체계에 따라 행동방식이 달라집니다. </para>
/// </summary>
public enum CharacterRankType
{
    /// <summary>
    /// 서열 타입 1입니다. 일반적인 경우를 의미합니다.
    /// </summary>
    Junior,

    /// <summary>
    /// 서열 타입 2입니다. Junior와 같지만 조금 더 높은 서열을 가질 경우입니다.
    /// </summary>
    Senior,

    /// <summary>
    /// 서열 타입 3입니다. 대체로 리더일 경우를 의미합니다.
    /// </summary>
    Leader,

    /// <summary>
    /// 서열 타입 4입니다. 대체로 부보스일 경우를 의미합니다.
    /// </summary>
    Vice,

    /// <summary>
    /// 서열 타입 5입니다. 대체로 보스일 경우를 의미합니다.
    /// </summary>
    Boss,

    /// <summary>
    /// 서열 타입 6입니다. AI가 생각하기에 아군 중 가장 중요한 인물을 의미합니다.
    /// <para> 대체로 실제 명령을 내리지 않고 보호 대상으로만 사용되기 위한 목적입니다. </para>
    /// </summary>
    VIP
}
