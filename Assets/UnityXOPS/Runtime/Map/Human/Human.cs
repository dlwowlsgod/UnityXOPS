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
        private Transform cameraRoot;
        public Transform CameraRoot => cameraRoot;

        [SerializeField]
        private HumanVisual humanVisual;
        public HumanVisual HumanVisual => humanVisual;

        [SerializeField] private HumanHitbox headHitbox;
        [SerializeField] private HumanHitbox bodyHitbox;
        [SerializeField] private HumanHitbox legHitbox;

        private HumanData m_humanData;
        private HumanTypeData m_humanTypeData;
        public HumanTypeData HumanTypeData => m_humanTypeData;

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

                int typeIndex = m_humanData.typeIndex;
                if (typeIndex >= 0 && typeIndex < humanParamData.humanTypeData.Count)
                {
                    m_humanTypeData = humanParamData.humanTypeData[typeIndex];
                }
            }

            humanVisual.CreateHumanVisual(m_humanData);

            var general = humanParamData.humanGeneralData;
            if (headHitbox != null) headHitbox.ApplySize(general);
            if (bodyHitbox != null) bodyHitbox.ApplySize(general);
            if (legHitbox  != null) legHitbox .ApplySize(general);

            float cameraAttachPosition = general.cameraAttachPosition;
            cameraRoot.localPosition = new Vector3(0, cameraAttachPosition, 0);

            hp = m_humanData.hp;
            team = m_humanDataParam.param2;
            alive = hp > 0;
        }
    }
}
