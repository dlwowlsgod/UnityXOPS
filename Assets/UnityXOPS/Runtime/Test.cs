using UnityEngine;
using JJLUtility;
using System.Collections.Generic;

namespace UnityXOPS
{
    public class Test : MonoBehaviour
    {
        private void Start()
        {
            string bd1Path = SafePath.Combine(Application.streamingAssetsPath, "data/map10/temp.bd1");
            MapLoader.LoadBlockData(bd1Path);
            string skyPath = SafePath.Combine(Application.streamingAssetsPath, "data/sky0/sky0.sky");
            MapLoader.LoadSkyData(new SkyData
            {
                skyMeshPath = "data/sky/sky.x",
                skyTexturePath = new List<string>
                {
                    "",
                    "data/sky/sky1.bmp",
                    "data/sky/sky2.bmp",
                    "data/sky/sky3.bmp",
                    "data/sky/sky4.bmp",
                    "data/sky/sky5.bmp",
                }
            }, 4);
        }
    }
}