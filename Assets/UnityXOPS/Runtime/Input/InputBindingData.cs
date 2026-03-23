using System;

namespace UnityXOPS
{
    /// <summary>
    /// 입력 바인딩 설정을 JSON으로 직렬화/역직렬화하기 위한 데이터 클래스.
    /// </summary>
    [Serializable]
    public class InputBindingData
    {
        // Look: 마우스 델타 + 방향키 컴포짓
        public string look;
        public string lookUp;
        public string lookDown;
        public string lookLeft;
        public string lookRight;

        // Move: WASD 컴포짓
        public string moveForward;
        public string moveBackward;
        public string moveLeft;
        public string moveRight;

        // 버튼 액션
        public string jump;
        public string walk;
        public string drop;
        public string fire;
        public string zoom;
        public string previous;
        public string next;
        public string reload;
        public string first;
        public string second;
    }
}
