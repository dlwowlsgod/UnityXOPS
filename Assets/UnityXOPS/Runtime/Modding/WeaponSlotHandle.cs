using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// 무기 뷰에 놓인 3D 무기 한 자리를 제어하는 핸들.
    /// 무기 모델을 만들고 비추는 일은 엔진이 하고, 이 핸들은 그 모델을 어디에 · 얼마나 크게 · 어느 방향으로
    /// 둘지만 정한다. 매 프레임 회전을 조금씩 바꾸면 무기가 도는 연출이 된다.
    /// 무기 뷰 카메라는 원점 쪽을 비추므로 위치는 원점 근처 값을 쓴다.
    /// </summary>
    [LuaCallCSharp]
    public class WeaponSlotHandle
    {
        private readonly Transform m_transform;

        /// <summary>
        /// 핸들을 생성한다.
        /// </summary>
        /// <param name="transform">대상 자리 Transform</param>
        public WeaponSlotHandle(Transform transform)
        {
            m_transform = transform;
        }

        /// <summary>
        /// 자리 위치를 설정한다.
        /// </summary>
        /// <param name="x">X(오른쪽 +)</param>
        /// <param name="y">Y(위쪽 +)</param>
        /// <param name="z">Z(앞쪽 +)</param>
        public void SetPosition(float x, float y, float z)
        {
            if (m_transform != null)
            {
                m_transform.localPosition = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// 자리 크기 배율을 설정한다. 무기마다 정해진 고유 크기에 이 값이 곱해진다.
        /// </summary>
        /// <param name="scale">크기 배율(1=기본)</param>
        public void SetScale(float scale)
        {
            if (m_transform != null)
            {
                m_transform.localScale = Vector3.one * scale;
            }
        }

        /// <summary>
        /// 자리 회전을 설정한다(도). 매 프레임 y를 조금씩 늘리면 무기가 빙글빙글 돈다.
        /// </summary>
        /// <param name="x">X(피치) 각도</param>
        /// <param name="y">Y(요) 각도</param>
        /// <param name="z">Z(롤) 각도</param>
        public void SetRotation(float x, float y, float z)
        {
            if (m_transform != null)
            {
                m_transform.localRotation = Quaternion.Euler(x, y, z);
            }
        }
    }
}
