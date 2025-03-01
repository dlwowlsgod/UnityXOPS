using System;

/// <summary>
/// <para>캐릭터의 AI 정보를 저장하기 위한 구조체입니다. 외부 정보 파일에서 받은 정보를 저장합니다.</para>
/// <para>캐릭터 AI 정보는 로드된 이후, 이 값이 직접적으로 사용되진 않습니다.</para>
/// </summary>
[Serializable]
public struct CharacterAIInfo
{
    public CharacterClassType ClassType;
}
