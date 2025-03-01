using System;

/// <summary>
/// <para>캐릭터의 메시 정보를 저장하기 위한 구조체입니다. 외부 정보 파일에서 받은 정보를 저장합니다.</para>
/// <para>캐릭터 메시 정보는 로드된 이후, 이 값이 직접적으로 사용되진 않습니다.</para>
/// </summary>
[Serializable]
public struct CharacterMeshInfo
{
    /// <summary>  
    /// 캐릭터의 머리 메시 인덱스 값입니다.
    /// </summary>
    public int HeadIndex;

    /// <summary>
    /// <para>캐릭터의 머리스타일 메시 인덱스 값입니다.</para>
    /// <para>머리스타일에 모자, 헬멧 등도 포함되어 있습니다.</para>
    /// </summary>
    public int HairIndex;

    /// <summary>
    /// 캐릭터의 눈 메시 인덱스 값입니다.
    /// </summary>
    public int EyeIndex;

    /// <summary>
    /// 캐릭터의 코 메시 인덱스 값입니다.
    /// </summary>
    public int NoseIndex;

    /// <summary>
    /// 캐릭터의 입 메시 인덱스 값입니다.
    /// </summary>
    public int MouthIndex;

    /// <summary>
    /// 캐릭터의 귀 메시 인덱스 값입니다.
    /// </summary>
    public int EarIndex;

    /// <summary>
    /// 캐릭터의 상체 메시 인덱스 값입니다.
    /// </summary>
    public int TorsoIndex;

    /// <summary>
    /// 캐릭터의 하체 메시 인덱스 값입니다.
    /// </summary>
    public int LegIndex;

    /// <summary>
    /// 캐릭터의 손 메시 인덱스 값입니다.
    /// </summary>
    public int HandIndex;

    /// <summary>
    /// 캐릭터의 1인칭 상체 메시 인덱스 값입니다.
    /// </summary>
    public int FPSTorsoIndex;

    /// <summary>
    /// 캐릭터의 1인칭 손 메시 인덱스 값입니다.
    /// </summary>
    public int FPSHandIndex;

    /// <summary>
    /// 캐릭터의 신발 메시 인덱스 값입니다.
    /// </summary>
    public int FeetIndex;

    /// <summary>
    /// 캐릭터 악세사리 슬롯 0의 메시 인덱스 값입니다.      
    /// </summary>
    public int Accessory0Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 1의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory1Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 2의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory2Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 3의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory3Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 4의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory4Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 5의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory5Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 6의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory6Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 7의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory7Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 8의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory8Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 9의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory9Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 10의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory10Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 11의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory11Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 12의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory12Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 13의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory13Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 14의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory14Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 15의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory15Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 16의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory16Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 17의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory17Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 18의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory18Index; 

    /// <summary>
    /// 캐릭터 악세사리 슬롯 19의 메시 인덱스 값입니다.
    /// </summary>
    public int Accessory19Index; 

    /// <summary>
    /// 캐릭터 정보가 없을 때 사용되는 기본 값입니다.
    /// </summary>
    public static CharacterMeshInfo Null => new()
    {
        
    };
}
