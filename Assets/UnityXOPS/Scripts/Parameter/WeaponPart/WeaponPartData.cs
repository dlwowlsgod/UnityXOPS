using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기의 파츠를 저장하는 데이터입니다. 무기 부품, 부착물, 탄창 등이 포함됩니다.
/// </summary>
[Serializable]
public struct WeaponPartData
{
    /// <summary>
    /// 무기 파츠의 이름입니다.
    /// </summary>
    public string Name;
    
    /// <summary>
    /// 무기 파츠의 로우 폴리 메시 경로입니다. 오브젝트가 여러개인 메시일 경우 먼저 불러와진 1개만 사용됩니다.
    /// </summary>
    public string LowPolyMeshPath;

    /// <summary>
    /// 무기 파츠의 로우 폴리 색상 택스쳐 경로입니다.
    /// </summary>
    public string LowPolyTexturePath;

    /// <summary>
    /// 무기 파츠의 로우 폴리 메탈릭 택스쳐 경로입니다. 알파 값은 Roughness 값입니다.
    /// </summary>
    public string LowPolyMetallicTexturePath;

    /// <summary>
    /// 무기 파츠의 로우 폴리 노말 택스쳐 경로입니다.
    /// </summary>
    public string LowPolyNormalTexturePath;

    /// <summary>
    /// 무기 파츠의 하이 폴리 메시 경로입니다.
    /// </summary>
    public string HighPolyMeshPath;

    /// <summary>
    /// 무기 파츠의 하이 폴리 색상 택스쳐 경로입니다.
    /// </summary>
    public string HighPolyTexturePath;

    /// <summary>
    /// 무기 파츠의 하이 폴리 메탈릭 택스쳐 경로입니다. 알파 값은 Roughness 값입니다.
    /// </summary>
    public string HighPolyMetallicTexturePath;

    /// <summary>
    /// 무기 파츠의 하이 폴리 노말 택스쳐 경로입니다.
    /// </summary>
    public string HighPolyNormalTexturePath;

    /// <summary>
    /// 무기 파츠의 타입입니다.
    /// </summary>
    public WeaponPartType PartType;

    /// <summary>
    /// 무기 파츠가 장착될 때 변경되는 무기의 특성을 저장합니다.
    /// </summary>
    public WeaponPartModifier PartModifier;

    /// <summary>
    /// 무기 파츠의 조인트 위치입니다. 위치, 회전, 크기값을 저장합니다.
    /// </summary>
    public List<Transform> PartJointTransform;
}
