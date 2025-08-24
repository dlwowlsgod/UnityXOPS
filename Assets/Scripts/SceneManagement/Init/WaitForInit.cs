using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 게임이 실행될 때 스크립트 실행 순서의 충돌을 막기 위해 먼저 실행되어야 할 스크립트를 나열하는 클래스입니다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WaitForInit : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Initialize());
        }

        /// <summary>
        /// 게임이 실행될 때 초기화해야 할 부분을 전부 초기화합니다. 이후 1프레임 기다린 후 게임이 정상 실행됩니다.
        /// </summary>
        /// <returns>코루틴 <see cref="IEnumerator">IEnumerator</see></returns>
        private IEnumerator Initialize()
        {
            ProfileManager.Instance.LoadProfile();
            ParameterManager.Instance.LoadParameters();
            
            yield return null;
            
            StateMachine.Instance.NextState(false, false);
        }
    }
}