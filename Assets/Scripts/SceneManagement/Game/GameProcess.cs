using UnityEngine;
using System.IO;

namespace UnityXOPS
{
    public class GameProcess : MonoBehaviour
    {
        private void Start()
        {
            if (StateMachine.Instance.CurrentState == GameState.GameStart)
            {
                var bd1Path = Path.Combine(Application.streamingAssetsPath, MissionLoader.Instance.bd1Path);
                if (File.Exists(bd1Path))
                {
                    BD1Reader.Instance.ReadBD1(bd1Path); 
                    BD1Loader.Instance.LoadBD1(bd1Path);
                }
                
                
                var pd1Path = Path.Combine(Application.streamingAssetsPath, MissionLoader.Instance.pd1Path);

                if (File.Exists(pd1Path))
                {
                    PD1Reader.Instance.ReadPD1(pd1Path);
                }
                
                SkyManager.Instance.LoadSky(MissionLoader.Instance.skyIndex);
                
                Clock.Instance.ResetClock();
                StateMachine.Instance.NextState();
            }
        }

        private void Update()
        {
            if (StateMachine.Instance.CurrentState == GameState.GameUpdate)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    BD1Loader.Instance.DestroyBD1();
                    BD1Reader.Instance.ClearBD1();
                    PD1Reader.Instance.ClearPD1();
                    SkyManager.Instance.DestroySky();
                    MissionLoader.Instance.ClearMission();
                    StateMachine.Instance.NextState(true, false);
                }

                if (Input.GetKeyDown(KeyCode.F12))
                {
                    BD1Loader.Instance.DestroyBD1();
                    BD1Reader.Instance.ClearBD1();
                    PD1Reader.Instance.ClearPD1();
                    SkyManager.Instance.DestroySky();
                    
                    StateMachine.Instance.NextState(false, true);   
                }
            }
            if (StateMachine.Instance.CurrentState == GameState.GameEnd)
            {
                StateMachine.Instance.NextState();
            }
        }
    }   
}