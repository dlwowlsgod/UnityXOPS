using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 Weapon 정보를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 에디터 상에서 생성하여 <see cref="ParameterManager">ParameterManager</see>에 추가할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "WeaponParameter", menuName = "UnityXOPS/WeaponParameter")]
    public class WeaponParameter : ScriptableObject
    {
        [Tooltip("Weapon의 이름입니다.")]
        public string finalName;
        [Tooltip("Weapon의 속성입니다.")]
        public WeaponFlags flags;
        [Tooltip("Weapon의 데미지입니다.")]
        public int damage;
        [Tooltip("Weapon의 관통력입니다.")]
        public int penetration;
        [Tooltip("Weapon의 연사속도입니다.")]
        public float fireRate;
        [Tooltip("Weapon의 총알속도입니다.")]
        public float velocity;
        [Tooltip("Weapon의 장탄수입니다.")]
        public float capacity;
        [Tooltip("Weapon의 재장전 시간입니다.")]
        public float reloadTime;
        [Tooltip("Weapon의 반동입니다.")]
        public float recoil;
        [Tooltip("Weapon의 조준 오차 범위입니다.")]
        public Vector2 errorRange;
        [Tooltip("Weapon의 모델들입니다.")]
        public List<WeaponModel> staticModels;
        [Tooltip("Weapon의 머즐 플래시 여부입니다.")]
        public bool muzzleFlash;
        [Tooltip("Weapon의 머즐 플래시 크기입니다.")]
        public float muzzleFlashScale;
        [Tooltip("Weapon의 머즐 플래시 텍스쳐 경로입니다.")]
        public string muzzleFlashTexturePath;
        [Tooltip("Weapon의 머즐 플래시 위치입니다.")]
        public Vector3 muzzleFlashPosition;
        [Tooltip("Weapon의 탄피 배출 여부입니다.")]
        public bool shellEjection;
        [Tooltip("Weapon의 탄피 배출 지연 시간입니다.")]
        public float shellEjectionDelayTime;
        [Tooltip("Weapon의 탄피 텍스쳐 경로입니다.")]
        public string shellTexturePath;
        [Tooltip("Weapon의 탄피 크기입니다.")]
        public float shellScale;
        [Tooltip("Weapon의 탄피 배출 위치입니다.")]
        public Vector3 shellEjectionPosition;
        [Tooltip("Weapon의 탄피 배출 방향입니다.")]
        public Vector3 shellEjectionDirection;
        [Tooltip("Weapon의 점사 횟수입니다. (1 : 단발, 1보다 큰 양수 : 그 횟수만큼 점사, 이외 값 : 연사)")]
        public int burst;
        [Tooltip("Weapon의 총알입니다.")]
        public int bulletIndex;
        [Tooltip("Weapon의 스코프입니다.")]
        public int scopeIndex;
        [Tooltip("Weapon의 무소음 여부입니다.")]
        public bool silent;
        [Tooltip("Weapon의 격발음 경로입니다.")]
        public string weaponSoundPath;
        [Tooltip("Weapon의 격발음 소리 크기입니다.")]
        public float soundVolume;
        [Tooltip("Weapon의 격발음 범위입니다.")]
        public float soundRadius;
        [Tooltip("Weapon의 손 모양입니다. (OutOfRange : 기본 None Hand")]
        public int handIndex;
        [Tooltip("Weapon의 전환 시 다음 무기 인덱스입니다. (OutOfRange: 적용안함)")]
        public int nextWeaponIndex;
        [Tooltip("Weapon의 전환 소요 시간입니다.")]
        public float nextWeaponSwitchTime;
        [Tooltip("Weapon의 무기 교체시 소요 시간입니다.")]
        public float swapTime;
        [Tooltip("Weapon의 초기 탄창값입니다.")]
        public int magazineCount;
    }

    /// <summary>
    /// <see cref="WeaponParameter">WeaponParameter</see>를 직렬화하기 위한 Wrapper 클래스입니다.
    /// </summary>
    [Serializable]
    public class WeaponParameterWrapper : IParameterData
    {
        public string finalName;
        public WeaponFlags flags;
        public int damage;
        public int penetration;
        public float fireRate;
        public float velocity;
        public float capacity;
        public float reloadTime;
        public float recoil;
        public Vector2 errorRange;
        public List<WeaponModel> staticModels;
        public bool muzzleFlash;
        public float muzzleFlashScale;
        public string muzzleFlashTexturePath;
        public Vector3 muzzleFlashPosition;
        public bool shellEjection;
        public float shellEjectionDelayTime;
        public string shellTexturePath;
        public float shellScale;
        public Vector3 shellEjectionPosition;
        public Vector3 shellEjectionDirection;
        public int burst;
        public int bulletIndex;
        public int scopeIndex;
        public bool silent;
        public string weaponSoundPath;
        public float soundVolume;
        public float soundRadius;
        public int handIndex;
        public int nextWeaponIndex;
        public float nextWeaponSwitchTime;
        public float swapTime;
        public int magazineCount;

        public string FinalName => finalName;
    }

    /// <summary>
    /// <see cref="WeaponParameterWrapper">WeaponParameterWrapper</see>를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class WeaponParameterList : IParameterList<WeaponParameterWrapper>
    {
        public List<WeaponParameterWrapper> items;
        public List<WeaponParameterWrapper> Items => items;
    }
    
    [Serializable]
    public class WeaponModel
    {
        [Tooltip("메시 경로입니다.")]
        public string meshPath;
        [Tooltip("텍스쳐 경로입니다.")]
        public string texturePath;
        [Tooltip("모델의 사이즈니다.")]
        public float scale;
        [Tooltip("모델의 위치입니다.")]
        public Vector3 position;
        [Tooltip("모델의 회전(오일러 각)입니다.")]
        public Vector3 rotation;
    }

    [Flags]
    public enum WeaponFlags
    {
        None = 0,
        Melee = 1 << 0, //아닐 시 Ranged. Melee Flag라고 해도 총알이 바로 사라지지 않음에 주의 (독자적인 금방 사라지는 Bullet 필요)
        DisposeEmptyAmmo = 1 << 1, //총알이 없으면 무기 사라짐 (GRENADE)
        DisposeIfShot = 1 << 2, //어떤 상황이든 한 번이라도 사용하면 무기 사라짐 (유리파편)
        CaptureItemFlag = 1 << 3, //CASE와 같은 무기 취급
    }
}