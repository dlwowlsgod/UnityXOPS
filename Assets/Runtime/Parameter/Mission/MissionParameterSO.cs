using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "MissionParameter", menuName = "XOPS Parameter/Mission Parameter", order = 1000)]
    public class MissionParameterSO : ParameterSO
    {
        public DemoData openingData;
        public CameraSequence[] openingCameraPositionSequence;
        public CameraSequence[] openingCameraRotationSequence;
        public TextSequence[] openingTextSequence;
        public DemoData[] demoData;
        public OfficialMissionParameterSO[] officialMissionParameterSOs;
        public AddonMissionParameterSO[] addonMissionParameterSOs;
        
        public override ParameterJSON Serialize()
        {
            return new MissionParameterJSON
            {
                name = name,
                openingData = openingData,
                openingCameraPositionSequence = openingCameraPositionSequence,
                openingCameraRotationSequence = openingCameraRotationSequence,
                openingTextSequence = openingTextSequence,
                demoData = demoData,
                officialMissionParameterJSONs = officialMissionParameterSOs
                    ?.Select(so => (OfficialMissionParameterJSON)so.Serialize()).ToArray()
            };
        }

        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is MissionParameterJSON missionJson))
            {
                return null;
            }

            name = missionJson.name;
            openingData = missionJson.openingData;
            openingCameraPositionSequence = missionJson.openingCameraPositionSequence;
            openingCameraRotationSequence = missionJson.openingCameraRotationSequence;
            openingTextSequence = missionJson.openingTextSequence;
            demoData = missionJson.demoData;
            officialMissionParameterSOs = missionJson.officialMissionParameterJSONs
                ?.Select(j => (OfficialMissionParameterSO)CreateInstance<OfficialMissionParameterSO>().Deserialize(j))
                .ToArray();
            
            return this;
        }
    }
}
