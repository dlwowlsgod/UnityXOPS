using UnityEngine;
using UnityEngine.InputSystem;
using JJLUtility;
using UnityXOPS.Modding;
using System;
using System.Collections.Generic;

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

        private void Start()
        {
            Keyboard = Keyboard.current;
            Mouse = Mouse.current;

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

        private void OnDestroy()
        {
            m_map?.Disable();
            m_map?.Dispose();
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
