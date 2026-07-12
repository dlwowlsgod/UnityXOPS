using UnityEngine;
using UnityEngine.InputSystem;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private InputAPI m_input;
        public InputAPI Input => m_input ??= new InputAPI(m_luaEnv);
    }

    /// <summary>
    /// 모드에 입력 액션 등록/조회/폴링을 제공하는 API 그룹. Lua에서는 XOPS.Input 으로 접근한다.
    /// </summary>
    [LuaCallCSharp]
    public class InputAPI
    {
        private readonly LuaEnv m_luaEnv;

        /// <summary>
        /// 입력 API 그룹을 생성한다.
        /// </summary>
        /// <param name="luaEnv">조회 결과 테이블 생성에 사용할 LuaEnv</param>
        public InputAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }

        /// <summary>
        /// 모드가 새 버튼 액션을 등록한다. 같은 이름이 이미 있으면 무시된다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <param name="binding">InputSystem 바인딩 경로 (예: "&lt;Keyboard&gt;/leftShift")</param>
        public void RegisterButton(string name, string binding)
        {
            InputActionDefinition def = new InputActionDefinition
            {
                name = name,
                type = "Button",
                bindings = new[] { binding },
            };
            InputManager.Instance.RegisterAction(def);
        }

        /// <summary>
        /// 모드가 새 축(Value) 액션을 등록한다. 스크롤 등 연속 값 입력에 사용한다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <param name="binding">InputSystem 바인딩 경로 (예: "&lt;Mouse&gt;/scroll/y")</param>
        public void RegisterAxis(string name, string binding)
        {
            InputActionDefinition def = new InputActionDefinition
            {
                name = name,
                type = "Value",
                bindings = new[] { binding },
            };
            InputManager.Instance.RegisterAction(def);
        }

        /// <summary>
        /// 등록된 모든 액션 이름을 Lua 배열 테이블(1-기반)로 반환한다.
        /// </summary>
        /// <returns>액션 이름들이 담긴 LuaTable</returns>
        public LuaTable GetActionNames()
        {
            LuaTable table = m_luaEnv.NewTable();
            int index = 1;
            foreach (string name in InputManager.Instance.GetActionNames())
            {
                table.Set(index++, name);
            }
            return table;
        }

        /// <summary>
        /// 지정한 액션의 유효 바인딩 경로들을 Lua 배열 테이블(1-기반)로 반환한다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>바인딩 경로 문자열이 담긴 LuaTable. 액션이 없으면 빈 테이블</returns>
        public LuaTable GetBindings(string name)
        {
            LuaTable table = m_luaEnv.NewTable();
            InputAction action = InputManager.Instance.GetAction(name);
            if (action != null)
            {
                int index = 1;
                foreach (InputBinding binding in action.bindings)
                {
                    // composite 헤더(2DVector 등)는 실제 컨트롤 경로가 아니므로 건너뛴다.
                    if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath))
                    {
                        continue;
                    }

                    table.Set(index++, binding.effectivePath);
                }
            }
            return table;
        }

        /// <summary>
        /// 액션이 현재 눌려 있는지 반환한다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>눌려 있으면 true, 액션이 없으면 false</returns>
        public bool IsPressed(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            return action != null && action.IsPressed();
        }

        /// <summary>
        /// 액션이 이번 프레임에 눌렸는지 반환한다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>이번 프레임에 눌렸으면 true, 액션이 없으면 false</returns>
        public bool WasPressed(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            return action != null && action.WasPressedThisFrame();
        }

        /// <summary>
        /// 액션이 이번 프레임에 떼어졌는지 반환한다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>이번 프레임에 떼어졌으면 true, 액션이 없으면 false</returns>
        public bool WasReleased(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            return action != null && action.WasReleasedThisFrame();
        }

        /// <summary>
        /// 2D 액션의 X축 값을 반환한다 (move/look 등).
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>X축 값, 액션이 없으면 0</returns>
        public float GetAxisX(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            return action != null ? action.ReadValue<Vector2>().x : 0f;
        }

        /// <summary>
        /// 2D 액션의 Y축 값을 반환한다 (move/look 등).
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>Y축 값, 액션이 없으면 0</returns>
        public float GetAxisY(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            return action != null ? action.ReadValue<Vector2>().y : 0f;
        }

        /// <summary>
        /// 1D 액션(축/버튼)의 현재 float 값을 반환한다. 2D 액션에는 GetAxisX/GetAxisY를 쓴다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>현재 값, 액션이 없거나 2D 액션이면 0</returns>
        public float GetValue(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            if (action == null)
            {
                return 0f;
            }

            try
            {
                return action.ReadValue<float>();
            }
            catch (System.InvalidOperationException)
            {
                return 0f;
            }
        }

        /// <summary>
        /// 액션의 타입 이름을 반환한다 (Button / Value / PassThrough).
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <returns>타입 문자열, 액션이 없으면 빈 문자열</returns>
        public string GetActionType(string name)
        {
            InputAction action = InputManager.Instance.GetAction(name);
            return action != null ? action.type.ToString() : "";
        }

        /// <summary>
        /// 이번 프레임에 타이핑된 텍스트를 반환한다. Lua 커스텀 입력 필드에서 글자를 받는 데 쓴다.
        /// 반환값은 XLua를 거치며 UTF-8 바이트열이 된다. Lua에서 바이트 32~126만 통과시키면 ASCII 전용이 되고,
        /// 일본어 등 멀티바이트/IME 입력은 통째로 버려진다(유니코드가 필요하면 문자 단위로 처리해야 함).
        /// </summary>
        /// <returns>이번 프레임 타이핑 문자열(없으면 빈 문자열)</returns>
        public string GetTypedText()
        {
            return InputManager.Instance.GetTypedText();
        }

        /// <summary>
        /// 이번 프레임에 백스페이스 키가 눌렸는지 반환한다. 입력 필드의 마지막 글자 삭제에 쓴다.
        /// </summary>
        /// <returns>눌렸으면 true</returns>
        public bool WasBackspacePressed()
        {
            return InputManager.Instance.WasBackspacePressed();
        }

        /// <summary>
        /// 이번 프레임에 처음 눌린 키/버튼의 바인딩 경로를 반환한다. 리바인딩 리스닝 중 키 캡처에 쓴다.
        /// </summary>
        /// <returns>바인딩 경로(예 "&lt;Keyboard&gt;/w"), 없으면 빈 문자열</returns>
        public string GetFirstPressedKeyPath()
        {
            return InputManager.Instance.GetFirstPressedKeyPath();
        }

        /// <summary>
        /// 단일 버튼 액션의 바인딩을 지정 경로로 교체한다(즉시 반영 + 저장 시 유지). 컴포짓 액션에는 쓰지 않는다.
        /// </summary>
        /// <param name="name">액션 이름</param>
        /// <param name="path">새 바인딩 경로</param>
        /// <returns>성공하면 true</returns>
        public bool SetActionBinding(string name, string path)
        {
            return InputManager.Instance.SetActionBinding(name, path);
        }

        /// <summary>
        /// 마우스 커서 모드를 설정한다. 씬 진입/전환 시 호출한다(예: 메뉴는 자유 커서, FPS는 중앙 고정).
        /// </summary>
        /// <param name="hideInWindow">true면 창 안에 커서가 있을 때 숨긴다.</param>
        /// <param name="centered">true면 커서를 화면 중앙에 고정(Locked)한다(FPS용). false면 자유 이동(메뉴용).</param>
        /// <param name="moveToCenter">true면 커서를 즉시 화면 중앙으로 이동시킨다.</param>
        public void SetMouseCursor(bool hideInWindow, bool centered, bool moveToCenter)
        {
            InputManager.MouseCursorMode(hideInWindow, centered, moveToCenter);
        }
    }
}
