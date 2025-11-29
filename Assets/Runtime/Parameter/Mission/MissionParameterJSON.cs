using System;

namespace UnityXOPS
{
    [Serializable]
    public class MissionParameterJSON : ParameterJSON
    {
        public DemoData openingData;
        public CameraSequence[] openingCameraPositionSequence;
        public CameraSequence[] openingCameraRotationSequence;
        public TextSequence[] openingTextSequence;
        public DemoData[] demoData;
        public OfficialMissionParameterJSON[] officialMissionParameterJSONs;
    }
}