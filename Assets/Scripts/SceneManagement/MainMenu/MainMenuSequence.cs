using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    public class MainMenuSequence : MonoBehaviour
    {
        private void Start()
        {
            if (StateMachine.Instance.CurrentState == GameState.MainMenuStart)
            {
                var optionChosen = Random.Range(0, ParameterManager.Instance.demoParameters.Count);
                var option = ParameterManager.Instance.demoParameters[optionChosen];
                var bd1Path = Path.Combine(Application.streamingAssetsPath, option.bd1Path);
                BlockDataReader.Instance.ReadBD1(bd1Path);
                BlockDataLoader.Instance.LoadBD1(bd1Path);
                SkyLoader.Instance.LoadSky(option.skyIndex);
                
                StateMachine.Instance.NextState();
            }
        }
    }
}