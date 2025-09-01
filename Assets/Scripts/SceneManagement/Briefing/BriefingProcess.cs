using UnityEngine;

namespace UnityXOPS
{
    public class BriefingProcess : MonoBehaviour
    {
        private void Start()
        {
            if (StateMachine.Instance.CurrentState == GameState.BriefingStart)
            {
                StateMachine.Instance.NextState();
            }
        }

        private void Update()
        {
            if (StateMachine.Instance.CurrentState == GameState.BriefingUpdate)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    MissionLoader.Instance.ClearMission();
                    StateMachine.Instance.NextState(true, false);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    StateMachine.Instance.NextState(false, false);   
                }
            }

            if (StateMachine.Instance.CurrentState == GameState.BriefingEnd)
            {
                StateMachine.Instance.NextState();
            }
        }
    }
}