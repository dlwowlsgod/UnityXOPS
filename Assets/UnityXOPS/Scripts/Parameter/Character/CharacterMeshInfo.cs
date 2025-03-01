using System;

/// <summary>
/// <para>캐릭터의 메시 정보를 저장하기 위한 구조체입니다. 외부 정보 파일에서 받은 정보를 저장합니다.</para>
/// <para>캐릭터 메시 정보는 로드된 이후, 이 값이 직접적으로 사용되진 않습니다.</para>
/// </summary>
[Serializable]
public struct CharacterMeshInfo
{
    /// <summary>
    /// 머리의 기본 골자가 되는 메시입니다. 머리카락, 헬멧 등이 포함됩니다.
    /// <para> 0의 경우 민머리입니다. </para>
    /// </summary>
    public int HeadIndex;

    /// <summary>
    /// 머리의 장식 메시입니다. 머리카락, 헬멧에 붙는 장식 등이 포함됩니다.
    /// <para> 0의 경우 장식이 없음입니다. </para>
    /// </summary>
    public int HeadAccessoryIndex;

    /// <summary>
    /// 머리의 코 메시입니다.
    /// <para> 0의 경우 코가 없음입니다. </para>
    /// </summary>
    public int HeadNoseIndex;

    /// <summary>
    /// 머리의 귀 메시입니다.
    /// <para> 0의 경우 귀가 없음입니다. </para>
    /// </summary>
    public int HeadEarIndex;

    /// <summary>
    /// 몸통의 기본 골자가 되는 메시입니다. 허리, 어깨, 팔 등이 포함됩니다.
    /// <para> 0의 경우 몸통이 없음입니다. </para>
    /// </summary>
    public int BodyTorsoIndex;

    /// <summary>
    /// 몸통의 비기능적인 메시입니다. 살이 노출된 부분을 표현하며 옷이 없는 부위의 나머지 부분입니다.
    /// <para> 0의 경우 비기능적인 메시가 없음입니다. </para>
    /// </summary>
    public int BodyNudeTorsoIndex;

    /// <summary>
    /// 몸통의 손 메시입니다.
    /// <para> 0의 경우 손이 맨살입니다. </para>
    /// </summary>
    public int BodyHandIndex;

    /// <summary>
    /// 1인칭 시점에서 보이는 몸통의 메시입니다. 어깨, 팔만만 포함됩니다.
    /// <para> 0의 경우 1인칭 시점에서 보이는 몸통이 모든 부분을 포함합니다. </para>
    /// </summary>
    public int Body1stTorsoIndex;

    /// <summary>
    /// 1인칭 시점에서 보이는 몸통의 비기능적인 메시입니다. 살이 노출된 부분을 표현하며 옷이 없는 부위의 나머지 부분입니다.
    /// <para> 0의 경우 1인칭 시점에서 보이는 몸통의 비기능적인 메시가 모든 부분을 포함합니다. </para>
    /// </summary>
    public int Body1stNudeTorsoIndex;
    
    /// <summary>
    /// 1인칭 시점에서 보이는 몸통의 손 메시입니다.
    /// <para> 0의 경우 1인칭 시점에서 보이는 몸통의 손이 맨살입니다. </para>
    /// </summary>
    public int Body1stHandIndex;

    /// <summary>
    /// 몸통의 다리 메시입니다.
    /// <para> 0의 경우 다리가 없음입니다. </para>
    /// </summary>
    public int BodyLegIndex;

    /// <summary>
    /// 몸통의 비기능적인 다리 메시입니다. 살이 노출된 부분을 표현하며 옷이 없는 부위의 나머지 부분입니다.
    /// <para> 0의 경우 비기능적인 다리가 모든 부분을 포함합니다. </para>
    /// </summary>
    public int BodyNudeLegIndex;

    /// <summary>
    /// 몸통의 발 메시입니다.
    /// <para> 0의 경우 발이 맨발입니다. </para>
    /// </summary>
    public int BodyFeetIndex;

    /// <summary>
    /// 캐릭터 정보가 없을 때 사용되는 기본 값입니다.
    /// </summary>
    public static CharacterMeshInfo Null => new()
    {
        HeadIndex = 0,
        HeadAccessoryIndex = 0,
        HeadNoseIndex = 0,
        HeadEarIndex = 0,
        BodyTorsoIndex = 0,
        BodyNudeTorsoIndex = 0,
        BodyHandIndex = 0,
        Body1stTorsoIndex = 0,
        Body1stNudeTorsoIndex = 0,
        Body1stHandIndex = 0,
        BodyLegIndex = 0,
        BodyNudeLegIndex = 0,
        BodyFeetIndex = 0,
    };
}
