# 멀티플레이(Netcode) 로드맵

> UnityXOPS에 PVE / PVP 멀티플레이를 도입하기 위한 설계 문서 + 진행 기록.
> "무엇을, 왜, 어떤 순서로" 를 정해두는 합의문이며, **Phase 1이 진행 중이다** (→ §0-1 진행 상황).
>
> 작성: 2026-07-21 · 갱신: 2026-07-22

---

## 0. 결론 요약

- **exe를 나누지 않는다.** 싱글 모딩용 / 멀티용 빌드 분리는 하지 않는다.
- **`Human`을 상속하거나 복제한 `HumanMultiplayer` 클래스도 만들지 않는다.**
- 대신 **어댑터 컴포넌트 1개**(`HumanNetSync : NetworkBehaviour`)를 프리팹에 선택적으로 붙인다.
  붙어 있으면 멀티, 없으면 싱글. **시뮬레이션 코드는 자기가 네트워크 위에 있는지 모른다.**
- 멀티플레이 구현은 **맨 마지막**. 그 앞의 준비 작업들은 전부 싱글플레이 품질을 그 자체로 올리는 작업이다.
- **PVE와 PVP는 싱글 미션 선택창에 끼워넣지 않고 별도 씬/모드로 분리한다.**

---

## 0-1. 진행 상황 (2026-07-22 기준)

작업 브랜치: **`QoL-road-to-multiplay(0.4)`** (main 기반, 리뷰 후 병합 예정).
원칙: **Phase 1은 동작 보존(behavior-preserving)** — 눈에 띄는 변화가 없어야 정상. 케이던스/게임 결과가 바뀌면 회귀 신호.

### ✅ 완료

**Phase 1-A — 입력 커맨드 구조체 추출** (코드리뷰 통과, 플레이테스트 미검증)
- `HumanInput` 구조체 신설(`HumanController.cs`) — 직렬화·리플레이 가능한 단일 입력 표면.
- **step 1 (이동/조준)**: `SetMoveFlag`+`SetYawPitch` 제거 → `HumanController.SetInput(in HumanInput)` 통합. PlayerController·AIBrain 둘 다 이 구조체를 채운다.
- **step 2 (무기 액션)**: `HumanWeaponAction` [Flags](발사/장전/전환/선택/드롭) + `Human.ApplyWeaponInput`. 플레이어 무기 직접 호출을 intent 플래그로.
- **AI 무기 호출은 직접 유지(의도적)**: `HaveWeapon`/`ControlWeapon`/`ThrowGrenade` 등은 한 틱 안에서 읽고-결정-행동을 동기적으로 하고 반환값을 결정에 쓴다(지연 intent 불가). 또 AI는 서버 권위라 입력 직렬화 대상이 아니다.
- **소비자 분리**: 이동/조준=`HumanController`, 무기=`Human`. 동기 실행이라 발사→반동→되읽기 결합을 그대로 보존.

**Phase 1-B step 1 — 틱 소스 통합** (코드리뷰 통과, 플레이테스트 미검증)
- 신규 `SimClock`(`Map/SimClock.cs`): DDOL 싱글턴, `[DefaultExecutionOrder(-200)]`, 단일 33.333Hz 누산기 + `ISimTickable` 등록. `SimClock.FrameRate`/`FrameTime` = 33.333 단일 소스.
- 중복 누산기 3개(`AIController`/`HumanCollision`/`EventManager`)를 SimClock으로 통합. `SimOrder`로 원본 프레임 순서 재현. 3개가 이미 락스텝이라 동작 보존, 실행순서만 명시화(개선).
- `AIController`는 이중 케이던스라 `SimTick`=판단(33Hz) / `FixedUpdate`=`ApplyMovement`(50Hz 브릿지)로 분리 — 브릿지는 step 4에서 흡수.
- **버그픽스**: 종료(quit) 시 간헐 NullReferenceException — `Register`/`Unregister`가 종료 중 null을 반환하는 `Instance`를 참조하던 것을, 등록 목록을 static으로 바꿔 인스턴스 의존을 제거. (`Loaded`=true인데 `Instance`=null인 종료 경합 창 때문)

### 📐 원본 프레임 순서 (openxops-analyzer 확인 — 남은 1-B 단계의 기준)

`objectmanager.cpp:2710` / `gamemain.cpp:2514` 기준 프레임당 순서:
`이동/맵충돌(O2) → 무기(O3) → SmallObject(O4) → 총알(O5) → 이펙트(O7) → 수류탄(O8) → 무기줍기(O9) → 인간간충돌(O10) → AI판단(P3) → 미션판정(P4) → 이벤트(P5) → 월드사운드`
- **AI 판단은 이동보다 뒤, 1프레임 지연**: AI가 세운 커맨드는 다음 프레임 이동이 소비. 우리 `HumanInput` 구조와 일치.
- 통합 완료된 3개의 상대 순서: 인간간충돌 → AI → 이벤트.

### ⏳ 미검증
에디터 컴파일 + Maingame 플레이테스트 미실시. 핵심 확인: 탄도 방향 · 에임 킥 반동(발사→반동→되읽기) · AI 전투/이동 무변경 · 인간간충돌 · 이벤트/미션 · 씬 전환 반복(SimClock 재등록). 상세 체크리스트는 로컬 `TODO.md`.

### ▶ 다음 작업
**1-B step 2 (Bullet을 틱으로)** — 가장 심각한 렌더레이트 탄도 문제. 상세는 §2 Phase 1-B.

---

## 1. 왜 exe를 나누지 않는가

처음에 검토했던 안: "멀티용 exe와 싱글(모딩)용 exe를 나누고, 멀티에서는 Lua를 포기한다."

**조사 결과 이 트레이드오프는 필요 없다.** 이유:

`maingame.lua` (848줄) 가 매 프레임 하는 일을 전부 뜯어보면 **읽기 전용 폴링**뿐이다.

```
GetHP / GetMagazine / GetReserveAmmo / GetWeaponName / GetErrorRange
GetActiveScope / GetScopeIndex / IsAlive / IsReloading / IsScoping
GetMessageId / GetMessageText / GetMessageAlpha / GetWallBlind
```

쓰기는 `ToggleScope`, `ToggleViewMode`, `SetFieldOfView`, `ConsumeHit`, 씬 전환뿐이다.
**Lua는 Human을 스폰하지 않고, 총알을 만지지 않고, 아무것도 움직이지 않는다.**

즉 현재 Lua는 이미 사실상 **클라이언트 프레젠테이션 레이어**다.
멀티에서 각 클라이언트가 로컬로 그대로 실행하면 되고, 서버 권위와 전혀 충돌하지 않는다.

> 이 프로젝트의 철학은 "출하되는 게임 = 첫 번째 모드"(Factorio 모델)다.
> 멀티에서 그걸 버리면 반쪽이 된다. 게다가 두 exe는 시간이 지나면 반드시 두 코드베이스가 된다.

## 1-2. 왜 `HumanMultiplayer` 병행 클래스를 만들지 않는가

`Human`의 상태(HP/팀/사망상태)와 `HumanController`의 이동 로직은 **싱글과 멀티가 완전히 동일해야 한다.**
클래스를 나누는 순간:

- 두 벌을 영원히 동기화 유지해야 한다
- 원본 OpenXOPS 등가성 검증을 두 번 해야 한다
- 버그가 한쪽에만 고쳐진다

### 대신 채택하는 구조

```
Human / HumanController          ← MonoBehaviour. 네트워크를 전혀 모른다 (지금 그대로)
        ▲ 읽고 씀
HumanNetSync : NetworkBehaviour  ← 신규. 있으면 멀티, 없으면 싱글
```

- **서버**: `Human` 상태를 읽어 `NetworkVariable`로 브로드캐스트
- **클라이언트**: 받은 상태를 입력 표면(`HumanController.SetInput(HumanInput)` / `Human.ApplyWeaponInput`)에 주입 — Phase 1-A에서 이 단일 입구가 이미 만들어짐
- **싱글플레이**: `NetworkManager` 자체가 없고, 프리팹에 이 컴포넌트가 없다

PVE는 이 구조에서 거의 공짜다 — `AIController`가 씬 레벨 싱글 컴포넌트라 **서버에서만 활성화**하면 끝난다.

---

## 2. 작업 순서

### Phase 1 — 싱글플레이 구조 정리 (멀티와 무관하게 이득)

아래 세 항목은 **지금 싱글플레이에도 실제 결함**이다. 멀티를 안 하더라도 고칠 가치가 있다.

#### 1-A. 입력 커맨드 구조체 추출  ✅ 완료 (§0-1)

당시 문제:

| 위치 | 문제 |
|---|---|
| `HumanController.cs:7-17` | `m_moveFlag`가 `[Flags]` 누적값. `Update`에서 쌓이고 `FixedUpdate`에서 소비·클리어 (`:171-172`) |
| `PlayerController.cs:156` | 조준이 절대 yaw/pitch 직접 주입. 델타가 아님 |
| `PlayerController.cs:181-202` | 발사/장전/무기교체/드롭을 `Human`·`Weapon` 메서드 **직접 호출** |

→ **직렬화하거나 리플레이할 수 있는 "입력" 이라는 대상이 코드에 존재하지 않는다.**

할 일: 한 틱 분량의 입력을 담는 `HumanInput` 구조체를 만들고,
발사/장전/교체/드롭도 직접 호출이 아니라 **플래그**로 바꾼다.
`PlayerController`(사람)와 `AIController`(AI)가 둘 다 이 구조체를 채우고,
`HumanController`가 그것만 소비한다.

부수 이득: AI와 플레이어가 같은 입구를 쓰게 되어 데모 리플레이·봇 테스트가 가능해진다.

#### 1-B. 시계를 하나로 수렴  🔄 step 1 완료 · step 2~5 남음

현재 **시계가 4개**다:

| 클럭 | 무엇이 도는가 |
|---|---|
| 렌더레이트 `Update` | `Human` 타이머, `Weapon` 연사/낙하, **`Bullet`**, `Effect`, `SmallObject` |
| 50Hz `FixedUpdate` | `HumanController` 이동/충돌 |
| 33.333Hz 누산기 | `AIController`, `HumanCollision`, `EventManager` |
| MonoBehaviour 실행 순서 | 위 셋 사이의 암묵적 순서 의존 |

가장 심각한 것: **총알이 렌더레이트로 날아간다.**
프레임레이트에 따라 탄도 적분 결과가 달라진다. 멀티에서는 곧바로 데미지 불일치다.

할 일: 게임플레이에 영향 있는 것은 **33.333Hz 단일 틱**으로 모은다.
(연출/보간만 렌더레이트에 남긴다.)

> 33.333fps 상수는 세 군데에 각각 하드코딩돼 있었다 — `AIController`/`HumanCollision`/`EventManager`.
> **이것부터 하나의 틱 소스(`SimClock`)로 합쳤다 → step 1 완료 (§0-1).**

**단일 틱 = 33.333Hz로 강제됨**: `AIController`/`EventManager`가 정수 프레임 카운트 확률 모델(cautioncnt=160, `(int)GAMEFPS*sec`)이라 이 레이트를 못 벗어난다. 따라서 `HumanController` 이동이 50Hz→33.33Hz로 내려와야 한다(step 4, 이동 재검증 필요). 원본도 이동이 33.333fps라 더 충실.

단계 (각 단계 독립 테스트 가능, §0-1 원본 프레임 순서 기준):
- [x] **step 1 — 틱 소스 통합**: `SimClock` 단일 드라이버로 3개 누산기 통합 (§0-1).
- [ ] **step 2 — Bullet을 틱으로** (가장 심각): `BulletManager` 탄도/충돌을 `SimClock`으로. 총알 visual 보간만 렌더레이트에.
- [ ] **step 3 — Weapon/Human 타이머를 틱으로**: fireRate·장전·전환·반동회복. 팔 애니메이션 등 연출은 렌더레이트.
- [ ] **step 4 — HumanController 이동 + Collision + AI를 틱으로** (리스크 핵심): 50Hz→33.33Hz, 이동 재검증 집중. `AIController` ApplyMovement 브릿지 흡수. 원본 순서(이동→충돌→AI→이벤트) 완전 정렬.
- [ ] **step 5 — SmallObject**: 게임플레이(피격 파괴)는 틱, 파편은 연출.

#### 1-C. RNG 스트림 분리

**"랜덤을 고정하자"가 아니다.** 게임플레이 랜덤이 프레임레이트에 오염되지 않게 하는 것이다.

현재는 시드 없는 전역 `UnityEngine.Random` **한 줄**을 전부가 공유한다:

```
UnityEngine.Random  ← 전역 스트림 하나
   ├─ Weapon.cs:355-372    산탄 퍼짐 / 조준 오차   ← 렌더레이트에서 뽑음
   ├─ Weapon.cs:174-183    반동 킥                 ← 렌더레이트
   ├─ AIBrain.cs:323       AI 판단                 ← 33.333Hz
   ├─ HumanController:432  낙하 데미지             ← 50Hz
   ├─ SmallObject.cs:232   파편 (순수 연출)        ← 렌더레이트
   └─ EffectManager:221    이펙트 변형 (순수 연출) ← 렌더레이트
```

같은 줄에서 **순서대로** 뽑아 쓰기 때문에:

- 144fps 유저와 60fps 유저는 **파편 이펙트가 소비하는 난수 개수가 다르다**
- 그러면 그 다음에 AI가 뽑는 값도 달라진다
- 즉 **연출이 게임플레이 결과를 바꾼다.** 프레임이 한 번 튀면 AI 판단과 산탄 패턴이 달라진다

분리 후:

```
GameplayRandom  ← 게임플레이 전용. 33.333Hz 틱에서만 뽑는다
   └─ 산탄, 조준 오차, 반동, AI 판단, 낙하 데미지, 랜덤 무기 스폰

VisualRandom    ← 연출 전용. 렌더레이트에서 뽑아도 무해
   └─ 파편, 이펙트 변형, 사운드 선택
```

**시드 고정은 선택 사항이다.** 분리만 해도 목적의 대부분이 달성된다.
거기에 미션 시작 시 시드를 고정하면 보너스로 리플레이·데스싱크 디버깅이 가능해지고,
멀티에서는 서버가 시드를 정해 배포하면 된다.

#### 1-D. 그 외 정리 (Phase 1 또는 Phase 3 초입)

- **무기 교체가 `Destroy` + `Instantiate`** (`HumanWeapon.cs:200-202, 234-236, 266-269, 308-310, 333-336, 408-413`)
  → 교체/드롭/줍기마다 GameObject를 새로 만든다. `NetworkObject` 아이덴티티와 상극.
  풀링 또는 모델 스왑으로 바꾼다. (`Bullet`/`Effect`는 이미 풀링되어 있다 — 그 패턴을 따른다.)
- **`MapLoader.Player`가 static 하나** (`PointData.cs:44`)
  → `AIController.GetManualHuman()`(`:69`)과 `PlayerAPI.Player`(`UnityXOPSAPIPlayer.cs:33`)가 둘 다 이 가정에 묶여 있다.
  커넥션별 플레이어 할당이 가능한 구조로 해체한다.
- **반동 읽기-되쓰기 루프** (`PlayerController.cs:208-209`)
  → 발사 후 `m_yaw = m_controller.Yaw;` 로 시뮬 상태를 입력 레이어가 다시 흡수한다.
  입력↔시뮬 양방향 결합이라 클라이언트 예측/보정과 싸운다.
- **`Physics.queriesHitBackfaces` 전역 토글** (`Bullet.cs:428-436`)
  → 전역 상태를 임시로 바꾼다. 병렬화하면 위험.

---

### Phase 2 — Lua 모딩 완성 + 데이터 Lua화

Phase 1 다음. 상세 설계는 별도 논의로 확정한다.

방향: `ParameterData` / `~~~Data` 계열을 **JSON 기본 + Lua 덮어쓰기** 하이브리드로 등록.
(관련 기존 문서: `docs/modding/Lua-Modding-Feasibility.md`)

#### 멀티를 대비해 지금부터 지켜야 할 것: Lua 신뢰 계층

exe를 나누는 대신, **Lua 표면을 두 등급으로 나눈다.** Phase 2 설계 시 이 경계를 유지한다.

| 등급 | 내용 | 멀티에서의 취급 |
|---|---|---|
| **프레젠테이션** | HUD 레이아웃, 폰트, 페이드, 크로스헤어, 스코프 — *현재의 Lua 전부* | 클라이언트 로컬 자유. 검증 불필요 |
| **정의 데이터** | 무기 수치, HumanType, AI 파라미터 — *Phase 2에서 추가될 것* | **서버가 소유.** 접속 시 서버 값을 클라에 배포 + 해시 대조 |

핵심: 정의 데이터는 **클라이언트가 자기 로컬 값을 쓰게 두면 안 된다.**
서버 값이 진실이고 클라는 받아쓴다.

이렇게 하면 **멀티 모딩이 그대로 열린다** — 호스트가 모드를 정하고, 참가자는 자동으로 그 룰로 플레이한다.
"멀티에서는 Lua를 포기한다"는 트레이드오프가 아예 사라진다.

향후 게임플레이 훅(데미지 수정 등 개입형)까지 열게 되면, 그건 **서버 전용 실행**으로 못박는다.

---

### Phase 3 — Netcode 도입

Phase 1·2가 끝난 뒤에 시작한다. 그 시점이면 이 작업은 "어댑터 붙이기"에 가까워진다.

#### 3-A. PVE 먼저

이유: AI 동기화만 하면 되므로 검증이 쉽다. 지연 보상이 필요 없다.

- `AIController`를 서버에서만 활성화
- `HumanNetSync`로 Human 상태 브로드캐스트
- 클라이언트는 **보간만** (예측 없음)
- `EventManager` 상태는 서버 권위 (상태가 `m_cursor[3]`, `m_waitcnt[3]`, `m_result`, `m_messageId` 정도로 매우 작아 동기화가 쉽다)
- `BulletManager`(pool 160) / `EffectManager`(pool 256)는 **고정 크기 풀 + 안정적 인덱스**라 그대로 네트워크 ID로 쓸 수 있다

#### 3-B. PVP

PVE가 안정된 뒤. 지연 보상(lag compensation)과 클라이언트 예측이 필요해지는 지점.
Phase 1-A/1-B/1-C가 제대로 되어 있어야 가능하다.

#### 3-C. 모드 분리

**PVE / PVP는 싱글 미션 선택창에 끼워넣지 않고 별도 씬으로 분리한다.**
싱글 미션 흐름(Briefing → Maingame → Result)과 규칙·수명주기가 다르기 때문.

---

## 3. 이 구조에서 유리한 점 (미리 확보된 것들)

조사 결과 이미 멀티에 유리하게 되어 있는 것들:

- **`Rigidbody` / `CharacterController` 를 아예 쓰지 않는다.** PhysX 시뮬레이션 동기화 문제가 없다.
  이동은 원본 `human::CollisionMap`을 손으로 포팅한 커스텀 코드라 **상태 벡터가 작고 명시적**이다
  (위치, `m_moveVelocity`, `m_rotationX`, `m_armRotationY`, `m_moveFlag`, 사망상태).
  Unity `Physics`는 **레이캐스트 질의로만** 쓴다.
- **AI가 이미 한 곳에 모여 있다** — `AIController` 씬 컴포넌트 하나가 `Dictionary<Human, AIBrain>`을 소유.
  클라이언트에서 끄기만 하면 된다.
- **`HumanController.TickEnabled` 라는 전역 시뮬레이션 게이트가 이미 있다** (`HumanController.cs:27`).
  `AIController` / `HumanCollision` / `EventManager` / `Bullet` / `WorldSound` 가 전부 이걸 본다.
- **Bullet / Effect가 고정 크기 풀**이라 네트워크 ID 매핑이 자연스럽다.
- **Lua가 시뮬레이션을 안 건드린다** (위 1장).

---

## 4. 미결 사항

- Phase 2 데이터 Lua화의 구체 설계 (등록 API 형태, JSON↔Lua 병합 규칙)
- PVP 규칙 (팀 구성, 리스폰, 승패 판정) — Phase 3-B 진입 시 결정
- 트랜스포트 / 접속 방식 (직접 IP, 릴레이, 로비)
- 최대 인원
