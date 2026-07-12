using UnityEngine;
using UnityEngine.InputSystem;
using JJLUtility;
using UnityXOPS.Modding;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityXOPS
{
    /// <summary>
    /// ConfigManager가 소유한 바인딩 정의로 InputSystem 액션을 data-driven으로 구성하고 관리하는 싱글톤 매니저.
    /// 코어 액션은 타입 접근자로, 그 외 액션은 GetAction(name)으로 이름 조회한다.
    /// </summary>
    public class InputManager : SingletonBehavior<InputManager>
    {
        public InputAction Look => GetAction("look");
        public InputAction Move => GetAction("move");
        public InputAction Jump => GetAction("jump");
        public InputAction Walk => GetAction("walk");
        public InputAction Drop => GetAction("drop");
        public InputAction Fire => GetAction("fire");
        public InputAction Zoom => GetAction("zoom");
        public InputAction Previous => GetAction("previous");
        public InputAction Next => GetAction("next");
        public InputAction Reload => GetAction("reload");
        public InputAction First => GetAction("first");
        public InputAction Second => GetAction("second");
        public InputAction Interact => GetAction("interact");
        public InputAction Escape => GetAction("escape");

        private readonly Dictionary<string, InputAction> m_actions = new(StringComparer.OrdinalIgnoreCase);

        private InputActionMap m_map;

        private const string k_inputModPath = "unitydata/input/binding.lua";

        public static Keyboard Keyboard { get; private set; }
        public static Mouse Mouse { get; private set; }

        private bool m_hideInWindow;

        private readonly StringBuilder m_typedBuffer = new StringBuilder();
        private Keyboard m_subscribedKeyboard;

        private void Start()
        {
            Keyboard = Keyboard.current;
            Mouse = Mouse.current;
            // 텍스트 입력 구독. 시작 시 키보드가 없거나(지연 열거) 핫플러그로 바뀔 수 있어 디바이스 변경도 감시한다.
            SubscribeKeyboardText();
            InputSystem.onDeviceChange += OnInputDeviceChange;

            // 바인딩 데이터는 ConfigManager가 소유(config.json)한다. ConfigManager.Awake가 먼저 끝나 있으므로 안전하게 읽는다.
            m_map = new InputActionMap("XOPS");
            InputActionDefinition[] bindings = ConfigManager.Instance.Bindings;
            if (bindings != null)
            {
                foreach (InputActionDefinition def in bindings)
                {
                    BuildAction(def);
                }
            }

            m_map.Enable();

            // 모드 입력 바인딩 스크립트 로드. 맵 빌드+Enable 이후라야 RegisterAction이 정상 동작한다.
            LuaManager.Instance.LoadSandboxedFile(k_inputModPath, "input_binding");
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
        }

        private void LateUpdate()
        {
            // 타이핑 버퍼는 매 프레임 소비 후 비운다(onTextInput은 Update 이전에 채워지고 Lua가 Update에서 읽는다).
            if (m_typedBuffer.Length > 0)
            {
                m_typedBuffer.Clear();
            }
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            if (m_subscribedKeyboard != null)
            {
                m_subscribedKeyboard.onTextInput -= OnTextInput;
            }
            m_map?.Disable();
            m_map?.Dispose();
        }

        /// <summary>
        /// 입력 디바이스 변경 콜백. 키보드가 붙거나 바뀌면 최신 current로 텍스트 입력을 재구독한다.
        /// </summary>
        /// <param name="device">변경된 디바이스</param>
        /// <param name="change">변경 종류</param>
        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard)
            {
                Keyboard = Keyboard.current;
                SubscribeKeyboardText();
            }
            else if (device is Mouse)
            {
                Mouse = Mouse.current;
            }
        }

        /// <summary>
        /// 현재 키보드의 onTextInput에 구독한다. 같은 디바이스면 무시하고, 바뀌었으면 기존 구독을 떼고 새로 건다(중복/누수 방지).
        /// </summary>
        private void SubscribeKeyboardText()
        {
            if (m_subscribedKeyboard == Keyboard)
            {
                return;
            }
            if (m_subscribedKeyboard != null)
            {
                m_subscribedKeyboard.onTextInput -= OnTextInput;
            }
            m_subscribedKeyboard = Keyboard;
            if (m_subscribedKeyboard != null)
            {
                m_subscribedKeyboard.onTextInput += OnTextInput;
            }
        }

        /// <summary>
        /// 키보드 텍스트 입력 콜백. 이번 프레임 타이핑 문자를 버퍼에 모은다.
        /// </summary>
        /// <param name="c">입력된 문자</param>
        private void OnTextInput(char c)
        {
            m_typedBuffer.Append(c);
        }

        /// <summary>
        /// 이번 프레임에 타이핑된 텍스트를 반환한다. 커스텀 입력 필드가 글자를 받는 데 쓴다(LateUpdate에서 비워짐).
        /// </summary>
        /// <returns>이번 프레임 타이핑 문자열(없으면 빈 문자열)</returns>
        public string GetTypedText()
        {
            return m_typedBuffer.ToString();
        }

        /// <summary>
        /// 이번 프레임에 백스페이스 키가 눌렸는지 반환한다. 입력 필드의 글자 삭제에 쓴다.
        /// </summary>
        /// <returns>이번 프레임에 눌렸으면 true</returns>
        public bool WasBackspacePressed()
        {
            return Keyboard != null && Keyboard.backspaceKey.wasPressedThisFrame;
        }

        // 리바인드에서 제외할 예약 키(메뉴/치트·기능키). 리스닝 중 이 키들은 캡처하지 않는다.
        private static readonly HashSet<string> k_reservedBindKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "escape", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12",
        };

        /// <summary>
        /// 이번 프레임에 처음 눌린 키/버튼의 InputSystem 바인딩 경로를 반환한다. 키 리바인딩(리스닝 중 키 캡처)에 쓴다.
        /// 키보드 전체 키 + 마우스 좌/우/휠 버튼을 검사하되, 예약 키(Escape/F1~F12)는 건너뛴다.
        /// </summary>
        /// <returns>바인딩 경로(예 "&lt;Keyboard&gt;/w", "&lt;Mouse&gt;/leftButton"), 없으면 빈 문자열</returns>
        public string GetFirstPressedKeyPath()
        {
            if (Keyboard != null)
            {
                foreach (var key in Keyboard.allKeys)
                {
                    if (key.wasPressedThisFrame && !k_reservedBindKeys.Contains(key.name))
                    {
                        return "<Keyboard>/" + key.name;
                    }
                }
            }
            if (Mouse != null)
            {
                if (Mouse.leftButton.wasPressedThisFrame) return "<Mouse>/leftButton";
                if (Mouse.rightButton.wasPressedThisFrame) return "<Mouse>/rightButton";
                if (Mouse.middleButton.wasPressedThisFrame) return "<Mouse>/middleButton";
            }
            return "";
        }

        /// <summary>
        /// 단일 버튼 액션의 바인딩(인덱스 0)을 지정 경로로 교체한다. 라이브 액션에 즉시 반영하고,
        /// ConfigManager의 bindings 데이터도 갱신해 저장(Save) 시 유지되게 한다. 컴포짓 액션(move/look)에는 쓰지 않는다.
        /// </summary>
        /// <param name="actionName">액션 이름</param>
        /// <param name="path">새 바인딩 경로(예 "&lt;Keyboard&gt;/space")</param>
        /// <returns>성공하면 true</returns>
        public bool SetActionBinding(string actionName, string path)
        {
            InputAction action = GetAction(actionName);
            if (action == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            // config bindings에 매칭 정의가 없거나(모드 등록 액션 등) 바인딩이 비면, 영속도 안 되고 인덱스 0도 없으므로 실패로 처리.
            InputActionDefinition def = FindBindingDefinition(actionName);
            if (def?.bindings == null || def.bindings.Length == 0)
            {
                return false;
            }

            action.ApplyBindingOverride(0, path);
            def.bindings[0] = path;   // 0번만 교체(복수 바인딩이면 길이 유지)
            return true;
        }

        /// <summary>
        /// ConfigManager의 bindings 데이터를 라이브 액션(인덱스 0)에 다시 적용한다.
        /// 옵션 BACK(되돌리기)/RESET(초기화)로 config.bindings가 바뀐 뒤 라이브를 동기화하는 데 쓴다.
        /// </summary>
        public void ReapplyBindings()
        {
            InputActionDefinition[] defs = ConfigManager.Instance.Config?.bindings;
            if (defs == null)
            {
                return;
            }
            foreach (InputActionDefinition def in defs)
            {
                if (def.bindings == null || def.bindings.Length == 0)
                {
                    continue;
                }
                InputAction action = GetAction(def.name);
                if (action != null && action.bindings.Count > 0)
                {
                    action.ApplyBindingOverride(0, def.bindings[0]);
                }
            }
        }

        /// <summary>
        /// ConfigManager의 bindings 배열에서 이름이 일치하는 액션 정의를 찾는다.
        /// </summary>
        /// <param name="actionName">액션 이름</param>
        /// <returns>일치하는 정의, 없으면 null</returns>
        private static InputActionDefinition FindBindingDefinition(string actionName)
        {
            InputActionDefinition[] defs = ConfigManager.Instance.Config?.bindings;
            if (defs == null)
            {
                return null;
            }
            foreach (InputActionDefinition def in defs)
            {
                if (string.Equals(def.name, actionName, StringComparison.OrdinalIgnoreCase))
                {
                    return def;
                }
            }
            return null;
        }

        /// <summary>
        /// 이름으로 등록된 입력 액션을 조회한다. 코어 접근자와 모드 API의 공통 진입점이다.
        /// </summary>
        /// <param name="name">액션 이름 (대소문자 무시)</param>
        /// <returns>해당 InputAction, 없으면 null</returns>
        public InputAction GetAction(string name)
        {
            m_actions.TryGetValue(name, out InputAction action);
            return action;
        }

        /// <summary>
        /// 현재 등록된 모든 액션 이름을 반환한다.
        /// </summary>
        /// <returns>액션 이름 컬렉션</returns>
        public IReadOnlyCollection<string> GetActionNames()
        {
            return m_actions.Keys;
        }

        /// <summary>
        /// 런타임에 액션 정의 하나를 추가 등록한다. 맵을 잠시 비활성화한 뒤 추가하고 다시 활성화한다.
        /// 이미 같은 이름이 등록돼 있으면 기존 액션을 그대로 반환한다.
        /// </summary>
        /// <param name="def">추가할 액션 정의</param>
        /// <returns>등록된(또는 기존) InputAction, 실패 시 null</returns>
        public InputAction RegisterAction(InputActionDefinition def)
        {
            if (m_map == null || def == null || string.IsNullOrEmpty(def.name))
            {
                return null;
            }

            if (m_actions.TryGetValue(def.name, out InputAction existing))
            {
                return existing;
            }

            bool wasEnabled = m_map.enabled;
            if (wasEnabled)
            {
                m_map.Disable();
            }

            BuildAction(def);

            if (wasEnabled)
            {
                m_map.Enable();
            }

            return GetAction(def.name);
        }

        /// <summary>
        /// 액션 정의 하나를 InputActionMap에 추가하고 이름→액션 사전에 등록한다.
        /// </summary>
        /// <param name="def">액션 이름/타입/바인딩/컴포짓 정의</param>
        private void BuildAction(InputActionDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.name))
            {
                return;
            }

            InputAction action = m_map.AddAction(def.name, ParseActionType(def.type));

            if (def.bindings != null)
            {
                foreach (string path in def.bindings)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        action.AddBinding(path);
                    }
                }
            }

            if (def.composites != null)
            {
                foreach (InputCompositeDefinition composite in def.composites)
                {
                    action.AddCompositeBinding(composite.type)
                        .With("Up", composite.up)
                        .With("Down", composite.down)
                        .With("Left", composite.left)
                        .With("Right", composite.right);
                }
            }

            m_actions[def.name] = action;
        }

        /// <summary>
        /// 문자열 타입 이름을 InputActionType으로 변환한다.
        /// </summary>
        /// <param name="type">"Button" / "Value" / "PassThrough" 중 하나</param>
        /// <returns>해당 InputActionType, 매칭 실패 시 Button</returns>
        private static InputActionType ParseActionType(string type)
        {
            if (Enum.TryParse(type, true, out InputActionType result))
            {
                return result;
            }

            return InputActionType.Button;
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
