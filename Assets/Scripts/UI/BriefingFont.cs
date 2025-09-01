using UnityEngine;

namespace UnityXOPS
{
    public class BriefingFont : GameFont
    {
        protected override void Start()
        {
            fontText = MissionLoader.Instance.longName;
            base.Start();
        }
    }
}