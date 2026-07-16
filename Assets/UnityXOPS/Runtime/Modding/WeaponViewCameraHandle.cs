using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// 무기 뷰를 비추는 카메라를 제어하는 핸들. 무기를 어떤 구도로 담을지(어디서·어느 방향으로·얼마나 넓게)를 정한다.
    /// 무기 자리를 옮기는 것과 카메라를 옮기는 것은 같은 결과를 낼 수 있으니 편한 쪽을 쓰면 되고,
    /// 매 프레임 위치/회전을 바꾸면 무기 주위를 도는 연출도 만들 수 있다.
    /// 무엇을 어떻게 그릴지(대상 레이어·출력 텍스처·잘림 거리)는 엔진이 정하며 여기서 건드릴 수 없다.
    /// </summary>
    [LuaCallCSharp]
    public class WeaponViewCameraHandle
    {
        private readonly Camera m_camera;

        /// <summary>
        /// 핸들을 생성한다.
        /// </summary>
        /// <param name="camera">대상 카메라</param>
        public WeaponViewCameraHandle(Camera camera)
        {
            m_camera = camera;
        }

        /// <summary>
        /// 카메라 위치를 설정한다. 무기 자리는 원점 근처에 있으므로 거기서 떨어진 위치를 준다(기본 z = -10).
        /// </summary>
        /// <param name="x">X(오른쪽 +)</param>
        /// <param name="y">Y(위쪽 +)</param>
        /// <param name="z">Z(앞쪽 +)</param>
        public void SetPosition(float x, float y, float z)
        {
            if (m_camera != null)
            {
                m_camera.transform.localPosition = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// 카메라 회전을 설정한다(도).
        /// </summary>
        /// <param name="x">X(피치) 각도</param>
        /// <param name="y">Y(요) 각도</param>
        /// <param name="z">Z(롤) 각도</param>
        public void SetRotation(float x, float y, float z)
        {
            if (m_camera != null)
            {
                m_camera.transform.localRotation = Quaternion.Euler(x, y, z);
            }
        }

        /// <summary>
        /// 시야각을 설정한다. 좁힐수록 무기가 크게(멀리서 당겨 찍은 느낌) 잡힌다.
        /// </summary>
        /// <param name="degrees">수직 시야각(도). 기본 65</param>
        public void SetFieldOfView(float degrees)
        {
            if (m_camera != null)
            {
                m_camera.fieldOfView = degrees;
            }
        }

        /// <summary>
        /// 무기 뒤에 깔리는 배경색을 설정한다.
        /// 기본은 완전 투명(알파 0)이라 무기 뷰 뒤의 다른 UI가 비쳐 보인다 — 알파를 올리면 그것들이 가려진다.
        /// </summary>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1). 0이면 뒤가 비친다</param>
        public void SetBackgroundColor(float r, float g, float b, float a)
        {
            if (m_camera != null)
            {
                m_camera.backgroundColor = new Color(r, g, b, a);
            }
        }
    }
}
