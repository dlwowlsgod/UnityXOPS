using UnityEngine;

namespace UnityXOPS
{
    public static class Timestep
    {
        public static void Initialize()
        {
            var fixedTime = ProfileLoader.GetProfileValue("Time", "FixedFrameRate", "30");
            var time = ProfileLoader.GetProfileValue("Time", "FrameRate", "0");
            
            var fixedTimeParse = Mathf.Abs(int.TryParse(fixedTime, out var fixedTimeInt) ? fixedTimeInt : 30);
            var timeParse = Mathf.Abs(int.TryParse(time, out var timeInt) ? timeInt : 0);

            QualitySettings.vSyncCount = 0;
            var systemMonitorSyncCount = Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = timeParse < 60 ? (int)systemMonitorSyncCount + 10 : timeParse;

            var finalFixedTime = Mathf.Clamp(fixedTimeParse, 30, 60);
            Time.fixedDeltaTime = 1f / finalFixedTime;
        }
    }
}