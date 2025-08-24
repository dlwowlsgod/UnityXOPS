using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 게임의 진행 시간을 기록하는 클래스입니다.
    /// </summary>
    public class Clock : Singleton<Clock>
    {
        [SerializeField]
        private bool pause;
        [SerializeField]
        private float process;
        
        public float Process => process;

        private void Update()
        {
            if (pause)
            {
                return;
            }
            
            process += Time.deltaTime;
        }

        /// <summary>
        /// 시간을 정지합니다. 실제 유니티 엔진의 프로세스가 정지하지는 않습니다.
        /// </summary>
        public void PauseClock()
        {
            pause = true;
        }

        /// <summary>
        /// 시간을 다시 흐르게 합니다.
        /// </summary>
        public void PlayClock()
        {
            pause = false;
        }

        /// <summary>
        /// 시간을 초기화합니다.
        /// </summary>
        public void ResetClock()
        {
            process = 0f;
        }
    }
}