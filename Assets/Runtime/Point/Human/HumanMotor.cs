using UnityEngine;
using KinematicCharacterController;

namespace UnityXOPS
{
    public class HumanMotor : MonoBehaviour, ICharacterController
    {
        private const float Gravity = -9.81f;
        private const float JumpHeight = 1.4f;
        
        private KinematicCharacterMotor _motor;
        
        private HumanTypeParameterSO _humanTypeSO;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _walkInput;

        private float _yRot = 0f;

        private bool _jumping;
        
        private void Awake()
        {
            _motor = GetComponent<KinematicCharacterMotor>();
            _motor.CharacterController = this;
            
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _jumpInput = false;
            _walkInput = false;
        }

        public void InitializeHumanMotor(HumanParameterSO humanSO, HumanTypeParameterSO humanTypeSO)
        {
            _yRot = transform.localEulerAngles.y;
            
            _motor.SetCapsuleDimensions(humanSO.controllerRadius, humanSO.controllerHeight, humanSO.controllerHeight / 2);
            
            _motor.MaxStableSlopeAngle = humanSO.slopeAngle;
            _motor.MaxStepHeight = humanSO.stepHeight;
            
            _humanTypeSO = humanTypeSO;
        }

        public void UpdateMoveInput(Vector2 moveInput, bool walkInput)
        {
            _moveInput = moveInput;
            _walkInput = walkInput;
        }
        
        public void UpdateLookInput(Vector2 lookInput)
        {
            _lookInput = lookInput;
            
        }
        
        public void UpdateJumpInput(bool jumpInput)
        {
            if (jumpInput)
            {
                _jumpInput = true;
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var mouseX = _lookInput.x * 10f; //sensitivity needs to pull out
            _yRot += mouseX * deltaTime;
            
            currentRotation = Quaternion.Euler(0f, _yRot, 0f);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.y < 0f)
                {
                    currentVelocity.y = 0f;
                }
                
                _jumping = false;

                if (_jumpInput && !_jumping)
                {
                    var jumpSpeed = Mathf.Sqrt(2f * JumpHeight * Mathf.Abs(Gravity));
                    currentVelocity.y = jumpSpeed;
                    
                    _motor.ForceUnground();

                    _jumping = true;
                }
            }
            else
            {
                currentVelocity.y += Gravity * deltaTime;
            }
            
            _jumpInput = false;

            Vector3 planarMove = Vector3.zero;
            Vector3 moveDirection;
            Vector3 worldSpaceMoveDirection;

            var humanSO = ParameterManager.Instance.HumanParameterSO;
            var humanScaleAverage = (humanSO.humanScale.x + humanSO.humanScale.y + humanSO.humanScale.z) / 3;
            
            if (_walkInput)
            {
                moveDirection = Vector3.forward;
                worldSpaceMoveDirection = _motor.TransientRotation * moveDirection;
                planarMove = worldSpaceMoveDirection * (_humanTypeSO.speed * humanScaleAverage);
            }

            else if (_moveInput != Vector2.zero)
            {
                //backward
                float speedMultiplier;
                if (_moveInput.y < 0)
                {
                    speedMultiplier = _humanTypeSO.runRegressSpeedMultiplier;
                }
                //forward
                else if (_moveInput.y > 0)
                {
                    //forward * sideways
                    if (Mathf.Abs(_moveInput.x) > 0.1f)
                    {
                        speedMultiplier = (_humanTypeSO.runProgressSpeedMultiplier + _humanTypeSO.runSidewaySpeedMultiplier) / 2;
                    }
                    //forward
                    else
                    {
                        speedMultiplier = _humanTypeSO.runProgressSpeedMultiplier;
                    }
                }
                //sideway
                else
                {
                    speedMultiplier = _humanTypeSO.runSidewaySpeedMultiplier;
                }
            
                var currentSpeed = _humanTypeSO.speed * speedMultiplier;
                moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
                worldSpaceMoveDirection = _motor.TransientRotation * moveDirection;
                planarMove = worldSpaceMoveDirection * (currentSpeed * humanScaleAverage);
            }

            currentVelocity.x = planarMove.x;
            currentVelocity.z = planarMove.z;
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (coll.isTrigger)
            {
                return false;
            }
            
            if (coll.gameObject.layer == LayerMask.NameToLayer("Block"))
            {
                return true;
            }

            return false;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            
        }
    }
}
