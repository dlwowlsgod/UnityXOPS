using System.Collections.Generic;

namespace UnityXOPS
{
    // AIBrain 무기 운용 partial (원본 HaveWeapon ai.cpp:899 / ControlWeapon ai.cpp:1033).
    public partial class AIBrain
    {
        /// <summary>
        /// 현재 슬롯이 맨손이거나 총탄 소진이면, 탄 있는 총이 든 다른 슬롯으로 전환. ACTION/CAUTION 에서만 호출.
        /// 원본 HaveWeapon — 두 슬롯 다 비면 아무것도 못 함(맨손 유지). SetSelectWeapon 이 전환 쿨다운(IsChanging) 가드.
        /// </summary>
        private void HaveWeapon()
        {
            Weapon cur = m_self.CurrentWeapon;
            if (!IsNoneWeapon(cur) && TotalAmmo(cur) > 0) return; // 멀쩡한 총 들고 있음

            int next = 1 - m_self.SelectWeapon;
            Weapon nextW = m_self.GetWeapon(next);
            if (!IsNoneWeapon(nextW) && TotalAmmo(nextW) > 0)
                m_self.SetSelectWeapon(next);
        }

        /// <summary>
        /// 현재 탄창이 비면(CurrentMagazine==0) 상태별 확률로 재장전 / 다른 슬롯 전환 / (예비탄도 0이면) 버림.
        /// 원본 ControlWeapon — 2단계 확률 게이트. 매 프레임 호출. 맨손/전환·재장전 중이면 no-op.
        /// </summary>
        private void ControlWeapon()
        {
            Weapon cur = m_self.CurrentWeapon;
            if (IsNoneWeapon(cur) || cur.IsFalling) return; // 맨손/낙하 무기는 처리 안 함 (원본 weapon NULL → return)
            if (IsCaseWeapon(cur)) return; // 케이스(서류가방 등 임무 아이템)는 탄약 0이어도 재장전/전환/버림 안 함
            if (m_self.IsChanging) return; // 전환/재장전 쿨다운 중 (원본 selectweaponcnt/weaponreloadcnt 가드)
            if (cur.CurrentMagazine > 0) return; // 탄창에 탄 있음 (원본 lnbs>0)

            // 1단계: 행동 시도 게이트 (NORMAL 1/1, CAUTION 1/10, ACTION 1/8).
            int gate = (m_mode == BattleMode.Normal) ? 1 : (m_mode == BattleMode.Caution) ? 10 : 8;
            if (GetRand(gate) != 0) return;

            // 예비탄도 0 → 빈 총 버림 (원본 nbs==0 → DumpWeapon). 다음 프레임 HaveWeapon 이 다른 슬롯 픽업.
            if (cur.ReserveAmmo == 0)
            {
                m_self.DropCurrentWeapon();
                return;
            }

            // 2단계: 재장전 vs 전환 확률. under/ways 로 리로드 확률 = (under+1)/ways.
            int ways, under;
            if (m_mode == BattleMode.Normal) { ways = 1; under = 0; } // 항상 재장전
            else if (m_mode == BattleMode.Caution) { ways = 5; under = 3; } // 4/5 재장전
            else if (!m_longAttack) { ways = 4; under = 2; } // ACTION 근거리 3/4 재장전
            else { ways = 3; under = 1; } // ACTION 원거리 2/3 재장전

            if (GetRand(ways) <= under) m_self.ReloadCurrentWeapon();
            else m_self.SetSelectWeapon(1 - m_self.SelectWeapon);
        }

        /// <summary>맨손(noneWeapon) 슬롯 판정 — WeaponIndex 가 noneWeaponIndex 이거나 무기/데이터 없음.</summary>
        private static bool IsNoneWeapon(Weapon w)
        {
            if (w == null || w.WeaponData == null) return true;
            int noneIdx = DataManager.Instance.WeaponParameterData.weaponGeneralData.noneWeaponIndex;
            return w.WeaponIndex == noneIdx;
        }

        /// <summary>케이스(서류가방 등) 무기 판정 — WeaponGeneralData.caseWeaponIndex 목록에 포함. 탄약 운용/버림 대상에서 제외.</summary>
        private static bool IsCaseWeapon(Weapon w)
        {
            if (w == null || w.WeaponData == null) return false;
            List<int> caseIndices = DataManager.Instance.WeaponParameterData.weaponGeneralData.caseWeaponIndex;
            return caseIndices != null && caseIndices.Contains(w.WeaponIndex);
        }

        /// <summary>무기의 총 보유 탄수 (탄창 + 예비). 원본 nbs.</summary>
        private static int TotalAmmo(Weapon w) => w == null ? 0 : w.CurrentMagazine + w.ReserveAmmo;
    }
}
