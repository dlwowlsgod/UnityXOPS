using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 맵에 배치된 인간 캐릭터의 데이터와 시각 표현을 관리하는 컴포넌트.
    /// </summary>
    public class Human : MonoBehaviour
    {
        [SerializeField]
        private float hp;
        public float HP => hp;

        [SerializeField]
        private int team;
        public int Team => team;

        [SerializeField]
        private bool alive;
        public bool Alive => alive;

        [SerializeField]
        private HumanVisual humanVisual;
        public HumanVisual HumanVisual => humanVisual;

        private HumanData m_humanData;
        private RawPointData m_humanParam, m_humanDataParam;

        /// <summary>
        /// 포인트 데이터와 파라미터로부터 인간 캐릭터를 생성 및 초기화한다.
        /// </summary>
        /// <param name="humanParam">인간 배치 포인트 데이터.</param>
        /// <param name="humanDataParam">인간 파라미터 포인트 데이터.</param>
        public void CreateHuman(RawPointData humanParam, RawPointData humanDataParam)
        {
            m_humanParam = humanParam;
            m_humanDataParam = humanDataParam;

            var humanParamData = DataManager.Instance.HumanParameterData;
            int humanIndex = m_humanDataParam.param1;
            if (humanIndex >= 0 && humanIndex < humanParamData.humanData.Count)
            {
                m_humanData = humanParamData.humanData[humanIndex];
            }

            humanVisual.CreateHumanVisual(m_humanData);

            hp = m_humanData.hp;
            team = m_humanDataParam.param2;
            alive = hp > 0;
        }
    }
}
