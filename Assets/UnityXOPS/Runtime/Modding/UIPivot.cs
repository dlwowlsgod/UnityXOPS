namespace UnityXOPS.Modding
{
    /// <summary>
    /// UI 요소의 앵커/피벗 기준. 9개 포인트 + 7개 stretch(확장) 모드.
    /// stretch는 anchorMin≠anchorMax로 펼쳐지며(풀폭/풀높이/풀스크린), 텍스트 정렬에는 쓰지 않는다.
    /// Lua에는 직접 노출되지 않으며, 파사드가 피벗 이름 문자열을 이 값으로 변환한다.
    /// </summary>
    public enum UIPivot
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,

        // stretch(확장) — 펼쳐지는 축은 sizeDelta가 0이면 부모를 꽉 채운다.
        StretchTop,      // 가로 풀폭, 상단 (height=두께)
        StretchMiddle,   // 가로 풀폭, 중앙
        StretchBottom,   // 가로 풀폭, 하단
        StretchLeft,     // 세로 풀높이, 좌측 (width=두께)
        StretchCenter,   // 세로 풀높이, 중앙
        StretchRight,    // 세로 풀높이, 우측
        StretchFull,     // 풀스크린 (양축 확장)
    }
}
