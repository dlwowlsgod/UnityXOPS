using UnityEngine;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 카메라의 위치/회전을 제어하는 전역 디렉터 싱글톤.
    /// 즉시 이동(Snap/GoTo/LookAt), 증감(Translate/Rotate), 현재값 조회를 제공한다.
    /// </summary>
    public class CameraDirector : SingletonBehavior<CameraDirector>
    {
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
