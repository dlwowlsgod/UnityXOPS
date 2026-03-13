using UnityEngine;
using JJLUtility;
using System.Collections.Generic;

namespace UnityXOPS
{
    public class Test : MonoBehaviour
    {
        private void Start()
        {
            MapLoader.LoadMissionData(0, false);
        }
    }
}