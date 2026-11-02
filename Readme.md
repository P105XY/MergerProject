# 프로젝트 현황과 과거 메카 작업 기록

- 최종 문서 갱신: 2026-11-01 (Asia/Seoul)
- 현재 프로젝트는 메카 프로젝트가 아니다. 아트 방향은 할로우 나이트를 참고한 스프라이트 기반 2D이며, 구체적인 대상·외형과 애니메이션 동작은 아직 지정되지 않았다.
- 아래 메카 모델·리그·조립 관련 내용은 과거 작업 기록이며 현재 프로젝트의 아트 방향이나 구현 목표가 아니다.
- 이 문서는 현재 규격, 파일 위치, 확인된 결과, 남은 작업을 다음 대화에 전달하는 기준이다.
- 작업 방식은 [AGENTS.md](AGENTS.md)를 따른다. 모델 제작·임포트 및 이전 조립 검증은 과거 보고서다. 기본 조립 기록은 `Artifacts/AssemblyFoundation/`, 개별 교체·해제의 최신 검증은 `Artifacts/AssemblyPartOperations/`에 기록한다.

## 현재 프로젝트 확인 — 2026-10-30

현재 작업 폴더의 `Assets/Function/`에는 플레이어·조각 수집과 적 관련 코드가 있다. [PlayerBase.cs](Assets/Function/Player/PlayerBase.cs)는 2D 횡스크롤 좌우 이동·점프, 공격 요청과 조각 수집·특수 사용 관리를 제공한다.

- 방향키 좌우로 이동하고 S를 눌러 지상 점프한다. `Rigidbody2D`와 `Collider2D`가 필요하며 중력·충돌체와 `Ground Layers`를 설정해야 한다. 이동 속도와 점프 속도는 Inspector에서 지정한다. 지면은 아래 방향 0.05 단위 캐스트와 위쪽 법선으로 판정한다.
- A를 누르고 있으면 `Attacks Per Second` 횟수/초로 마지막 좌우 이동 방향의 사각 범위를 검사하고 각 `EnemyBase`에 `AttackPower`만큼 피해를 한 번씩 적용한 뒤 `AttackRequested` 이벤트를 발생시킨다. 적의 여러 Collider가 겹쳐도 한 공격에서 중복 피해를 주지 않으며 트리거는 검사하지 않는다. `Attack Reach`, `Attack Size`, `Attack Layers`는 Inspector에서 설정한다. 조합이 준비된 동안 D를 누르면 특수 기능을 한 번 사용한다. 특수 효과는 별도 구현해야 한다.
- 최대 체력은 Inspector에서 지정하고 시작 시 현재 체력으로 초기화한다. `ApplyDamage`는 양수 유한 피해를 적용하며 0 미만으로 내려가지 않는다. 사망 처리는 없다.
- 드롭된 조각에 닿으면 A/B/C별 개수를 누적한다. `TypeACount`, `TypeBCount`, `TypeCCount`는 아직 조합되지 않은 수집 개수다. 기존 Inspector `GameObject` 배열은 런타임 수집 개수로 대체했으며 배열에 입력했던 참조는 자동 이전되지 않는다. 현재 Assets의 씬·프리팹·데이터 에셋에서 해당 필드 저장은 발견되지 않았다.
- 총 3개를 모으면 순서와 무관한 `ActiveCombination`의 A/B/C 개수로 조합이 확정되고 수집 개수는 0으로 바뀌며 `SpecialUsesRemaining`이 3이 된다. 준비 중에는 추가 조각을 수집하지 않으며, 조각은 월드에 남는다. 세 번 사용하면 조합이 초기화되고 다시 수집한다. `IsSpecialAvailable`은 UI 활성화에 사용할 수 있다.
- `SpecialUsed` 이벤트는 사용할 때마다 플레이어와 확정된 `FragmentCombination`을 전달한다. 세 번째 사용에서도 해당 조합을 전달한다. 기존 `SpecialRequested` 이벤트도 유효한 사용마다 발생한다. 조합별 구체적 효과는 이벤트 구독자가 구현해야 하며, 구독자가 없어도 사용 횟수는 소비된다. `TryCollectFragment`와 `TryUseSpecial`은 외부 코드에서 호출할 수 있다.
- 새 Input System 키보드를 직접 읽는다. 키 변경, 게임패드, 캐릭터 프리팹 연결 및 애니메이션은 미구현이다. Rigidbody2D는 Dynamic 및 적절한 회전 고정과 지형 충돌 설정이 필요하다.

`Mech.Function.EnemyBase`는 Inspector에서 최대 체력, 그로기 수치, 그로기 저항, 공격력, 공격 속도를 설정한다. 기본값은 각각 100, 0, 0, 10, 1이며 수치 단위와 밸런스는 아직 확정되지 않았다. 현재 체력은 Awake에서 최대 체력으로 초기화하고 `ApplyDamage`는 양수 유한 피해를 적용하여 0 이상으로 제한한다. 파생 클래스에서 Awake를 재정의하면 `base.Awake()`를 호출해야 한다.

적마다 Inspector의 `Held Object Type`에서 A/B/C 중 하나를 선택하고 `Held Object`에 드롭할 조각 프리팹을 지정한다. 체력이 0이 되는 유효한 피해를 받으면 적 위치에 프리팹을 한 번 생성하고, 루트의 `FragmentPickup`을 추가 또는 초기화해 해당 종류를 전달한 뒤 `FragmentDropped` 이벤트를 발생시킨다. 미지정이면 경고하고 드롭하지 않는다. 개발자가 선택한 종류가 수집 종류의 기준이다.

조각 프리팹에는 활성 `Collider2D`가 필요하다. 플레이어와 조각이 겹칠 때 `OnTriggerStay2D`가 호출되도록 적어도 한 Collider를 Trigger로 설정하고, 물리 레이어 충돌 설정도 허용해야 한다. 플레이어의 `Rigidbody2D`는 기존 코드가 요구한다. 수집 성공 시 조각 오브젝트를 제거하며, 특수 기능이 준비된 동안 겹쳐 있는 조각은 사용 종료 후에도 계속 겹쳐 있으면 수집을 다시 시도한다. 실제 프리팹과 물리 설정은 아직 만들어지지 않았다.

적 FSM은 초기 `Idle`과 `Patrol`, `Chase`, `Attack`, `Groggy`, `Dead` 상태를 제공한다. `TryChangeState`로 수동 전환하며 정의되지 않은 값과 동일 상태는 거부하고, 성공 시 `StateChanged` 이벤트에 이전·새 상태를 전달한다. 치명적인 피해는 `Dead`로 전환하고 `IsDefeated`를 설정하며 `Defeated` 이벤트를 한 번 발생시킨다. 처치 후 재피해·다른 상태 전환은 거부한다. 단순한 수동 `Dead` 전환은 처치·드롭을 실행하지 않는다. 공통 적 오브젝트 자동 제거, 다른 상태 행동·자동 전환, 그로기 누적·회복·저항 계산은 미구현이다.

- 적 종류는 `FragmentEnemy`, `NormalEnemy`, `AdvancedEnemy : NormalEnemy`, `BossEnemy`의 네 클래스다. 조각 적은 실제 플레이어 피해를 기준으로 Inspector 체력을 조절해야 한두 번에 처치할 수 있다. 일반과 고급은 공통 데이터·처치를 상속하며 능력치 차이와 행동은 개발자가 설정·구현한다.
- [TestEnemy](Assets/Function/Enemy/TestEnemy.cs)는 `EnemyBase`를 직접 상속한다. Inspector에서 `Held Object Type`과 `Held Object`를 지정하면 Player 공격으로 체력이 0이 될 때 공통 처치 처리로 조각 하나가 드롭된다. 미지정 시 경고만 발생한다.
- 보스는 `Fragment Enemy Prefab`, 선택적인 `Fragment Spawn Point`, `Fragment Spawn Interval`을 Inspector에서 설정한다. 살아 있는 동안 기본 5초마다 조각 적 하나를 생성하며 지점 미설정 시 보스 위치를 사용한다. 프리팹 미설정 또는 주기 0 이하는 생성을 중단한다. `UpdateBossPattern()`은 파생 클래스에서 구현할 패턴 확장 지점이며 구체적 패턴은 아직 없다.
- 조합에 필요한 조각 수와 특수 사용 횟수는 현재 각각 코드 상수 3이다. N개로의 확장과 조합별 실제 효과·UI는 후속 작업이다.

이전 기록의 `Assets/Mesh/`, 파츠·조립·무기 코드와 `Artifacts/`는 현재 작업 폴더에서 발견되지 않았다. 이전 적 코드는 없었으며 이번에 새로 작성했다. 차이가 발생한 원인은 확인하지 못했다. 아래 1절의 기존 상태표·파일표, 2절의 디렉터리 예시 및 3–9절의 구현·검증·인계 설명은 **과거 기록과 목표 규격**으로 보존하며 현재 파일의 존재나 구현 완료를 뜻하지 않는다. 2절의 개발·명명·테스트 및 서식 규칙은 계속 적용한다. 과거 보고서는 현재 실행 결과가 아니다. 현재 작업 폴더에는 `.git`이 있다.

### 플레이 테스트 씬

[PlayTestScene](Assets/Scenes/TestScene/PlayTestScene.unity)을 Unity에서 열고 Play를 누른 뒤 Game 화면을 클릭한다. 방향키로 이동, S로 점프, A를 누르고 있으면 정면 공격, 조각 3개 수집 후 D로 특수 요청을 사용할 수 있다. 세 적은 각각 A/B/C 조각을 드롭하고, 테스트 표시에는 수집 개수와 특수 사용 횟수가 나온다. 이 씬은 각 적 체력 30, 플레이어 공격력 10·초당 공격 2회이며, 개발용 단색 도형과 씬 안의 조각 원본을 사용한다. 별도 조각 프리팹이나 확정 아트가 아니다. 씬 전용 [PlayTestDisplay.cs](Assets/Scenes/TestScene/PlayTestDisplay.cs)는 임시 도형·상태 표시와 적 외형 비활성화를 담당한다. 조합별 실제 특수 효과는 없다.

다음 작업은 사용자 요청에 따라 조합별 특수 효과와 UI를 구현하고 실제 캐릭터·조각 에셋을 연결하는 것이다. 기존 구현이나 에셋을 별도 요청 없이 복원하지 않는다.

## 1. 현재 상태와 빠른 시작

| 항목 | 현재 상태 |
| --- | --- |
| LAHIRE | 날렵한 외형, 팔다리 굽힘, 측면 볼륨 및 머리·어깨 수정본과 애니메이션 저장 |
| SUNSHINE | 공통 골격을 유지한 사각 외장 모델 저장. 기존 LAHIRE 파츠는 Blender 파일 안에도 숨김 보관 |
| 공통 리그 | 15개 본: 기존 14개 + chest 아래 back_weapon. 외장 슬롯은 기존 13개 유지 |
| 애니메이션 | Idle / Walk / Boost, Blender 데모 별도 |
| Blender 조립 | 명시적 소켓 마커를 사용하는 13개 슬롯 교체 도구 구현 및 검증 기록 있음 |
| Unity | 두 모델의 FBX·Generic Avatar·URP 재질·Controller·Prefab 저장, 공유 클립 검증 기록 있음 |
| Unity 런타임 조립 | 5개 파츠의 조립·개별 교체·해제·재장착 지원. 몸통 교체 시 나머지 파츠 재연결. 소스만 구현, 상세는 7절 |
| 조립 애니메이션 | 현재 기본 클래스에는 미연결. 기존 모델의 Avatar·Controller·클립은 보존 |
| 파츠 능력치 | 공통 기본 체력·무게와 역할별 수치 필드 추가. Inspector 입력 및 읽기 프로퍼티 제공. 합산·피해 처리는 미구현 |
| 부스터 파츠 | `BoosterPartBase`의 소모량·상승·전진 속도를 PlayerBase에서 사용. 조립기 부스터 슬롯은 미구현 |
| 무기 | 공통 피해량·사정거리·무게, 근접 쿨타임, 원거리 연사력. PlayerBase의 양팔·등 투사체 발사 및 피해 인터페이스 구현. 근접 공격·자동 장착·교체는 미구현 |
| 플레이어 | PlayerBase에서 직접 입력, 이동·점프·부스트·상승·하강 및 양팔/등 투사체 구현. 실제 사용에는 컴포넌트와 데이터 연결 필요 |
| 적 | EnemyBase에 체력·공격 능력치·무기 종류와 수동 FSM 틀 구현. 플레이어 투사체 피해 수신 가능, AI 행동은 미구현 |

### 사용할 파일

| 목적 | 파일 |
| --- | --- |
| 두 모델의 최신 15본 편집·등 무기 본 | [Mech_BackWeapon.blend](Artifacts/BackWeaponRig/Mech_BackWeapon.blend) |
| LAHIRE 원본 애니메이션 편집(이전 14본) | [TYPE_LAHIRE_animated.blend](Artifacts/TYPE_LAHIRE/TYPE_LAHIRE_animated.blend) |
| LAHIRE 파츠 조립 | [TYPE_LAHIRE_modular.blend](Artifacts/TYPE_LAHIRE/modular_parts/TYPE_LAHIRE_modular.blend) |
| 이전 14본 SUNSHINE 편집·조립 및 두 모델 데이터 확인 | [GAN01_SUNSHINE.blend](Artifacts/GAN01_SUNSHINE/GAN01_SUNSHINE.blend) |
| Unity LAHIRE 배치 | [LAHIRE.prefab](Assets/Mesh/LAHIRE/LAHIRE.prefab) |
| Unity SUNSHINE 배치 | [SUNSHINE.prefab](Assets/Mesh/SUNSHINE/SUNSHINE.prefab) |
| Blender 애니메이션 영상 | [TYPE_LAHIRE_animations.mp4](Artifacts/TYPE_LAHIRE/TYPE_LAHIRE_animations.mp4) |

Unity에서는 `.prefab`을 Scene/Hierarchy에 배치한다. 기본 상태는 Idle이다. Blender 데모는 1–330 프레임, 30fps이며 Space로 재생한다. 원본 LAHIRE가 SUNSHINE으로 덮어써진 것이 아니다. 이전 모델링 단계의 `.blend`와 `.blend1`도 보존되어 있다.

## 2. 환경과 디렉터리

| 항목 | 확인된 값 / 근거 |
| --- | --- |
| Unity | 6000.6.6f1, `ProjectSettings/ProjectVersion.txt` |
| URP | 17.6.0, `Packages/manifest.json` |
| Blender | 실제 실행 버전은 작업 시작 때 확인. 애드온의 최소 버전 표기는 4.2.0이며 이것이 전체 스크립트의 동작 보증 버전은 아님 |
| Blender 통신 | 사용 가능한 Blender MCP로 열린 파일·씬을 먼저 조회 |
| Unity 통신 | 로컬 클라이언트 `Artifacts/UnityImport/unity_mcp_client.py`와 패키지 `Packages/com.coplaydev.unity-mcp/`가 있음. 설치 파일 존재와 현재 서버 연결 상태는 별개 |

```text
Mech/
├─ AGENTS.md
├─ Readme.md
├─ Assets/
│  ├─ Function/Enemy/ # EnemyBase.cs (적 능력치·피해 수신·FSM 틀)
│  ├─ Function/Player/ # PlayerBase.cs (이동·부스터·투사체 제어)
│  ├─ Function/PartBase/ # 공통 PartBase + 5개 조립 파츠 + BoosterPartBase
│  ├─ Function/WeaponBase/ # WeaponBase + 근접/원거리/등 무기 기본 클래스
│  ├─ Function/Assembly/ # MechAssembler.cs, BackWeaponMount.cs
│  ├─ Editor/MechModelImport.cs
│  └─ Mesh/
│     ├─ LAHIRE/    # LAHIRE.fbx, .prefab, .controller, Materials/, materials.json
│     └─ SUNSHINE/  # SUNSHINE.fbx, .prefab, .controller, Materials/, materials.json
├─ Artifacts/
│  ├─ Assembly/     # 이전 구현의 과거 보고서·프리뷰
│  ├─ EnemyBase/ # 적 기본 클래스 검증 코드와 결과
│  ├─ PlayerBase/ # 임시 테스트 코드와 검증 결과
│  ├─ BackWeaponRig/ # 최신 15본 원본·프로필·변경 전 백업·검증·프리뷰
│  ├─ AssemblyFoundation/ # 기본 조립 검증 코드·이전 결과
│  ├─ AssemblyPartOperations/ # 개별 교체·해제 검증 코드·최신 결과
│  ├─ TYPE_LAHIRE/  # 단계별 원본, 스크립트, 프리뷰, 애니메이션
│  │  └─ modular_parts/ # 조립 애드온, 소켓 프로필, 테스트
│  ├─ GAN01_SUNSHINE/   # 원본, 제작 스크립트, 프리뷰, 검증
│  └─ UnityImport/     # 내보내기, 통신 도구, 검증 결과, Unity 프리뷰
├─ Packages/
└─ ProjectSettings/
```

`Library/`, `Temp/`는 재생성 가능한 Unity 작업 데이터다. 원본 제작 자료는 Assets 밖의 Artifacts에도 있으므로 Assets만 복사하면 Blender 원본과 작업 기록이 빠진다. 보관 시 두 문서, Assets와 그 `.meta`, Artifacts, Packages, ProjectSettings를 함께 보관한다. 현재 루트에 `.git`은 확인되지 않았으며 버전 관리 도입은 별도 미진행 사항이다.

### 개발·명명·테스트 규칙

1. 프로젝트에서 작성하거나 수정하는 모든 클래스 이름은 **PascalCase**로 작성한다. 예: `MechMovementController`, `ModularPartValidator`. 외부 패키지와 Unity·Blender API의 기존 이름은 해당 API 규약을 유지하며, 기존 클래스의 일괄 변경은 별도 요청 없이 수행하지 않는다.
2. 구현과 기술 판단은 가능한 한 인터넷에서 확인한 **공식 문서와 원 제작자의 자료**를 근거로 진행한다. Unity·Blender·사용 라이브러리의 적용 버전을 확인하고 해당 버전의 문서·API·공식 소스를 우선한다. 참고한 링크와 적용 근거는 관련 작업 기록에 남기고, 확인하지 못한 내용과 추정은 구분한다.
3. 소스코드·클래스를 추가할 때는 클래스·메서드·변수의 이름만 읽어도 역할, 동작, 값의 의미를 파악할 수 있도록 구성한다. 예: `ValidateSocketCompatibility`, `maximumMovementSpeed`. 기능 설명을 주석에만 의존하지 않으며, 필요한 설계 이유·제약에 대한 주석은 허용한다.
4. 테스트 코드를 작성할 때는 각 테스트의 **입력값과 기대 결과를 주석으로 명시**한다. 실행 결과에도 입력값·기대값·실제값을 출력하고, assertion으로 성공·실패를 판정한다. 예: `입력: 이동 속도 -1 → 기대: 유효성 검사 false → 실제: false`. 테스트를 실행하지 않았다면 예상 결과와 실제 실행 결과를 구분하여 기록한다.
5. 앞으로 새로 만드는 함수·메서드에는 선언 바로 위에 **어떤 기능을 하는지 설명하는 한국어 한 줄 주석**을 작성한다. 언어에 맞는 한 줄 주석 문법을 사용하며, 역할을 드러내는 이름도 유지한다.

이 규칙은 이후 코드 작성·수정과 테스트 추가의 기준이다. 이번 규칙 추가만을 이유로 기존 코드·에셋을 일괄 재작성하지 않는다.

### 제어문 서식과 산출물 범위

- `if`, `for`, `while` 제어문 블록과 앞뒤의 다른 문장·제어문 블록 사이에는 빈 줄 한 줄을 둔다.
- 여는 중괄호는 조건문·반복문과 같은 줄에 쓰고, 닫는 중괄호는 별도 줄에 쓴다. 본문은 들여쓰기하며, 예시처럼 여는 중괄호 바로 다음과 닫는 중괄호 바로 앞에는 빈 줄을 넣지 않는다. 한 문장짜리 본문에도 중괄호를 사용하고 한 줄로 압축하지 않는다.

```csharp
if (canAssemble) {
    AssembleSelectedParts();
}

for (int partIndex = 0; partIndex < selectedParts.Count; partIndex++) {
    ValidatePart(selectedParts[partIndex]);
}

while (pendingParts.Count > 0) {
    AttachPart(pendingParts.Dequeue());
}
```

- 소스코드 작성 요청에는 소스코드만 작성한다. 편의·시연 목적의 샘플 프리팹, 씬, 샘플 데이터 에셋을 임의로 추가하지 않는다.
- 프리팹 제작 요청에는 프리팹만 제작한다. 이를 이유로 요청하지 않은 새 소스코드를 함께 추가하지 않는다.
- 소스코드와 프리팹이 모두 필요하면 각각 필요한 이유와 생성할 파일 범위를 설명하고, 사용자에게 물어 확인받은 뒤 함께 진행한다. 이미 두 산출물의 범위가 명시적으로 승인된 경우 같은 확인을 반복하지 않는다.
- 샘플은 사용자가 요청한 범위에서만 만든다. 기능 구현이나 검증이 필요하다는 이유만으로 샘플 생성을 승인받은 것으로 간주하지 않는다.
- 이 규칙은 이후 작업에 적용한다. 기존 소스의 일괄 서식 변경이나 기존 샘플 삭제는 별도 요청 없이 수행하지 않는다. 기존 예제 생성 메뉴의 실행도 샘플 생성 요청 범위에 포함될 때만 수행한다.

## 3. 모델 외형의 의도

- **LAHIRE:** TYPE-LAHIRE를 참고한 해석 모델. 날렵한 실루엣과 다리 비율, 굽힌 팔·다리, 슬림한 몸통, 측면에서도 충분한 두께, 날렵한 어깨·머리를 목표로 수정했다. 외형 수정 중 간소화한 본은 유지했다.
- **SUNSHINE:** GAN01-SUNSHINE-L 참고, 사각 장갑·무장 외장·각진 발을 우선한다. 조인트의 작은 다각형 힌지는 남아 있다. 기존 본 길이와 관절 간격을 유지했으므로 원작 비율을 그대로 복제한 모델은 아니다.
- 두 모델 모두 구현과 검증 기록이 있다는 의미이며, 원작과의 완전한 일치나 모든 외형의 사용자 최종 승인을 의미하지 않는다.

사용자 지정 참고: [TYPE-LAHIRE](https://namu.wiki/w/TYPE-LAHIRE), [GAN01-SUNSHINE](https://namu.wiki/w/GAN01-SUNSHINE).
SUNSHINE 기존 제작 기록에는 나무위키 페이지를 읽지 못하여 [Kotobukiya 제품 이미지](https://www.kotobukiya.co.jp/en/product/detail/p4934054062966/) 등을 사용했다고 명시되어 있다. 상세 근거는 [기존 안내](Artifacts/GAN01_SUNSHINE/README.txt)를 참조한다.

## 4. 공통 골격 규격

현재 규격은 기존 14개 본에 등 무기용 `back_weapon`을 추가한 15본이다. Blender Armature 오브젝트 이름은 내보내기 스크립트 호환을 위해 `LAHIRE_RIG | 14 bones`로 유지하며, 이름의 숫자는 현재 개수를 뜻하지 않는다. SUNSHINE도 이 리그를 재사용한다. Unity 내보내기는 복제 리그를 `MechRig`로 명명한다. root는 제어용 본이며 대응하는 외장 슬롯은 없다.

```text
root
└─ pelvis
   ├─ chest
   │  ├─ head
   │  ├─ back_weapon
   │  ├─ upper_arm.L
   │  │  └─ forearm.L
   │  └─ upper_arm.R
   │     └─ forearm.R
   ├─ thigh.L
   │  └─ shin.L
   │     └─ foot.L
   └─ thigh.R
      └─ shin.R
         └─ foot.R
```

- 본 이름은 대소문자·점·L/R까지 정확하게 유지한다. 이름뿐 아니라 부모 계층, REST 행렬, 본 길이·롤, 애니메이션 경로도 호환성의 일부다.
- 현재 기계 판독 기준은 [15본 socket_profile.json](Artifacts/BackWeaponRig/socket_profile.json)의 `rig_bones`, `slots`, `mounts`다. [이전 14본 프로필](Artifacts/TYPE_LAHIRE/modular_parts/socket_profile.json)은 과거 원본의 기준으로 보존한다. 기존 14본과 13개 슬롯의 좌표는 동일하다. REST 행렬과 소켓 목표 좌표를 문서에 중복 복사하지 않는다.
- 프로필의 REST 행렬은 Blender Armature 공간 기준이다. Unity 변환 행렬과 좌표 변환 없이 직접 비교하지 않는다.
- Blender는 +Z 위, 모델/걷기 진행 방향은 -Y 기준이다. Unity 결과는 +Y 위, +Z 정면이며 프리팹 루트의 기본 변환을 기준으로 한다.
- 파츠마다 해당 본 이름의 정점 그룹 하나, 모든 정점 웨이트 1을 사용한다. 외장은 강체로 움직인다.
- `back_weapon`은 `chest`의 비연결·비변형 자식 본이다. Blender Armature 공간 head `(0, 1.35, 5.5)`, tail `(0, 1.35, 5.95)`, roll 0으로 몸통 뒤(+Y)에 둔다. 외장 웨이트는 배정하지 않으며 무기 오브젝트의 부모로 사용한다.
- 13개 슬롯: `pelvis`, `chest`, `head`, `upper_arm.L`, `forearm.L`, `upper_arm.R`, `forearm.R`, `thigh.L`, `shin.L`, `foot.L`, `thigh.R`, `shin.R`, `foot.R`.
- Unity 파츠 이름은 `Part_` 뒤에 슬롯의 점을 밑줄로 치환한다. 예: `upper_arm.L` → `Part_upper_arm_L`.

골격을 유지하면서 긴 파츠를 장착하는 현재 방식은 메시를 기존 관절 간격에 맞춘다. 새 파츠 길이 그대로 캐릭터 키·어깨 폭·보폭을 바꾸려면 새 골격 프로필, 바인딩 및 애니메이션/접지 재검증이 필요하다.

## 5. Blender 파츠 조립 규격

도구: [mech_parts.py](Artifacts/TYPE_LAHIRE/modular_parts/mech_parts.py), 설치용 [mech_parts.zip](Artifacts/TYPE_LAHIRE/modular_parts/mech_parts.zip), 상세 [사용안내.txt](Artifacts/TYPE_LAHIRE/modular_parts/사용안내.txt).

애드온을 설치·활성화하거나 `.blend`의 Text Editor에서 내장 `mech_parts.py`를 실행한 뒤 3D View의 `N > Mech Parts`를 사용한다. 이전 세션의 등록 상태는 재실행 후 보장되지 않는다.

1. Object Mode에서 슬롯과 Source mesh를 지정한다. 소스는 한 개 강체 세그먼트이며, 기존 리그·모디파이어·제약·애니메이션은 작업용 복사본에서 정리한다.
2. `Create source socket markers`로 마커를 만들고 실제 관절에 배치한다. 자동 생성 위치는 예시이며 관절 자동 인식 결과가 아니다.
3. `Sockets placed on actual joints`를 확인한 뒤 `Check socket fit`을 실행한다.
4. 적합한 Fit 모드로 `Fit and replace part`를 실행한다. 이전 파츠는 숨김 보관하며 `Restore previous part`로 복원한다.
5. 세 액션에서 파츠 간 간섭과 발바닥 접지를 시각적으로 확인한다.

| 슬롯 | 필수 소켓 역할 |
| --- | --- |
| pelvis | mount, chest, hip.L, hip.R |
| chest | mount, neck, shoulder.L, shoulder.R |
| head | mount, tip |
| upper_arm.L/R | mount, joint_out(팔꿈치) |
| forearm.L/R | mount, tip |
| thigh.L/R | mount, joint_out(무릎) |
| shin.L/R | mount, joint_out(발목) |
| foot.L/R | mount, tip, sole |

마커는 소스 메시의 직접 자식 Empty이며 `mp.socket` 속성으로 역할을 표시한다. mount의 로컬 +Y는 대응 본의 head→tail 방향이고 +X/+Z로 비틀림 방향을 맞춘다. tip은 외장 끝이 아니라 본 길이의 기준이다. sole은 발바닥 접지 기준이다.

| Fit 모드 | 처리 |
| --- | --- |
| Rigid only | 위치·회전만 정렬, 길이가 맞지 않으면 거부 |
| Uniform scale | 균일 배율로 정렬, 연결점 불일치 시 거부 |
| Fit joint axes | mount의 축별 배율로 조정. 형상 비율이 바뀔 수 있음 |

기본 소켓 허용 오차는 **0.005 Blender 단위**, 배율 범위는 **1/4–4배**다. 누락/중복 소켓, 다른 슬롯·좌우, 미확인 마커, 음수·붕괴·과도한 스케일, 전단 마커, 호환되지 않는 연결점, Shape Keys나 기존 변형 설정이 남은 소스 등을 거부한다. 외장 충돌의 자동 해결, 임의 조인트 인식, 연성 케이블·천, 추가 팔·역관절·4족은 현재 범위 밖이다.

## 6. 애니메이션 규격

기준은 30fps다. Blender의 루프 끝 중복 키를 포함한 범위와 반복 구간을 구분한다.

| Unity 클립 | Blender Action | 키 범위 | 길이 | 반복/이동 |
| --- | --- | --- | --- | --- |
| Idle | `01_IDLE_Loop` | 1–91, 91은 1과 동일 | 3초 | 반복 |
| Walk | `02_WALK_InPlace_Loop` | 1–61, 61은 1과 동일 | 2초 | 반복, 제자리 |
| Boost | `03_BOOST_Dash` | 1–91 | 3초 | 비반복, 수직 동작 포함 |
| 내보내지 않음 | `00_DEMO_Idle_Walk_Boost` | 1–330 | 데모 약 11초 | 이동·카메라 연출 포함 |

Blender 기존 안내의 걷기 이동 참고값은 로컬 -Y로 1.05 Blender 단위/초다. 데모의 부스터 구간은 전방 6단위 이동한다. 이 값은 게임 이동 코드의 구현 완료를 의미하지 않는다.

Unity Controller는 `Idle`, `Walk`, `Boost`라는 Trigger와 같은 이름의 상태를 가진다. 기본 Idle, AnyState에서 Trigger로 전환, 자기 상태 재전환 차단, Boost는 exitTime 1에서 Idle로 복귀한다. 코드의 전환 duration 값은 0.12이며 단위를 변경할 때 `hasFixedDuration` 설정도 확인한다.

```csharp
// 해당 캐릭터 Animator 참조를 확보한 게임 코드에서 호출
animator.SetTrigger("Walk");
animator.SetTrigger("Boost");
animator.SetTrigger("Idle");
```

`Animator.applyRootMotion = false`. 수평 이동은 게임 코드에서 구현해야 한다. Boost의 수직 자세 움직임은 본에 유지된다. Blender의 `boost_power` 기반 분사 Shape Key 드라이버·무대·카메라·조명은 Unity 모델 FBX에 포함하지 않는다.

## 7. Unity 내보내기·재임포트 절차

### 내보내기

[export_unity.py](Artifacts/UnityImport/export_unity.py)는 **Blender 내부에서 실행하는 스크립트**다. 일반 Python에서 직접 실행하지 않는다. 실행 전 저장된 작업과 대상 씬을 확인한다.

- 기존 스크립트는 프로젝트 절대 경로와 오브젝트 이름을 하드코딩한다. 프로젝트 이동·파츠 교체 후에는 이를 먼저 수정한다.
- 필요한 원본은 `LAHIRE_RIG | 14 bones`, LAHIRE의 `LAHIRE A6 | <slot>`(머리·상완) 및 `LAHIRE V5 | <slot>`(그 외), `SUNSHINE | <slot>`, 세 원본 Action이다. 선택된 임의 메시를 자동 수집하는 범용 내보내기가 아니다.
- 두 모델을 포함하는 SUNSHINE 원본을 출발점으로 삼되, 실행 전 실제 오브젝트 이름과 데이터 존재를 확인한다.
- 임시 복제 리그를 Blender Z축으로 180도 회전하여 Unity +Z 정면을 맞춘다. 원본 리그 자체를 회전하지 않는다.
- 선택된 메시·Armature만 FBX로 출력하고 leaf bone은 추가하지 않는다. `axis_forward='-Z'`, `axis_up='Y'`, 단위 적용, global scale 1, 세 NLA 액션만 베이크한다. 키 간격 1, 단순화 0이다.
- `Assets/Mesh/<모델명>/<모델명>.fbx`와 `materials.json`을 생성하고 임시 데이터를 정리한다. 종료 시 프레임·선택을 복원한다.
- 마지막으로 `Artifacts/UnityImport/pending.txt`를 기록한다. Unity 편집기 도구가 이를 감지하면 재빌드할 수 있으므로 실행 전에 아래 재작성 범위를 확인한다.

### Unity 처리

도구: [MechModelImport.cs](Assets/Editor/MechModelImport.cs).

1. Unity가 컴파일을 마친 상태에서 `Tools > Mech > Rebuild LAHIRE and SUNSHINE Assets`를 사용한다. pending 작업은 편집기에서 자동 처리될 수도 있다.
2. 도구는 Generic / Create From This Model / motion node `root`로 설정한다. 본 계층을 보존하고 Optimize Game Objects는 끄며 애니메이션 압축은 Off다.
3. `materials.json`의 값을 URP/Lit 재질로 재구성하고 FBX 재질에 연결한다. roughness를 smoothness로 변환하고 색 공간을 처리한다.
4. Controller의 기존 상태와 파라미터, 모델 Prefab을 재작성한다. 수동 게임 로직·Controller 확장을 추가한 후에는 그대로 덮어쓰지 말고 파생 Prefab이나 별도 Controller 등으로 사용자 수정분을 보존한다.
5. 완료 여부는 [unity_import_report.json](Artifacts/UnityImport/unity_import_report.json)의 `success`, `error` 및 각 모델 결과를 확인한다. 단순 파일 존재만으로 성공을 판단하지 않는다.
6. 필요하면 `Tools > Mech > Render Imported Model Previews`로 프리뷰를 갱신한다.

기존 두 모델은 각자 Avatar·Controller·FBX 내 클립을 유지한다. 현재 조립 코드는 아래 기본 클래스 단계이며 기존 모델의 본이나 렌더러에 연결하지 않았다.

### Unity 5개 파츠 조립 — 기본 클래스 및 개별 교체·해제

기본 클래스 재구현 작업을 시작했을 때 `Assets/Function/`은 비어 있었다. 이전 코드·샘플의 삭제 원인은 확인되지 않았으며 복구하지 않았다. 사용자의 재구현 범위에 맞춰 소스만 작성했다. 과거 32조합·애니메이션 검증 결과는 현재 구현의 결과가 아니다.

네임스페이스는 `Mech.Function`이다. [PartBase.cs](Assets/Function/PartBase/PartBase.cs)는 공통 추상 MonoBehaviour이며 연결점으로 부모를 바꾸고 로컬 위치·회전을 정렬한다. 아래 5개 클래스는 이를 상속하는 확장 가능한 기본 컴포넌트다.

| 파츠 | 기본 클래스 | 역할 |
| --- | --- | --- |
| 머리 | [HeadPartBase](Assets/Function/PartBase/HeadPartBase.cs) | 몸통의 HeadAttachment에 결합 |
| 몸통 | [BodyPartBase](Assets/Function/PartBase/BodyPartBase.cs) | 머리·왼팔·오른팔·다리의 4개 연결점 보관 |
| 왼팔 | [LeftArmPartBase](Assets/Function/PartBase/LeftArmPartBase.cs) | 몸통의 LeftArmAttachment에 결합 |
| 오른팔 | [RightArmPartBase](Assets/Function/PartBase/RightArmPartBase.cs) | 몸통의 RightArmAttachment에 결합 |
| 다리 | [LegPartBase](Assets/Function/PartBase/LegPartBase.cs) | 양쪽 다리 한 세트를 LegsAttachment에 결합 |

[MechAssembler.cs](Assets/Function/Assembly/MechAssembler.cs)의 `TryAssemble(head, body, leftArm, rightArm, legs, out error)`가 다섯 인스턴스를 조립한다. 자동 Start 실행이나 에셋 생성은 없다.

1. 같은 씬에 있는 독립적인 파츠 루트 5개와 별도 MechAssembler를 전달한다. 각 루트에 해당 기본 클래스 또는 파생 클래스가 있어야 한다. 프로젝트의 프리팹 에셋 자체는 입력으로 사용할 수 없다.
2. BodyPartBase의 직렬화 필드 또는 `SetAttachments(head, leftArm, rightArm, legs)`로 몸통 내부의 서로 다른 자식 Transform 4개를 지정한다. 머리·어깨·다리 연결점의 실제 위치는 제작자가 지정한다.
3. 검증 성공 시 몸통은 assembler 아래로, 나머지 네 파츠는 몸통의 각 연결점 아래로 붙는다. 파츠 루트의 로컬 위치는 0, 회전은 identity가 된다. 최초 장착 때 로컬 스케일을 기억하고 이후 재장착·몸통 교체에서도 같은 값을 사용한다. 부모 스케일은 상속되므로 장착 전후 월드 크기가 같다는 보장은 없다. 파츠 원점·축·장착용 크기는 최초 장착 전에 준비한다.
4. 누락 파츠, 잘못되거나 중복된 연결점, 중첩·순환 계층, 다른 조립기가 소유한 파츠를 거부한다. 검증 실패 시 Transform과 조립 상태를 변경하지 않고 false와 오류를 반환한다. 현재 참조는 Head / Body / LeftArm / RightArm / Legs로 조회한다. IsAssembled는 5개 슬롯에 살아 있는 참조가 모두 있는지 계산하며, 해제하면 false, 모두 재장착하면 true가 된다. PartBase.Owner는 현재 소유 조립기이며 개별 해제 후 null이다.

**개별 교체·해제 API**

| 슬롯 | 교체 또는 빈 슬롯 장착 | 해제 |
| --- | --- | --- |
| 머리 | TryReplaceHead | TryDetachHead |
| 몸통 | TryReplaceBody | TryDetachBody |
| 왼팔 | TryReplaceLeftArm | TryDetachLeftArm |
| 오른팔 | TryReplaceRightArm | TryDetachRightArm |
| 다리 세트 | TryReplaceLegs | TryDetachLegs |

- 교체 서명: `TryReplaceHead(HeadPartBase replacement, out HeadPartBase removedPart, out string error)`. 나머지도 해당 파츠 타입을 사용한다. 해제 서명: `TryDetachHead(out HeadPartBase removedPart, out string error)`.
- 교체 성공 시 이전 파츠를 removedPart로 반환한다. 빈 슬롯을 채웠다면 removedPart는 null이다. 해제 성공 시 해당 슬롯을 비우고 제거한 파츠를 반환한다. 파츠를 Destroy하거나 비활성화하지 않는다.
- 교체되어 나온 파츠·해제 파츠는 씬 루트로 이동하며 `SetParent(null, true)`로 월드 위치·회전·스케일을 유지한다. 회전과 비균일 스케일이 결합된 전단 변환은 Unity Transform의 표현 한계가 있으므로 모든 계층의 정확한 월드 행렬 보존을 보장하지 않는다. 검증은 균일 스케일과 회전 조합에서 수행했다.
- **몸통 교체:** 장착 중인 머리·양팔·다리를 새 몸통의 각 연결점으로 옮긴 뒤 이전 몸통을 분리한다. 비어 있던 슬롯은 계속 비어 있다. 모든 사전 검증을 통과한 후에만 계층을 변경한다.
- **몸통 해제:** 나머지 장착 파츠의 참조·소유권을 유지하고 조립기 루트 아래로 이동해 월드 자세를 유지한다. 몸통이 없는 동안 이 파츠들은 개별 해제할 수 있지만 새 머리·팔·다리 장착은 거부한다. 새 몸통을 장착하면 남은 파츠를 해당 연결점으로 다시 정렬한다.
- null 후보, 이미 장착된 동일 후보, 다른 조립기가 소유한 후보, 다른 씬의 후보, 현재 파츠와 중첩된 후보는 실패한다. 빈 슬롯을 다시 해제해도 실패한다. 실패 시 removedPart=null이고 error에 이유를 제공한다.
- 완전 빈 조립기는 TryReplaceBody부터 시작해 개별 슬롯을 채울 수도 있다. TryAssemble은 모든 슬롯이 비어 있을 때만 허용하며, 부분 조립 상태에서는 개별 교체 API를 사용한다. 전 슬롯 해제 후에는 반환된 파츠로 TryAssemble을 다시 호출할 수 있다.

**현재 범위:** 파츠 루트 Transform의 조립·교체·해제까지 구현했다. 조립 상태는 런타임 메모리 기준이며 저장·로드/도메인 리로드 복구는 미구현이다. 외부 코드가 장착 파츠의 부모·몸통 연결점을 직접 바꾸는 것은 지원하지 않으며, 감지한 불일치는 오류로 반환한다. 파츠 파괴 후 자동 생성·복구, 호출 도중 사용자 콜백에 의한 재진입, 물리 조인트·스키닝·애니메이션 통합은 지원 범위 밖이다. 개별 능력치의 직렬화 필드는 아래와 같이 추가했다. 조립기에는 프리팹·샘플 씬·데이터 에셋·선택 UI·전투 기능을 만들지 않았으며 PlayerBase의 별도 전투 범위는 아래를 따른다.

### 파츠 내부 데이터 — 2026-10-28

공통 정의는 `PartBase`에 두며 모든 파츠가 상속한다. 값은 static 공유값이 아니라 각 컴포넌트 인스턴스에 별도로 저장한다. 좌우 팔은 각 기본 클래스에 같은 종류의 필드를 가지며 값은 독립적이다.

| 적용 클래스 | 데이터 / 공개 읽기 프로퍼티 |
| --- | --- |
| 모든 파츠 (`PartBase`) | 기본 체력 `BaseHealth`, 무게 `Weight` |
| `HeadPartBase` | 락온 속도 `LockOnSpeed`, 락온 거리 `LockOnDistance` |
| `LeftArmPartBase`, `RightArmPartBase` | 반동 제어 `RecoilControl`, 명중률 `Accuracy`, 무기 적성 `WeaponAptitude` |
| `BodyPartBase` | 상태이상 저항력 `StatusEffectResistance`, 에너지 최대치 `MaxEnergy`, 에너지 회복 속도 `EnergyRecoveryRate` |
| `LegPartBase` | 걷기 속도 `WalkSpeed`; 기본 체력·무게도 상속 |
| [BoosterPartBase](Assets/Function/PartBase/BoosterPartBase.cs) | 에너지 소모량 `EnergyConsumption`, 상승 속도 `AscentSpeed`, 전진 속도 `ForwardSpeed`; 기본 체력·무게도 상속 |

- 모든 항목은 private `float` + `[SerializeField, Min(0f)]`이며 Inspector에서 입력하고 public getter로 읽는다. 파생 클래스에서도 getter로 접근할 수 있다.
- 구체적인 밸런스 값은 지정되지 않아 초기값은 0이다. `Min`은 Inspector의 입력 하한이며 런타임 수치 검증이나 피해 처리를 구현한 것은 아니다.
- 명중률·저항력·무기 적성은 우선 수치형 능력치다. 퍼센트 범위, 무기 종류별 분류, 락온 속도 환산식, 무게·거리·속도의 단위와 합성 공식은 아직 확정하지 않았다.
- 설계 목표는 각 파츠 체력의 총합을 플레이어 체력으로 사용하는 것이다. 기존 조립기는 머리 + 몸통 + 왼팔 + 오른팔 + 다리 세트의 5개 논리 파츠를 다룬다. 별도 부스터도 `PartBase`의 체력·무게를 상속하므로 후속 장착·합산 구현에 포함한다. 현재 `LegPartBase`는 양쪽 다리 한 세트이므로 중복 합산하지 않는다.
- 부스터 분리 시 다리의 `boostSpeed` 필드와 `BoostSpeed` 프로퍼티만 제거했다. 걷기 속도와 공통 체력·무게는 유지한다. 기존 에셋에서 `boostSpeed` 저장값·코드 참조는 발견되지 않아 자동 값 이전은 하지 않았다.
- 부스터는 PlayerBase에서 직접 참조하여 추진 및 에너지 차감에 사용한다. 소모량은 초당으로 해석하며, `MechAssembler`의 부스터 슬롯·장착·교체 API는 아직 없다. 기존 몸통의 에너지 최대치·초당 회복 속도를 사용한다.
- 이번 범위는 데이터 필드 추가까지다. 조립체 능력치 합산, 현재 체력·피해·회복, 파츠 교체 시 체력 정책 및 다른 능력치의 합성 규칙은 후속 구현 대상이다. `BaseHealth`는 피해를 받아 줄어드는 현재 체력이 아니다.
- 공식 근거: [SerializeField](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeField.html), [MinAttribute](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MinAttribute.html). 비공개 필드의 직렬화 및 Inspector 입력 하한 용도로 적용했다.

**모델 연결 시 주의:** 이 코드는 파츠 루트 Transform을 붙이는 단계다. 기존 LAHIRE/SUNSHINE의 SkinnedMeshRenderer를 단순 이동하는 것으로 공통 골격 조립이 완료되지는 않는다. 13메시를 5개 논리 파츠로 묶는 어댑터, bones/rootBone 재바인딩, 공통 애니메이션과 다리별 override는 현재 미구현이다. 기존 본 오브젝트를 이 파츠 루트로 지정해 이동시키지 않는다. 모델 통합 요청 시 기존 14본 및 새 등 무기 본의 경로·REST·기존 액션을 보존하는 별도 연결 구현과 검증이 필요하다.

공식 근거: [Unity 6 Transform.SetParent](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Transform.SetParent.html), [Transform.IsChildOf](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Transform.IsChildOf.html). SetParent의 로컬 변환 기준과 IsChildOf가 자기 자신에도 true를 반환한다는 동작을 반영했다. 설치된 6000.6.6f1에서도 두 API 존재를 reflection으로 확인하고 실제 조립을 검증했다. 해제는 SetParent의 worldPositionStays=true를 사용하며, 파츠 생존 여부는 [Unity Object의 null 비교](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object-operator_eq.html)를 따른다.

### 무기 내부 데이터 — 2026-10-29

무기는 `Mech.Function` 네임스페이스의 별도 [WeaponBase 폴더](Assets/Function/WeaponBase)에 정의한다.

```text
PartBase (공통 체력·무게)
└─ WeaponBase (추상 공통 데이터)
   ├─ MeleeWeaponBase
   ├─ RangeWeaponBase
   └─ BackWeaponBase
```

| 정의 클래스 | 데이터 / 공개 읽기 프로퍼티 |
| --- | --- |
| [WeaponBase](Assets/Function/WeaponBase/WeaponBase.cs) | 피해량 `Damage`, 사정거리 `Range`, 상속한 무게 `Weight`·기본 체력 `BaseHealth` |
| [MeleeWeaponBase](Assets/Function/WeaponBase/MeleeWeaponBase.cs) | 공통 데이터 상속 + 쿨타임 `Cooldown` |
| [RangeWeaponBase](Assets/Function/WeaponBase/RangeWeaponBase.cs) | 공통 데이터 상속 + 연사력 `FireRate` |
| [BackWeaponBase](Assets/Function/WeaponBase/BackWeaponBase.cs) | 공통 데이터 상속 + IsWeaponActive / SetWeaponActive(bool) |

- 기존 방식과 같은 private float + `[SerializeField, Min(0f)]`, public getter이며 초기값은 0이다. 값은 무기 인스턴스별로 독립적이다.
- 사용자가 지정한 클래스 이름 `RangeWeaponBase`를 그대로 사용한다. 세 파생 클래스는 나중에 개별 무기가 상속할 수 있다.
- 근접·원거리·등 무기 모두 `PartBase`의 직렬화된 `weight` 필드와 공개 `Weight` 프로퍼티를 상속한다. 각 무기 Inspector에서 Weight를 입력하며 `weapon.Weight`로 읽는다. 중복 무게 필드는 선언하지 않는다.
- `BackWeaponBase`는 `WeaponBase`를 직접 상속한다. 등 장착 위치만으로 발사 방식이 확정되지는 않으므로 연사력·쿨타임은 임의로 추가하지 않았다. 등 장착 본과 활성 상태에 따른 회전은 아래와 같이 구현했다. 자동 장착·교체·조립기 연결은 미구현이다.
- 사용자 확인에 따라 근접 무기는 연사력을 가지지 않고 쿨타임을 사용한다. Cooldown은 초 단위 대기 시간으로 해석한다. 원거리 FireRate의 단위·공격 주기 계산은 후속 공격 구현에서 확정하며, 실제 쿨타임 타이머·연사 동작은 아직 구현하지 않았다.
- 기존의 모든 파츠 체력·무게 규칙에 맞춰 `PartBase`를 상속한다. 총 체력·무게 합산, 근접 판정, 공격 애니메이션은 아직 미구현이다. 현재 PlayerBase의 원거리/등 투사체 발사와 피해 전달 범위는 아래 절을 따른다.
- 필드 직렬화의 공식 근거는 위 파츠 데이터 절의 SerializeField/MinAttribute 문서를 따른다. 프리팹·씬·샘플 데이터 에셋은 생성하지 않았다.

### 등 무기 본과 활성 회전

- [BackWeaponMount.cs](Assets/Function/Assembly/BackWeaponMount.cs)는 모델 루트 또는 `chest` 같은 **본의 조상 오브젝트**에 붙인다. `Part_chest` 메시 오브젝트는 본의 조상이 아니므로 그곳에 붙이지 않는다.
- `Rotation Bone`에는 `MechRig/root/pelvis/chest/back_weapon`, `Back Weapon`에는 그 본 아래에 배치한 `BackWeaponBase`를 지정한다. 코드 연결은 `TryBind(bone, weapon, out error)`이며, 올바르지 않은 본/무기 계층을 거부한다. 무기 Transform의 위치·축 맞춤은 제작자가 수행한다.
- 무기 `SetWeaponActive(true)`는 전개, `false`는 수납 상태다. 실제 GameObject 또는 무기 컴포넌트가 비활성화되거나 본에서 분리되면 수납 상태로 처리한다. 무기 참조가 없어도 수납 상태다. 이 플래그 자체는 발사/피해 처리를 실행하지 않으며 PlayerBase는 등 발사 입력을 이 상태에 반영한다.
- `Inactive Rotation Offset` 기본 `(0,0,0)`, `Active Rotation Offset` 기본 `(90,0,0)`, `Rotation Speed` 기본 `180`도/초. 로컬 REST 회전 뒤에 오프셋을 적용한다. 90도 전환은 기본 0.5초이며 속도 0은 회전을 정지한다. 무기 모델의 전방 축에 맞춰 각도를 변경한다.
- Animator 평가 후 `LateUpdate`에서 회전을 적용하며 진행 각도는 별도 보관한다. Idle/Walk/Boost가 본 값을 매 프레임 써도 전개 진행이 초기화되지 않는다. 컴포넌트를 끄면 캡처한 REST 회전으로 복구한다.
- `BodyPartBase.BackWeaponMount`는 Inspector로 연결할 참조를 제공한다. 기존 `MechAssembler`의 5슬롯 장착·교체 로직에 등 무기를 자동 편입하지 않았다.
- 기존 모델 프리팹은 FBX의 새 본을 상속한다. 프리팹에 새 스크립트나 무기 인스턴스를 자동 추가하지 않았다. 위 컴포넌트와 무기 참조를 설정한 인스턴스에서 회전한다. 기존 몸통 메시 안의 무장처럼 보이는 외장은 자동 분리하거나 새 본에 재웨이트하지 않았다.
- 최신 Blender 원본은 `Artifacts/BackWeaponRig/Mech_BackWeapon.blend`다. 이전 `.blend`는 14본 스냅샷이며 최신 Unity 모델을 내보낼 때 사용하지 않는다. 파츠 교체 애드온은 기존 14본과 `back_weapon`을 포함한 15본을 모두 허용하며 13개 슬롯은 유지한다.
- FBX만 갱신할 때 `export_unity.py`에 `PRESERVE_MATERIALS=True`, `QUEUE_UNITY_IMPORT=False`, 임시 `EXPORT_ROOT`를 전달해 확인 후 복사한다. 이번 작업은 재빌드 메뉴를 실행하지 않았고 기존 Prefab/Controller/재질/`.meta` 90개 파일의 해시가 모두 보존되었다. 재빌드 메뉴는 현재 15본을 검증하지만 기존대로 프리팹·Controller를 재작성하므로 주의한다.

### 플레이어 기본 조작

[PlayerBase.cs](Assets/Function/Player/PlayerBase.cs)는 `CharacterController`를 요구하고, 별도로 입력된 파츠·무기 데이터를 사용한다. 기존 모델 프리팹에 PlayerBase, CharacterController, 파츠와 무기 컴포넌트가 자동 설정되어 있지 않으므로 실제 게임에서 사용할 플레이어 인스턴스에 연결해야 한다. 이 작업은 소스 범위이며 프리팹·씬을 만들지 않았다.

- `Assembler`를 지정하면 현재 조립기의 Body/Legs를 사용하며, 지정하지 않으면 직접 `Body`/`Legs` 참조를 사용한다. 별도 `Booster`, 좌우 `RangeWeaponBase`, `BackWeaponBase`, 각 발사 위치와 전방(+Z)을 가리키는 `Muzzle`을 할당한다. `CharacterController`의 크기·중심을 모델과 지형에 맞춘다. 모든 파츠 수치는 기본 0이므로 Inspector에서 입력해야 한다.
- 기존 Input System 장치를 직접 읽는다. 키보드/마우스: **WASD 또는 방향키** 이동, **왼쪽 Shift** 부스트, **E** 상승, **C** 하강, **Space** 점프, 마우스 **왼쪽/오른쪽/가운데** 버튼으로 왼팔/오른팔/등 무기를 발사한다. 게임패드: 왼쪽 스틱 이동, 오른쪽 숄더 부스트, 북쪽 버튼 상승, 동쪽 버튼 하강, 남쪽 버튼 점프, 왼쪽/오른쪽 트리거 양팔, 왼쪽 숄더 등 무기. 입력 에셋을 수정하지 않았고 이 키는 현재 코드에 정해져 있어 액션 리바인딩은 미구현이다. `Read Default Input`을 끄고 외부 코드에서 `Simulate`를 프레임당 한 번 호출할 수도 있다.
- 이동은 플레이어 로컬 좌표 기준이며 다리 `WalkSpeed`를 쓴다. 부스트는 입력 방향으로 부스터 `ForwardSpeed`, 이동 입력이 없으면 플레이어 전방으로 추진한다. 상승은 부스터 `AscentSpeed`, 하강은 PlayerBase `Descent Speed`(기본 6), 점프는 `Jump Height`(기본 2)와 중력 `Gravity`(기본 20)로 계산한다. 지상에서만 점프한다. 카메라 조준/회전은 별도 기능이다.
- 몸통 에너지는 처음 몸통을 얻을 때 최대치로 채우고, 기존 에너지를 새 몸통 최대치에 맞게 제한한다. 부스트와 상승의 에너지 소비는 부스터 `EnergyConsumption`을 초당 기준으로 적용하며 동시에 사용해도 한 번만 차감한다. 잔량이 한 프레임 비용보다 작으면 그 비율만큼 추진한다. 추진 입력을 놓으면 몸통 `EnergyRecoveryRate`만큼 초당 회복한다. 소모량 0은 무료 추진이며 몸통 없이 양수 소모량 부스터는 작동하지 않는다.
- 원거리 무기는 `FireRate`를 초당 발사 수로 사용하고 0이면 발사하지 않는다. `BackWeaponBase`에는 연사력 데이터가 없으므로 PlayerBase의 `Back Shots Per Second` 기본 1을 사용한다. 등 발사 버튼을 누르는 동안 활성화 플래그도 켠다. 본 회전은 해당 등 무기와 [BackWeaponMount](Assets/Function/Assembly/BackWeaponMount.cs)가 별도 연결되어 있을 때 작동한다. 등 본의 전개 완료를 기다리는 지연은 없다. 근접 무기 공격은 이 발사 로직에 포함되지 않는다.
- 탄환/미사일은 발사 위치의 전방으로 일정 속도 직선 이동하며 경로를 연속 검사한다. `Left/Right Projectile Speed` 기본 60, `Back Projectile Speed` 기본 30 Unity 단위/초, 무기 `Range` 도달 시 소멸한다. 본인 계층과 시각 오브젝트는 충돌에서 제외하고, 트리거 Collider는 무시하며 `Weapon Hit Layers`로 충돌 대상을 지정한다. 기존 무기의 Damage를 발사 시 복사한다. 발사 지점이 다른 Collider 내부에 있으면 일반 Raycast처럼 그 Collider를 놓칠 수 있으므로 포구를 외부에 둔다. 유도·중력·폭발 범위는 미구현이다.
- 투사체 프리팹이 없는 경우 이동 및 충돌 판정은 보이지 않게 동작한다. 각 `Projectile Visual`에 시각 전용 프리팹을 지정하면 발사와 함께 생성·이동·소멸한다. Collider/Rigidbody를 통한 별도 물리 동작은 사용하지 않는 시각 전용 에셋을 연결한다. 타격 대상의 Collider 또는 조상에 `IWeaponDamageReceiver`를 구현한 활성 컴포넌트가 있으면 `ApplyWeaponDamage(damage, weapon)`을 호출한다. `EnemyBase`는 이 인터페이스를 구현하여 체력을 감소시킨다. 이펙트는 `WeaponFired`/`ProjectileHit` 이벤트에 연결할 수 있다.
- 기존 Idle/Walk/Boost Animator와의 자동 상태 전환, 근접 공격, 플레이어 파츠 체력 합산, 파츠 교체에 따른 무기 재장착, 피격 이펙트는 미구현이다. 플레이어 컴포넌트가 비활성화되면 활성 투사체와 등 무기 활성 상태를 정리한다.

### 적 기본 클래스와 FSM 틀

[EnemyBase.cs](Assets/Function/Enemy/EnemyBase.cs)는 `MonoBehaviour`이며 적 인스턴스의 Inspector에서 `Max Health`, `Attack Damage`, `Attack Range`, `Weapon Type`, `Attack Frequency`, `Attack Speed`를 설정한다. 기본 숫자는 모두 0이며 구체적인 능력치는 제작자가 입력한다. 공격 빈도와 공격 속도는 별도 필드로 보관하며 단위·계산식은 아직 정하지 않았다. 공격력과 사정거리는 적 기본 데이터이며 별도 무기 능력치와의 합산 규칙은 미구현이다.

- 무기 종류는 `None`, `Melee`, `Ranged`로 기록한다. 등 장착은 위치이므로 별도 공격 종류로 지정하지 않았다. 종류 선택만으로 무기가 생성·장착되거나 공격하지 않는다.
- `Awake`에서 현재 체력을 최대 체력으로 초기화한다. `ApplyWeaponDamage`는 양수 유한 피해를 받아 체력을 0 이상으로 제한하며, `IsDefeated`는 체력이 0 이하인지 나타낸다. 체력 0은 패배 여부만 나타내며 오브젝트 제거·상태 전환은 하지 않는다. 기본 최대 체력 0으로 두면 시작부터 패배 상태이므로 양수로 설정한다. 파생 클래스에서 `Awake`를 재정의할 때 `base.Awake()`를 호출한다.
- 플레이어 투사체의 충돌 Collider 또는 조상에 이 컴포넌트가 있고 활성화되어 있으면 `IWeaponDamageReceiver`를 통해 피해가 전달된다. Collider나 적 프리팹은 자동 생성하지 않는다.
- FSM은 초기 `Idle`과 `Patrol`, `Chase`, `Attack`, `Dead` 상태 이름, `CurrentState`, `TryChangeState`와 `StateChanged` 이벤트만 제공한다. 동일 상태와 정의되지 않은 값은 거부한다. 상태 행동, 전환 조건, 체력 0 시 `Dead` 자동 전환 등은 구현하지 않은 틀이다.

## 8. 검증 근거와 현재 한계

### 저장된 검증 결과

| 근거 | 기록된 결과 |
| --- | --- |
| [animation_validation.json](Artifacts/TYPE_LAHIRE/animation_validation.json) | 14개 본 REST 유지, Idle/Walk 루프 및 발 목표 오차, 데모 접지 이동 검증 |
| [조립 validation.json](Artifacts/TYPE_LAHIRE/modular_parts/validation.json) | 13개 슬롯, 세 액션의 합계 17개 프레임 비교. 최대 정점 오차 약 3.88e-6, REST/액션 키 유지, 거부 조건 및 패널 검사 통과 |
| [SUNSHINE validation.json](Artifacts/GAN01_SUNSHINE/validation.json) | 14본·13파츠, 기존 리그/액션 유지. Idle 91 / Walk 61 / Boost 91 프레임 발 높이 검사 |
| [Unity import report](Artifacts/UnityImport/unity_import_report.json) | success=true, 두 모델 모두 13 renderer·14 bone·유효 Avatar, 세 클립 및 공유 애니메이션 경로 확인, 최대 공유 자세 위치 차이 0 |

Unity 공유 검사는 LAHIRE의 세 클립을 두 프리팹에 적용하여 각각 0%, 25%, 50%, 75% 시점을 샘플링한다. 본 위치 차이 0.0001 초과 또는 회전 차이 0.01도 초과를 실패로 처리한다. 전체 프레임의 모든 외장 충돌이나 모든 Controller 전환을 검증한 것은 아니다.

저장된 Unity 보고서의 메시 정점 합계는 LAHIRE 66,696 / SUNSHINE 27,224, Idle 시 렌더러 bounds 높이는 약 7.81549 / 7.30481 Unity 단위다. 이는 당시 임포트 결과이며 고정 디자인 목표나 원작 실제 높이가 아니다. Blender 원본 정점 수와는 모디파이어·분할 때문에 다를 수 있다.

프리뷰: [LAHIRE Unity](Artifacts/UnityImport/LAHIRE_Unity.png), [SUNSHINE Unity](Artifacts/UnityImport/SUNSHINE_Unity.png), [SUNSHINE 측면](Artifacts/GAN01_SUNSHINE/GAN01_SUNSHINE_side.png).

### 개별 교체·해제 최신 검증

- [검증 코드](Artifacts/AssemblyPartOperations/verify_part_operations.cs), [실행 결과](Artifacts/AssemblyPartOperations/verification_results.json): Unity 6000.6.6f1에서 **새 검증 60/60 및 기존 기본 조립 회귀 검증 13/13 통과**. MCP execute_code로 임시 Preview Scene에서 assertion을 실행했으며 Test Runner/PlayMode 실행 결과가 아니다.
- 5개 슬롯 각각의 교체·반환 참조·소유 해제·월드 자세·나머지 파츠 유지, 개별 해제, 빈 슬롯 해제 거부, 재장착, 3회 반복 시 장착 스케일 유지, null·동일 후보·타 조립기 소유·다른 씬·중첩 후보 거부를 확인했다.
- 잘못된 몸통 연결점 4종, 몸통 없는 상태의 교체 거부, 대기 파츠 개별 해제, 부분 조립의 몸통 복구, 부분 조립에서 일괄 조립 거부, 몸통부터 개별 조립, 전체 해제 후 재조립, 외부 계층 수정 감지를 확인했다.
- 검증 코드의 첫 컴파일 시 구버전 GetInstanceID API가 오류로 거부되었다. GetEntityId로 수정 후 60건을 실제 실행했으며 최초 컴파일 실패는 통과 건수에 포함하지 않는다.
- 최종 Unity 콘솔 오류 0건. 임시 씬 정리 후 기존 SampleScene은 dirty=false, rootCount=3이다. 작업 전후 기존 프리팹·씬·FBX·Controller·.meta 92개 해시가 동일하며 해당 종류의 신규 파일도 없다.
- 모델 렌더링·애니메이션·물리·PlayMode 생명주기·비균일 스케일 전단 보존은 이번 검증 범위가 아니다.

### 기본 클래스 구현 당시 검증 — 2026-10-27

아래 보고서는 기본 클래스 구현 당시의 13건 실행 결과다. 이후 서식 수정에서는 기능 검증을 재실행하지 않았으며, 이번 교체·해제 확장에서는 같은 검증 코드를 다시 실행해 13건 통과를 확인했다. 최신 결과는 위 교체·해제 보고서를 따른다. [서식 대조 결과](Artifacts/AssemblyFoundation/formatting_verification.json)에서 변경 전후 비어 있지 않은 모든 줄이 동일함을 확인했다.

- [검증 코드](Artifacts/AssemblyFoundation/verify_assembly.cs), [실행 결과](Artifacts/AssemblyFoundation/verification_results.json): Unity 6000.6.6f1에서 **13/13 통과**. MCP execute_code로 임시 Preview Scene에서 assertion을 실행했다. Test Runner의 EditMode/PlayMode 실행 결과와 구분한다.
- 정상 5파츠 조립의 부모·로컬 위치·회전·스케일 유지·월드 위치·참조, 파츠별 누락 5건, 연결점 누락·외부·중복·몸통 자신 4건, 순환·중첩 계층 2건, 완료 후 재호출 1건을 확인했다. 실패 시 기존 상태 보존도 확인했다.
- Unity 콘솔 오류 0건. 검증 후 임시 씬을 정리했고 기존 SampleScene은 dirty=false, rootCount=3으로 작업 전과 같았다. 프리팹·샘플 에셋을 저장하지 않았다.
- 기존 모델 메시·애니메이션, PlayMode의 생명주기 및 프리팹 연결은 이번에 검증하지 않았다.

### 이전 조립 구현의 과거 검증 — 2026-10-27

아래 보고서는 현재 기본 클래스 재구현 이전 기록이다. 해당 구현 소스와 샘플은 이번 작업 시작 시 없었으며, 아래 통과 수치로 현재 기능의 모델·애니메이션 지원을 주장하지 않는다.

- [EditMode 보고서](Artifacts/Assembly/editmode_results.json): **35/35 통과**. 두 모델의 5슬롯 조합 32개에서 단일 Animator·유효 Generic Avatar·14개 본 경로·13개 메시·선택한 재질·공유 클립을 확인했다. Idle/Walk/Boost 각각 0%, 25%, 50%, 75%의 모든 베이크 정점을 동일 클립을 적용한 원본과 비교했으며 최대 차이는 0이었다. 잘못된 슬롯, 누락된 본의 거부와 기존 조립 유지, 다리별 클립 교체·복구도 검사했다.
- [PlayMode 보고서](Artifacts/Assembly/playmode_results.json): **3/3 통과**. Start 자동 조립, Walk/Boost 트리거 및 Boost→Idle 전환, 실행 중 몸통 교체와 프레임 종료 후 기존 리그·컨트롤러 제거, 다리 override의 실제 재생 클립 및 기본 복귀를 확인했다. 전환 검사는 `Animator.Update`로 시간을 진행했다.
- 첫 PlayMode 요청은 새 테스트 어셈블리가 아직 임포트되지 않아 발견된 테스트가 0개였다. 전체 에셋 갱신 후 3개를 실제 실행했으며, 0개 실행은 검증 성공 근거에 포함하지 않는다.
- 수치 비교는 기존 동작 보존을 확인한 것이며 모든 외장 간섭·접지의 개선을 의미하지 않는다. 혼합 조합 Idle 프리뷰 한 시점을 시각 확인했고, 32개 조합의 전체 프레임 시각 검토나 사용자 시각 승인은 수행하지 않았다.

### 알려진 한계와 미구현

- SUNSHINE 걷기의 발 가장자리에서 기준 지면 Z=0.075 대비 최대 약 0.004773 Blender 단위 겹침이 보고되어 있다. 기존 발 회전 키를 유지한 결과다.
- 외장 충돌·기계적 가동 한계는 정밀 물리 검증이 아니다. 새로운 파츠는 어깨/몸통, 허벅지/골반, 무릎, 발끝, 무기/팔, 분사구를 다시 시각 검토한다.
- 부스터 파티클 연결은 미구현이다. 기본 PlayerBase 이동 제어는 구현되어 있으며 연결 방법은 7절을 따른다. 현재 조립 기능은 5개 기본 클래스의 결합·개별 교체·해제까지이며, 모델 리그 바인딩·조립체 애니메이션·조립 UI·저장/로드는 후속 구현 대상이다.
- 임의 메시의 관절 자동 추정, 모든 체형·골격에 대한 자동 리타기팅은 구현되지 않았다.
- Materials 폴더에는 `.001` 접미사를 포함한 재생성 흔적이 있다. 미사용 여부는 참조를 확인해야 하며 이번 문서 작업에서 삭제하지 않았다.
- 저장된 검증 보고서는 에셋 변경 후 자동으로 유효해지는 보증서가 아니다. 해당 파이프라인을 다시 실행해 갱신해야 한다.

최신 등 무기 리그 검증은 [Blender 검증](Artifacts/BackWeaponRig/blender_validation.json), [Unity 변경 전후 비교](Artifacts/BackWeaponRig/model_comparison.txt), [회전·공유 클립 검증](Artifacts/BackWeaponRig/rotation_and_shared_clips.txt), [보존 파일 해시 검사](Artifacts/BackWeaponRig/preserved_asset_hashes.json)에 있다. Unity 프리뷰: [LAHIRE](Artifacts/BackWeaponRig/LAHIRE_Unity.png), [SUNSHINE](Artifacts/BackWeaponRig/SUNSHINE_Unity.png). 기존 14본 보고서는 과거 기록이다. 새 본은 실제 무기 메시를 부착하지 않은 상태이므로 전개 시 무기 외장 간섭은 아직 검사할 대상이 없다.

## 9. 다음 대화에서 이어가기

현재 미완료로 진행 중인 구현은 없다. 다음 변경 요청이 오면 이 문서와 관련 파일을 읽고 대상 모델·작업 범위를 확인한다. 아래는 사용자 요청 시 수행할 후보이며 자동 착수할 작업 목록은 아니다.

| 후보 | 시작점 / 완료 확인 |
| --- | --- |
| 외형 추가 개선 | 해당 원본 `.blend`, 동일 시점·카메라의 전후 프리뷰, 기존 리그/액션 보존 |
| 조립 기능 확장 | `Assets/Function/PartBase/`와 `Assets/Function/Assembly/MechAssembler.cs`에서 시작. 개별 교체·해제 API를 기반으로 요청에 따라 파생 클래스·모델 바인딩·저장/로드를 추가. 프리팹은 별도 요청 전 생성하지 않음 |
| 공통 애니메이션 관리 | 기존 모델별 Avatar·Controller를 보존하고, 조립체 모델 바인딩 이후 공유 클립·다리 override를 새로 연결·검증 |
| 플레이어 확장 | PlayerBase 컴포넌트·파츠 및 무기 참조 설정 후 Animator, 조준, 무기 이펙트·미사일 유도, 실제 게임 씬 연결 |
| 적 기능 확장 | EnemyBase의 FSM 상태별 행동·전환 조건, 공격 빈도와 공격 속도의 단위, 무기 연결, 사망 처리 및 적 인스턴스 구성 |

새 대화의 시작 요청 예:

> 이 프로젝트의 AGENTS.md와 Readme.md를 읽고 현재 상태를 확인한 뒤, [이번 작업]을 진행해라. 완료 후 규격·검증 결과·남은 작업을 Readme.md에 갱신해라.

## 10. 작업 이력과 축적 방식

새 결과는 이 섹션에 날짜별로 추가하고, 위의 현재 규격도 함께 갱신한다. 원시 로그를 모두 붙이지 말고 결정과 검증 근거를 연결한다. 확정되지 않은 제안과 완료된 구현을 분리한다.

### 2026-10-25 — 기존 작업 기준선 정리

- 요청/목적: TYPE-LAHIRE 제작, 실루엣·깊이·머리·어깨 수정, 간소화 본 보존, Idle/Walk/Boost 제작, Blender 파츠 조립, 사각형 SUNSHINE 제작, 두 모델 Unity 임포트.
- 결과: 1–8절의 원본·도구·Unity 에셋이 저장되어 있다. 이전 단계는 `Artifacts/TYPE_LAHIRE/`의 이름별 `.blend`로 남아 있다.
- 결정: 기존 14개 본과 액션을 보존하고 외형을 맞춘다. 조립은 제작자가 지정한 소켓을 기준으로 한다. Unity는 Generic 및 모델별 독립 폴더로 구성한다.
- 검증 근거: 8절의 저장된 JSON 네 종류와 프리뷰. 이번 문서 작성에서는 파일과 구현을 읽어 확인했으며 앱 검증을 재실행하지 않았다.
- 남은 일: 8절의 알려진 한계 및 9절의 후보. 원작 형상 일치와 임의 메시 자동 처리 완료로 해석하지 않는다.

### 2026-10-25 — 프로젝트 지침·규격 문서 추가

- 요청: AGENTS.md와 Readme.md에 진행 결과를 저장하고 다음 대화에서 이어갈 수 있도록 구성.
- 변경: 루트 `AGENTS.md`, `Readme.md` 신규 작성. 원본 모델·코드·Unity 에셋은 변경하지 않음.
- 결정: 작업 원칙은 AGENTS, 상세 규격·현재 상태·이력은 Readme, 큰 데이터와 검증 결과는 기존 Artifacts를 기준으로 관리.
- 검증: 문서의 로컬 링크 존재, 본 이름·계층·슬롯·소켓 규격과 JSON/코드의 일치, Unity 버전·보고서 수치 대조. 문서 변경이므로 Blender/Unity 재빌드는 수행하지 않음.
- 인계: 이후 의미 있는 작업마다 현재 상태·규격·한계·다음 작업 및 이력 갱신. 사용자가 새 작업을 지정하면 해당 범위에서 시작.

### 2026-10-25 — 개발·명명·테스트 규칙 추가

- 요청/목적: 클래스 PascalCase, 공식 문서 중심 구현, 역할을 드러내는 코드 이름, 테스트 입력·기대 결과 주석 및 실행 결과 출력 규칙을 명문화.
- 변경 파일: `Readme.md`의 2절에 개발 규칙 추가, `AGENTS.md`의 구현과 검증에 해당 규칙을 따르는 작업 원칙 추가.
- 결정: 상세 기준은 Readme에 유지하고 AGENTS에서 연결한다. 외부 API 이름과 기존 에셋 참조를 보존하며, 기존 코드의 일괄 이름 변경은 수행하지 않는다.
- 검증: 두 문서의 규칙·참조 경로를 대조하고 요청한 네 항목의 반영 여부를 확인. 문서만 변경했으며 Blender/Unity 조작·모델 재생성·테스트 실행은 수행하지 않음.
- 인계: 다음 코드·클래스·테스트 추가 시 2절의 개발 규칙을 적용한다. 이번 요청에 따른 미완료 작업은 없음.

### 2026-10-27 — Unity 5슬롯 외장 조립 초기 구현 (과거 구현)

아래는 당시 기록이다. 현재 소스·사용법·인계는 이후 기본 클래스 재구현 기록과 7절을 따른다.

- 요청/목적: Function 폴더에서 머리·왼팔·오른팔·몸통·다리를 섞어 조립하고, 몸통 골격에 연결하며 공통 애니메이션과 다리별 선택적 애니메이션을 지원.
- 변경 파일·결과물: `Assets/Function/Assembly/Runtime/` 런타임 코드, `Editor/` 예제 생성·Inspector·프리뷰 도구, `Samples/` 10개 부품 정의·3개 조립 정의·3개 조립 프리팹, `Tests/` EditMode/PlayMode 테스트, `Artifacts/Assembly/` 결과 JSON과 혼합 프리뷰. `Readme.md`, `AGENTS.md` 갱신.
- 결정: 13개 기존 강체 메시를 5개 선택 슬롯으로 묶되 본 계층·원본 모델은 유지. 몸통이 단일 리그·Avatar를 제공하고 외장은 경로로 재바인딩. 모든 기본 조합은 LAHIRE Controller/클립을 공유하며, 다리 세트가 지정한 클립만 인스턴스별 Override Controller로 대체.
- 검증: Unity 6000.6.6f1에서 EditMode 35/35, PlayMode 3/3 통과. 32조합 × 3클립 × 4시점의 원본 대비 베이크 정점 최대 차이 0. 근거는 `Artifacts/Assembly/editmode_results.json`, `playmode_results.json`, `MixedAssembly.png`. 테스트 입력·기대·실제 결과는 주석과 출력에 기록.
- 한계: 교체 시 Idle 재시작 및 새 Animator 생성. 새 보행 제작·하체 전용 블렌딩·접지 개선·모든 조합의 무간섭은 미검증/미구현. 상세는 7–8절. 시각 품질에 대한 사용자 최종 승인은 받지 않음.
- 당시 인계: `MixedAssembly.prefab` 배치 후 Play. 현재는 해당 샘플이 없으므로 이 사용법은 적용하지 않는다.

### 2026-10-27 — 제어문 서식·산출물 범위 규약 추가

- 요청/목적: 제어문 사이 빈 줄, 같은 줄의 여는 중괄호와 본문 개행, 소스코드·프리팹 생성 범위 분리, 두 산출물이 필요할 때 사전 확인 규칙을 지속 적용.
- 변경 파일: 기존 `Readme.md` 2절에 규칙과 C# 서식 예시 추가, `AGENTS.md`에 작업 원칙 반영.
- 결정: 기존 기준 문서를 갱신하여 중복 Readme를 만들지 않는다. 샘플은 명시적 요청 범위에서만 생성하며 소스코드와 프리팹을 함께 만들 필요가 있으면 생성 이유·범위를 확인받는다.
- 검증: 요청한 네 규칙과 두 문서의 표현을 대조하고 서식 예시의 빈 줄·중괄호 위치를 확인. 문서만 변경했으며 코드·프리팹·샘플 변경 및 앱 조작·테스트 실행은 수행하지 않음.
- 인계: 이후 신규 코드와 수정 범위에 위 서식을 적용한다. 기존 코드 일괄 포맷 변경이나 샘플 삭제는 이번 요청에 포함하지 않는다.

### 2026-10-27 — 5개 파츠 기본 클래스와 조립 소스 재구현

- 요청/목적: 프리팹을 만들지 않고 Function/PartBase에 머리·몸통·왼팔·오른팔·다리 기본 클래스를 먼저 구현하고 최초 조립까지 진행.
- 시작 상태: Assets/Function은 비어 있었고 문서만 이전 구현을 설명했다. 삭제 원인은 확인하지 못했으며 이전 코드·샘플은 복원하지 않았다.
- 변경 파일: `Assets/Function/PartBase/`의 PartBase 및 5개 클래스, `Assets/Function/Assembly/MechAssembler.cs`, Unity가 생성한 해당 .meta, `Artifacts/AssemblyFoundation/verify_assembly.cs`, `verification_results.json`, `Readme.md`, `AGENTS.md`.
- 결정: 공통 추상 기반과 역할별 컴포넌트를 먼저 분리하고 몸통의 명시적 연결점으로 최초 조립한다. 요청한 기본 단계에 맞춰 교체·모델 바인딩·애니메이션은 미구현으로 남긴다. 기존 모델 리그 규격은 변경하지 않았다.
- 검증: Unity 6000.6.6f1 컴파일 후 MCP를 통한 임시 Preview Scene assertion 13/13 통과, 콘솔 오류 0건. 근거는 `Artifacts/AssemblyFoundation/verification_results.json`. 입력·기대·실제 출력과 실패 시 상태 유지 검사를 포함한다. 테스트 씬은 메모리에서 정리했고 기존 SampleScene dirty=false/rootCount=3을 확인했다.
- 범위: 프리팹·샘플 씬·데이터 에셋 생성 없음. 기존 모델·Controller·Animator·본 변경 없음. 모델 외형·애니메이션과 PlayMode는 검증하지 않았다.
- 인계: 현재 요청 범위 완료. 다음 요청 시 위 기본 클래스에서 확장한다. 프리팹을 만들거나 이전 샘플 생성기를 복구하는 작업에 자동 착수하지 않는다.

### 2026-10-27 — README 예시에 맞춘 조립 소스 서식 수정

- 요청/목적: 현재 README 규약에 맞춰 직전 작성한 조립 소스를 수정.
- 변경 파일: `Assets/Function/Assembly/MechAssembler.cs`, `Artifacts/AssemblyFoundation/verify_assembly.cs`, `Readme.md`, `AGENTS.md`, `Artifacts/AssemblyFoundation/formatting_verification.json`.
- 결정: 최신 README의 코드 예시를 기준으로 제어문 여는 중괄호 다음·닫는 중괄호 앞의 빈 줄을 제거하고 블록 사이의 빈 줄은 유지했다. 예시와 상충하던 README·AGENTS의 본문 앞뒤 빈 줄 지침도 같은 기준으로 정리했다.
- 검증: Function의 소스 7개와 검증 코드 1개를 대조했다. 실제 변경은 2개 파일의 빈 줄 제거뿐이며, 변경 전후 비어 있지 않은 모든 줄이 동일함을 assertion으로 확인했다. 클래스 이름·API·동작·테스트 주석·출력은 유지했다. 근거는 `Artifacts/AssemblyFoundation/formatting_verification.json`.
- 미실행: 공백만 변경하여 Unity 컴파일·기능 테스트 재실행과 앱 조작은 수행하지 않았다. 기존 13/13 보고서는 이전 실행 기록으로 보존했다.
- 인계: 요청 범위 완료. 이후 제어문은 2절의 예시와 같이 블록 안쪽 추가 빈 줄 없이 작성한다. 모델·프리팹·씬 변경이나 새 샘플 생성 없음.

### 2026-10-27 — 개별 파츠 교체·해제 구현

- 요청/목적: README 규약을 유지하면서 조립된 5개 파츠의 개별 교체·해제를 코드만으로 구현.
- 변경 파일: `Assets/Function/Assembly/MechAssembler.cs`, `Assets/Function/PartBase/PartBase.cs`, `Artifacts/AssemblyPartOperations/verify_part_operations.cs`, `verification_results.json`, `Readme.md`, `AGENTS.md`.
- 결정: 슬롯별 타입 지정 API, 반환 파츠와 오류를 제공한다. 교체·해제한 파츠는 삭제하지 않고 씬 루트에 유지한다. 몸통 교체는 나머지 파츠를 새 연결점으로 이동하고, 몸통만 해제하면 다른 파츠는 조립기 루트에서 대기한다. 소유 조립기를 추적해 중복 장착을 거부하고 최초 장착 스케일을 기억해 재장착 시 크기 누적을 막는다.
- 규격 변경: IsAssembled를 최초 조립 완료 기록에서 현재 5개 슬롯 생존 여부로 변경했다. 부분 조립은 개별 API로 채우며 완전히 비운 뒤에는 TryAssemble을 재사용한다. 기존 모델 본·리그 규격은 변경하지 않았다.
- 검증: Unity 컴파일 후 신규 교체·해제 검증 60/60, 기본 조립 회귀 검증 13/13 통과. 코드·결과·최초 검증 스크립트 컴파일 오류와 수정 사항은 `Artifacts/AssemblyPartOperations/`에 기록했다. 최종 콘솔 오류 0, SampleScene dirty=false/rootCount=3, 기존 대상 에셋·.meta 92개 해시 동일. 제어문 빈 줄·중괄호 및 PascalCase 확인.
- 한계·미실행: PlayMode, 모델·애니메이션·물리 통합, 전단 변환, 도메인 리로드 복구는 미검증/미구현. 프리팹·샘플 씬·데이터 에셋은 생성하지 않았다.
- 인계: 요청 범위 완료. 7절의 슬롯별 TryReplace/TryDetach API를 사용한다. 후속 요청 시 모델 바인딩 등을 확장하며 프리팹 생성에 자동 착수하지 않는다.

### 2026-10-28 — 파츠 기본 클래스에 내부 데이터 추가

- 요청/목적: 각 파츠가 자체 체력을 보유하고 최종 조립 능력치의 기반이 되도록 기본 체력·무게와 역할별 데이터를 추가.
- 변경 파일: `Assets/Function/PartBase/`의 기존 6개 클래스 파일 및 `Readme.md`. `MechAssembler`와 기존 연결·교체·해제 메서드는 변경하지 않음.
- 결정: 기본 체력과 무게는 공통 `PartBase`에서 상속하여 다리에도 포함. 역할별 데이터는 해당 기본 클래스에 직렬화 float로 선언하고 읽기 프로퍼티 제공. 값은 파츠 인스턴스별로 독립적이며 임의 밸런스 수치는 설정하지 않음.
- 검증: Unity 6000.6.6f1에서 스크립트 컴파일 완료, 콘솔 오류 0건. 컴파일된 5개 파츠 타입을 reflection으로 조회하여 공통/역할별 float 프로퍼티를 확인. 기존 SampleScene은 작업 전후 dirty=false. 단순 필드 추가이므로 별도 테스트 코드·프리팹·씬·샘플 에셋을 생성하지 않음.
- 한계·미실행: Inspector 값 편집·저장 왕복, PlayMode 전투, 조립 능력치 합산은 검증/구현하지 않음. 기존 조립 회귀 테스트는 이번에 재실행하지 않음.
- 인계: 7절의 데이터 API로 값을 읽는다. 후속 합산 구현은 다리 세트를 포함한 5개 논리 파츠를 기준으로 하며 현재 체력·교체 시 체력 정책을 별도로 설계한다.

### 2026-10-28–29 — 부스터 데이터 분리

- 요청/목적: 다리의 부스터 성능을 제거하고 별도 부스터 기본 클래스에 에너지 소모량·상승·전진 속도를 추가.
- 변경 파일: `Assets/Function/PartBase/LegPartBase.cs`, 신규 `BoosterPartBase.cs` 및 Unity 생성 `.meta`, `AGENTS.md`, `Readme.md`.
- 결정: `LegPartBase.BoostSpeed`만 제거하고 걷기 속도 유지. `BoosterPartBase : PartBase`에 float 필드 3개와 public getter를 추가하여 기존 Inspector 입력 방식과 체력·무게 상속 규칙 유지. 초기값은 0이며 밸런스 수치는 미지정.
- 검증: 최초 스크립트 새로고침 후 reflection 조회에서 신규 타입이 아직 발견되지 않아 에셋 전체 새로고침과 재컴파일을 수행했다. 이후 Unity 6000.6.6f1에서 컴파일 완료·콘솔 오류 0건을 확인했다. 컴파일된 LegPartBase에는 BaseHealth/WalkSpeed/Weight만, BoosterPartBase에는 BaseHealth/Weight/EnergyConsumption/AscentSpeed/ForwardSpeed가 있음을 reflection으로 확인했다. 신규 `.meta` 생성과 기존 SampleScene dirty=false를 확인했다. Assets/Function에서 제거한 BoostSpeed 참조는 남아 있지 않다. 별도 테스트 코드·PlayMode 테스트는 실행하지 않았다.
- 범위: 소스 데이터 정의만 변경. 부스터 장착·교체·합산·추진·에너지 차감은 미구현이며 기존 모델·본·애니메이션·프리팹·씬은 수정하지 않음.
- 인계: 부스터 성능은 별도 클래스에서 읽고, 이후 조립기를 확장할 때 부스터 연결과 체력·무게 합산을 함께 반영한다.

### 2026-10-29 — 무기 기본 클래스와 공통 데이터 추가

- 요청/목적: WeaponBase, MeleeWeaponBase, RangeWeaponBase 추가. 공통 피해량·사정거리, 원거리 연사력, 근접 쿨타임 정의.
- 변경 파일: `Assets/Function/WeaponBase/`의 소스 3개와 Unity 생성 `.meta`, `AGENTS.md`, `Readme.md`.
- 결정: 추상 WeaponBase에 공통 데이터를 정의하고 근접·원거리가 상속한다. 기존 파츠 규칙에 맞춰 체력·무게도 상속한다. 사용자의 추가 설명을 반영해 연사력은 RangeWeaponBase에만, 쿨타임은 MeleeWeaponBase에만 둔다.
- 검증: 최종 분리 후 Unity 6000.6.6f1 컴파일 완료·콘솔 오류 0건. reflection으로 WeaponBase에는 Damage/Range와 공통 BaseHealth/Weight, MeleeWeaponBase에는 추가 Cooldown만, RangeWeaponBase에는 추가 FireRate만 있음을 확인했다. 세 클래스 상속 관계와 SampleScene dirty=false를 확인했다. 단순 데이터 선언이므로 별도 테스트 코드·PlayMode 테스트는 실행하지 않았다.
- 범위·인계: 요청한 데이터 기반까지 구현. 장착·발사·근접 판정·피해 적용·합산은 미구현. 실제 동작 구현 시 근접 Cooldown과 원거리 FireRate를 구분한다.

### 2026-10-29 — 무기 무게 스테이터스 확인·명시

- 요청/목적: 무기에도 무게 스테이터스 추가.
- 확인: 소스에서 `PartBase`의 `[SerializeField, Min(0f)] private float weight`와 `Weight` getter, WeaponBase 및 근접·원거리의 상속 관계를 재확인했다. 기존 구현으로 이미 무기별 무게 입력·조회가 가능하다.
- 변경: `Readme.md`의 무기 데이터 표와 사용 설명에 상속된 무게를 명시했다. 중복 필드나 새 API는 추가하지 않았다.
- 검증·인계: 소스·문서 대조만 수행. 코드 변경이 없어 Unity 재컴파일·앱 조작·테스트는 재실행하지 않았다. 총 무게 합산과 이동 성능 반영은 여전히 후속 구현 대상이다.

### 2026-10-29 — 등 무기 기본 클래스 추가

- 요청/목적: 등 무기 파츠용 BackWeaponBase 추가.
- 변경 파일: `Assets/Function/WeaponBase/BackWeaponBase.cs` 및 Unity 생성 `.meta`, `AGENTS.md`, `Readme.md`.
- 결정: WeaponBase를 직접 상속하여 피해량·사정거리와 공통 체력·무게를 사용한다. 요청되지 않은 전용 능력치·연사력·쿨타임은 추가하지 않았다.
- 검증: Unity 6000.6.6f1 컴파일 완료·콘솔 오류 0건. reflection으로 부모 WeaponBase 및 BaseHealth/Damage/Range/Weight 프로퍼티 확인. SampleScene은 전후 dirty=false. 단순 클래스 선언이므로 별도 테스트 코드·PlayMode 테스트는 실행하지 않았다.
- 범위·인계: 소스 기본 클래스만 추가. 프리팹·씬·모델 변경 없음. 등 무기 장착·교체·발사와 조립기 연결은 후속 구현 대상이다.

### 이후 기록 템플릿

```text
YYYY-MM-DD — 작업명
- 요청/목적:
- 변경 파일·결과물:
- 결정과 이유 / 규격 변경:
- 검증: 실행 환경, 방법·범위, 결과, 보고서·프리뷰 경로
- 한계·미실행 항목 / 사용자 승인 여부(해당할 때):
- 남은 작업·재개 위치:
```

### 2026-10-29 — 공통 등 무기 본과 활성 회전 추가

- 요청/목적: 몸통 뒤에 등 무기 장착 본을 만들고 무기 활성 상태로 회전하도록 구성.
- 규격 결정: 기존 14본·REST·액션·13외장 슬롯을 유지하고 `chest/back_weapon` 하나를 추가해 총 15본으로 확장. 좌우 독립 무기 본은 만들지 않았다. 현재 리그와 이전 14본 원본은 위 4절 및 파일 표로 구분한다.
- 변경: 최신 `.blend`·프로필을 `Artifacts/BackWeaponRig/`에 저장, 두 `Assets/Mesh/<모델>/<모델>.fbx` 갱신. `BackWeaponMount.cs` 추가, `BackWeaponBase.cs` 활성 API 및 `BodyPartBase.cs` 참조 추가. 내보내기·Unity 임포터·Blender 애드온 소스와 ZIP 갱신. AGENTS/Readme 갱신.
- 보존: 작업 시작 때 저장되지 않은 Blender 세션을 `Artifacts/BackWeaponRig/backups/SessionBeforeBackWeapon.blend`에 복사 저장. 변경 전 FBX·파이프라인도 backups에 보관. 기존 프리팹·Controller·재질·meta·manifest 90개는 SHA-256 동일.
- 검증: Blender 5.2.3 LTS에서 기존 본 REST, 모든 액션 키, 메시 좌표·토폴로지·웨이트 불변 확인. Unity에서 유효 Generic Avatar·13렌더러·재질과 본 참조·Idle/Walk/Boost 길이 및 루프 확인. 변경 전후 24개 모델/클립/시점의 본 행렬과 베이크 정점 3,386,496개 float 차이 0. 회전·바인딩 거부·활성 해제·양방향 공유 클립 경로/자세 44개 assertion 통과. 15본 애드온 검증 및 기존 13개 슬롯 소켓 좌표 차이 0, Unity 컴파일 오류 로그 0. 두 Unity 프리뷰와 Blender 후면/측면 X-ray 확인.
- 검증 범위: Preview Scene에서 클립 샘플링 및 회전 업데이트를 직접 호출한 EditMode 검증이다. PlayMode 전체 게임 루프, 실제 등 무기 메시의 가동 간섭은 미검증. 예비 내보내기에서 없는 복제 재질을 발견해 manifest 이름으로 임시 복사하도록 수정한 뒤 재실행했고, 웨이트 검사는 bones 배열 포함 여부 대신 실제 양수 정점 웨이트로 판정하도록 수정했다.
- 근거: Unity 공식 [LateUpdate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html), [Quaternion.RotateTowards](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Quaternion.RotateTowards.html). Blender 문서 조회는 실패하여 설치된 bpy API와 실행 검증으로 확인.
- 인계: 무기 인스턴스를 새 본 아래에 배치하고 BackWeaponMount를 연결하면 활성 플래그로 회전한다. 실제 무기 메시 분리·제작, 프리팹 설정 저장, 5슬롯 조립기와의 자동 무기 장착/교체, 발사는 별도 요청 범위다.

### 2026-10-29 — PlayerBase 기본 이동·투사체 제어

- 요청/목적: Player 폴더의 PlayerBase에 이동, 양팔·등 무기 발사, 부스트, 상승·하강, 점프 구현. 사용자는 즉시 명중 대신 실제 이동하는 탄환·미사일 방식을 선택했다.
- 변경: `Assets/Function/Player/PlayerBase.cs`, Unity 생성 폴더/스크립트 `.meta`, `Artifacts/PlayerBase/verify_player.cs`와 `verification.txt`, AGENTS/Readme. 소스 요청 범위에 따라 씬·프리팹·투사체 시각 에셋은 생성하지 않았다.
- 결정: CharacterController와 Input System 장치 직접 입력, 파츠의 속도·에너지 데이터 사용, 직선 이동 투사체의 구간별 충돌 검사, 피해 수신 인터페이스와 발사/명중 이벤트. 등 무기 연사력은 해당 기본 클래스에 값이 없어 플레이어 설정의 임시 초당 1발을 기본으로 둔다. 실제 무기 발사 지점·외형·피해 수신기 연결은 게임 구성 단계에서 필요하다.
- 검증: Unity 6000.6.6f1 컴파일 오류 0. 임시 Preview Scene에서 이동 거리, 부스트·상승·하강·지상 점프, 에너지 소모/회복/고갈, 3개 무기의 투사체 이동과 명중/피해 전달/쿨다운/사거리 종료 등 16개 assertion 통과. 테스트 프로브는 메모리에서만 생성했고 결과는 [verification.txt](Artifacts/PlayerBase/verification.txt), 코드는 [verify_player.cs](Artifacts/PlayerBase/verify_player.cs)에 저장했다. 초기에 즉시 명중 구현을 시도했으나 사용자 선택을 반영해 제거했다.
- 근거: [CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html), [isGrounded](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController-isGrounded.html), [PhysicsScene.Raycast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PhysicsScene.Raycast.html), [GetPhysicsScene](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PhysicsSceneExtensions.GetPhysicsScene.html). 입력 장치 API는 설치된 Input System 1.20.0과 실제 컴파일로 확인했다.
- 한계/인계: 테스트는 PlayMode와 실제 기기 입력, 프리팹 연결을 검증하지 않았다. 회전·카메라·Animator 연동, 적 체력, 근접 공격, 투사체 외형과 미사일 유도는 별도 구현 대상이다. PlayerBase를 실제 인스턴스에 설정할 때 7절의 파츠·발사 위치와 속도, 시각 프리팹, 피해 수신기를 연결한다.

### 2026-10-29 — EnemyBase 적 데이터와 FSM 틀

- 요청/목적: `Assets/Function/Enemy/EnemyBase.cs`에 체력, 공격력·사정거리·무기종류·공격 빈도·공격 속도와 빈 FSM 구현.
- 변경 파일: EnemyBase 소스 및 Unity 생성 `.meta`, `Artifacts/EnemyBase/verify_enemy.cs`와 `verification.txt`, `AGENTS.md`, `Readme.md`. 프리팹·씬은 생성하지 않았다.
- 결정: 설정 가능한 최대 체력과 런타임 현재 체력을 분리하고 PlayerBase의 `IWeaponDamageReceiver`를 구현해 실제 투사체 피해를 수신한다. 무기 종류는 미지정/근접/원거리로 구분하고 공격 빈도와 속도의 구체 단위는 미결정으로 둔다. FSM은 수동 상태 변경·이벤트만 제공하며 사망 자동 전환이나 AI 행동은 추가하지 않았다.
- 검증: Unity 6000.6.6f1 컴파일 오류 0, 임시 Preview Scene에서 데이터 읽기·상태 전환/거부·유효하지 않은 피해 무시·PlayerBase 투사체에 의한 체력 감소·체력 하한과 FSM 비자동 전환 등 [6개 assertion](Artifacts/EnemyBase/verification.txt) 통과. 테스트 코드는 [verify_enemy.cs](Artifacts/EnemyBase/verify_enemy.cs). PlayMode와 프리팹 구성은 미검증.
- 근거: Unity 공식 [SerializeField](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeField.html), [Awake](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.Awake.html). `Awake`는 실제 PlayMode에서 한 번 호출되며 이번 Preview Scene 테스트에서는 직접 호출해 확인했다.
- 인계: 공격 행동, 플레이어 추적·순찰·상태 자동 전환, 사망 처리, 적 프리팹 및 수치 설정은 후속 구현이다.

### 2026-10-30 — 함수 기능 주석 규칙 추가

- 요청/목적: 이후 새로 생성하는 함수의 기능을 한국어 한 줄 주석으로 설명하도록 개발 규약에 추가.
- 변경 파일: `Readme.md`, `AGENTS.md`.
- 결정: 선언 바로 위에 언어에 맞는 한 줄 주석을 작성하며, 기존의 역할이 드러나는 이름 규칙도 유지한다. 기존 함수를 일괄 수정하지 않는다.
- 검증: 두 문서의 규칙과 적용 범위를 대조했다. 문서만 변경했으므로 Unity·Blender 조작이나 코드 테스트는 수행하지 않았다.
- 인계: 이후 새 함수·메서드 작성 시 이 규칙을 적용한다.

### 2026-10-30 — 플레이어 기본 클래스 생성

- 요청/목적: `Assets/Function/Player/`에 플레이어블 캐릭터의 기본 클래스 생성.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Readme.md`, `AGENTS.md`.
- 결정: 기존 네임스페이스 규격 `Mech.Function`을 유지하고 Unity 컴포넌트로 확장할 수 있도록 `MonoBehaviour`를 상속하는 빈 클래스를 작성했다. 구체적인 플레이어 기능은 요청되지 않아 추가하지 않았다. 근거: Unity 공식 [MonoBehaviour](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.html).
- 검증: 파일 경로, 클래스명·네임스페이스·상속과 현재 `Assets/Function/`의 파일 목록을 확인했다. Unity 컴파일과 PlayMode 검증은 실행하지 않았다. `.meta` 생성 여부는 Unity 임포트 후 확인해야 한다.
- 차이·인계: 작업 전 `Assets/Function/`은 비어 있었고 이전 기록의 `Assets/Mesh/`와 `Artifacts/`도 발견되지 않았다. 원인은 미확인이다. 과거 구현과 검증은 현재 결과가 아니며 이후 요청에 따라 기본 클래스를 확장한다.

### 2026-10-30 — 2D 플레이어 입력과 능력치

- 요청/목적: 방향키 이동, A 공격·S 점프·D 특수 입력과 체력·공격력·공격 속도, A/B/C 객체 배열을 구현. 사용자 확인에 따라 2D 횡스크롤 좌우 이동 및 종류별 개수 기준을 사용했다.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Readme.md`, `AGENTS.md`.
- 결정: `Rigidbody2D` 속도와 지면 캐스트로 이동·점프한다. 공격 속도는 초당 횟수로 해석하고 공격·특수의 미지정 효과는 이벤트로 연결한다. 객체는 종류별 `GameObject` 배열의 유효한 항목 수를 제공한다.
- 검증: Unity 6000.6.6f1 설치본의 CoreModule·Physics2DModule과 프로젝트의 Input System DLL을 참조하여 임시 .NET 9 프로젝트에서 컴파일 성공(경고 0, 오류 0). 첫 .NET 8 시도는 로컬 참조 팩 부족으로 실패해 .NET 9로 재시도했다. Unity 에디터 컴파일·PlayMode, 실제 키 입력 및 물리 동작은 실행하지 않았다.
- 근거: Unity 공식 [Rigidbody2D.linearVelocity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody2D-linearVelocity.html), [Rigidbody2D.Cast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody2D.Cast.html). Input System 문서 조회는 실패해 프로젝트에 설치된 DLL을 사용한 컴파일로 API를 확인했다.
- 인계: 실제 공격 판정, 특수 효과와 객체의 종류 판별·수집·소비·조합 조건은 별도 구현이 필요하다. 씬과 프리팹은 만들지 않았다.

### 2026-10-30 — 적 기본 클래스와 FSM

- 요청/목적: `Assets/Function/Enemy/`에 체력, 그로기 수치·저항, 공격력·속도와 FSM을 가진 기본 클래스 생성.
- 변경 파일: `Assets/Function/Enemy/EnemyBase.cs`, `Readme.md`, `AGENTS.md`.
- 결정: Inspector 설정 데이터와 현재 체력을 분리하고, 명시적 상태 전환과 이벤트를 제공한다. 그로기 계산·상태 행동과 자동 전환의 세부 규칙은 정해지지 않아 구현하지 않았다.
- 검증: Unity 6000.6.6f1 설치본의 CoreModule·Physics2DModule과 프로젝트 Input System DLL을 참조하는 임시 .NET 9 프로젝트에서 플레이어·적 코드를 함께 컴파일했다. 최초에는 Inspector 설정 필드 두 개의 미할당 경고가 발생해 0 초기값을 명시했고, 재컴파일 결과 경고 0·오류 0이다. Unity 에디터 컴파일, PlayMode, FSM과 피해 동작의 실행 검증은 하지 않았다. `.meta` 생성 여부는 Unity 임포트 후 확인한다.
- 근거: Unity 공식 [SerializeField](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SerializeField.html), [Awake](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MonoBehaviour.Awake.html).
- 인계: 적 행동·자동 상태 전환, 그로기 수치의 변화와 저항 공식, 사망 처리 및 플레이어 공격 연결은 후속 작업이다. 프리팹·씬은 생성하지 않았다.

### 2026-10-30 — 적의 A/B/C 객체 지정

- 요청/목적: 적이 보유하는 A/B/C 객체 하나를 개발자가 Inspector에서 지정.
- 변경 파일: `Assets/Function/Enemy/EnemyBase.cs`, `Readme.md`, `AGENTS.md`.
- 결정: 객체 종류 열거형과 `GameObject` 참조를 각각 직렬화하고 읽기 프로퍼티를 제공한다. 현재 플레이어 배열에도 별도의 객체 타입 정보가 없으므로 종류와 참조의 일치 여부를 자동 판별하지 않는다.
- 검증: Unity 6000.6.6f1 설치본과 프로젝트 Input System DLL을 참조한 임시 .NET 9 프로젝트에서 플레이어·적 코드 컴파일 성공, 경고 0·오류 0. Unity Inspector 설정·저장과 PlayMode는 검증하지 않았다.
- 인계: 개발자가 적마다 종류와 객체를 일치시켜 할당한다. 미할당 거부, 드롭·수집 및 플레이어 배열 연결은 미구현이다.

### 2026-10-30 — 적 네 종류와 조각 드롭·보스 생성

- 요청/목적: 조각·일반·고급·보스 적 구조, 처치 시 조각 드롭, 보스의 조각 적 생성을 구현하고 특수 조합 게임 규칙을 기록.
- 변경 파일: `Assets/Function/Enemy/EnemyBase.cs`, `FragmentEnemy.cs`, `NormalEnemy.cs`, `AdvancedEnemy.cs`, `BossEnemy.cs`, `Readme.md`, `AGENTS.md`.
- 결정: 공통 치명상 처리에서 지정 조각 프리팹을 한 번 생성하고 보스는 설정 주기마다 조각 적을 생성한다. 조각 적의 한두 방 처치와 일반·고급의 차이는 수치 설정에 달려 있으며, 구체적 보스 패턴과 플레이어 조합은 미정으로 둔다. 기존 수동 Dead 전환과 치명상 처리를 구분한다.
- 검증: Unity 설치본 DLL을 참조하는 임시 .NET 9 프로젝트에서 컴파일을 확인했다. 별도 임시 Unity API 대역 기반 실행에서 무효 피해, 치명상·반복 피해의 단일 드롭과 이벤트, 처치 후 전환 거부, 보스 생성 시점·처치 후 중단의 assertion 11개 통과. 이 실행은 Unity 에디터·실제 물리·프리팹 테스트가 아니다. Unity 컴파일, PlayMode, Inspector 설정과 에셋 임포트는 미실행이다.
- 근거: Unity 공식 [Object.Instantiate](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Object.Instantiate.html), [Time.deltaTime](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Time-deltaTime.html).
- 인계: 각 적에 조각 프리팹을, 보스에 조각 적 프리팹을 연결한다. 플레이어 공격·조각 수집과 조합식, 3회 사용·특수 버튼 활성화, 실제 보스 패턴·적 제거 정책은 후속 구현이다. 프리팹·씬은 생성하지 않았다.

### 2026-10-30 — 조각 수집과 특수 기능 사용 주기

- 요청/목적: 적이 드롭한 A/B/C 조각 3개를 수집해 순서와 무관한 조합으로 특수 기능을 준비하고 3회 사용 후 다시 수집.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Assets/Function/Player/FragmentPickup.cs`, `Assets/Function/Enemy/EnemyBase.cs`, `Readme.md`, `AGENTS.md`.
- 결정: 드롭 시 종류를 수집 컴포넌트에 전달하고 플레이어 접촉 시 수집한다. 실제 효과가 미정이므로 조합별 개수를 `SpecialUsed` 이벤트로 전달한다. 준비 중 추가 수집은 거부하고 월드에 보존한다. 기존 Inspector 객체 배열은 런타임 개수로 교체했으며 기존 참조 자동 이전은 없다.
- 검증: Unity 6000.6.6f1 설치본 DLL과 프로젝트 Input System DLL을 참조해 .NET 9에서 컴파일 성공(경고 0·오류 0). 임시 Unity API 대역 기반 실행에서 준비 전 사용 거부, A/C/A 조합과 3회 사용, 활성 중 수집 거부, 재수집 및 조각 중복 수집 거부 등 assertion 19개 통과. Assets의 씬·프리팹·데이터 에셋에서 기존 배열 필드 직렬화 기록은 발견되지 않았다. Unity 에디터 컴파일, 실제 Trigger·키 입력·프리팹과 PlayMode는 검증하지 않았다.
- 근거: Unity 공식 [OnTriggerStay2D](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MonoBehaviour.OnTriggerStay2D.html), [Object.Destroy](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Object.Destroy.html), [GameObject.AddComponent](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/GameObject.AddComponent.html).
- 인계: Trigger·레이어와 조각 프리팹을 설정하고 조합별 효과 및 특수 버튼 UI를 연결한다. 수집·사용 수량 설정의 일반화는 미구현이다. 프리팹·씬은 생성하지 않았다.

### 2026-11-01 — TestEnemy 기본 클래스

- 요청/목적: `EnemyBase`를 상속하고 사망 시 플레이어용 조각 하나를 떨어뜨리는 `TestEnemy` 생성.
- 변경 파일: `Assets/Function/Enemy/TestEnemy.cs`, `Readme.md`, `AGENTS.md`.
- 결정: 기존 `EnemyBase`의 Inspector 조각 종류·프리팹 설정과 치명상 시 단일 드롭을 그대로 상속한다. 동작을 중복 구현하지 않으며 프리팹 미지정 시 조각이 나오지 않는 기존 조건을 유지한다.
- 검증: Unity 6000.6.6f1 설치본 DLL과 프로젝트 Input System DLL을 참조한 임시 .NET 9 컴파일에서 경고 0·오류 0. 임시 Unity API 대역 기반 실행에서 B 조각 지정, 치명상 이후 반복 피해 시 드롭 1회, 조각 종류·객체 및 Dead 상태 등 assertion 5개 통과. Unity 에디터·PlayMode, 실제 프리팹·Trigger는 검증하지 않았다.
- 근거: 기존 공통 처리의 Unity 공식 [Object.Instantiate](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Object.Instantiate.html) 근거를 재사용한다.
- 인계: TestEnemy 컴포넌트의 Inspector에 조각 종류와 프리팹을 지정하고, 나중에 실제 플레이어 공격 판정과 연결한다. 프리팹·씬은 생성하지 않았다. 새 스크립트 `.meta` 생성 여부는 Unity 임포트 후 확인한다.

### 2026-11-01 — 현재 프로젝트의 메카 방향 정정

- 요청/목적: 현재 프로젝트가 메카가 아님을 명시.
- 변경 파일: `Readme.md`.
- 결정: 과거 메카 관련 기록은 보존하되 현재 프로젝트의 아트 방향이나 구현 목표로 취급하지 않는다. 새로운 아트 방향은 사용자 지정 전까지 미확정으로 둔다.
- 검증: 문서의 도입부와 현재 상태 설명을 대조했다. 문서만 변경하여 Unity·Blender 조작 및 코드 테스트는 수행하지 않았다.
- 인계: 이후 아트 구현은 사용자가 지정하는 현재 프로젝트의 방향을 기준으로 진행한다.

### 2026-11-01 — 2D 아트 방향 기록

- 요청/목적: 할로우 나이트를 참고한 스프라이트 기반 2D 아트 방향을 기억한다.
- 변경 파일: `Readme.md`.
- 결정: 참고 방향만 기록하고 구체적인 대상·외형·애니메이션 동작은 확정하지 않았다.
- 검증: 문서의 현재 프로젝트 설명과 작업 기록을 대조했다. 문서만 변경하여 Unity·Blender 조작과 코드 테스트는 수행하지 않았다.
- 인계: 아트 구현 대상과 동작은 이후 사용자 요청을 따른다.

### 2026-11-01 — 플레이어 공격 연결과 플레이 테스트 씬

- 요청/목적: 플레이어 공격으로 TestEnemy에 피해를 주고 처치 시 조각을 드롭하며 직접 플레이할 수 있는 테스트 씬 생성.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Assets/Scenes/TestScene/PlayTestScene.unity`, `Assets/Scenes/TestScene/PlayTestDisplay.cs` 및 Unity 생성 `.meta`, `Readme.md`, `AGENTS.md`. Unity가 `ProjectSettings/SceneTemplateSettings.json`을 자동 생성했다. 프리팹은 생성하지 않았다.
- 결정: 물리 씬의 정면 사각 범위로 공격 대상을 찾고 적별로 한 번 피해를 적용한다. 씬 안의 원본 조각과 임시 단색 도형을 사용해 A/B/C 적 3개와 수집·특수 횟수를 확인한다. 실제 특수 효과·최종 아트는 추가하지 않았다.
- 검증: Unity 6000.6.6f1에서 실제 Play 모드로 이동·오른쪽 공격, 세 TestEnemy 처치와 각 조각 드롭·수집, 특수 횟수 3 표시 후 D 3회로 0 복귀를 화면에서 확인했다. 최초 실행에서는 스프라이트 미지정 시 생성된 Collider 크기가 0.0001이라 바닥을 통과했으며 씬의 Box 크기 1·조각 Circle 반경 0.5로 수정하고 위 과정을 재실행했다. 씬 스크립트 GUID 3종이 모두 실제 `.meta`와 일치함을 확인했다. 테스트 시 콘솔 오류 수는 0이었으나 `referenced script (Unknown) ... missing` 경고 1건이 표시되었고 원인은 확인하지 못했다. 콘솔에는 Unity AI 관련 `NoSubscription` 로그도 있었다.
- 근거: Unity 공식 [PhysicsScene2D.OverlapBox](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PhysicsScene2D.OverlapBox.html), [Object.Instantiate](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Object.Instantiate.html).
- 범위·인계: 왼쪽 공격, 범위 밖·복수 Collider의 개별 경계, 실제 캐릭터 에셋 및 특수 효과는 PlayMode에서 검증하지 않았다. 저장 전 기존 SampleScene은 dirty=false였으며 테스트 씬을 별도로 열고 종료했다. 표시된 누락 스크립트 경고의 원인은 추가 확인이 필요하다.
