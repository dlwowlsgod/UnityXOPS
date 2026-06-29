using System;

namespace UnityXOPS
{
    /// <summary>
    /// 입력 바인딩 설정 전체를 JSON으로 직렬화/역직렬화하기 위한 루트 데이터 클래스.
    /// 고정 필드 대신 액션 정의 리스트를 담아 data-driven으로 액션을 구성한다.
    /// </summary>
    [Serializable]
    public class InputBindingData
    {
        public InputActionDefinition[] actions;
    }

    /// <summary>
    /// 단일 입력 액션의 정의. 이름, 액션 타입, 단순 바인딩, 컴포짓 바인딩을 가진다.
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
