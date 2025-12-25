using UnityEngine;

namespace UnityXOPS
{
    public class HumanCamera : MonoBehaviour
    {
        [SerializeField]
        private Transform tpsPivotRoot;
        [SerializeField]
        private Transform tpsCameraRoot;

        private float _tpsDistance;
        
        public void InitializeHumanCamera(HumanParameterSO humanSO)
        {
            _tpsDistance = humanSO.tpsCameraDistance;
            transform.localPosition = new Vector3(0, humanSO.viewportHeight, 0);
            tpsCameraRoot.parent.localPosition = new Vector3(0, 0, -_tpsDistance);
        }

        public void UpdateTPSCamera()
        {
            var hit = Physics.Raycast(transform.position, -transform.forward, out var hitInfo, 20,
                LayerMask.GetMask("Block"));
            tpsPivotRoot.localPosition = hit ? new Vector3(0, 0, -Mathf.Min(hitInfo.distance, _tpsDistance)) : new Vector3(0, 0, -_tpsDistance);
        }

        public Transform FPSRoot => transform;
        public Transform TPSRoot => tpsCameraRoot;
    }
}
