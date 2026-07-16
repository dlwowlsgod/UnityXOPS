using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private CameraAPI m_camera;
        public CameraAPI Camera => m_camera ??= new CameraAPI(m_luaEnv);
    }

    /// <summary>
    /// 모드에 메인 카메라 제어(즉시 이동/증감/조회)를 제공하는 API 그룹.
    /// Lua에서는 XOPS.Camera 로 접근한다. 좌표는 모두 Unity 월드 좌표다.
    /// </summary>
    [LuaCallCSharp]
    public class CameraAPI
    {
        private readonly LuaEnv m_luaEnv;

        /// <summary>
        /// 카메라 API 그룹을 생성한다.
        /// </summary>
        /// <param name="luaEnv">조회 결과 테이블 생성에 사용할 LuaEnv.</param>
        public CameraAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }

        /// <summary>
        /// 카메라가 벽 안에 파묻혔는지 상/하/좌/우 네 방향으로 검사한다.
        /// 시선축에서 시야각의 1/4 만큼 돌린 방향의 코앞 지점이 벽 속인지 보므로, 벽이 화면 가운데 쪽으로
        /// 꽤 파고들어야 걸린다(가장자리만 살짝 닿는 정도로는 안 걸린다). 스코프로 시야각이 좁아지면 함께 좁아진다.
        /// 걸린 방향의 화면 절반을 검게 덮으면 벽 너머가 비쳐 보이는 것을 가릴 수 있다.
        /// </summary>
        /// <returns>{ top, bottom, left, right } — 그 방향이 벽 속이면 true. 플레이어가 없거나 죽었으면 전부 false.</returns>
        public LuaTable GetWallBlind()
        {
            CameraDirector.Instance.CheckWallBlind(out bool top, out bool bottom, out bool left, out bool right);

            LuaTable t = m_luaEnv.NewTable();
            t.Set("top", top);
            t.Set("bottom", bottom);
            t.Set("left", left);
            t.Set("right", right);
            return t;
        }

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

        /// <summary>
        /// 카메라를 지정 화면비로 레터박스/필러박스 한다. 화면이 콘텐츠보다 넓으면 좌우, 좁으면 상하에 검은 띠가 생긴다.
        /// 예: SetAspectRatio(4, 3)이면 16:9 화면에서 좌우 띠. width/height가 화면비와 같으면 꽉 채운다.
        /// </summary>
        /// <param name="width">콘텐츠 가로 비(예: 4, 16). 0 이하면 레터박스 해제</param>
        /// <param name="height">콘텐츠 세로 비(예: 3, 9). 0 이하면 레터박스 해제</param>
        public void SetAspectRatio(float width, float height)
        {
            LetterboxController.Instance.SetTargetAspect((width > 0f && height > 0f) ? width / height : 0f);
        }

        /// <summary>
        /// 레터박스를 해제하고 화면을 꽉 채운다.
        /// </summary>
        public void ResetAspectRatio()
        {
            LetterboxController.Instance.SetTargetAspect(0f);
        }

        /// <summary>현재 뷰포트(레터박스 후)의 정규화 X(0~1, 화면 좌측 기준). UI를 게임 영역에 맞출 때 쓴다.</summary>
        public float GetViewportX() => LetterboxController.Instance.Viewport.x;

        /// <summary>현재 뷰포트의 정규화 Y(0~1, 화면 하단 기준).</summary>
        public float GetViewportY() => LetterboxController.Instance.Viewport.y;

        /// <summary>현재 뷰포트의 정규화 폭(0~1).</summary>
        public float GetViewportWidth() => LetterboxController.Instance.Viewport.width;

        /// <summary>현재 뷰포트의 정규화 높이(0~1).</summary>
        public float GetViewportHeight() => LetterboxController.Instance.Viewport.height;

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
