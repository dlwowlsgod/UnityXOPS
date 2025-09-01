using UnityEngine;
using System.IO;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝의 시간의 흐름에 따른 진행과정을 담당하는 클래스입니다.
    /// </summary>
    public class OpeningProcess : MonoBehaviour
    {
        [SerializeField]
        private string bd1Path;
        [SerializeField]
        private string pd1Path;
        [SerializeField]
        private int skyIndex;
        
        private void Start()
        {
            if (StateMachine.Instance.CurrentState == GameState.OpeningStart)
            {
                var bd1 = Path.Combine(Application.streamingAssetsPath, bd1Path);
                BD1Reader.Instance.ReadBD1(bd1);
                BD1Loader.Instance.LoadBD1(bd1);
                
                var pd1 = Path.Combine(Application.streamingAssetsPath, pd1Path);
                PD1Reader.Instance.ReadPD1(pd1);
                
                SkyManager.Instance.LoadSky(skyIndex);
                
                Clock.Instance.ResetClock();
                
                StateMachine.Instance.NextState(false, false);
            }
        }

        private void Update()
        {
            var clock = Clock.Instance.Process;
            if (StateMachine.Instance.CurrentState == GameState.OpeningUpdate)
            {
                if (Input.anyKeyDown)
                {
                    StateMachine.Instance.NextState(false, false);
                }
                if (clock >= 17.5f)
                {
                    StateMachine.Instance.NextState(false, false);
                }
            }

            if (StateMachine.Instance.CurrentState == GameState.OpeningEnd)
            {
                BD1Loader.Instance.DestroyBD1();
                BD1Reader.Instance.ClearBD1();
                
                PD1Reader.Instance.ClearPD1();
                
                SkyManager.Instance.DestroySky();
                
                StateMachine.Instance.NextState();
            }
        }
    }
}