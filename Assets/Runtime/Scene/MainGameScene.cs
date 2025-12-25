using UnityEngine;

namespace UnityXOPS
{
    public class MainGameScene : MonoBehaviour
    {
        [SerializeField]
        private Transform originalCameraRoot;
        
        [SerializeField]
        private Camera mainCamera;

        [SerializeField]
        private Human currentHuman;

        [SerializeField]
        private bool tpsMode;
        
        private HumanParameterSO _humanSO;
        
        private void Start()
        {
            var tr = ParameterManager.Instance.MissionParameterSO.officialMissionParameterSOs[0];
            MapManager.Instance.LoadMap(tr);
            currentHuman = MapManager.Instance.Player;
            
            _humanSO = ParameterManager.Instance.HumanParameterSO;
        }

        private void Update()
        {
            UpdateCoreInput();
            UpdatePlayer();
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            //camera
            if (currentHuman == null)
            {
                mainCamera.transform.parent = originalCameraRoot;
                mainCamera.transform.localPosition = new Vector3(0, _humanSO.viewportHeight, 0);
                mainCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);
                return;
            }

            mainCamera.transform.parent = tpsMode ? currentHuman.GetTPSCameraRoot() : currentHuman.GetFPSCameraRoot();
            mainCamera.transform.localPosition = Vector3.zero;
            mainCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);

            if (tpsMode)
            {
                currentHuman.UpdateTPSCamera();
            }
        }

        private void UpdatePlayer()
        {
            currentHuman.Move(InputManager.Instance.MoveInput, InputManager.Instance.WalkInput);
            currentHuman.Look(InputManager.Instance.LookInput);
            currentHuman.Jump(InputManager.Instance.JumpInput);
        }

        private void UpdateCoreInput()
        {
            //tps toggle
            if (Input.GetKeyDown(KeyCode.F1))
            {
                tpsMode = !tpsMode;
            }
            
            //ui toggle
            
            //cheat: change human
            if (Input.GetKey(KeyCode.F8))
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    currentHuman = MapManager.Instance.PreviousHuman();
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    currentHuman = MapManager.Instance.NextHuman();
                }
            }
        }
    }
}
