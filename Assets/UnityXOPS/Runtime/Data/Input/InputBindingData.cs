using System;

namespace UnityXOPS
{
    /// <summary>
    /// 단일 입력 액션의 정의. 이름, 액션 타입, 단순 바인딩, 컴포짓 바인딩을 가진다.
    /// 입력 바인딩은 ConfigManager(config.json)가 소유하며, InputManager가 이 정의로 InputSystem 액션을 빌드한다.
    /// </summary>
    [Serializable]
    public class InputActionDefinition
    {
        public string name;
        public string type;
        public string[] bindings;
        public InputCompositeDefinition[] composites;
    }

    /// <summary>
    /// 컴포짓 바인딩 정의. 현재는 방향 4개를 묶는 2DVector 형식을 지원한다.
    /// </summary>
    [Serializable]
    public class InputCompositeDefinition
    {
        public string type;
        public string up;
        public string down;
        public string left;
        public string right;
    }
}
