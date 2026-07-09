using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private CameraAPI m_camera;
        public CameraAPI Camera => m_camera ??= new CameraAPI();
    }

    /// <summary>
    /// 모드에 메인 카메라 제어(즉시 이동/증감/조회)를 제공하는 API 그룹.
    /// Lua에서는 XOPS.Camera 로 접근한다. 좌표는 모두 Unity 월드 좌표다.
    /// </summary>
    [LuaCallCSharp]
    public class CameraAPI
    {
        /// <summary>
        /// 카메라를 플레이어 위치로 즉시 이동시킨다.
        /// </summary>
        public void SnapToPlayer()
        {
            CameraDirector.Instance.SnapToPlayer();
        }

        /// <summary>
        /// 카메라를 지정 인덱스의 Human 위치로 즉시 이동시킨다.
        /// </summary>
        /// <param name="index">Human 리스트 인덱스</param>
        public void SnapToHuman(int index)
        {
            CameraDirector.Instance.SnapToHuman(index);
        }

        /// <summary>
        /// 카메라를 지정 좌표로 즉시 이동시킨다.
        /// </summary>
        /// <param name="x">월드 X</param>
        /// <param name="y">월드 Y</param>
        /// <param name="z">월드 Z</param>
        public void GoTo(float x, float y, float z)
        {
            CameraDirector.Instance.GoTo(x, y, z);
        }

        /// <summary>
        /// 카메라가 지정 좌표를 바라보도록 회전시킨다.
        /// </summary>
        /// <param name="x">바라볼 지점의 월드 X</param>
        /// <param name="y">바라볼 지점의 월드 Y</param>
        /// <param name="z">바라볼 지점의 월드 Z</param>
        public void LookAt(float x, float y, float z)
        {
            CameraDirector.Instance.LookAt(x, y, z);
        }

        /// <summary>
        /// 카메라 오일러 회전을 지정 각도로 즉시 설정한다.
        /// </summary>
        /// <param name="x">X(피치) 각도</param>
        /// <param name="y">Y(요) 각도</param>
        /// <param name="z">Z(롤) 각도</param>
        public void SetEuler(float x, float y, float z)
        {
            CameraDirector.Instance.SetEuler(x, y, z);
        }

        /// <summary>
        /// 카메라 위치를 지정 벡터만큼 증감시킨다. (매 프레임 증감 연출용)
        /// </summary>
        /// <param name="dx">X 증감량</param>
        /// <param name="dy">Y 증감량</param>
        /// <param name="dz">Z 증감량</param>
        public void Translate(float dx, float dy, float dz)
        {
            CameraDirector.Instance.Translate(dx, dy, dz);
        }

        /// <summary>
        /// 카메라 오일러 회전을 지정 각도만큼 증감시킨다. (매 프레임 증감 연출용)
        /// </summary>
        /// <param name="dx">X(피치) 증감 각도</param>
        /// <param name="dy">Y(요) 증감 각도</param>
        /// <param name="dz">Z(롤) 증감 각도</param>
        public void Rotate(float dx, float dy, float dz)
        {
            CameraDirector.Instance.Rotate(dx, dy, dz);
        }

        /// <summary>
        /// 카메라 시야각(FOV)을 설정한다.
        /// </summary>
        /// <param name="fieldOfView">수직 시야각(도)</param>
        public void SetFieldOfView(float fieldOfView)
        {
            CameraDirector.Instance.SetFieldOfView(fieldOfView);
        }

        /// <summary>현재 카메라 위치의 X를 반환한다.</summary>
        public float GetX() => CameraDirector.Instance.GetX();

        /// <summary>현재 카메라 위치의 Y를 반환한다.</summary>
        public float GetY() => CameraDirector.Instance.GetY();

        /// <summary>현재 카메라 위치의 Z를 반환한다.</summary>
        public float GetZ() => CameraDirector.Instance.GetZ();

        /// <summary>현재 카메라 오일러 회전의 X(피치)를 반환한다.</summary>
        public float GetEulerX() => CameraDirector.Instance.GetEulerX();

        /// <summary>현재 카메라 오일러 회전의 Y(요)를 반환한다.</summary>
        public float GetEulerY() => CameraDirector.Instance.GetEulerY();

        /// <summary>현재 카메라 오일러 회전의 Z(롤)를 반환한다.</summary>
        public float GetEulerZ() => CameraDirector.Instance.GetEulerZ();
    }
}
