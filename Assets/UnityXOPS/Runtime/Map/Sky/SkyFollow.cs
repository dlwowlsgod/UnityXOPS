using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 매 LateUpdate마다 Camera.main의 월드 위치로 자신의 위치를 동기화한다.
    /// SkyMesh 쉐이더는 _WorldSpaceCameraPos + vertex로 렌더링하므로 화면 표시에는 트랜스폼이 영향을 주지 않지만,
    /// Unity 프러스텀 컬링은 Renderer.bounds(= transform 기반)를 사용하므로 skyRoot가 카메라에서 멀어지면 컬링돼 사라진다.
    /// </summary>
    public class SkyFollow : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            transform.position = cam.transform.position;
        }
    }
}
