using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    public class Human : Point
    {
        [SerializeField]
        private HumanMotor humanMotor;
        [SerializeField]
        private HumanVisual humanVisual;
        [SerializeField]
        private HumanCamera humanCamera;
        
        [SerializeField]
        private HumanDataParameterSO humanDataSO;
        [SerializeField]
        private HumanTypeParameterSO humanTypeSO;
        [SerializeField]
        private HumanVisualParameterSO humanVisualSO;

        [SerializeField]
        private int hp;
        [SerializeField]
        private bool alive;

        private HumanParameterSO _humanSO;

        private float _xRot = 0f;
        private void Awake()
        {
            _humanSO = ParameterManager.Instance.HumanParameterSO;
        }

        public void InitializeHuman(int p0, int p1, int p2)
        {
            var dataSOs = _humanSO.humanDataParameterSOs;
            var typeSOs = _humanSO.humanTypeParameterSOs;
            var visualSOs = _humanSO.humanVisualParameterSOs;
            
            //p0 = humanParameter, p1 = path, p2 = humanIdentifyNumber
            param0 = p0;
            param1 = p1;
            param2 = p2;

            humanDataSO = dataSOs[MapManager.Instance.HumanParameter.GetValueOrDefault(param0, 0)];
            humanTypeSO = typeSOs[humanDataSO.typeIndex < 0 || humanDataSO.typeIndex >= typeSOs.Length ? 0 : humanDataSO.typeIndex];
            humanVisualSO = visualSOs[humanDataSO.visualIndex < 0 || humanDataSO.visualIndex >= visualSOs.Length ? 0 : humanDataSO.visualIndex];
            hp = humanDataSO.hp;
            alive = hp > 0;
            
            humanMotor.InitializeHumanMotor(_humanSO, humanTypeSO);
            humanVisual.InitializeHumanVisual(_humanSO, humanVisualSO);
            humanCamera.InitializeHumanCamera(_humanSO);

            name = $"{humanDataSO.name} ({param0}:{param1}:{param2})";
        }

        public Transform GetFPSCameraRoot() => humanCamera.FPSRoot;
        public Transform GetTPSCameraRoot() => humanCamera.TPSRoot;
        
        public void UpdateTPSCamera() 
            => humanCamera.UpdateTPSCamera();

        public void Move(Vector2 moveVector, bool walk) => humanMotor.UpdateMoveInput(moveVector, walk);

        public void Look(Vector2 lookVector)
        {
            var transformX = GetFPSCameraRoot();
            var mouseY = lookVector.y * 10f; //sensitivity needs to pull out
            _xRot += mouseY * Time.deltaTime;
            _xRot = Mathf.Clamp(_xRot, -70f, 70f);
            transformX.localRotation = Quaternion.Euler(_xRot, 0f, 0f);
            
            humanMotor.UpdateLookInput(lookVector);
        }
        public void Jump(bool jump) => humanMotor.UpdateJumpInput(jump);
    }
}