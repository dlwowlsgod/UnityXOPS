using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 원본 OpenXOPS 월드 사운드(SoundManager::GetWorldSound)의 AI 인지 부분 포팅. 음원이 발생하면 청취 범위 내
    /// 비플레이어 Human 에게 위협 신호(Human.NotifyThreatHeard)를 보낸다. AI 는 NORMAL/CAUTION 에서 이를 소비해 경계 전환.
    ///
    /// 실제 오디오 재생(hyu/격발음 등)은 SoundManager 가 별도 처리 — 이 클래스는 AI 인지 전용(소리 좌표·방향은 안 씀, 원본도 카운트만).
    /// 원본은 registry+pull 이지만 Unity 는 음원(발사/총알/폭발) Tick 케이던스가 AI(33.333fps)와 달라 push 가 견고하다.
    /// 청취자 머리 높이(cameraAttachPosition) 기준, 거리 임계는 HumanGeneralData(aiHear*) 데이터값.
    /// </summary>
    public static class WorldSound
    {
        /// <summary>
        /// 한 지점에서 난 소리. 청취자 팀에 따라 거리 임계가 다르다 (적=enemyDist, 아군=allyDist; allyDist≤0 이면 아군은 못 들음).
        /// 발포음: enemyDist=일반/소음, allyDist=가까이만. 폭발: 팀 무관(enemyDist==allyDist).
        /// </summary>
        public static void EmitPointSound(Vector3 pos, int sourceTeam, float enemyDist, float allyDist)
        {
            // 게임플레이(Maingame)에서만 — 원본 demomode==false 게이트. 메뉴/브리핑 데모에서는 AI 가 없으므로 무시.
            if (!HumanController.TickEnabled) return;

            var humans = MapLoader.Humans;
            if (humans == null) return;
            Human player = MapLoader.Player;
            float eye = DataManager.Instance.HumanParameterData.humanGeneralData.cameraAttachPosition;

            for (int i = 0; i < humans.Count; i++)
            {
                Human h = humans[i];
                if (h == null || h == player || !h.Alive) continue;

                float maxDist = (h.Team == sourceTeam) ? allyDist : enemyDist;
                if (maxDist <= 0f) continue;

                Vector3 head = h.transform.position + Vector3.up * eye;
                if ((head - pos).sqrMagnitude < maxDist * maxDist)
                    h.NotifyThreatHeard();
            }
        }
    }
}
