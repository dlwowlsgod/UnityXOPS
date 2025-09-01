using UnityEngine;

namespace UnityXOPS
{
    public class BriefingContent : OSFont
    {
        protected override void Start()
        {
            base.Start();
            Text.text = MissionLoader.Instance.briefingContent;
        }
    }
}
