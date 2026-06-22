using JJLUtility;
using JJLUtility.IO;
using UnityEngine;

namespace UnityXOPS
{
    // AIBrain 좀비 근접 전투 partial (원본 Action 좀비 분기 ai.cpp:559-838 / objectmanager.cpp:2405-2528).
    public partial class AIBrain
    {
        private bool IsZombie()
        {
            HumanTypeData t = m_self.HumanTypeData;
            return t != null && t.zombie;
        }

        // 좀비 할퀴기 팔 목표 pitch (m_pitch space). 데이터 aiZombieArmAngle 는 원본 space(음수=아래, armAngleInitial 과 동일 규약).
        // 부호 반전해서 m_pitch 로. 값을 키울수록(원본 space) 팔이 더 위로 올라감.
        private float ZombieArmPitch => -GeneralData.aiZombieArmAngle;

        /// <summary>
        /// 좀비 전투 — 적을 향해 선회, 할퀴기 팔 자세, 근접 끌어당김, 정면 돌격, 50프레임 주기 근접 공격.
        /// 총 발사 없음(맨손). 회피 스트레이핑(MoveRandom)도 안 함.
        /// </summary>
        private void ZombieFight(Vector3 d, float dist, float atanx, float dz)
        {
            // 적 방향으로 선회
            if (atanx > dz) m_turnRight = true;
            if (atanx < -dz) m_turnLeft = true;

            // 팔(조준) 각도 — 할퀴기 자세로 수렴 (데이터 aiZombieArmAngle, 원본 AI_ZOMBIEATTACK_ARMRY -15°). ±1° 데드밴드.
            float armTarget = ZombieArmPitch;
            if (m_pitch > armTarget + 1f) m_turnUp = true;
            if (m_pitch < armTarget - 1f) m_turnDown = true;

            // 끌어당김(引き付け) — 근접 시 적을 끌고 + 적 시점 흔들기.
            ZombieGrab(d, dist);

            // 돌격 — 정면 ±25° 안에서 접근(걷기). ±15° + 근접 + 주기조건이면 달려듦(걷기 플래그 제거 = 달리기).
            if (Mathf.Abs(atanx) < k_zombieApproachTol)
            {
                m_moveIntent |= HumanMoveFlag.Forward;
                bool charge = Mathf.Abs(atanx) < k_zombieChargeTol
                           && dist < k_zombieChargeDist
                           && (m_actionCnt % k_zombieAttackPeriod) > k_zombieChargeWindow;
                if (!charge) m_moveIntent |= HumanMoveFlag.Walk;
            }

            // 공격 판정 — 50프레임마다 (원본 actioncnt%50==0).
            if (m_actionCnt % k_zombieAttackPeriod == 0) ZombieMeleeAttack();
        }

        /// <summary>근접(거리/높이차 임계) 시 적을 좀비 쪽으로 끌어당기고 적 시점을 ±2°/프레임 흔든다. 원본 ai.cpp:748-769.</summary>
        private void ZombieGrab(Vector3 d, float dist)
        {
            if (m_enemy == null) return;
            if (dist >= k_zombieGrabDist) return;
            if (Mathf.Abs(d.y) >= k_zombieGrabHeight) return;

            // 적을 좀비 쪽으로 끌어당김 (원본 AddPosOrder 0.5/frame). 수평만. 무기 넉백과 동일 속도 경로.
            Vector3 pull = m_self.transform.position - m_enemy.transform.position;
            pull.y = 0f;
            if (pull.sqrMagnitude > 1e-6f)
            {
                HumanController vc = m_enemy.GetComponent<HumanController>();
                if (vc != null) vc.AddKnockbackVector(pull.normalized, k_zombiePullSpeed);
            }

            // 적 시점 흔들기 — 무기 반동과 동일 경로(AddViewRecoil)라 플레이어/AI 피해자 모두 적용됨.
            m_enemy.AddViewRecoil(Random.Range(-k_zombieViewShake, k_zombieViewShake),
                                  Random.Range(-k_zombieViewShake, k_zombieViewShake));
        }

        /// <summary>정면 공격 지점(반경 0.33) 안의 적에게 근접 데미지. 원본 ObjectManager::CheckZombieAttack (objectmanager.cpp:2416).</summary>
        private void ZombieMeleeAttack()
        {
            HumanGeneralData gen = GeneralData;
            float eyeH = gen.cameraAttachPosition; // 원본 VIEW_HEIGHT 19 → 눈높이
            float bandH = gen.controllerHeight; // 원본 HUMAN_HEIGHT 20 → 수직 밴드 폭

            Vector3 self = m_self.transform.position;
            Vector3 fwd = Quaternion.Euler(0f, m_yaw, 0f) * Vector3.forward;
            // 공격 지점 = 정면 0.2 앞 (수평), 높이 = 발 + 2·눈높이 - 0.05 (원본 (feet+VIEW)+VIEW-0.5).
            Vector3 ap = self + fwd * k_zombieAttackOffset;
            float apY = self.y + eyeH + eyeH - 0.05f;
            float r2 = k_zombieAttackRadius * k_zombieAttackRadius;

            var humans = MapLoader.Humans;
            if (humans == null) return;
            for (int i = 0; i < humans.Count; i++)
            {
                Human t = humans[i];
                if (t == null || t == m_self || !t.Alive) continue;
                if (t.Team == m_self.Team) continue;

                Vector3 tp = t.transform.position;
                float ax = ap.x - tp.x;
                float az = ap.z - tp.z;
                if (ax * ax + az * az >= r2) continue; // 수평 반경 밖

                float ty = tp.y + eyeH; // 피해자 눈높이
                if (apY < ty || apY > ty + bandH) continue; // 수직 범위 밖

                ZombieHit(t);
            }
        }

        /// <summary>좀비 근접 명중 — 데미지 + 피격방향 + 조준오차 + 피 이펙트 + 공격음. 원본 HitZombieAttack (objectmanager.cpp:2451).</summary>
        private void ZombieHit(Human victim)
        {
            HumanTypeData atkType = m_self.HumanTypeData;
            IntRange dmg = atkType.zombieMeleeDamageRange;
            int dealt = Random.Range(dmg.min, dmg.max); // 원본 15+GetRand(5) = 15~19 (max exclusive)
            victim.ApplyDamage(dealt);

            // 피격 방향 (좀비→피해자, 총알 진행방향 규약) — 사망 쓰러짐 방향용.
            Vector3 dir = victim.transform.position - m_self.transform.position;
            victim.SetHitYaw(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);

            // 피격 조준 오차 (원본 ReactionGunsightErrorRange = 10).
            victim.SetHitReaction(k_zombieHitReaction);

            // 피 이펙트 (피해자 타입 blood, 데이터) — 좀비 피격 flowing=true 라 데미지 비례 분사 포함(원본 objectmanager.cpp:2515).
            HumanTypeData vt = victim.HumanTypeData;
            if (vt != null)
                EffectManager.Instance.Play(vt.bloodEffectIndex,
                    victim.transform.position + Vector3.up * GeneralData.cameraAttachPosition, dealt);

            // 공격음 (원본은 일반 피탄음 재사용). 데이터 경로.
            if (!string.IsNullOrEmpty(atkType.zombieAttackSound))
            {
                AudioClip clip = SoundLoader.LoadAudio(
                    SafePath.Combine(Application.streamingAssetsPath, atkType.zombieAttackSound));
                if (clip != null) SoundManager.Instance.PlayAt(clip, victim.transform.position, k_zombieAttackVolume);
            }

            // 좀비 타격음 AI 인지 (원본 HIT_HUMAN_ZOMBIE, 팀 무관 → enemyDist==allyDist). 근처 AI 경계 트리거.
            float zd = GeneralData.aiHearHitHumanZombie;
            WorldSound.EmitPointSound(victim.transform.position, m_self.Team, zd, zd);
        }
    }
}
