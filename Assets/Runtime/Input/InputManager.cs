using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityXOPS
{
    public class InputManager : Singleton<InputManager>
    {
        [SerializeField] private InputActionAsset inputActions;
        
        private Dictionary<string, InputActionWrapper> _actionWrappers = new Dictionary<string, InputActionWrapper>();
        
        [SerializeField] private Vector2 moveInput;
        public Vector2 MoveInput => moveInput;
        [SerializeField] private Vector2 lookInput;
        public Vector2 LookInput => lookInput;
        [SerializeField] private bool fireInput;
        public bool FireInput => fireInput;
        [SerializeField] private bool zoomInput;
        public bool ZoomInput => zoomInput;
        [SerializeField] private bool primaryWeaponInput;
        public bool PrimaryWeaponInput => primaryWeaponInput;
        [SerializeField] private bool secondaryWeaponInput;
        public bool SecondaryWeaponInput => secondaryWeaponInput;
        [SerializeField] private bool jumpInput;
        public bool JumpInput => jumpInput;
        [SerializeField] private bool interactInput;
        public bool InteractInput => interactInput;
        [SerializeField] private bool throwInput;
        public bool ThrowInput => throwInput;
        [SerializeField] private bool reloadInput;
        public bool ReloadInput => reloadInput;
        [SerializeField] private bool previousInput;
        public bool PreviousInput => previousInput;
        [SerializeField] private bool nextInput;
        public bool NextInput => nextInput;
        [SerializeField] private bool walkInput;
        public bool WalkInput => walkInput;

        protected override void Awake()
        {
            base.Awake();
            InitializeActions();
        }

        private void InitializeActions()
        {
            if (inputActions == null) return;

            // Vector2
            _actionWrappers["Move"] = new InputActionWrapper(
                inputActions.FindAction("Player/Move"),
                ctx => moveInput = ctx.ReadValue<Vector2>(),
                ctx => moveInput = Vector2.zero
            );
            
            _actionWrappers["Look"] = new InputActionWrapper(
                inputActions.FindAction("Player/Look"),
                ctx => lookInput = ctx.ReadValue<Vector2>(),
                ctx => lookInput = Vector2.zero
            );

            // Bool 
            var boolActions = new Dictionary<string, Action<bool>>
            {
                ["Fire"] = value => fireInput = value,
                ["Zoom"] = value => zoomInput = value,
                ["PrimaryWeapon"] = value => primaryWeaponInput = value,
                ["SecondaryWeapon"] = value => secondaryWeaponInput = value,
                ["Jump"] = value => jumpInput = value,
                ["Interact"] = value => interactInput = value,
                ["Throw"] = value => throwInput = value,
                ["Reload"] = value => reloadInput = value,
                ["Previous"] = value => previousInput = value,
                ["Next"] = value => nextInput = value,
                ["Walk"] = value => walkInput = value
            };

            foreach (var kvp in boolActions)
            {
                _actionWrappers[kvp.Key] = new InputActionWrapper(
                    inputActions.FindAction($"Player/{kvp.Key}"),
                    ctx => kvp.Value(true),
                    ctx => kvp.Value(false)
                );
            }
        }

        private void OnEnable()
        {
            foreach (var wrapper in _actionWrappers.Values)
            {
                wrapper.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (var wrapper in _actionWrappers.Values)
            {
                wrapper.Disable();
            }
        }

        public void ResetAllInputs()
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            fireInput = false;
            zoomInput = false;
            primaryWeaponInput = false;
            secondaryWeaponInput = false;
            jumpInput = false;
            interactInput = false;
            throwInput = false;
            reloadInput = false;
            previousInput = false;
            nextInput = false;
            walkInput = false;
        }
    }

    public class InputActionWrapper
    {
        private readonly InputAction _action;
        private readonly Action<InputAction.CallbackContext> _onPerformed;
        private readonly Action<InputAction.CallbackContext> _onCanceled;

        public InputActionWrapper(InputAction action, 
                                 Action<InputAction.CallbackContext> onPerformed, 
                                 Action<InputAction.CallbackContext> onCanceled)
        {
            _action = action;
            _onPerformed = onPerformed;
            _onCanceled = onCanceled;
        }

        public void Enable()
        {
            if (_action == null) return;
            
            _action.Enable();
            _action.performed += _onPerformed;
            _action.canceled += _onCanceled;
        }

        public void Disable()
        {
            if (_action == null) return;
            
            _action.Disable();
            _action.performed -= _onPerformed;
            _action.canceled -= _onCanceled;
        }
    }
}