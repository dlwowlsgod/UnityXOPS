using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 무기 모델의 메시, 텍스처, 총구 섬광, 탄피 배출 파라미터를 담는 컨테이너 클래스.
    /// </summary>
    [Serializable]
    public class WeaponModelData
    {
        public string name;
        public List<string> textures;
        public List<ModelData> modelData;
        public string muzzleFlashTexture;
        public Vector3 muzzleFlashOffset;
        public float muzzleFlashSize;
        public string shellTexture;
        public Vector3 shellEjectOffset;
        public Vector3 shellEjectDirection;
        public float shellEjectSpeed;
        public float shellEjectDelay;
        public float shellSize;
        public int leftArmIndex;
        public bool fixLeftArm;
        public int rightArmIndex;
        public bool fixRightArm;
    }

    /// <summary>
    /// 탄피 배출 시점을 정의하는 열거형.
    /// </summary>
    public enum ShellEjectMode
    {
        OnFire,
        OnReload,
        None
    }
}
