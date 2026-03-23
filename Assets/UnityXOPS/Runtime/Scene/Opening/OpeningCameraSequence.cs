using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 씬에서 카메라의 이동과 회전 애니메이션을 제어하는 컴포넌트.
    /// </summary>
    public class OpeningCameraSequence : MonoBehaviour
    {
        [SerializeField]
        private Camera mainCamera;

        private Vector3 m_addPos;
        private Vector3 m_addEuler;
        private float m_time;
        private OpeningCameraData m_data;

        /// <summary>
        /// 카메라 초기 위치와 회전값을 설정하고 애니메이션 상태를 초기화한다.
        /// </summary>
        private void Start()
        {
            m_time = Time.time;
            m_data = GetComponent<OpeningScene>().OpeningData.openingCameraData;

            mainCamera.transform.position = m_data.initialPosition;
            mainCamera.transform.eulerAngles = m_data.initialEuler;
            m_addPos = Vector3.zero;
            m_addEuler = Vector3.zero;
        }

        /// <summary>
        /// 매 프레임마다 카메라의 위치와 회전 오프셋을 갱신한다.
        /// </summary>
        private void Update()
        {
            float t = Time.time - m_time;
            float dt = Time.deltaTime;

            UpdateAxis(ref m_addPos, m_data.posAnim, t, dt);
            UpdateAxis(ref m_addEuler, m_data.rotAnim, t, dt);

            mainCamera.transform.position += m_addPos * dt;
            mainCamera.transform.eulerAngles += m_addEuler * dt;
        }

        /// <summary>
        /// 경과 시간에 따라 가속·등속·감쇠 구간을 적용하여 단일 축의 속도 벡터를 갱신한다.
        /// </summary>
        void UpdateAxis(ref Vector3 add, OpeningCameraAnimation anim, float t, float dt)
        {
            // 감쇠 계수: 원본의 0.8/frame을 프레임레이트 독립으로 변환
            float decay = Mathf.Pow(anim.smoothFactor, dt * 33.333f);

            if (t < anim.accelStart)
            {
                add = Vector3.zero;
            }
            else if (t < anim.accelEnd)
            {
                // 지수 평활: add += (target - add) / 5  →  Lerp with decay
                add = Vector3.Lerp(anim.targetAdd, add, decay);
            }
            else if (anim.constantEnd < 0f || t < anim.constantEnd)
            {
                // 등속 구간
                add = anim.targetAdd;
            }
            else
            {
                // 감쇠 구간: add *= 0.8/frame
                add *= decay;
            }
        }
    }
}
