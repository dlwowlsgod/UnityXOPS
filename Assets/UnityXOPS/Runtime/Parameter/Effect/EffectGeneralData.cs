using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 이펙트가 공유하는 텍스처 경로 목록을 담는 컨테이너 클래스.
    /// 원본 OpenXOPS Resource->LoadEffectTexture 가 하드코딩한 4 개 dds 순서를 그대로 따름:
    /// [0]=blood, [1]=mflash, [2]=smoke, [3]=yakkyou.
    /// </summary>
    [Serializable]
    public class EffectGeneralData
    {
        public List<string> texturePaths;
    }
}
