using System;
using UnityEngine;

/// <summary>
/// 무기 파츠의 수정자입니다.
/// </summary>
[Serializable]
public struct WeaponPartModifier
{
    public bool ModifyLeftHandIK;
    public Transform ModifiedLeftHandIK;
    public bool ModifyRightHandIK;
    public Transform ModifiedRightHandIK;
    public bool ModifyAccuracy;
    public Vector3 ModifiedMinAccuracy;
    public Vector3 ModifiedMaxAccuracy;
    public bool ModifyGunshotSoundRadiusFlag;
    public bool ModifyBulletTrailFlag;
    public bool ModifyWeaponSoundFlag;
}
