namespace UnityXOPS
{
    /// <summary>
    /// PD1 이벤트 노드의 종류 (원본 P1 = Unity param0). OpenXOPS event.cpp:288-345 switch 대응.
    /// 10~11 = 즉시 종료(미션 결과), 14·18·19 = 즉시 실행(action), 12·13·15·16·17 = 조건 대기(trigger).
    /// (29 OpenXOPS判定 분기는 1차 미지원 — k_maxParameterCount=20 범위 밖이라 PD1 정렬에서 제외됨)
    /// </summary>
    public enum EventType
    {
        MissionComplete = 10, // 任務達成 — 미션 성공 (종료)
        MissionFailed = 11, // 任務失敗 — 미션 실패 (종료)
        WaitDeath = 12, // 死亡待ち — 대상(param1) 사망 대기
        WaitArrival = 13, // 到着待ち — 대상(param1)이 노드 좌표 반경 도착 대기
        ChangeToWalk = 14, // 歩きに変更 — 대상 AI패스(param1) 이동모드를 걷기(0)로 변경
        WaitBreakObject = 15, // 小物破壊待ち — 대상 소품(param1) 파괴 대기
        WaitCase = 16, // ケース待ち — 대상(param1) 도착 + 케이스 무기 소지 대기
        WaitTime = 17, // 時間待ち — param1 초 경과 대기
        Message = 18, // メッセージ — 메시지(param1=ID) 표시
        ChangeTeam = 19, // チーム変更 — 대상(param1) 팀번호를 0으로 변경
    }
}
