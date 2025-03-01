using System;

/// <summary>
/// <para>캐릭터의 AI 정보를 저장하기 위한 구조체입니다. 외부 정보 파일에서 받은 정보를 저장합니다.</para>
/// <para>캐릭터 AI 정보는 로드된 이후, 이 값이 직접적으로 사용되진 않습니다.</para>
/// </summary>
[Serializable]
public struct CharacterAIInfo
{
    /// <summary>
    /// 캐릭터의 기본 행동 골자를 정의합니다. 직업군으로 생각하면 쉽습니다.
    /// </summary>
    public CharacterRoleType RoleType;

    /// <summary>
    /// 캐릭터의 행동 시의 서열을 정의합니다. 상위 개체가 하위 개체를 지휘하고, 하위 개체가 상위 개체를 보호합니다.
    /// </summary>
    public CharacterRankType RankType;

    /// <summary>
    /// 캐릭터가 일반적인 상태일때 적을 볼 수 있는 거리를 정의합니다.
    /// </summary>
    public int EngageRangeOnNormal;

    /// <summary>
    /// 캐릭터가 의심 상태일때 적을 볼 수 있는 거리를 정의합니다.
    /// </summary>
    public int EngageRangeOnSuspicious;

    /// <summary>
    /// 캐릭터가 전투 상태일때 적을 볼 수 있는 거리를 정의합니다.
    /// </summary>
    public int EngageRangeOnCombat;

    /// <summary>
    /// 캐릭터가 일반 상태일 때 인간을 포착했을때 공격, 의심 대상임을 판단하는 시간입니다.
    /// </summary>
    public int JudgeTimeWhenFoundHumanShapeOnNormal;

    /// <summary>
    /// 캐릭터가 의심 상태일 때 인간을 포착했을때 공격, 의심 대상임을 판단하는 시간입니다.
    /// </summary>
    public int JudgeTimeWhenFoundHumanShapeOnSuspicious;

    /// <summary>
    /// 캐릭터가 전투 상태일 때 인간을 포착했을때 공격, 의심 대상임을 판단하는 시간입니다.
    /// </summary>
    public int JudgeTimeWhenFoundHumanShapeOnCombat;

    /// <summary>
    /// 캐릭터가 일반 상태일 때 수상한 점을 볼 경우 지속되는 수색 상태의 시간입니다.
    /// </summary>
    public int SuspiciousDurationOnNormal;

    /// <summary>
    /// 캐릭터가 의심 상태일 때 수상한 점을 볼 경우 지속되는 수색 상태의 시간입니다.
    /// </summary>
    public int SuspiciousDurationOnSuspicious;

    /// <summary>
    /// 캐릭터가 전투 상태일 때 수상한 점을 볼 경우 지속되는 수색 상태의 시간입니다.
    /// </summary>
    public int SuspiciousDurationOnCombat;

    /// <summary>
    /// 캐릭터가 어떤 상황에서 의심 상태로 도입될 지에 대한 플래그입니다.
    /// <para>SuspiciousFlag와 CombatFlag에 속하지 않는 경우 일반 상태를 지속합니다.</para>
    /// </summary>
    public CharacterSuspiciousFlag SuspiciousFlag;

    /// <summary>
    /// 캐릭터가 어떤 상황에서 전투 상태로 도입될 지에 대한 플래그입니다.
    /// <para>SuspiciousFlag와 CombatFlag에 속하지 않는 경우 일반 상태를 지속합니다.</para>
    /// </summary>
    public CharacterSuspiciousFlag CombatFlag;
}
