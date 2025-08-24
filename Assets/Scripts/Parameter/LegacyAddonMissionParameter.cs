using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 공식 임무를 담는 Parameter입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptableObject">ScriptableObject</see>입니다.
    /// 런타임에 <see cref="ParameterManager">ParameterManager</see>에 자동으로 추가됩니다.
    /// 에디터 생성을 하지 않기 때문에 툴팁이 없습니다.
    /// </remarks>
    public class LegacyAddonMissionParameter : ScriptableObject
    {
        public string finalName;
        public string longName;
        public string bd1Path;
        public string pd1Path;
        public int skyIndex;
        public bool adjustCollision;
        public bool darkScreen;
        public string addonObjectTxtPath;
        public string imagePath0;
        public string imagePath1;
        public string briefing;
    }
}