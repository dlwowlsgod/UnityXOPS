using System;
using UnityEngine.InputSystem;

namespace UnityXOPS
{
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