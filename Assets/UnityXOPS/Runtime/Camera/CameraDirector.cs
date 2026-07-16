using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 카메라의 위치/회전을 제어하는 전역 디렉터 싱글톤.
    /// 즉시 이동(Snap/GoTo/LookAt), 증감(Translate/Rotate), 현재값 조회와 카메라 기준 벽 파묻힘 질의를 제공한다.
    /// </summary>
    public class CameraDirector : SingletonBehavior<CameraDirector>
    {
        // 벽 블라인드 프로브를 near-clip 사각형 바깥으로 더 밀어내는 여유값. 0 이면 정확히 가장자리.
        // 화면이 뚫려 보이는데 블라인드가 안 걸리는 사례가 남으면 이 값을 키운다.
        private const float k_blindEdgeInflate = 0f;

        private Camera m_camera;

        private Camera Cam
        {
            get
            {
                if (m_camera == null)
                {
                    m_camera = Camera.main;
                }
                return m_camera;
            }
        }

        /// <summary>
        /// 카메라가 벽 안에 파묻혔는지 상/하/좌/우 네 방향으로 검사한다 (원본 OpenXOPS gamemain.cpp:2973-3003 벽 블라인드).
        /// near-clip 사각형의 각 변 중점이 블록 내부인지 본다 — 화면이 실제로 뚫려 보이기 시작하는 경계가 near-clip 면이므로,
        /// 그 지점을 직접 짚어야 뚫림을 놓치지 않는다. 시야각/종횡비가 바뀌면 사각형도 따라가므로 스코프·화면비 대응이 자동이다.
        /// 플레이어가 없거나 죽었으면 전부 false 다(원본 hp > 0 조건).
        ///
        /// 원본과 다른 부분(의도적): 원본은 시선축에서 시야각의 1/4 만큼 돌린 거리 1.2 지점을 봤는데,
        /// 그건 시야 한참 안쪽이라 가장자리로 벽이 파고들면 화면이 뚫리는데도 안 걸린다. 실제로 그 증상이 확인돼
        /// near-clip 가장자리 기준으로 되돌렸다 — 원본보다 일찍/자주 걸리지만 뚫림이 없다. 되돌리지 말 것.
        /// </summary>
        /// <param name="top">위쪽이 벽 안이면 true</param>
        /// <param name="bottom">아래쪽이 벽 안이면 true</param>
        /// <param name="left">왼쪽이 벽 안이면 true</param>
        /// <param name="right">오른쪽이 벽 안이면 true</param>
        public void CheckWallBlind(out bool top, out bool bottom, out bool left, out bool right)
        {
            top = false;
            bottom = false;
            left = false;
            right = false;

            Human player = MapLoader.Player;
            if (Cam == null || player == null || !player.Alive)
            {
                return;
            }

            Transform t = Cam.transform;
            Vector3 camRight = t.right;
            Vector3 camUp = t.up;

            float near = Cam.nearClipPlane;
            float halfH = near * Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad) + k_blindEdgeInflate;
            float halfW = halfH * Cam.aspect;
            Vector3 nearCenter = t.position + t.forward * near;

            top = MapLoader.IsInsideBlock(nearCenter + camUp * halfH);
            bottom = MapLoader.IsInsideBlock(nearCenter - camUp * halfH);
            left = MapLoader.IsInsideBlock(nearCenter - camRight * halfW);
            right = MapLoader.IsInsideBlock(nearCenter + camRight * halfW);
        }

        /// <summary>
        /// 카메라를 플레이어 위치로 즉시 이동시킨다. 플레이어가 없으면 무시한다.
        /// </summary>
        public void SnapToPlayer()
        {
            Human player = MapLoader.Player;
            if (Cam != null && player != null)
            {
                Cam.transform.position = player.transform.position;
            }
        }

        /// <summary>
        /// 카메라를 지정 인덱스의 Human 위치로 즉시 이동시킨다. 범위를 벗어나면 무시한다.
        /// </summary>
        /// <param name="index">MapLoader.Humans 리스트 인덱스</param>
        public void SnapToHuman(int index)
        {
            var humans = MapLoader.Humans;
            if (Cam != null && humans != null && index >= 0 && index < humans.Count)
            {
                Cam.transform.position = humans[index].transform.position;
            }
        }

        /// <summary>
        /// 카메라를 지정 좌표로 즉시 이동시킨다.
        /// </summary>
        /// <param name="x">월드 X</param>
        /// <param name="y">월드 Y</param>
        /// <param name="z">월드 Z</param>
        public void GoTo(float x, float y, float z)
        {
            if (Cam != null)
            {
                Cam.transform.position = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// 카메라가 지정 좌표를 바라보도록 회전시킨다.
        /// </summary>
        /// <param name="x">바라볼 지점의 월드 X</param>
        /// <param name="y">바라볼 지점의 월드 Y</param>
        /// <param name="z">바라볼 지점의 월드 Z</param>
        public void LookAt(float x, float y, float z)
        {
            if (Cam != null)
            {
                Cam.transform.LookAt(new Vector3(x, y, z));
            }
        }

        /// <summary>
        /// 카메라 오일러 회전을 지정 각도로 즉시 설정한다.
        /// </summary>
        /// <param name="x">X(피치) 각도</param>
        /// <param name="y">Y(요) 각도</param>
        /// <param name="z">Z(롤) 각도</param>
        public void SetEuler(float x, float y, float z)
        {
            if (Cam != null)
            {
                Cam.transform.eulerAngles = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// 카메라 위치를 지정 벡터만큼 증감시킨다.
        /// </summary>
        /// <param name="dx">X 증감량</param>
        /// <param name="dy">Y 증감량</param>
        /// <param name="dz">Z 증감량</param>
        public void Translate(float dx, float dy, float dz)
        {
            if (Cam != null)
            {
                Cam.transform.position += new Vector3(dx, dy, dz);
            }
        }

        /// <summary>
        /// 카메라 오일러 회전을 지정 각도만큼 증감시킨다.
        /// </summary>
        /// <param name="dx">X(피치) 증감 각도</param>
        /// <param name="dy">Y(요) 증감 각도</param>
        /// <param name="dz">Z(롤) 증감 각도</param>
        public void Rotate(float dx, float dy, float dz)
        {
            if (Cam != null)
            {
                Cam.transform.eulerAngles += new Vector3(dx, dy, dz);
            }
        }

        /// <summary>
        /// 카메라 렌더링을 켜고 끈다. 씬 전환 직전에 꺼서 전환 중 무너지는 씬이 렌더되는 것을 막는 용도.
        /// 다음 씬의 카메라는 별개라 기본 on 상태이며, 파괴된 구 카메라는 다음 접근 시 자동 재참조된다.
        /// </summary>
        /// <param name="active">true면 렌더링 on, false면 off</param>
        public void SetActive(bool active)
        {
            if (Cam != null)
            {
                Cam.enabled = active;
            }
        }

        /// <summary>
        /// 카메라 시야각(FOV, degree)을 설정한다. 카메라가 없으면 무시한다.
        /// </summary>
        /// <param name="fieldOfView">수직 시야각(도).</param>
        public void SetFieldOfView(float fieldOfView)
        {
            if (Cam != null)
            {
                Cam.fieldOfView = fieldOfView;
            }
        }

        /// <summary>현재 카메라 위치의 X를 반환한다. 카메라가 없으면 0.</summary>
        public float GetX() => Cam != null ? Cam.transform.position.x : 0f;

        /// <summary>현재 카메라 위치의 Y를 반환한다. 카메라가 없으면 0.</summary>
        public float GetY() => Cam != null ? Cam.transform.position.y : 0f;

        /// <summary>현재 카메라 위치의 Z를 반환한다. 카메라가 없으면 0.</summary>
        public float GetZ() => Cam != null ? Cam.transform.position.z : 0f;

        /// <summary>현재 카메라 오일러 회전의 X(피치)를 반환한다. 카메라가 없으면 0.</summary>
        public float GetEulerX() => Cam != null ? Cam.transform.eulerAngles.x : 0f;

        /// <summary>현재 카메라 오일러 회전의 Y(요)를 반환한다. 카메라가 없으면 0.</summary>
        public float GetEulerY() => Cam != null ? Cam.transform.eulerAngles.y : 0f;

        /// <summary>현재 카메라 오일러 회전의 Z(롤)를 반환한다. 카메라가 없으면 0.</summary>
        public float GetEulerZ() => Cam != null ? Cam.transform.eulerAngles.z : 0f;
    }
}
