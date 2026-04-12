using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 무기 파라미터(공용, 데이터, 탄환, 스코프, 모델)를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class WeaponParameterData
    {
        public WeaponGeneralData weaponGeneralData;
        public List<WeaponData> weaponData;
        public List<BulletData> bulletData;
        public List<ScopeData> scopeData;
        public List<WeaponModelData> weaponModelData;
    }
}
