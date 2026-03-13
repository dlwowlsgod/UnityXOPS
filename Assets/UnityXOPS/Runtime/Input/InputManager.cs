using UnityEngine;
using UnityEngine.InputSystem;
using JJLUtility;
using System.IO;

namespace UnityXOPS
{
    public class InputManager : SingletonBehavior<InputManager>
    {
        public InputAction Look     { get; private set; }
        public InputAction Move     { get; private set; }
        public InputAction Jump     { get; private set; }
        public InputAction Walk     { get; private set; }
        public InputAction Drop     { get; private set; }
        public InputAction Fire     { get; private set; }
        public InputAction Zoom     { get; private set; }
        public InputAction Previous { get; private set; }
        public InputAction Next     { get; private set; }
        public InputAction Reload   { get; private set; }
        public InputAction First    { get; private set; }
        public InputAction Second   { get; private set; }

        private InputActionMap m_map;

        private const string bindingsPath = "unitydata/input_bindings.json";

        private void Start()
        {
            string fullPath = SafePath.Combine(Application.streamingAssetsPath, bindingsPath);
            string json = File.ReadAllText(fullPath);
            var binding = JsonUtility.FromJson<InputBindingData>(json);

            m_map = new InputActionMap("XOPS");

            Look = m_map.AddAction("Look", InputActionType.PassThrough);
            Look.AddBinding(binding.look);
            Look.AddCompositeBinding("2DVector")
                .With("Up",    binding.lookUp)
                .With("Down",  binding.lookDown)
                .With("Left",  binding.lookLeft)
                .With("Right", binding.lookRight);

            Move = m_map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up",    binding.moveForward)
                .With("Down",  binding.moveBackward)
                .With("Left",  binding.moveLeft)
                .With("Right", binding.moveRight);

            Jump     = m_map.AddAction("Jump");     Jump.AddBinding(binding.jump);
            Walk     = m_map.AddAction("Walk");     Walk.AddBinding(binding.walk);
            Drop     = m_map.AddAction("Drop");     Drop.AddBinding(binding.drop);
            Fire     = m_map.AddAction("Fire");     Fire.AddBinding(binding.fire);
            Zoom     = m_map.AddAction("Zoom");     Zoom.AddBinding(binding.zoom);
            Previous = m_map.AddAction("Previous"); Previous.AddBinding(binding.previous);
            Next     = m_map.AddAction("Next");     Next.AddBinding(binding.next);
            Reload   = m_map.AddAction("Reload");   Reload.AddBinding(binding.reload);
            First    = m_map.AddAction("First");    First.AddBinding(binding.first);
            Second   = m_map.AddAction("Second");   Second.AddBinding(binding.second);

            m_map.Enable();
        }

#if UNITY_EDITOR
        [SerializeField] private Vector2 lookValue;
        [SerializeField] private Vector2 moveValue;
        [SerializeField] private bool jumpValue;
        [SerializeField] private bool walkValue;
        [SerializeField] private bool dropValue;
        [SerializeField] private bool fireValue;
        [SerializeField] private bool zoomValue;
        [SerializeField] private bool previousValue;
        [SerializeField] private bool nextValue;
        [SerializeField] private bool reloadValue;
        [SerializeField] private bool firstValue;
        [SerializeField] private bool secondValue;

        private void Update()
        {
            if (m_map == null) return;

            lookValue     = Look.ReadValue<Vector2>();
            moveValue     = Move.ReadValue<Vector2>();
            jumpValue     = Jump.IsPressed();
            walkValue     = Walk.IsPressed();
            dropValue     = Drop.IsPressed();
            fireValue     = Fire.IsPressed();
            zoomValue     = Zoom.IsPressed();
            previousValue = Previous.IsPressed();
            nextValue     = Next.IsPressed();
            reloadValue   = Reload.IsPressed();
            firstValue    = First.IsPressed();
            secondValue   = Second.IsPressed();
        }

#endif
        private void OnDestroy()
        {
            m_map?.Disable();
            m_map?.Dispose();
        }
    }
}
