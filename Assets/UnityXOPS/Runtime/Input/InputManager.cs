using UnityEngine;
using UnityEngine.InputSystem;
using JJLUtility;
using System.IO;

namespace UnityXOPS
{
    /// <summary>
    /// JSON 바인딩 파일을 읽어 InputSystem 액션을 구성하고 키보드/마우스 입력을 관리하는 싱글톤 매니저.
    /// </summary>
    public class InputManager : SingletonBehavior<InputManager>
    {
        public InputAction Look { get; private set; }
        public InputAction Move { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Walk { get; private set; }
        public InputAction Drop { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction Zoom { get; private set; }
        public InputAction Previous { get; private set; }
        public InputAction Next { get; private set; }
        public InputAction Reload { get; private set; }
        public InputAction First { get; private set; }
        public InputAction Second { get; private set; }
        public InputAction Interact { get; private set; }

        private InputActionMap m_map;

        private const string k_bindingsPath = "unitydata/input_bindings.json";

        public static Keyboard Keyboard {  get; private set; }
        public static Mouse Mouse { get; private set; }

        private bool m_hideInWindow;

        private void Start()
        {
            Keyboard = Keyboard.current;
            Mouse = Mouse.current;

            string fullPath = SafePath.Combine(Application.streamingAssetsPath, k_bindingsPath);
            string json = File.ReadAllText(fullPath);
            var binding = JsonUtility.FromJson<InputBindingData>(json);

            m_map = new InputActionMap("XOPS");

            Look = m_map.AddAction("Look", InputActionType.PassThrough);
            Look.AddBinding(binding.look);
            Look.AddCompositeBinding("2DVector")
                .With("Up", binding.lookUp)
                .With("Down", binding.lookDown)
                .With("Left", binding.lookLeft)
                .With("Right", binding.lookRight);

            Move = m_map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", binding.moveForward)
                .With("Down", binding.moveBackward)
                .With("Left", binding.moveLeft)
                .With("Right", binding.moveRight);

            Jump = m_map.AddAction("Jump");
            Jump.AddBinding(binding.jump);

            Walk = m_map.AddAction("Walk");
            Walk.AddBinding(binding.walk);

            Drop = m_map.AddAction("Drop");
            Drop.AddBinding(binding.drop);

            Fire = m_map.AddAction("Fire");
            Fire.AddBinding(binding.fire);

            Zoom = m_map.AddAction("Zoom");
            Zoom.AddBinding(binding.zoom);

            Previous = m_map.AddAction("Previous");
            Previous.AddBinding(binding.previous);

            Next = m_map.AddAction("Next");
            Next.AddBinding(binding.next);

            Reload = m_map.AddAction("Reload");
            Reload.AddBinding(binding.reload);

            First = m_map.AddAction("First");
            First.AddBinding(binding.first);

            Second = m_map.AddAction("Second");
            Second.AddBinding(binding.second);

            Interact = m_map.AddAction("Interact");
            Interact.AddBinding(binding.interact);

            m_map.Enable();
        }

        private void Update()
        {
            if (m_hideInWindow && Mouse != null)
            {
                var pos = Mouse.position.ReadValue();
                bool inside = pos.x >= 0 && pos.x < Screen.width
                           && pos.y >= 0 && pos.y < Screen.height;
                Cursor.visible = !inside;
            }

#if UNITY_EDITOR
            UpdateDebugValues();
#endif
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
        [SerializeField] private bool interactValue;

        /// <summary>
        /// 에디터 인스펙터에서 입력값을 실시간으로 확인할 수 있도록 직렬화 필드를 갱신한다.
        /// </summary>
        private void UpdateDebugValues()
        {
            if (m_map == null) return;

            lookValue = Look.ReadValue<Vector2>();
            moveValue = Move.ReadValue<Vector2>();
            jumpValue = Jump.IsPressed();
            walkValue = Walk.IsPressed();
            dropValue = Drop.IsPressed();
            fireValue = Fire.IsPressed();
            zoomValue = Zoom.IsPressed();
            previousValue = Previous.IsPressed();
            nextValue = Next.IsPressed();
            reloadValue = Reload.IsPressed();
            firstValue = First.IsPressed();
            secondValue = Second.IsPressed();
            interactValue = Interact.IsPressed();
        }
#endif
        private void OnDestroy()
        {
            m_map?.Disable();
            m_map?.Dispose();
        }

        /// <summary>
        /// 마우스 커서 표시 여부, 잠금 모드, 화면 중앙 이동 여부를 설정한다.
        /// </summary>
        /// <param name="hideInWindow">true면 창 안에 마우스가 있을 때만 커서를 숨긴다.</param>
        /// <param name="centered">true면 커서를 중앙에 고정(Locked)한다.</param>
        /// <param name="moveToCenter">true면 커서를 즉시 화면 중앙으로 이동시킨다.</param>
        public static void MouseCursorMode(bool hideInWindow, bool centered, bool moveToCenter)
        {
            Instance.m_hideInWindow = hideInWindow;
            if (!hideInWindow)
            {
                Cursor.visible = true;
            }

            if (moveToCenter && Mouse != null)
            {
                Mouse.WarpCursorPosition(new Vector2(Screen.width / 2, Screen.height / 2));
            }

            if (centered)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}
