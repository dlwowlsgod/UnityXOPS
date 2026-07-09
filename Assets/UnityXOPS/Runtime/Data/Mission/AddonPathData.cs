using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// addon.json을 역직렬화하는 데이터 클래스. StreamingAssets 기준 상대 경로 목록과 페이지 이름 목록을 담는다.
    /// addonPath의 각 경로가 어드온 맵 팩(페이지) 하나가 되고, 같은 인덱스의 addonName이 그 페이지 이름이 된다.
    /// addonName이 없거나 길이가 addonPath보다 짧으면 해당 페이지 이름은 빈 문자열로 처리한다(기준은 addonPath).
    /// </summary>
    [Serializable]
    public class AddonPathData
    {
        public List<string> addonPath;
        public List<string> addonName;
    }
}
