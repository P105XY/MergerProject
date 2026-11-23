# 프로젝트 현황과 과거 메카 작업 기록

- 최종 문서 갱신: 2026-11-23 (Asia/Seoul)
- 현재 프로젝트는 메카 프로젝트가 아니다. 아트는 스프라이트 기반 2D이며, 게임 분위기의 참고작은 데드 셀, 할로우 나이트, Hellfurnace다. 할로우 나이트와 Hellfurnace처럼 윤곽선이 있는 그래픽을 게임 전반에 적용한다. 플레이어는 현대 어반 콘셉트의 인간형이며 오른팔과 왼다리가 기계로 대체되어 있고 오른팔로 공격한다. 인게임 스프라이트는 약 5등신을 목표로 한다. [스탠딩 시안 v2](Artifacts/PlayerStandingConcept/PlayerStandingConcept_v2.png)는 검토용이며 외형은 아직 승인되지 않았다.
- 플레이어 조작감의 참고도 할로우 나이트로 지정되었다. 실제 원작 수치는 측정하지 않았으며, 빠른 반응을 목표로 시험 설정을 비교한다.
- 아래 메카 모델·리그·조립 관련 내용은 과거 작업 기록이며 현재 프로젝트의 아트 방향이나 구현 목표가 아니다.
- 이 문서는 현재 규격, 파일 위치, 확인된 결과, 남은 작업을 다음 대화에 전달하는 기준이다.
- 작업 방식은 [AGENTS.md](AGENTS.md)를 따른다. 모델 제작·임포트 및 이전 조립 검증은 과거 보고서다. 기본 조립 기록은 `Artifacts/AssemblyFoundation/`, 개별 교체·해제의 최신 검증은 `Artifacts/AssemblyPartOperations/`에 기록한다.

## 방향키 무기 조합 (시험 구현)

### 게이지와 조합 조작

1. 플레이어 공격이 적에게 피해를 세 번 줄 때마다 파편 게이지가 한 칸 찬다. 적의 공격·사망과 ABC 투창 명중으로 드롭된 파편을 S 공격으로 수집해도 한 칸이 즉시 찬다. 게이지는 세 칸까지 채우며, 드롭은 게임 시간 15초 뒤에 사라진다.
2. 게이지가 가득 차고 장착된 무기가 없을 때 D를 누르면 집중한다. 집중 중에는 좌우 이동 입력이 막히고 ←·↓·→가 조합에 들어간다. 게임 속도와 고정 물리 간격은 기존 값의 15%를 적용한다. 집중은 실제 시간으로 최대 5초다.
3. 최근 세 방향 입력의 개수로 조합을 결정한다. 네 번째부터는 새 입력이 가장 오래된 입력을 교체한다. 세 입력을 채우기 전에 D를 놓으면 입력만 폐기하고 다시 집중할 수 있다.
4. 완성 상태에서 D를 놓으면 게이지 세 칸을 소비하고 무기 2회를 얻는다. 실제 시간 5초가 지나면 방향을 폐기하고 게이지를 보존한다. 두 경우 모두 원래 게임 속도로 돌아와 실제 시간 3초 동안 집중에 새로 진입할 수 없다.
5. 무기를 보유한 동안 D를 새로 누르면 한 번 시전한다. 시전이 시작되면 한 번 차감하고, 두 번 다 쓰면 다시 무기를 조합할 수 있다. 무기 보유 중에도 게이지를 채울 수 있다.

아티팩트 효과는 기존 레시피·장비 API를 통해 대응 무기의 동작이나 수치를 보정한다. 아티팩트별 강화 수치와 장착 화면은 후속 항목이다.

### 무기 10종

조합의 A는 ←, B는 ↓, C는 →다. 같은 방향을 여러 번 입력할 수 있다. `← ↓ →`와 `→ ← ↓`는 모두 ABC다. 아래의 세부 수치는 시제품 기준이다.

| 조합 | 입력 예 | 무기 | 발동 효과 |
| --- | --- | --- | --- |
| AAA | ← ← ← | 강타 | 바로 앞의 적 하나를 강하게 친다. |
| AAB | ← ← ↓ | 늘어나는 칼날 | 제자리에서 칼날을 뻗어 첫 적을 찌른다. 칼끝으로 맞히면 추가 피해를 준다. |
| AAC | ← ← → | 넓은 베기 | 전방을 부채꼴로 베어 범위 안의 적들을 공격한다. |
| ABB | ← ↓ ↓ | 관통 검기 | 직선으로 검기를 날려 경로상의 적들을 관통한다. |
| ABC | ← ↓ → | 관통 투창 | 빠른 창을 던져 적들을 관통하고, 맞힌 적마다 수집 가능한 파편 한 개를 떨어뜨린다. |
| ACC | ← → → | 회전 베기 | 몸 주위를 한 바퀴 베어 가까이 붙은 적들을 공격한다. |
| BBB | ↓ ↓ ↓ | 5발 사격 | 전방 20°에 직선탄 다섯 발을 동시에 발사한다. |
| BBC | ↓ ↓ → | 폭발탄 | 탄이 적이나 지형에 명중하면 주변에 폭발 피해를 준다. |
| BCC | ↓ → → | 산탄 | 전방 총 10°에 산탄 세 발을 동시에 발사한다. |
| CCC | → → → | 광역 파동 | 플레이어를 중심으로 넓은 범위에 피해를 준다. |

## 현재 프로젝트 확인 — 2026-10-30

Git 관리: 루트 `.gitignore`의 `.DS_Store` 규칙으로 모든 하위 폴더의 macOS 메타데이터를 제외한다. 기존 추적 파일 3개는 로컬 파일을 보존하고 인덱스에서 제거했다. 이 제거는 다음 커밋에 포함해야 하며 과거 커밋 기록은 유지한다.

현재 작업 폴더의 `Assets/Function/`에는 플레이어·조각 수집, 적 및 환경 장애물 코드가 있다. [PlayerBase.cs](Assets/Function/Player/PlayerBase.cs)는 2D 횡스크롤 좌우 이동·점프, 공격, 접촉 피해, 조각 수집·특수 사용을 관리한다.

- 방향키 좌우로 가속하고 입력을 놓으면 감속하며 A로 점프한다. `Rigidbody2D`와 `Collider2D`가 필요하고 중력·충돌체와 `Ground Layers`를 설정해야 한다. 지면은 아래 방향 0.05 단위 캐스트와 위쪽 법선으로 판정하며, 상승 중에는 점프 가능 상태를 갱신하지 않는다.
- `maximumMovementSpeed` 5는 목표 최대 속도(단위/초), `Movement Acceleration` 기본 120과 `Movement Deceleration` 기본 180은 속도 변화량(단위/초²)이다. 입력 시 목표 속도로 가속하고 해제 시 0으로 감속한다. 방향 전환에는 가속도를 사용하며 공중에서도 같은 가속·감속을 적용한다. 가속도·감속도 0은 해당 속도 변화를 멈춘다.
- `Jump Force`는 점프 시작 시 적용하는 위쪽 속도(단위/초)다. `Maximum Jump Height`는 이륙 당시 Rigidbody2D 위치에서의 상승 높이 상한이다. `PlayerBase` 소스 초기값은 각각 35와 30이다. 실제 높이는 초기 속도·하향 중력·충돌에도 좌우된다. 높이 또는 점프력 0이면 점프하지 않는다. Dynamic 바디와 하향 중력을 사용한다.
- `Gravity Scale`과 `Movement Friction`을 Awake에서 Rigidbody2D에 적용한다. `PlayerBase`의 코드 중력 배율은 9.8이다. 배율 9.8은 월드 중력 Y=-9.81에 곱해져 플레이어에 약 -96.14 유닛/초²를 적용한다. 마찰 기본값은 0이며 플레이어별 런타임 PhysicsMaterial2D에 기존 바디 재질의 탄성 값을 복사한다. 런타임 재질은 OnDestroy에서 해제한다. 개별 Collider에 별도 재질을 지정한 다른 구성은 추가 확인이 필요하다.
- `Coyote Time` 기본 0.12초 동안 발판을 벗어난 뒤에도 한 번 점프할 수 있다. 점프에 사용하면 소진하며 공중 재점프는 허용하지 않는다. 착지하면 다시 점프할 수 있고 버튼을 계속 누르는 것만으로 자동 재점프하지 않는다.
- A의 점프 입력 버퍼 기본값은 0.01초다. 착지 직전 입력은 첫 접지 판정에서 한 번 사용하고, 만료된 입력은 버린다. 접지는 직전 물리 단계의 결과이므로 착지 시점을 한 물리 단계 범위에서 보정한다. 현재 물리 단계 간격은 0.02초다. 사망, 비활성화, 체크포인트 복원 시 버퍼를 지운다.
- 점프 최고점에 도달하거나 상승 중 A를 놓으면 `jumpFallGravityMultiplier` 기본값 1.55를 해당 시점의 Rigidbody2D 중력 배율에 곱한다. 기본 배율 9.8에서는 15.19를 적용하고 착지 시 기존 배율을 복원한다. 물리 갱신 사이의 빠른 탭에도 적용하며 점프 경로 예측에도 같은 중력 전환을 반영한다.
- `PlayerBase`의 능력치·이동·점프·공격·특수 목록은 비직렬화 private 필드다. 두 테스트 씬을 불러오면 소스 초기값을 사용한다.
- 위 이동·점프 수치는 `PlayerBase`와 두 테스트 씬의 공통 기준이다. 사용자가 해당 수치 변경을 명시적으로 요청할 때만 수정한다.
- S를 누르고 있으면 `Attacks Per Second` 횟수/초로 조준 방향에 맞춘 회전 사각 범위를 검사한다. 적은 일반 Collider로 피해를 판정하고 조각은 일반 Collider와 Trigger 모두로 수집을 판정한다. 적의 여러 Collider가 겹쳐도 피해는 한 번만 적용한다. 처리 후 `AttackRequested` 이벤트를 발생시킨다. 공격 범위와 레이어는 `PlayerBase` 코드에서 설정한다. D는 게이지가 찼을 때 집중을, 이미 무기를 얻었을 때 시전을 시작한다.
- 집중 밖에서 S 일반 공격과 방향성 D 무기는 누른 방향키의 상·하·좌·우 방향을 사용한다. 상하와 좌우가 동시에 입력되면 상하가 우선이고 반대 방향 두 개는 해당 축에서 상쇄된다. 조준에 사용할 입력이 없으면 마지막으로 바라본 좌우를 사용한다. 점프 후 ↓를 누른 채 S 또는 D를 입력하면 아래를 공격한다. 집중 중 방향키는 기존 조합에 사용한다.
- 시작 시 현재 체력을 코드의 최대 체력 100으로 초기화한다. `ApplyDamage`로 체력이 처음 0이 되면 사망 상태에서 입력·조각 수집·특수 사용을 멈추고 마지막 체크포인트를 불러온다. 유효한 저장이 없으면 사망 상태로 남는다. 넉백·사망 및 피격 연출은 후속 작업이다.
- 활성화된 적에 닿으면 적의 `Attack Power`만큼 체력이 줄어든다. 사망 상태이거나 비활성화된 적은 접촉 피해를 주지 않는다. 적과 장애물은 일반 충돌과 트리거를 모두 지원하며, 피해 컴포넌트는 Collider가 달린 오브젝트 또는 그 부모에서 찾는다.
- 환경에 접촉 피해를 주려면 [DamagedObstacle](Assets/Function/Environment/DamagedObstacle.cs)을 붙이고 Inspector에서 `Damage`(기본 10)를 설정한다. 같은 오브젝트 또는 자식에 `Collider2D`가 필요하며, 플레이어와 장애물의 물리 레이어 간 충돌을 허용해야 한다. `Is Trigger`를 켜면 통과하면서 피해를 받고, 끄면 일반 물리 충돌과 함께 피해를 받는다.
- 플레이어의 `contactDamageCooldown`은 코드에서 0.75초다. 적과 장애물이 이 간격을 공유하므로 여러 콜라이더에 한꺼번에 닿아도 한 번만 피해를 받는다. 접촉이 이어지면 간격이 지난 뒤 다시 피해를 받고, 접촉이 끝나면 추가 피해를 받지 않는다. 외부의 직접 `ApplyDamage` 호출에는 이 간격을 적용하지 않는다.
- `WeaponFocus`는 방향 조합 입력, 세 칸의 파편 게이지, 명중 진행도와 실제 시간 제한을 관리한다. S 근접 공격으로 드롭된 파편을 수집하며 파편의 기존 A/B/C 분류는 게이지 충전량에 영향을 주지 않는다.
- 드롭된 조각은 게임 시간 기준 15초 동안 유지된다. 0~10초는 원래 모습으로 표시하고, 10~13초에는 0.3초마다 원래 Alpha에서 0으로 흐려졌다 돌아온다. 13~15초에는 같은 왕복을 0.1초마다 반복하고 15초에 제거한다. `Time.timeAsDouble`을 사용해 게임 시간 배율에 따라 수명과 흐림이 함께 진행된다.
- 게이지가 가득 찼을 때 추가 파편은 수명이 끝날 때까지 월드에 남는다. 집중이 만료되거나 플레이어가 사망·비활성화되거나 저장 상태를 복원하면 집중과 임시 방향 입력을 해제하고 게임 속도를 복원한다.
- `TryCollectFragment`와 `TryUseSpecial`은 외부 코드에서 호출할 수 있다. `TryUseSpecial`은 효과가 실행에 성공한 뒤에만 횟수를 한 번 줄이고 `SpecialUsed`와 `SpecialRequested`를 발행한다. `SpecialUsed`는 마지막 사용에도 해당 시전의 `FragmentCombination`을 전달한다. 유효한 효과를 선택하지 못하거나 실행이 실패하면 횟수를 보존한다.
- 새 Input System 키보드를 직접 읽는다. 키 변경, 게임패드, 캐릭터 프리팹 연결 및 애니메이션은 미구현이다. Rigidbody2D는 Dynamic 및 적절한 회전 고정과 지형 충돌 설정이 필요하다.

### 특수 효과와 장비 보정

- `Assets/Function/Special/`의 `SpecialRecipe`는 A/B/C 개수와 기본 효과를 설정하는 에셋이다. Create → Special → Recipe로 생성하고 개수 합을 3으로 지정한다. 플레이어의 비직렬화 `specialRecipes` 목록은 현재 빈 배열이며, 등록된 레시피가 해당 조합에 없으면 10종의 기본 조합 공격을 사용한다. 같은 개수 조합을 중복 등록하면 배열의 첫 레시피를 사용한다.
- `SpecialEffectDefinition`의 파생 클래스는 `CreateCast`에서 매번 새로운 `ISpecialEffect` 인스턴스를 반환한다. `SpecialContext`는 시전자, 조합, 시전 위치, 좌우 바라보는 방향과 공격 조준 벡터를 보관한다. 방향성 근접 판정과 투사체는 시전 순간의 조준을 사용한다. 산탄과 다발탄은 그 방향을 중심으로 퍼지고 회전 베기와 광역 파동은 시전자를 중심으로 원형 판정을 사용한다. `ISpecialEffect.TryExecute`는 실제 효과를 시작했을 때 성공을 반환한다. `SpecialModifierDefinition`은 `ISpecialModifier`를 구현하며 호환되는 시전 인스턴스의 수치나 동작을 변경한다.
- Create → Special → Equipment로 생성한 장비 에셋은 실행 중 `SetSpecialEquipment`로 장착 목록을 교체한다. `Target Recipe`를 비우면 모든 조합에 적용한다. 기본 조합 공격에는 대상 레시피의 A/B/C 개수가 일치하는 장비도 적용한다. 교체 효과가 여러 개면 `Replacement Priority`가 가장 높은 장비를 선택하고, 동점이면 장착 목록에서 앞선 장비를 선택한다. 최종 효과를 생성한 뒤 적용 대상인 장비의 보정을 장착 순서와 각 장비의 보정 목록 순서대로 적용한다.
- `FragmentSpecialEffect`는 `ISpecialEffect`를 구현하고 피해 배수, 사거리, 칼날 폭과 칼끝 피해, 파편 각도와 피해, 투사체 개수와 속도, 관통, 폭발 반경의 변경 가능한 프로퍼티를 제공한다. `ISpecialModifier.Apply`에서 타입을 확인해 이번 시전의 수치를 수정할 수 있다. Create → Special → Fragment Effect의 `FragmentSpecialDefinition`은 등록 레시피나 장비에서 사용할 조합 공격을 선택한다.
- `SpecialEffectVisual`은 10종 특수 공격의 범위와 탄두·꼬리, 명중점 폭발을 월드 공간에 그린다. 베기와 파동은 피해 판정에 전달한 반경·각도, 폭발은 같은 중심과 반경을 사용한다. 범위 표시는 지형에 가려진 부분을 포함한 판정 시도 범위이며, 실제 타격에는 지형 차단 검사도 적용한다. 장비로 범위나 투사체 개수를 바꾸면 시각 효과에도 반영된다. 캐릭터 공격 애니메이션과 장비 화면은 후속 작업이다.

기본 조합의 아래 수치는 구현 초기값이다. 피해량은 시전 시점 플레이어 공격력의 배수이며 `FragmentSpecialEffect.cs`에서 기본값을 조정한다. 칼날과 범위 공격은 지형에 가려진 적을 제외한다.

| 조합 | 공격 | 피해 배수 | 기본 범위와 동작 |
| --- | --- | --- | --- |
| AAA | 강타 | 6배 | 전방 사거리 1.8, 가장 가까운 적 한 개 |
| AAB | 늘어나는 칼날 (`Extending Blade`) | 기본 3배, 칼끝 4.5배 | 제자리에서 전방 4 × 0.55까지 첫 적을 찌른다. 충돌 지점이 끝쪽 25%에 있으면 칼끝 피해를 준다. |
| AAC | 넓은 베기 | 3배 | 전방 반경 3.2, 부채꼴 120도 |
| ABB | 관통 검기 | 3배 | 속도 18, 사거리 14, 적 관통 |
| ABC | 관통 투창 (`Piercing Spear`) | 적마다 2.5배 | 속도 32, 사거리 18. 적을 관통하며 적마다 파편 한 개를 생성한다. |
| ACC | 회전 베기 | 3배 | 시전자 중심 반경 2.5 |
| BBB | 5발 사격 (`Five Shot`) | 탄당 1배 | 속도 24, 사거리 20. 20도에 직선탄 다섯 발을 동시에 발사한다. |
| BBC | 폭발탄 | 3배 | 속도 18, 사거리 16, 명중점 반경 1.8 폭발 |
| BCC | 산탄 (`Buckshot`) | 탄당 1배 | 속도 16, 사거리 16. -5°, 0°, +5°로 산탄 세 발을 동시에 발사한다. |
| CCC | 광역 파동 | 2.5배 | 시전자 중심 반경 6 |

BBC는 탄 하나를 발사하며 적이나 지형에 닿으면 직접 명중한 적을 포함한 범위에 한 번 폭발 피해를 준다. ABB와 ABC는 적마다 한 번씩 피해를 주고 지형에서 멈춘다. BBB와 BCC의 개별 탄은 처음 맞힌 적에서 멈춘다. 시전 성공은 적 명중 여부와 별개로 효과가 시작되었는지를 기준으로 한다.

`Mech.Function.EnemyBase`는 Inspector에서 최대 체력, 그로기 수치, 그로기 저항, 공격력, 공격 속도를 설정한다. 기본값은 각각 100, 0, 0, 10, 1이며 수치 단위와 밸런스는 아직 확정되지 않았다. 현재 체력은 Awake에서 최대 체력으로 초기화하고 `ApplyDamage`는 양수 유한 피해를 적용하여 0 이상으로 제한한다. 파생 클래스에서 Awake를 재정의하면 `base.Awake()`를 호출해야 한다.

적마다 Inspector의 `Held Object Type`에서 A/B/C 중 하나를 선택하고 `Held Object`에 드롭할 조각 프리팹을 지정한다. 공격 동작을 실행하거나 체력이 0이 되면 파편을 드롭한다. 접촉형 적은 플레이어에게 접촉 피해를 실제로 줬을 때 드롭한다. 지상 보스는 기존 패턴별 보상 수량을 적용한다. 원본 미지정 시 기본 표시와 트리거가 있는 파편이 생성되며, 일반 드롭은 아래쪽 바닥을 찾으면 그 근처에 배치한다. `Held Object Type`은 기존 드롭 분류와 이벤트 호환에 사용한다.

조각 프리팹에는 플레이어의 공격 레이어에 속한 활성 `Collider2D`가 필요하다. Trigger를 사용하면 조각을 통과할 수 있고 S 공격으로 수집한다. Trigger 조각을 공격 범위 쿼리에 포함하려면 현재 프로젝트처럼 `Queries Hit Triggers`를 켠다. 수집 성공 시 조각 오브젝트를 제거한다. 게이지가 가득 차 있으면 파편은 월드에 남으며, 수명 안에 게이지를 소비하고 다시 공격하면 수집할 수 있다. 보스 테스트 구역에는 B 조각 드롭 프리팹이 연결되어 있다. [BossFragmentB.prefab](Assets/Prefabs/PlayTest/BossFragmentB.prefab)

적 FSM은 초기 `Idle`과 `Patrol`, `Chase`, `Attack`, `Groggy`, `Dead`, `Windup` 상태를 제공한다. `TryChangeState`로 수동 전환하며 정의되지 않은 값과 동일 상태는 거부하고, 성공 시 `StateChanged` 이벤트에 이전·새 상태를 전달한다. 치명적인 피해는 `Dead`로 전환하고 `IsDefeated`를 설정하며 `Defeated` 이벤트를 한 번 발생시킨다. 처치 후 재피해·다른 상태 전환은 거부한다. 단순한 수동 `Dead` 전환은 처치·드롭을 실행하지 않는다. 공통 적 오브젝트 자동 제거와 그로기 누적·회복·저항 계산은 미구현이다.

- 플레이어의 근접 공격은 `ApplyPlayerDamage(damage, attacker)`를 호출한다. 유효한 피해가 명중하면 적 스프라이트는 실제 시간 기준 0.03초 동안 밝은 회색 sRGB `(230, 230, 230)`을 표시한다. 투명도와 실루엣을 유지하고 시간이 지난 뒤 첫 갱신 또는 비활성화 시 원래 머티리얼을 복원한다. [EnemyWhiteFlash](Assets/Function/Enemy/EnemyWhiteFlash.cs)
- 명중하면 공격자와 피격 대상의 `Animator` 재생 속도를 0으로 설정하고 실제 시간 기준 0.01초 뒤 기존 속도로 복원한다. 직교 메인 카메라에는 렌더링 중 화면 기준 상하좌우 방향으로 약 5픽셀의 흔들림을 현재 소스 기준 0.03초 적용하며, 이동량은 지정한 화면 경계까지 제한한다. 두 효과의 표시와 복원은 프레임 단위로 처리한다. 투사체나 특수 효과에서 공격자의 `Animator`도 정지하려면 `attacker`를 전달한다. 일반 `ApplyDamage`는 체력과 처치만 처리한다. [GameHitStop](Assets/Function/GameHitStop.cs), [AttackCameraShake](Assets/Function/AttackCameraShake.cs)
- 기존 등급 클래스는 `FragmentEnemy`, `NormalEnemy`, `AdvancedEnemy : NormalEnemy`, `BossEnemy`다. 조각 적은 실제 플레이어 피해를 기준으로 Inspector 체력을 조절해야 한두 번에 처치할 수 있다. 아래 행동 클래스는 별도로 `EnemyBase`의 능력치·처치·조각 드롭을 상속한다.
- [TestEnemy](Assets/Function/Enemy/TestEnemy.cs)는 `EnemyBase`를 직접 상속한다. Inspector에서 `Held Object Type`과 `Held Object`를 지정하면 Player 공격으로 체력이 0이 될 때 공통 처치 처리로 조각 하나가 드롭된다. 미지정 시 경고만 발생한다.
- 조합에 필요한 조각 수와 특수 사용 횟수는 현재 각각 코드 상수 3이다. N개로의 확장과 조합별 실제 효과·UI는 후속 작업이다.

### 보스 패턴

[BossEnemy](Assets/Function/Enemy/BossEnemy.cs)의 최대 체력과 시작 체력은 2250이다. 활성화 3초 뒤 살아 있는 플레이어가 있으면 돌진을 시작한다. 일반 공격은 돌진 → 내려찍기 → 광역 순서로 반복하며, 일반 공격 두 번을 완료할 때마다 다음 패턴으로 조각 적 소환을 실행한다. 패턴을 끝낸 시점부터 일반 공격 뒤에는 3초, 소환 뒤에는 5초를 기다린다.

| 패턴 | 동작 | 패턴 종료 시 조각 드롭 |
| --- | --- | --- |
| 돌진 | 사거리 제한 없이 시작 순간 플레이어가 있는 좌우 방향을 정하고 벽까지 수평으로 돌진한다. 기본 속도는 초당 20 Unity 단위다. 경로의 플레이어에게 피해를 준다. | 벽 충돌 후 1개 |
| 내려찍기 | 발밑 높이부터 전방에 기본 2 × 2.5 크기의 공격 트리거를 열어 한 번 판정한다. | 1개 |
| 광역 공격 | 같은 방식으로 전방에 기본 6 × 2.5 크기의 공격 트리거를 열어 한 번 판정한다. | 2개 |
| 조각 적 소환 | 보스의 발밑 높이에서 벽 안쪽의 빈 바닥을 찾아 조각 적 3마리를 소환한다. 소환 적은 고정된 장애물로 동작하며 접촉 피해를 주고, 처치하면 지정한 조각을 떨어뜨린다. | 소환 적 처치 시 지정 조각 |

패턴 보상 조각은 보스의 `Held Object`와 `Held Object Type`을 우선 사용한다. 보스의 `Held Object`가 비어 있으면 `Fragment Enemy Prefab`의 드롭 설정을 사용한다. 현재 테스트 보스는 B 조각을 드롭한다. 돌진 후에는 벽 반대쪽, 나머지 공격 후에는 보스가 바라보는 쪽에 조각을 배치하며 배치 방향에 벽이 있으면 반대쪽을 사용한다. 보상 조각도 S 공격으로 수집하고 15초 수명을 적용한다.

보스와 조각 적을 포함한 모든 살아 있는 적은 접촉 시 각자의 `Attack Power`만큼 피해를 준다. 보스의 돌진·내려찍기·광역 공격도 같은 피해량과 플레이어의 공통 재피격 간격을 사용한다. 보스의 Inspector에서 `Fragment Enemy Prefab`에 고체 Collider2D와 `Held Object`·`Held Object Type`이 설정된 조각 적을 연결한다. `Environment Layers`에는 고체 바닥·벽 레이어를 포함하고, `Fragment Spawn Spacing`에 맞춰 세 마리가 설 바닥을 확보한다. 프리팹이 비어 있거나 빈 바닥이 부족하면 경고를 기록한다. PlayTestScene에는 테스트용 보스와 소환 적 프리팹을 연결했다. 확정 외형과 공격 애니메이션은 후속 작업이다.

### 비행 보스

`FlyingBossEnemy`의 시작 체력과 최대 체력은 3400이다. `Arena Bounds`에 연결한 사각 경계의 가로 중앙선에서 좌우로 비행하고, 기본 진폭 0.35만큼 위아래로 움직인다. 경계가 비어 있으면 배치 위치를 중심으로 좌우 8유닛을 사용한다. 다음 다섯 패턴을 순서대로 반복하며, 기본 첫 대기와 각 패턴 종료 후 대기는 3초다. [FlyingBossEnemy.cs](Assets/Function/Enemy/FlyingBossEnemy.cs)

| 패턴 | 동작 |
| --- | --- |
| 조각 적 소환 | 바닥 위에 B 조각을 떨어뜨리는 조각 적을 3~5마리 띄운다. 소환된 적은 작은 진폭으로 부유하고 접촉 피해를 준다. |
| 돌진 적 소환 | 첫 적을 바로 소환하고 2초 간격으로 두 마리를 더 소환한다. 각 적은 생성 순간의 플레이어 방향으로 직진하며 지형 충돌 또는 수명 만료 시 제거된다. |
| 직접 돌진 | 플레이어의 발진 시점 위치를 향해 2D 방향을 고정한다. 벽·바닥 등 고체 지형과 충돌하면 공중 순찰로 돌아간다. |
| 포물선 사격 | 플레이어 위치를 조준한 중앙탄과 좌우 15도에 한 발씩, 총 3발을 발사한다. 탄은 낙하 가속도를 받고 지형이나 플레이어에 닿으면 제거된다. |
| 방사형 사격 | X자로 직선탄 4발을 쏘고 1.5초 뒤 십자로 4발을 쏜다. |

`FlyingBossTestScene`을 열고 Play를 누르면 바로 시험할 수 있다. 경기장에는 바닥·양쪽 벽·천장과 비행 중앙선, 시작 체크포인트가 있다. 좌우 방향키로 이동하고 A로 점프, S로 공격한다. 기존 테스트용 B 조각 적 프리팹을 재사용했으며 보스, 돌진 적, 투사체에는 단색 테스트 외형을 연결했다. [FlyingBossTestScene](Assets/Scenes/TestScene/FlyingBossTestScene.unity), [FlyingTestBoss.prefab](Assets/Prefabs/PlayTest/FlyingTestBoss.prefab)

### 적 행동 클래스

세 클래스는 Inspector의 `Target Player`를 우선 사용한다. 지정하지 않으면 같은 씬에서 체력이 남아 있는 활성 플레이어를 찾는다.

| 클래스 | 동작과 설정 |
| --- | --- |
| [MeleeEnemy](Assets/Function/Enemy/MeleeEnemy.cs) | 플레이어의 좌우 위치를 `Move Speed`(기본 3)로 계속 추적한다. 별도 공격 동작 없이 기존 접촉 피해를 준다. 같은 오브젝트에 Dynamic `Rigidbody2D`와 `Collider2D`를 설정한다. |
| [ChargeEnemy](Assets/Function/Enemy/ChargeEnemy.cs) | 고정 높이에서 정지하다가 `Detection Range`(기본 12) 이내의 플레이어를 탐지하면 0.5초간 `Windup` 상태로 대기한다. 발진 순간 대상의 좌우 방향으로 1초 동안 최대 300 Unity 월드 단위를 이동하며, 돌진 시작은 기본 5초 간격이다. 탐지한 대상이 살아 있고 활성 상태라면 범위를 벗어나도 주기를 이어간다. 바디를 Kinematic으로 설정하고 `Collider2D` 모양으로 경로를 검사해 벽에서 정지하며, 플레이어 접촉 피해에는 공통 재피격 간격을 적용한다. |
| [FlyingEnemy](Assets/Function/Enemy/FlyingEnemy.cs) | 배치된 위치에서 `Attack Speed`(기본 초당 1회)에 맞춰 한 발씩 쏜다. 발사 순간의 플레이어 위치를 향해 기본 속도 50으로 직진하며, 플레이어 명중 시 `Attack Power`만큼 피해를 준다. 이동은 구현하지 않았으므로 공중에 배치하고 바디가 있다면 중력을 적용하지 않는다. |

`FlyingEnemy`는 `Projectile Spawn Point` 미지정 시 적의 위치에서 발사한다. [EnemyProjectile](Assets/Function/Enemy/EnemyProjectile.cs)은 비행 경로의 플레이어 또는 첫 일반 지형에 닿거나 수명(기본 4초)이 끝나면 제거된다. 기본 투사체는 충돌 판정만 있으며, 화면에 표시하려면 SpriteRenderer 등을 갖춘 투사체 프리팹을 `Projectile Prefab`에 연결한다. 새 적과 투사체 프리팹은 저장된 씬에 배치하지 않았다.

2026-10-30 확인 당시 이전 기록의 `Assets/Mesh/`, 파츠·조립·무기 코드 및 당시 메카 관련 `Artifacts/`는 발견되지 않았다. 아래 1절의 기존 상태표·파일표, 2절의 디렉터리 예시 및 3–9절의 구현·검증·인계 설명은 **과거 기록과 목표 규격**으로 보존하며 현재 파일의 존재나 구현 완료를 뜻하지 않는다. 2절의 개발·명명·테스트 및 서식 규칙은 계속 적용한다. 과거 보고서는 현재 실행 결과가 아니다. 현재 작업 폴더에는 `.git`이 있다.

### 플레이 테스트 씬

[PlayTestScene](Assets/Scenes/TestScene/PlayTestScene.unity)을 Unity에서 열고 Play를 누른 뒤 Game 화면을 클릭한다. 방향키로 이동하고 A로 점프하며 S로 공격한다. 공격 명중이나 파편 수집으로 게이지를 채운 뒤 D를 누른 상태에서 방향 세 번을 입력하고 놓는다. 다음 D 입력으로 획득한 무기를 시전한다. 시작 구역의 적 세 마리는 각각 체력 30이며 씬 안의 원본에서 A/B/C 조각을 드롭한다. 플레이어의 코드 공격력은 10이며 초당 한 번 공격할 수 있다. 씬 전용 [PlayTestDisplay.cs](Assets/Scenes/TestScene/PlayTestDisplay.cs)는 시작 구역의 임시 도형·상태 표시와 적 외형 비활성화를 담당한다.

PlayTestScene과 FlyingBossTestScene의 화면 왼쪽 아래에 정사각형 네 칸을 표시한다. 1~3번은 평상시 파편 게이지와 명중 진행도를, 집중 중에는 최근 방향 입력을 표시한다. 4번은 집중 가능 여부·집중 상태나 활성 무기의 이름과 남은 사용 횟수를 표시한다. 장비가 효과를 바꾸면 교체된 무기 이름을 표시한다.

[PlayTestScene 게임 화면](Artifacts/FragmentSlots/PlayTestScene/03-cab.png)과 [조각 획득부터 세 번째 시전까지의 HUD 캡처](Artifacts/FragmentSlots/PlayTestScene/hud_sequence.png)는 2026-11-19 당시 조각 조합 방식의 Unity macOS 플레이어 화면이다.

시작 구역에서 오른쪽으로 이동하면 `BOSS ARENA` 표지와 입구 체크포인트가 나온다. 오른쪽으로 이동하면서 A를 길게 눌러 낮은 입구 벽을 넘는다. 보스가 벽에서 방 안을 바라볼 때 바닥의 빨간 표식은 내려찍기, 주황 표식은 광역 공격 범위를 가리킨다. 테스트 보스는 씬 시작 뒤 기존 패턴을 자동으로 진행한다.

씬을 불러올 때 메인 직교 카메라는 [PlayerCameraFollow](Assets/Function/PlayerCameraFollow.cs)의 공통 기본값을 사용한다. 직교 크기는 10이고 같은 씬의 플레이어를 `Smooth Time` 0.3초로 수평 추적한다. 두 테스트 씬은 고정 Y 높이와 X 오프셋 +2를 사용한다. 별도 설정이 필요한 씬은 메인 카메라의 `Player Camera Follow` 컴포넌트에서 `Orthographic Size`, `Follow Offset`, `Follow Vertical`을 지정한다.

월드 좌표의 경계를 적용하려면 `Limit To Bounds`를 켜고 `World Bounds` 사각형의 왼쪽 아래 X·Y 및 폭·높이를 입력한다. 씬 뷰에서 카메라를 선택하면 경계를 확인할 수 있다. 화면 비율과 실제 표시 영역을 기준으로 카메라 위치와 명중 흔들림을 제한한다. 경계가 화면보다 작은 축에서는 직교 크기와 오브젝트의 화면 배율을 유지하고 그 축의 표시 영역을 잘라낸다. 두 테스트 씬의 경계는 기준 좌표가 정해지기 전까지 꺼 둔다.

보스 프리팹은 시작 체력 2250이며, 소환 적은 체력 20·접촉 피해 10으로 설정했다. 소환 적은 기본 공격 두 번에 처치되고 B 조각을 떨어뜨린다. [TestBoss.prefab](Assets/Prefabs/PlayTest/TestBoss.prefab), [BossFragmentEnemy.prefab](Assets/Prefabs/PlayTest/BossFragmentEnemy.prefab)

A로 점프가 시작되면 게임 화면에 민트색 실선으로 지나온 경로를, 노란 점으로 현재 입력 기준의 예상 경로를 표시한다. 방향을 바꾸거나 A를 일찍 놓으면 예측을 갱신하며, 예측은 첫 충돌 또는 최대 1.8초까지만 표시한다. 착지 후 실선은 1.25초 동안 남는다. [점프 경로 코드](Assets/Scenes/TestScene/JumpPathDisplay.cs), [게임 화면 예시](Artifacts/JumpTrajectory/jump_path_centered.png)

배치된 A/B/C 적의 접촉 피해는 각 10이며, 적에게 계속 닿아 있으면 기본 0.75초 간격으로 체력이 줄어든다.

플레이어가 적을 처치하면 테스트 표시 코드가 적의 충돌체를 끄고 WhiteFlash가 끝나면 외형을 숨긴다.

다음 작업은 사망 UI와 오디오 믹서를 연결하는 것이다. 세 적과 위험 장애물의 씬 배치, 플레이어 피격·사망 연출, 실제 캐릭터·조각 에셋과 공격 애니메이션 연결도 남아 있다.

이동 구현 당시의 [C# 대역 검증](Artifacts/PlayerMovement/verification.txt), [이전 비교 측정](Artifacts/PlayerMovementPlayTest/results.json), [설정 적용 후 최신 PlayMode 검증](Artifacts/ResponsiveMovementApplied/verification.json)을 구분한다. 최신 측정은 저장된 씬을 MCP로 실행·일시 정지한 뒤 PlayerBase.FixedUpdate와 실제 PhysicsScene2D를 고정 간격으로 진행한 결과다. OS 키보드·마우스를 사용하지 않았으며 Update의 실제 입력 수신과 사용자의 주관적인 조작감은 검증하지 않았다.

2026-11-07 빠른 반응 설정의 당시 측정에서는 가속도 120·감속도 180·점프력 18.5·중력 배율 5.5·마찰 0에서 가속 0.04초·정지 0.04초·방향 전환 0.10초·높은 점프 체공 0.66초를 기록했다. 해당 점프 측정은 당시 높이 상한 3·해제 배율 0.5의 결과이며, 현재 점프 설정은 위의 플레이어 규격을 따른다.

### 자동 저장·이어하기

살아 있는 플레이어가 [Checkpoint](Assets/Function/Progress/Checkpoint.cs) 트리거에 진입하면 자동 저장을 예약한다. 같은 프레임의 충돌과 아이템 획득을 처리한 뒤 플레이어가 살아 있으면 **진행 슬롯 하나**를 갱신한다. 마지막으로 저장한 씬과 체크포인트 ID, 파편 게이지와 누적 명중, 활성 무기 조합과 남은 사용 횟수, 일회성 아이템 획득 ID를 기록한다. 이전 저장에서 미조합 파편 개수는 게이지로, 활성 조합은 새 무기로 옮긴다. 이전 사용 횟수는 최대 2회까지 복원한다.

- 체크포인트 외에도 중요 아이템 획득 직후, 실제 플레이 시간 누적 3분마다, 지역 이동을 마쳤을 때, 플레이 중 정상 종료할 때 같은 슬롯에 저장한다. 주기 타이머는 타이틀·사망·일시 정지·로드 중에는 멈춘다. 정상 종료 저장은 활성 지역에서 살아 있는 플레이어의 진행을 기록한다. [ProgressAutosave.cs](Assets/Function/Progress/ProgressAutosave.cs)
- 중요 아이템·주기·종료 저장은 현재 지역의 마지막 체크포인트를 복귀 지점으로 사용한다. 현재 지역에서 체크포인트에 닿은 기록이 없으면 지역 시작 위치를 저장한다. 지역 이동은 도착 씬의 지정 체크포인트나 시작 위치를 저장한다.
- [CheckpointProgress](Assets/Function/Progress/CheckpointProgress.cs)의 `HasSave()`로 저장 유무를 확인하고 `TryContinue()`로 이어하기를 시작한다. `TryContinue()`의 반환값은 로드 요청을 시작했는지 나타내며, 실제 복원 성공 여부는 `LoadFinished`로 받을 수 있다. 사망 후 다시하기도 같은 경로를 자동으로 호출한다.
- 복원 시 씬을 다시 불러 일반 적을 초기 체력과 위치에 리스폰한다. 체크포인트에 배치한 플레이어는 최대 체력을 얻고 저장 시점의 게이지·명중 진행도·무기와 남은 횟수를 복원한다. 리스폰한 적의 조각은 다시 획득할 수 있다. 별도 보스 처치나 문 상태를 영구 보관하는 기능은 아직 없다.
- 진행 파일은 `Application.persistentDataPath/checkpoint.json`이다. 임시 파일로 쓰고 이전 정상 저장은 `checkpoint.json.bak`에 보관한다. 주 파일이 없거나 손상되면 정상 백업을 읽는다. 동일한 스냅샷을 다시 저장할 때는 파일을 교체하지 않는다. 그래픽·사운드 옵션은 별도 저장을 사용한다.
- 씬에 활성 플레이어 한 명을 두고, 체크포인트마다 씬 내에서 고유한 `Checkpoint Id`와 활성 `BoxCollider2D` 트리거를 지정한다. `Spawn Point`가 없으면 체크포인트 위치에서 시작한다. 씬은 빌드 목록에 등록해야 한다.
- PlayTestScene에는 시작 부근 X=-3.5, 중간 X=0.5, 보스 입구 X=6.5에 체크포인트가 있다. 빌드 목록에는 `SampleScene`에 이어 `PlayTestScene`을 등록했다. 새 게임에서 첫 자동 저장 전에 사망하면 사망 상태로 남는다. 사망 UI는 후속 작업이다.

지역 출구에는 `RegionTransition`과 `BoxCollider2D` 트리거를 두고 Inspector의 `Destination Scene Path`에 빌드 등록된 지역 씬 경로를 지정한다. `Destination Checkpoint Id`를 비우면 도착 씬에 배치된 플레이어의 시작 위치를 사용한다. 지정할 경우 해당 씬에 활성 플레이어 한 명과 그 ID의 체크포인트를 두고, 도착 위치는 출구 트리거 밖에 배치한다. 이동 중에는 현재 체력을 유지하며 이어하기와 사망 후 다시하기 때는 최대 체력으로 회복한다. 실제 지역 통로는 맵을 구성할 때 배치한다. [RegionTransition.cs](Assets/Function/Progress/RegionTransition.cs)

### 일회성 아이템

아이템 오브젝트에 `OneTimePickup`과 `BoxCollider2D` 트리거를 두고 Inspector의 `Item Id`에 게임 전체에서 고유한 고정 ID를 지정한다. 예를 들어 폐광 열쇠는 `mine.old_key`로 지정한다. 최초 획득 시 아이템을 숨기고 `On Collected`를 한 번 호출한다. 지급이나 연출이 필요하면 이 이벤트에 연결한다. [OneTimePickup.cs](Assets/Function/Progress/OneTimePickup.cs)

자동 저장은 획득한 ID를 `CollectedItemIds`에 기록한다. 중요 아이템은 Inspector에서 `Auto Save On Collect`를 켜면 획득 보상을 처리한 뒤 즉시 저장한다. 이어하기와 사망 후 다시하기는 이 목록으로 복원하며, 저장된 아이템은 숨기고 저장 이후에 주운 아이템은 다시 배치된 상태로 돌아간다. 기존 JSON에 이 필드가 없으면 빈 목록으로 읽는다. 새 게임은 빈 획득 목록으로 시작한다.

### 타이틀 화면

[SampleScene](Assets/Scenes/SampleScene.unity)은 빌드의 첫 씬이며 새 게임·이어하기·옵션 버튼을 표시한다. [TitleMenu.cs](Assets/Function/UI/TitleMenu.cs)가 동작을 연결하고 화면 배치는 [UXML](Assets/UI/Title/TitleMenu.uxml), 모양은 [USS](Assets/UI/Title/TitleMenu.uss)에서 편집한다.

- 새 게임은 PlayTestScene의 기본 위치에서 조각·특수 횟수 없이 시작한다. 기존 슬롯은 새 게임에서 첫 자동 저장이 성공하면 갱신된다. 그 전에 사망해도 이전 게임으로 복원하지 않는다.
- 이어하기는 유효한 저장이 있을 때 활성화되며 마지막 체크포인트와 저장된 진행을 복원한다.
- 옵션은 화면 설정과 전체·BGM·효과음 볼륨을 편집한다. 적용 및 저장을 누르면 게임에 반영하고, 뒤로를 누르면 현재 편집값을 버린다. 한글은 운영체제에 설치된 지원 글꼴을 사용한다.

### 게임 옵션 코드

[GameOptions](Assets/Function/Options/GameOptions.cs)는 해상도와 화면 모드, 품질 프리셋, 수직 동기화, 프레임 제한, 전체·BGM·효과음 볼륨을 관리한다. 첫 씬에서 기존 저장 옵션을 불러온다.

- UI에서는 `GetCurrent()`로 편집용 복사본을 받아 값을 바꾸고 `Apply(values)`를 호출한다. `Save()`는 적용한 값을 `PlayerPrefs`에 저장하고 `Load()`는 저장 값으로 복원한다. `ResetToDefaults()`는 시작 시 기록한 기본값을 적용하며 저장은 `Save()`에서 진행한다.
- 품질 프리셋은 `QualitySettings.names`에서 고르고 이름으로 저장한다. `FrameRateLimit`의 `-1`은 플랫폼 기본값이며, 데스크톱에서 VSync가 켜져 있으면 VSync가 프레임 간격을 결정한다. 화면 모드와 해상도 적용 결과는 플랫폼과 지원 모드에 따라 달라진다.
- 전체 볼륨은 `AudioListener`에 즉시 적용된다. BGM과 효과음은 AudioMixer 그룹에 소스를 연결하고 노출한 볼륨 파라미터 이름을 각각 `MusicVolume`, `EffectsVolume`으로 지정한다. `Start` 이후 `GameOptions.TryBindAudioMixer(mixer)`를 호출하면 현재 값이 적용된다. 타이틀 옵션에서 개별 값을 저장할 수 있으며 실제 믹서 에셋 연결은 후속 작업이다.

### 현재 Unity MCP 연결 — 2026-11-06

CoplayDev MCP for Unity 10.3.0을 공식 Git 저장소의 `v10.3.0` 태그로 설치했다. `Packages/manifest.json`에 패키지를 추가하고 Unity가 `packages-lock.json`에 커밋 `aa5fc638d623f56178d50329d9ad8c541a57fe66`을 기록했다. MCP 서버도 10.3.0이며 HTTP Local `http://127.0.0.1:8080`으로 연결했다. 편집기 실행 시 자동 연결 옵션을 활성화했으나, 재시작 후 자동 재연결은 아직 검증하지 않았다.

설치 전에는 해당 브리지가 없어 `no_unity_session`이 반환되었다. 설치 후 활성 씬 조회와 C# 실행이 성공했고 `PlayTestScene`, dirty=false, 비Play 상태 및 MCP 어셈블리 로드를 확인했다. 플레이어의 최대 속도 5·점프력 8·코요테 시간 0.12·높이 상한 3·가속도 40·감속도 50도 Unity의 실제 SerializedObject에서 조회했다. 별도 Unity AI Assistant의 `NoSubscription` 예외는 있었으며 MCP 연결과 C# 컴파일 오류는 발견되지 않았다. 근거는 [설치 검증](Artifacts/UnityMcpInstall/verification.json), [공식 설치 안내](https://github.com/CoplayDev/unity-mcp#quickstart)다.

**이후 Unity 작업과 테스트는 MCP·콘솔·CLI로 수행하고 키보드·마우스 조작이나 입력 포커스를 가져가는 UI 자동화는 사용하지 않는다.** 설치 초기에는 창을 사용했으나 사용자의 추가 지시 이후부터는 MCP만 사용했다. 아래 2절의 이전 메카 프로젝트 통신 도구 경로는 현재 설치 경로를 뜻하지 않는다.

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

6. **불필요한 기능은 구현하지 않는다.** 사용자가 명시적으로 요청하지 않은 기능은 구현 범위에 넣지 않는다. 요청에 없는 기능이 필요하다고 판단되면, 필요한 이유를 설명하고 사용자에게 먼저 물어본다. 사용자가 승인한 뒤에만 구현한다.

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
| 특수 효과 구현 | `SpecialEffectDefinition`과 `SpecialModifierDefinition`을 상속해 실제 효과를 작성하고 `SpecialRecipe`와 `SpecialEquipment`에 연결 |
| 적 기능 확장 | EnemyBase의 FSM 상태별 행동·전환 조건, 공격 빈도와 공격 속도의 단위, 무기 연결, 사망 처리 및 적 인스턴스 구성 |
| 보스 연출 | PlayTestScene의 보스 프리팹과 경기장에서 공격 예고·애니메이션 및 확정 외형을 연결 |

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

### 2026-11-02 — .DS_Store Git 추적 제외

- 요청/목적: macOS `.DS_Store`가 반복해서 커밋 대상에 나타나는 문제 해결.
- 변경 파일: `.gitignore`, `Readme.md`; 기존 `Assets/.DS_Store`, `ProjectSettings/.DS_Store`, `ProjectSettings/Packages/.DS_Store`는 인덱스에서만 제거.
- 결정: 경로 구분자 없는 `.DS_Store` 규칙으로 전체 하위 폴더에 적용하고 로컬 파일과 과거 커밋은 보존한다.
- 검증: `git check-ignore -v`로 루트 및 기존 세 경로의 제외를 확인했고 `git ls-files`에서 추적 항목이 없음을 확인했다. `ls`로 로컬 세 파일 보존, `git diff --check`로 공백 오류 없음을 확인했다.
- 인계: 추적 제거 3개는 스테이징되어 있다. `.gitignore`와 이 문서를 함께 커밋하면 된다. 커밋·푸시는 수행하지 않았다.

### 2026-11-06 — 가속·감속 이동과 가변 높이 점프

- 요청/목적: 플랫폼 게임 방식의 가속도·최대 속도·감속도 이동, 코요테 타임·점프력·높이 조절 구현. 이번 범위는 이동·점프이며 기존 공격·피해 처리의 추가 구현은 하지 않는다.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Artifacts/PlayerMovement/`의 검증 코드·프로젝트·결과, `Readme.md`, `AGENTS.md`.
- 결정: FixedUpdate의 시간 간격으로 속도를 변경하고 기존 Rigidbody2D 중력을 사용한다. 점프 시작 속도·최대 상승 높이·조기 해제 배율을 분리했다. 코요테 시간은 이륙 시 소진한다. 기존 속도 필드는 FormerlySerializedAs로 연결하고 테스트 씬과 `.meta`는 보존한다.
- 검증: Unity 6000.6.6f1 CoreModule·Physics2DModule과 프로젝트 Input System DLL을 참조한 임시 .NET 9 컴파일 성공(경고 0·오류 0). [검증 코드](Artifacts/PlayerMovement/MovementChecks.cs)와 [Unity API 대역](Artifacts/PlayerMovement/UnityTestDoubles.cs)으로 실제 PlayerBase 콜백을 호출하여 가속·감속·최대 속도·서로 다른 물리 간격, 코요테 허용·만료, 공중 점프 거부, 빠른 탭·조기 해제, 높이 제한·재착지 등 27/27 assertion 통과. 단순 하향 중력 적분에서 유지 점프 최고점 약 2.996, 탭 점프 약 0.776 단위였다. 이는 Unity 물리 실행값이 아니다.
- 범위 검증: Attack·ApplyDamage·조각 수집·특수 사용 함수 본문 및 기존 테스트 씬·PlayerBase `.meta`가 HEAD와 동일함을 확인했다. 근거는 [scope_validation.json](Artifacts/PlayerMovement/scope_validation.json). 테스트 재실행은 .NET 9가 설치된 환경에서 `dotnet run --project Artifacts/PlayerMovement/MovementChecks.csproj`를 사용한다.
- 근거: Unity 6000.6 공식 [Mathf.MoveTowards](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mathf.MoveTowards.html), [Rigidbody2D.linearVelocity](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Rigidbody2D-linearVelocity.html). 새 Input System의 S 키 유지 상태는 설치된 DLL을 참조한 컴파일로 확인했다.
- 미실행·인계: Unity MCP 세션이 없고 앱 상태 조회도 시간 초과되어 Unity 컴파일·PlayMode·실제 키보드 및 접지·경사면·천장·이동 발판·직렬화 저장 왕복은 검증하지 못했다. 기존 PlayTestScene에서 이동 감각과 새 Inspector 수치를 조정하고 실제 Unity 동작을 확인해야 한다. 새 씬·프리팹·샘플은 생성하지 않았다.

### 2026-11-06 — Unity MCP 연결 진단

- 요청/목적: Unity 도구가 편집기 세션에 연결되지 않는 이유 확인.
- 변경 파일: `Readme.md`.
- 확인: 현재 프로젝트의 manifest·lock·Assets와 ScriptAssemblies에서 CoplayDev 브리지 패키지·코드를 발견하지 못했다. 이번 활성 씬 조회도 no_unity_session으로 실패했다. 설치된 Unity AI Assistant의 로컬 공식 문서는 별도 relay·IPC 기반 MCP 연결을 설명한다. 현재 채팅 도구가 요구하는 브리지와 연결 경로가 다르다.
- 근거: `Packages/manifest.json`, `Packages/packages-lock.json`, 설치 패키지의 `Documentation~/integration/unity-mcp-overview.md`, [CoplayDev 공식 README](https://github.com/CoplayDev/unity-mcp).
- 인계: 현재 Unity 도구에 맞는 브리지 설치와 세션 연결이 필요하다. 패키지·프로젝트 설정은 변경하지 않았고 앱의 현재 열린 씬·저장 상태는 확인하지 못했다.

### 2026-11-06 — Unity MCP 설치와 연결 완료

- 요청/목적: 앞서 진단한 누락 브리지를 설치하고 Unity 연결을 복구. 추가 지시에 따라 이후 Unity 작업·테스트의 키보드·마우스 조작 금지 규칙을 기록.
- 변경 파일: `Packages/manifest.json`, Unity 생성 `Packages/packages-lock.json`, `Artifacts/UnityMcpInstall/verification.json`, `Readme.md`, `AGENTS.md`.
- 결정: 기존 실행 중인 서버와 동일한 공식 10.3.0 태그를 고정 설치하고 localhost HTTP로 연결했다. 자동 시작 옵션도 활성화했다. 패키지·게임 코드의 다른 의존성 버전은 변경하지 않았다.
- 검증: Unity 6000.6.6f1의 Package Manager 다운로드와 컴파일 완료, MCP 창 Session Active를 확인했다. 사용자 추가 지시 후 MCP로 활성 씬 조회·C# 코드 실행·컴파일 상태와 콘솔을 확인했다. `PlayTestScene` dirty=false/rootCount=10, isPlaying=false, isCompiling=false 및 MCPForUnity.Editor 로드=true. 현재 플레이어 이동 설정값 조회도 성공했다. 상세는 설치 검증 JSON을 따른다.
- 관찰: 별도 Unity AI Assistant에서 NoSubscription 예외 1건이 조회됐으며 MCP 설치·연결을 막지 않았다. 셸의 초기 GitHub 주소 해석 실패는 승인된 네트워크 접근으로 재시도하여 해결했다. 실제 프로세스 조회에는 Unity Hub·라이선스 프로세스만 있어 대상 프로젝트 편집기를 정상 실행했다.
- 범위·인계: 플레이 테스트·게임 동작 검증이나 편집기 재시작 후 자동 재연결은 실행하지 않았다. 앞으로 테스트는 Unity MCP 또는 콘솔·CLI로 진행하고 사용자 키보드·마우스 입력을 점유하지 않는다.

### 2026-11-06 — MCP 기반 이동 PlayMode 테스트와 반응 비교

- 요청/목적: 키보드·마우스를 점유하지 않는 플레이 테스트와 사용자 지정 할로우 나이트 조작감 방향의 비교.
- 변경 파일: `Artifacts/PlayerMovementPlayTest/measure_movement.cs`, `results.json`, `Readme.md`, `AGENTS.md`. 런타임 비교만 수행하고 PlayerBase 소스와 씬·프리팹 설정은 변경하지 않았다.
- 방법: 저장된 PlayTestScene dirty=false 확인 후 MCP로 PlayMode에 진입해 일시 정지했다. 이동 입력 필드와 버튼 상태를 테스트 코드로 전달하고 실제 PlayerBase.FixedUpdate·PhysicsScene2D.Simulate를 약 0.02초 간격으로 실행했다. 현재 적은 실행 중에만 비활성화했고 코요테 확인용 발판도 메모리에만 만들었다. OS 입력 장치는 사용하지 않았다.
- 결과: 기본 설정 16/16, 빠른 반응 후보 16/16 assertion 통과. 가속·정지·반대 방향 전환, 유지/조기 해제 점프·착지·높이 상한·공중 재점프 거부·코요테 0.06초 허용과 0.18초 거부를 확인했다. 기본 가속 0.14초/정지 0.10초/반전 0.26초/점프 최고점 0.62초/체공 1.42초, 후보 0.04/0.04/0.10/0.32/0.66초였다. 두 설정의 최고 높이는 약 2.996/2.978 단위다. 전체 수치는 [results.json](Artifacts/PlayerMovementPlayTest/results.json)을 따른다.
- 시도·수정: 첫 측정 코드의 PhysicsSceneExtensions2D 네임스페이스가 잘못되어 컴파일에 실패했고 reflection으로 확인 후 수정했다. 초기 고중력 후보는 기본 마찰 때문에 속도가 4.568로 낮아져 최대 속도 95% 도달 검사에 실패했다. 플레이어에 임시 마찰 0 재질을 적용한 뒤 같은 검증을 재실행해 통과했다. 이 실패를 통과 건수에 포함하지 않았다.
- 정리·보존: 임시 발판과 물리 재질을 제거하고 PlayMode를 종료했다. PlayTestScene dirty=false/rootCount=10, isPlaying=false/isPaused=false, 원래 가속 40·감속 50·점프력 8·중력 배율 1·기본 재질을 확인했다. PlayerBase SHA-256은 이전 구현 기록과 동일하고 씬은 HEAD와 바이트 단위로 동일했다.
- 근거: Unity 공식 [PhysicsScene2D.Simulate](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PhysicsScene2D.Simulate.html) 및 설치 Unity의 reflection API 조회.
- 한계·인계: Update 입력 수신·실제 장치 지연, 사용자 주관적 조작감, 경사면·벽/천장·이동 발판은 이번에 확인하지 않았다. 원작 할로우 나이트의 수치를 재현·측정한 결과가 아니며 후보의 영구 적용과 최종 조작감 승인은 아직 진행하지 않았다.

### 2026-11-07 — 빠른 반응 설정 영구 적용과 MCP 재검증

- 요청/목적: 승인된 가속도 120·감속도 180·점프력 18.5·중력 배율 5.5·마찰 0을 PlayerBase와 PlayTestScene에 적용하고 MCP로 재검증.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Assets/Scenes/TestScene/PlayTestScene.unity`, `Artifacts/ResponsiveMovementApplied/`, 기존 대역 검증 코드 2개, `Readme.md`, `AGENTS.md`.
- 결정: PlayerBase의 직렬화 기본값을 변경하고 중력·마찰 설정을 추가했다. Awake에서 바디 중력과 전용 런타임 재질을 적용하며 OnDestroy에서 해당 재질을 해제한다. 씬에도 같은 설정과 바디 중력을 저장했다. 새 프리팹·물리 재질 에셋은 만들지 않았다.
- 검증: Unity 컴파일 오류 조회 0건. 저장된 씬을 새로 실행하여 실제 설정 120/180/18.5/5.5/0을 확인했고, 새 PlayerBase 생성 시 기본값도 동일했다. 최종 MCP PlayMode 이동·점프 검사 16/16 통과. 가속·정지 0.04초, 반전 0.10초, 높은 점프 최고점 2.978·체공 0.66초, 조기 해제 점프 최고점 1.662·체공 0.46초였으며 코요테 0.06초 허용/0.18초 거부와 공중 재점프 거부를 재확인했다. 상세는 [verification.json](Artifacts/ResponsiveMovementApplied/verification.json).
- 회귀: 이전 대역 테스트는 비교 기준 40/50/8을 명시하도록 변경하고 새 물리 재질 API 대역을 추가해 27/27 통과했다. 이 테스트는 최신 기본값의 조작감 측정이 아니라 고정 기준의 메커니즘 회귀이며 Unity PlayMode 결과와 구분한다.
- 정리·수정: 최초 OnDestroy의 바디 재질 복원 코드가 Play 종료 후 dirty 표시를 남겨 해당 변경을 제거했다. 메모리 씬의 복사본을 [보존](Artifacts/ResponsiveMovementApplied/PlayTestScene_memory_backup_20261107.unity)하고 저장 씬과 바이트 일치를 확인한 뒤 다시 열어 최종 테스트를 수행했다. 최종 EditMode dirty=false/rootCount=10, isPlaying=false/isPaused=false, 임시 발판 없음, 런타임 재질 잔존 0개를 확인했다. 저장되지 않은 씬 다시 열기가 처음 자동 승인 검토에서 거부되었으나 복사 보존·동일성 확인 후 안전하게 재실행했다.
- 범위·인계: 키보드·마우스 조작 없이 MCP만 사용했다. 실제 입력 지연·주관적 조작감·경사면·벽/천장·이동 발판 및 별도 Collider 재질 구성은 미검증이다. 공격·피해 처리는 이번 범위에서 변경하지 않았다.

### 2026-11-07 — 적과 환경 장애물의 접촉 피해

- 요청/목적: 적과 특정 환경 물체에 닿으면 플레이어 체력이 줄도록 연결하고 `Assets/Function/Environment/`에 `DamagedObstacle`을 추가한다.
- 변경 파일: `Assets/Function/Player/PlayerBase.cs`, `Assets/Function/Environment/DamagedObstacle.cs`와 Unity 생성 폴더·스크립트 `.meta`, `Artifacts/ContactDamagePlayTest/`, `Artifacts/PlayerMovement/UnityTestDoubles.cs`, `Readme.md`, `AGENTS.md`.
- 결정: 적의 기존 공격력과 장애물의 Inspector 피해량을 사용하고, 여러 접촉의 피해를 합산하지 않도록 공통 재피격 간격을 둔다. 첫 검증에서 정지한 물리 바디의 트리거 재피격이 누락되어 진입·이탈 콜백으로 접촉을 기록하고 물리 갱신에서 지속 피해를 확인하도록 수정했다. Unity 6000.6 공식 [Collision2D.collider](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Collision2D-collider.html), [일반 충돌 지속 콜백](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MonoBehaviour.OnCollisionStay2D.html), [트리거 지속 콜백](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MonoBehaviour.OnTriggerStay2D.html)을 참고했다.
- 검증: Unity 6000.6.6f1에서 C# 컴파일 오류 조회 0건. MCP로 PlayTestScene을 실행하여 임시 오브젝트의 실제 2D 물리 충돌을 검사했다. 일반·트리거 접촉, 사망 상태 적·비활성 장애물·일반 지형 무시, 동시 접촉 한 번만 적용, 재피격 간격, 접촉 종료, 체력 하한 및 조각 수집 등 [16/16 통과](Artifacts/ContactDamagePlayTest/verification.json). [재현 코드](Artifacts/ContactDamagePlayTest/verify_contact_damage.cs)와 별도로 Unity API 대역 기반 [이동 회귀 27/27](Artifacts/ContactDamagePlayTest/movement_regression.txt)을 기록했다.
- 정리·인계: 키보드·마우스 조작 없이 검증했다. PlayMode를 종료하고 임시 오브젝트가 없으며 활성 씬이 dirty=false인 것을 확인했다. 저장된 씬의 해시는 작업 전후 동일하다. 장애물 배치, 피격 연출, 넉백 및 플레이어 사망 처리는 후속 작업이다.

### 2026-11-07 — 근접 추적·돌진·공중 적 행동 구현

- 요청/목적: 접촉만 하는 근접 추적 적, 인지 후 0.5초 준비하고 5초마다 1초간 300 이동하는 돌진 적, 발사 순간의 플레이어 위치로 속도 50의 투사체 한 발을 쏘는 공중 적을 구현한다.
- 변경 파일: `Assets/Function/Enemy/`의 `MeleeEnemy`, `ChargeEnemy`, `FlyingEnemy`, 공통 `TargetingEnemy`, `EnemyProjectile` 및 Unity 생성 `.meta`, `EnemyBase.cs`, `PlayerBase.cs`, `Artifacts/EnemyBehaviors/`, 이동 검증 프로젝트, `Readme.md`, `AGENTS.md`.
- 결정: 300을 Unity 월드 단위로 적용한다. 프로젝트의 2D 이동 속도 제한 100을 유지하고 돌진 이동량과 충돌을 별도로 계산해 이동 단계를 건너뛰는 플레이어도 맞도록 했다. 돌진 준비 상태 `Windup`을 기존 FSM 끝에 추가했다. 투사체는 발사 시 방향을 고정하고 공격 속도에 따라 한 발씩 발사한다. Unity 6000.6 공식 [Rigidbody2D.Cast](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Rigidbody2D.Cast.html)와 [PhysicsScene2D.CircleCast](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PhysicsScene2D.CircleCast.html)를 참고했다.
- 검증: Unity 6000.6.6f1 컴파일 오류 조회 0건. MCP PlayMode [행동 검사 22/22](Artifacts/EnemyBehaviors/verification.json) 통과. 돌진 준비 0.5초·이동 1초·거리 300.0002·재돌진 간격 5초, 근접 추적·접촉 피해, 직선 투사체 속도 50.0045·회피·벽 차단을 확인했다. 기존 접촉 피해 16/16과 .NET Unity API 대역 이동 검사 27/27도 재실행했다. [재현 코드](Artifacts/EnemyBehaviors/verify_enemy_behaviors.cs)의 초기 실행은 에디터 첫 갱신 전에 가까운 대상이 투사체에 맞아 발사 방향 샘플을 놓쳤다. 관측용 대상을 멀리 배치해 다시 실행했으며 초기 실행은 통과 건수에 포함하지 않았다.
- 정리·인계: 키보드·마우스 조작 없이 실행했다. 임시 적·플레이어·투사체 정리 후 PlayMode를 종료했고 저장 씬 해시는 작업 전후 동일했다. 별도 씬·프리팹·시각 에셋은 만들지 않았다. 실제 배치, 투사체 외형, 경사면·발판 끝에서의 돌진 연출은 후속 작업이다.

### 2026-11-07 — 진행 저장·다시하기 조사와 게임 옵션 코드

- 현재 체력·조각·특수 상태, 플레이어 사망 처리와 씬 등록 범위를 확인하고 체크포인트 자동 저장, 이어하기, 사망 후 다시하기의 초안을 작성했다. 진행 저장용 JSON과 씬 복원은 체크포인트 및 리스폰 규칙이 확정되면 구현한다. [Unity 사용자 데이터 경로](https://docs.unity3d.com/kr/current/ScriptReference/Application-persistentDataPath.html)
- `Assets/Function/Options/GameOptions.cs`와 Unity 생성 `.meta`를 추가했다. 화면·품질·프레임·볼륨 설정을 적용하고 버전이 포함된 PlayerPrefs 키에 저장한다. 첫 씬에서 자동으로 불러오고, 오디오 채널은 향후 AudioMixer 그룹에 연결한다. [Unity PlayerPrefs](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/playerprefs), [해상도 설정](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/screen/setresolution), [AudioMixer 파라미터 적용](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/audio/audiomixer/setfloat)
- Unity 6000.6.6f1에서 컴파일 오류 0건을 확인했다. 키보드·마우스 조작 없이 MCP PlayMode에서 옵션 적용·저장·복원·손상값 처리 23건과 PlayMode 재진입 시 자동 복원 1건을 검증했다. 검사에 사용한 설정 키를 제거하고 PlayMode를 종료했다. 씬과 프로젝트 설정 파일의 해시는 전후 일치했다. 독립 실행 빌드의 화면 전환과 실제 믹서를 통한 음원 재생은 후속 검증 대상이다. [검증 코드](Artifacts/GameOptions/verify_options.cs), [검증 결과](Artifacts/GameOptions/verification.json)

### 2026-11-07 — 단일 체크포인트 자동 저장과 사망 후 복원

- 사용자가 확정한 자동 저장 슬롯 하나를 구현했다. 이어하기와 사망 후 다시하기는 마지막 정상 저장에서 씬과 체크포인트를 찾아 최대 체력과 저장 당시 조각·활성 조합·특수 횟수를 복원한다. 씬을 다시 불러 일반 적을 리스폰한다.
- `Assets/Function/Progress/`의 체크포인트·슬롯·복원 코드, `PlayerBase.cs`의 사망 전환, `PlayerCheckpointState.cs`, Unity 생성 `.meta`, 기존 이동 테스트 대역을 변경했다. `Assets/Scenes/TestScene/PlayTestScene.unity`에 체크포인트 두 개를 배치하고 `ProjectSettings/EditorBuildSettings.asset`에 등록했다. 안정적인 씬 경로를 저장하고 로드할 때 현재 빌드 인덱스로 변환한다. [Unity 씬 인덱스 조회](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneUtility.GetBuildIndexByScenePath.html), [Unity 비동기 씬 로드](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/scenemanagement/scenemanager/loadsceneasync)
- 저장은 버전·조각 데이터 유효성 검사와 이전 정상본 보존을 사용한다. [Unity 영구 데이터 경로](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/application/persistentdatapath), [.NET 파일 교체 및 백업](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace)
- Unity MCP PlayMode에서 별도 임시 슬롯으로 실제 물리 자동 저장, 슬롯 갱신, 다른 씬에서 이어하기, 사망 후 다시하기, 일반 적 리스폰, 저장 손상 시 백업 복원 등 40/40을 통과했다. 콘솔 오류 0건, 기존 이동 대역 검사 27/27이다. 임시 저장 폴더를 정리하고 PlayMode를 종료했다. 별도 독립 실행 빌드와 UI는 검증하지 않았다. [검증 코드](Artifacts/CheckpointProgress/verify_checkpoint_progress.cs), [검증 결과](Artifacts/CheckpointProgress/verification.json)

### 2026-11-07 — 새 게임·이어하기·옵션 타이틀

- 빌드의 첫 씬인 `SampleScene`에 타이틀 UI를 연결했다. `TitleMenu.cs`, `Assets/UI/Title/`의 UXML·USS·테마·패널 설정과 Unity 생성 `.meta`를 추가했다. 이어하기는 유효한 슬롯이 있을 때 활성화하고 옵션 화면은 기존 `GameOptions`를 호출한다. [Unity 패널 설정](https://docs.unity3d.com/cn/current/ScriptReference/UIElements.PanelSettings.html), [UI Toolkit 입력](https://docs.unity3d.com/cn/6000.0/ScriptReference/UIElements.UIToolkitInputConfiguration.SetRuntimeInputBackend.html)
- `CheckpointProgress.cs`에 새 게임 진입점을 추가했다. 시작 위치에서 새 진행을 열고, 기존 슬롯은 첫 체크포인트를 저장할 때 갱신한다. 그 전의 사망은 기존 저장을 불러오지 않는다.
- 열린 편집기의 MCP가 가져오기 도중 응답하지 않아 별도 복사본에서 Unity 6000.6.6f1 명령줄 PlayMode 검증을 수행했다. 실제 UI Toolkit 버튼 이벤트로 빈 슬롯 비활성화, 체크포인트 복원, 새 게임의 슬롯 갱신, 옵션 적용·저장·취소 등 22/22를 확인했다. 키보드·마우스 입력과 기존 진행 슬롯은 사용하지 않았다. 독립 실행 빌드와 실제 BGM·효과음 믹서 재생은 별도 검증 대상이다. [검증 코드](Artifacts/TitleMenu/verify_title_menu.cs), [검증 결과](Artifacts/TitleMenu/verification.json)

### 2026-11-08 — 일회성 아이템 획득 저장

- `OneTimePickup.cs`, `CheckpointProgress.cs`, `CheckpointSaveStore.cs`를 연결했다. 기존 세이브와 호환되는 선택 필드 `CollectedItemIds`에 체크포인트 당시의 획득 목록을 기록한다. 이어하기와 사망 후 다시하기는 같은 경로에서 목록을 복원하고 씬의 획득 완료 아이템을 숨긴다. [Unity JSON 역직렬화](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/jsonutility/fromjson), [Unity 씬 로드 이벤트](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/scenemanagement/scenemanager/sceneloaded)
- Unity 6000.6.6f1 MCP PlayMode에서 임시 슬롯과 실제 2D 트리거로 기존 세이브 호환, 체크포인트 저장, 저장 후 획득 롤백, 이어하기·사망 후 복원, 보상 중복 방지, 새 게임 등 24/24를 확인했다. 콘솔 오류는 0건이며 임시 슬롯을 정리했다. 실제 아이템은 콘텐츠 배치 시 컴포넌트와 ID를 연결한다. [검증 코드](Artifacts/OneTimePickup/verify_one_time_pickup.cs), [검증 결과](Artifacts/OneTimePickup/verification.json)

### 2026-11-08 — 체크포인트 자동 저장 순서 보강

- 체크포인트와 일회성 아이템에 동시에 닿았을 때 저장이 먼저 실행되어 실제로 획득한 ID가 파일에서 빠지는 문제를 재현했다. `Checkpoint.cs`에서 진입 시 저장을 예약하고 같은 프레임의 충돌·획득 처리가 끝난 뒤 `LateUpdate`에서 저장한다. [Unity 업데이트 순서](https://docs.unity.com/en-us/engine/6000.6/manual/scripting/managing-update-order)
- Unity MCP PlayMode에서 임시 슬롯으로 동시 접촉 4/4와 기존 체크포인트·이어하기·사망 후 복원·백업 회귀 40/40을 통과했다. 콘솔 오류는 0건이며 검증 후 임시 슬롯을 정리했다. [동시 접촉 검증 코드](Artifacts/CheckpointAutosave/verify_overlap.cs), [검증 결과](Artifacts/CheckpointAutosave/verification.json)

### 2026-11-08 — 중요 아이템·3분 간격·정상 종료·지역 이동 자동 저장

- 체크포인트 외 자동 저장 시점을 사용자 지정대로 추가했다. `CheckpointProgress.cs`와 `CheckpointSaveStore.cs`는 기존 단일 슬롯을 사용한다. `ProgressAutosave.cs`에서 누적 플레이 시간과 종료 이벤트를 받고 `OneTimePickup.cs`에서 중요 아이템을 지정한다. `RegionTransition.cs`로 도착 지점을 연결하며 `PlayerBase.cs`는 지역 이동에서 현재 체력을 이어받는다. [Unity 종료 이벤트](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/onapplicationquit), [Unity 비보정 프레임 시간](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/time/unscaleddeltatime)
- Unity MCP PlayMode에서 179초/180초를 타이머에 전달해 경계를 확인했고, 실제 아이템·지역 트리거와 편집기 플레이 종료 이벤트를 임시 슬롯으로 검사했다. 새 동작 19/19와 체크포인트·사망 후 복원·백업 회귀 40/40을 통과했다. 독립 실행 빌드의 종료는 별도로 검증하지 않았다. [검증 코드](Artifacts/ProgressAutosave/verify_autosave_triggers.cs), [검증 결과](Artifacts/ProgressAutosave/verification.json)

### 2026-11-08 — 플레이 테스트의 점프 경로 표시

- `PlayerBase.cs`에서 유효한 점프의 시작을 알리고 실제 이동과 같은 수평 가속·감속 및 점프 높이 제한 계산을 예측에 제공한다. `PlayTestDisplay.cs`와 `JumpPathDisplay.cs`는 게임 화면에서 이륙 이후의 실선과 현재 입력에 따른 예상 점을 표시한다. 몸체 충돌체를 예상 경로에서 검사해 첫 장애물에서 예측을 끝낸다. [Unity Rigidbody2D.Cast](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/cast)
- Unity MCP PlayMode에서 실제 2D 물리를 사용해 경로 표시·공중 재입력·착지·조기 해제·반전·장애물 10/10을 확인했다. 유지 점프의 실제·예상 최고 높이는 모두 2.978 유닛이며 착지 X 오차는 약 0.101 유닛이었다. 기존 이동 대역 검사 27/27도 통과했다. 씬 파일 해시는 일치했고 검증은 키보드·마우스 입력 없이 수행했다. [검증 코드](Artifacts/JumpTrajectory/verify_jump_path.cs), [검증 결과](Artifacts/JumpTrajectory/verification.json)

### 2026-11-08 — 적 접촉 피해 확인과 체력 표시

- 플레이어의 기존 접촉 피해를 실제로 확인할 수 있도록 `PlayTestDisplay.cs`에 현재·최대 체력을 표시했다. 편집기에서 테스트 씬을 바로 실행할 때 점프 표시 참조가 비어 HUD가 중단되는 경우도 재현하여, 표시를 그리기 전에 안전하게 초기화한다.
- Unity MCP PlayMode에서 배치된 A/B/C 적의 최초 피해, 0.75초 재피격, 접촉 해제를 검증해 6/6을 통과했다. 게임 화면에서 HP 60/100을 확인했다. 독립 실행 빌드는 검증하지 않았으며 편집기의 Unity AI 구독 진단 한 건이 별도로 남았다. [검증 코드](Artifacts/PlayTestEnemyContact/verify_scene_contacts.cs), [검증 결과](Artifacts/PlayTestEnemyContact/verification.json)

### 2026-11-08 — 특수 효과 선택과 장비 보정

- `Assets/Function/Special/`에 실행·보정 인터페이스와 레시피·장비 설정을 추가했다. `PlayerBase.cs`는 활성 조합의 레시피와 현재 장비로 교체 효과를 선택하고 보정을 적용한다. 공유 설정과 이번 시전 인스턴스를 분리하고 실행 성공 후에만 횟수를 차감한다. [Unity ScriptableObject](https://docs.unity.com/en-us/engine/6000.6/manual/scripting/get-started/fundamental-unity-types/class-scriptable-object)
- Unity 6000.6.6f1 MCP PlayMode에서 임시 저장 슬롯과 검증용 효과로 조합 선택, 발사 수, 효과 교체 우선순위, 실패 보존, 성공 시 1회 차감 등을 23/23 확인했다. 컴파일 오류는 0건이며 기존 Unity API 사용 중단 경고 4건이 표시됐다. 이동 대역 검사에 새 소스 참조를 추가해 27/27을 확인했다. 씬 파일 해시는 같았으며 키보드·마우스 입력을 사용하지 않았다. [검증 코드](Artifacts/SpecialAbility/verify_special_ability.cs), [검증 결과](Artifacts/SpecialAbility/verification.json)
- 인계: 실제 조합 효과와 장비 데이터를 만든 뒤 Inspector에서 연결한다. 현재 PlayTestScene에는 특수 효과 레시피가 없으므로 D 입력 시 횟수를 보존한다.

### 2026-11-08 — 적 피격 WhiteFlash

- `EnemyBase.cs`, `PlayerBase.cs`에서 플레이어 공격 피해를 구분하고 `EnemyWhiteFlash.cs`와 URP 2D [셰이더](Assets/Function/Enemy/Resources/EnemyWhiteFlash.shader)를 추가했다. 요청한 밝기를 흰색에서 채널마다 25를 뺀 sRGB `(230, 230, 230)`으로 해석했다. `PlayTestDisplay.cs`는 치명타에서 충돌을 끄고 플래시 종료 후 적을 숨긴다. [Unity SpriteRenderer 색상](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/spriterenderer/color), [Unity 셰이더 블렌딩](https://docs.unity3d.com/6000.6/Documentation/Manual/SL-Blend.html)
- Unity MCP PlayMode에서 실제 근접 공격 판정과 URP 렌더 픽셀, 재피격, 비활성화, 치명타 및 원래 모습 복원을 13/13 확인했다. 불투명 픽셀은 RGB `(230, 230, 230)`으로 출력됐고 별도 피격의 복원 관측값은 0.0501초였다. 프레임 수는 실행 환경의 렌더 속도에 따라 달라진다. 컴파일·셰이더 오류 0건, 이동 대역 검사 27/27, 씬 파일 해시 일치를 확인했다. 독립 실행 빌드는 검증하지 않았다. [검증 코드](Artifacts/EnemyWhiteFlash/verify_enemy_white_flash.cs), [검증 결과](Artifacts/EnemyWhiteFlash/verification.json)

### 2026-11-08 — 게임 분위기와 그래픽 방향

- 요청/목적: 아트 기본 자료로 게임 분위기의 참고작과 공통 그래픽 방향을 기록한다.
- 변경 파일: `Readme.md`.
- 결정: 기존 할로우 나이트 중심의 아트 참고에 데드 셀과 Hellfurnace를 분위기 참고작으로 추가하고, 게임 전반에 윤곽선이 있는 그래픽을 적용한다. 스프라이트 기반 2D 방향은 유지한다.
- 검증: 문서 도입부와 이력을 대조했다. 문서만 변경하여 Unity·Blender 조작과 코드 테스트는 수행하지 않았다.
- 인계: 플레이어 대기 스프라이트의 구체적인 외형과 참고 자료가 필요하다.

### 2026-11-08 — 플레이어 스탠딩 시안

- 요청/목적: Notion Player character 참고 이미지를 보고 현대 어반 인간형 주인공의 스탠딩 스프라이트 시안을 제작한다. 오른팔과 왼다리는 기계이며 오른팔로 공격한다.
- 변경 파일: [스탠딩 시안 PNG](Artifacts/PlayerStandingConcept/PlayerStandingConcept_v1.png), `Readme.md`.
- 결정: Notion의 세 이미지에서 확인한 액션 게임 인간형 비율과 읽기 쉬운 실루엣을 참고해, 윤곽선이 있는 현대 스트리트웨어와 기계 팔다리를 제안했다. 이미지 생성 도구에는 참고 이미지를 직접 전달하지 못해 화면에서 확인한 특성을 텍스트로 설명했다. 외형·의상·색상은 시안이며 사용자 승인 전이다.
- 생성 지시: 인간형 비율의 전신 대기 자세, 현대 스트리트웨어, 캐릭터 기준 오른팔과 왼다리의 기계 의수·의족, 윤곽선과 단순한 음영, 투명 배경, 한 인물.
- 검증: 생성 결과를 시각적으로 확인해 기계 팔다리의 좌우와 전신 구도를 살폈다. `sips`에서 PNG 1199×1312 및 알파 채널 존재를 확인했다. Unity 임포트·실제 게임 크기 가독성·애니메이션은 검증하지 않았다.
- 인계: 사용자 검토에 따라 디자인을 조정하고, 이후 애니메이션 프레임·Unity 적용을 별도 진행한다. 이 이미지는 단일 정지 프레임이다.

### 2026-11-08 — 플레이어 스탠딩 시안 5등신화

- 요청/목적: 인게임 스프라이트를 위해 기존 주인공 시안을 약 5등신으로 데포르메한다.
- 변경 파일: [스탠딩 시안 v2](Artifacts/PlayerStandingConcept/PlayerStandingConcept_v2.png), `Readme.md`. 기존 v1은 보존한다.
- 결정: 기본 이미지 생성 도구로 v1을 편집해 인체 비율을 압축하고 외곽선과 세부 형태를 단순화했다. 어반 의상, 오른팔과 왼다리의 기계 구성은 유지했다. 생성 지시: 기존 인물을 약 5등신 인간형 대기 자세로 바꾸고 복장·색상·기계 팔다리 위치를 보존하며 투명 배경과 선명한 윤곽선으로 표현.
- 검증: 이미지에서 기계 팔다리 위치와 전신 구도, 압축된 비율을 시각 확인했다. `sips`에서 PNG 1206×1305와 알파 채널 존재를 확인했다. 정확한 등신 수의 계측, Unity 임포트·실제 게임 크기 가독성·애니메이션은 확인하지 않았다.
- 인계: 시각 디자인은 사용자 검토 전이다. 단일 정지 프레임이며 대기 애니메이션과 Unity 적용은 후속 작업이다.

### 2026-11-09 — 보스 패턴과 조각 적 소환

- 보스의 시작·최대 체력을 2250으로 설정했다. `BossEnemy.cs`는 벽까지 돌진하고 전방 트리거로 내려찍기와 광역 공격을 판정한다. 공격 두 번을 완료하면 조각 적 세 마리를 소환하며 패턴 종료 뒤 일반 공격은 3초, 소환은 5초를 기다린다. `FragmentEnemy.cs`는 소환 적을 바닥에 고정하고 처치 시 충돌을 끈다. 모든 적의 접촉 피해는 각자의 `Attack Power`를 사용한다. [Unity Rigidbody2D.Cast](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/cast), [Unity PhysicsScene2D.Raycast](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/physicsscene2d/raycast)
- Unity 6000.6.6f1 CLI의 임시 프로젝트 PlayMode에서 고정 갱신과 실제 2D 물리를 수동 진행해 벽 정지, 두 전방 공격, 소환 순서·위치·고정·접촉 피해·드롭, 처치 후 중단을 19/19 확인했다. 3초·5초는 예약 시각과 기한 전 정지를 확인했고 패턴 사이의 대기는 검증 코드에서 앞당겼다. 이동 대역 검사도 27/27을 통과했다. [검증 코드](Artifacts/BossPatterns/verify_boss_patterns.cs), [검증 결과](Artifacts/BossPatterns/verification.json)
- 인계는 실제 경기장에 보스를 배치하고 원하는 조각을 드롭하는 프리팹을 연결하는 것이다. 기본 돌진 속도와 전방 공격 범위는 보스 Inspector에서 조정한다.

### 2026-11-09 — PlayTestScene 보스 구역과 프리팹

- `Assets/Prefabs/PlayTest/`에 보스·고정 조각 적·B 조각 프리팹과 공통 테스트 스프라이트를 저장했다. 기존 PlayTestScene 오른쪽에 연결된 바닥과 두 벽을 배치했다. 플레이어 점프로 입구를 넘을 수 있으며, 벽은 보스 돌진을 멈춘다. 입구 체크포인트와 두 공격 범위 표식을 추가하고 카메라는 수평으로 플레이어를 따라가도록 설정했다. [Unity 프리팹 인스턴스](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/prefabutility/instantiateprefab), [Unity Position Constraint](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/animations/positionconstraint)
- Unity CLI에서 저장된 씬을 다시 열어 프리팹 참조, 카메라 대상, 체크포인트 저장, 실제 물리 점프, 양방향 벽 정지, 네 패턴, 접촉 피해와 B 조각 수집을 12/12 확인했다. 물리를 수동 진행하고 패턴 사이의 대기를 앞당겼다. 실제 화면 렌더링은 별도로 확인하지 않았다. [생성 코드](Artifacts/BossArena/prepare_boss_arena.cs), [검증 코드](Artifacts/BossArena/verify_boss_arena.cs), [검증 결과](Artifacts/BossArena/verification.json)
- 인계는 PlayTestScene의 오른쪽 보스 구역에서 플레이하며 패턴을 조정하는 것이다. 보스와 소환 적의 외형은 개발용 도형이다.

### 2026-11-09 — 보스전 자연 진행과 사망 후 복귀 검증

- PlayTestScene을 복사해 Unity 6000.6.6f1 macOS 검증 빌드로 실행했다. 관찰 코드는 플레이어 위치만 옮기고 실제 게임 루프가 물리와 패턴 타이머를 진행하게 했다. 키보드·마우스 입력을 사용하지 않았으며 체크포인트는 임시 슬롯에 저장했다.
- 두 번의 소환에서 각각 세 마리가 추가됐다. 패턴 종료부터 다음 시작까지 게임 시간으로 돌진 → 내려찍기 3.000초, 내려찍기 → 소환 3.000초, 소환 → 광역 5.000초, 광역 → 돌진 3.000초, 돌진 → 소환 3.000초를 관측했다. 실시간으로는 3초 구간이 2.998~3.000초, 5초 구간이 4.997초였다.
- 소환 적과 반복 접촉해 체력이 0이 되자 실제 씬 재로드가 실행됐다. 새 플레이어는 입구 체크포인트 `playtest-boss-entrance`의 좌표 `(6.500, 0.410, 0.000)`에서 체력 100으로 복원됐고, 보스는 체력 2250과 소환 적 0마리 상태로 초기화됐다. 총 23/23을 통과했다. [빌드 코드](Artifacts/BossArenaLive/build_boss_arena_live.cs), [관찰 코드](Artifacts/BossArenaLive/observe_boss_arena.cs), [검증 결과](Artifacts/BossArenaLive/verification.json)

### 2026-11-09 — 비행 보스와 전용 테스트 아레나

- `FlyingBossEnemy.cs`, `BossChargeMinion.cs`, `EnemyDash.cs`로 체력 3400의 부유 보스와 다섯 패턴을 구현했다. `FragmentEnemy.cs`는 비행 보스가 소환할 때 바닥 위에서 부유하고, `EnemyProjectile.cs`는 선택적 낙하 가속도로 포물선을 지원한다. 기본 패턴 간격은 3초로 정했다. [Unity Rigidbody2D.Cast](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/cast), [Unity PhysicsScene2D.Raycast](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/physicsscene2d/raycast)
- `FlyingBossTestScene`을 빌드 목록에 추가하고 `Assets/Prefabs/PlayTest/`의 비행 보스, 돌진 소환 적, 투사체 프리팹과 기존 B 조각 적을 연결했다. 기존 PlayTestScene의 파일 해시는 일치했다. [아레나 생성 코드](Artifacts/FlyingBossArena/prepare_flying_boss_arena.cs)
- Unity 6000.6.6f1 macOS 임시 빌드를 키보드·마우스 조작 없이 두 번 실행했다. 벽 돌진 시나리오 21/21과 바닥 돌진 시나리오 22/22가 통과했다. 실제 게임 루프에서 조각 적 4·5마리 소환, 돌진 적의 2.000초 간격과 방향 고정, X자·십자 1.500초 간격, 포물선 3발과 방사형 4+4발, 투사체의 지형 충돌 제거를 확인했다. 관찰 코드는 플레이어를 검증 위치로 옮기고 발사 후 별도 지형 충돌용 투사체를 만들었다. [관찰 코드](Artifacts/FlyingBossArena/verify_flying_boss_live.cs), [검증 결과](Artifacts/FlyingBossArena/verification.json)
- 인계는 전용 아레나에서 패턴 조작감과 난이도를 직접 조정하는 것이다. 외형은 테스트 도형이며 검증은 그래픽 없이 실행했다.

### 2026-11-10 — 플레이어 점프 설정과 중력 배율 9.8 적용

- 소스 초기값을 바꿔도 두 씬에 저장된 기존 Inspector 값이 실행에 사용되고 있었다. `PlayerBase.cs`와 두 테스트 씬의 중력 배율을 9.8로 맞추고 사용자가 수정한 점프 시작 속도 50과 높이 상한 30을 씬에 반영했다. 해제 배율 1.5는 기존 `Clamp01`에서 1이 되므로 씬에는 1을 저장했다. 최대 이동 속도는 5다. [Unity 직렬화 필드](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/serializefield), [Unity 중력 배율](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/gravityscale)
- Unity CLI에서 두 씬의 Inspector 값을 읽고 PlayTestScene의 실제 2D 물리를 진행해 16/16을 확인했다. 점프 시작 속도 50, 첫 물리 스텝 후 속도 48.07724, 조기 해제 순간 속도 유지와 실제 상승 12.502유닛을 기록했다. 기존 이동 대역 검사의 0.5 해제 배율은 검증용 입력으로 명시해 27/27을 통과했다. 다음 조정은 씬의 Player Inspector에서 비교한다. [검증 코드](Artifacts/PlayerGravityTuning/verify_player_gravity_tuning.cs), [검증 결과](Artifacts/PlayerGravityTuning/verification.json)

### 2026-11-10 — 플레이어 점프 설정 재확인

- 현재 소스 초기값은 점프 시작 속도 18.5, 높이 상한 3, 해제 배율 1, 중력 배율 9.8이다. 기존 두 씬은 속도 50과 높이 상한 30을 저장하며, 해제 배율과 중력 배율은 소스와 같다. 이번 요청은 재확인이므로 해당 차이를 문서에 기록했다.
- 현재 소스와 씬으로 Unity CLI 검증을 다시 실행해 16/16을 통과했다. 두 씬의 저장값을 읽었고 PlayTestScene에서 점프 시작 속도 50, 첫 물리 스텝 뒤 48.07724, 실제 상승 12.502유닛을 다시 측정했다. [재검증 결과](Artifacts/PlayerGravityTuning/recheck_2026-11-10.json)

### 2026-11-10 — 점프 버튼 해제 시 수직 속도 반전

- `PlayerBase.cs`의 해제 배율 범위를 -1~1로 넓히고 소스 기본값 및 PlayTestScene·FlyingBossTestScene에 -1을 저장했다. 상승 중 S를 놓으면 수직 속도에 한 번 곱해 같은 크기로 하강한다. 점프 시작 속도는 소스 기본값 12.5와 두 씬의 저장값 50으로 구분한다.
- 키보드·마우스 없이 Unity CLI에서 두 씬을 각각 실행하고 실제 2D 물리를 진행했다. 두 씬 모두 해제 직전 +44.23172에서 직후 -44.23172로 반전하고 다음 물리 스텝에 하강했다. 각 씬 5/5와 이동 대역 검사 29/29를 통과했다. [검증 코드](Artifacts/JumpReleaseDive/verify_jump_release_dive.cs), [검증 결과](Artifacts/JumpReleaseDive/verification.json)

### 2026-11-10 — 두 테스트 씬의 점프력 절반 조정

- PlayTestScene과 FlyingBossTestScene의 `Jump Force` 저장값을 50에서 25로 낮췄다. 두 씬의 중력 배율은 9.8이고 해제 배율은 -1이다. 새 컴포넌트의 소스 초기값은 12.5다.
- 키보드·마우스 없이 두 씬을 Unity CLI PlayMode에서 검증해 총 8/8을 통과했다. S를 유지한 상태로 3.001유닛 상승했고, 비행 보스 씬에서 플레이어 충돌체와 천장 사이 최소 여유는 3.188유닛이었다. 지상 보스 구역의 입구 벽도 실제 점프로 통과했다. [검증 코드](Artifacts/PlayerJumpHalf/verify_player_jump_half.cs), [검증 결과](Artifacts/PlayerJumpHalf/verification.json)

### 2026-11-10 — 유지 점프와 짧은 점프 높이 조정

- 두 테스트 씬의 `Jump Force`는 25에서 27.82, `Jump Release Multiplier`는 -1에서 0.704로 조정했다. 상승 중 해제 직후에는 위쪽 속도가 남아 중력으로 점차 상승을 끝낸다. 짧은 입력의 비교 기준은 물리 3스텝인 0.06초다.
- 두 씬을 Unity CLI PlayMode에서 키보드·마우스 없이 검증해 12/12를 통과했다. 유지 점프는 3.001→3.752유닛으로 1.250배, 0.06초 점프는 1.269→2.538유닛으로 2.000배였다. 즉시 탭은 1.802유닛 상승했다. 지상 보스 입구 벽을 통과했고 비행 보스 씬의 천장 여유는 2.436유닛이었다. 이후 조작감 비교는 같은 입력 길이와 씬을 기준으로 한다. [검증 코드](Artifacts/PlayerJumpRetune/verify_player_jump_retune.cs), [검증 결과](Artifacts/PlayerJumpRetune/verification.json)

### 2026-11-10 — 이동·점프 공통 기준 확정

- 두 테스트 씬의 현재 값을 `PlayerBase.cs` 초기값에 맞췄다. 차이가 있던 점프 시작 속도, 높이 상한, 중력 배율, 해제 배율에 27.82, 30, 9.8, 0.704를 적용했다. 이동·점프 공통 기준의 변경은 사용자가 명시적으로 요청할 때만 진행하도록 `AGENTS.md`에 기록했다.
- Unity CLI의 편집 모드에서 새 PlayerBase와 두 씬의 저장된 컴포넌트를 확인해 29/29를 통과했다. 다음 조정 전에도 같은 기준 검사로 세 곳의 일치를 확인할 수 있다. [검증 코드](Artifacts/PlayerMovementBaseline/verify_player_movement_baseline.cs), [검증 결과](Artifacts/PlayerMovementBaseline/verification.json)

### 2026-11-10 — 점프 해제·최고점의 하강 충격량

- `PlayerBase.cs`와 두 씬에 `Jump Descent Speed Boost` 1.5유닛/초를 추가했다. 상승 중 S를 놓거나 최고점에서 상승이 끝나면 `AddForce`의 충격량 모드로 아래 방향 힘을 점프당 한 번 적용한다. 조기 해제 시 기존 배율 적용 뒤에 힘을 준다. `JumpPathDisplay.cs`는 최고점의 하강 충격량도 예측한다. [Unity Rigidbody2D.AddForce](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/addforce)
- Unity CLI PlayMode에서 키보드·마우스 없이 두 씬을 각각 검사해 16/16을 통과했다. 질량 2.5인 검증 바디에서도 해제와 최고점의 수직 속도가 추가로 1.5유닛/초 감소했고, 중복 적용 방지·중력 설정·최고점 예측 일치를 확인했다. 이동 대역 검사도 33/33을 통과했다. [검증 코드](Artifacts/JumpDescentImpulse/verify_jump_descent_impulse.cs), [검증 결과](Artifacts/JumpDescentImpulse/verification.json)

### 2026-11-10 — PlayerBase 설정의 씬 직렬화 제거

- 지정된 능력치·이동·점프·공격·특수 목록 20개에서 `SerializeField`와 관련 Inspector 속성을 제거했다. 두 씬을 불러오면 코드 초기값을 적용하므로 기존 저장값 27.82와 초당 공격 2회 대신 점프력 35와 초당 공격 1회를 사용한다. 특수 레시피와 장비 목록은 코드의 빈 배열로 시작한다. [Unity 직렬화 필드](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/serializefield)
- Unity CLI에서 두 씬의 직렬화 속성 수 0, 코드 초기값, PlayTestScene의 실제 시작 체력과 점프 시작 속도를 확인해 9/9를 통과했다. [검증 코드](Artifacts/PlayerCodeSettings/verify_player_code_settings.cs), [검증 결과](Artifacts/PlayerCodeSettings/verification.json)

### 2026-11-10 — PlayerBase 직렬화 제거 작업 복구

- 마지막 작업의 검증 복사본을 기준으로 `PlayerBase.cs`에서 빠진 점프 해제 배율 0.704와 적용 로직, 하강 충격량 계산을 복구했다. 설정 필드는 비직렬화 상태이며 점프력 35와 초당 공격 1회를 사용한다.
- 복구 파일을 검증 복사본과 대조했다. 이번 복구의 테스트는 사용자 요청으로 생략했다.

### 2026-11-10 — 점프 하강 충격량을 중력 배율에 연동

- `PlayerBase.cs`의 설정 필드가 비직렬화 상태임을 확인했다. 고정 하강 속도 보정을 `Gravity Scale × 0.75`의 하향 충격량으로 교체했다. 상승 중 입력 해제와 최고점 도달 중 먼저 일어난 시점에 점프당 한 번 적용하며 궤적 예측도 같은 충격량을 사용한다.
- `Artifacts/PlayerMovement/MovementChecks.cs`의 기존 하강 검증 입력과 기대값을 새 공식에 맞췄다. 소스와 문서를 대조했고 플레이 테스트 및 검증 코드는 실행하지 않았다.

### 2026-11-10 — 점프 해제와 최고점의 하강 강도 조정

- `PlayerBase.cs`에서 S를 놓을 때 Y 속도에 곱하던 해제 배율을 제거했다. `jumpDescentStrength`를 비직렬화 필드로 분리해 기본값 0.5를 두고, S 해제와 최고점에서 `Gravity Scale × jumpDescentStrength`를 `AddForce`의 충격량 모드로 점프당 한 번 적용한다. 예상 점프 경로도 같은 공식을 사용한다.
- [이동 검증 코드](Artifacts/PlayerMovement/MovementChecks.cs)를 갱신해 .NET 대역 검사 29/29를 통과했다. 중력 배율 9.8, 질량 2, 해제 전 Y 속도 6에서 적용 후 3.55를 확인했다. Unity 플레이 테스트는 생략했다.

### 2026-11-11 — 현재 Y 속도를 기준으로 점프 해제 복구

- `PlayerBase.cs`에서 상승 중 S를 놓을 때 현재 Y 속도에 비직렬화 배율 `jumpReleaseMultiplier = -0.75f`를 점프당 한 번 곱한다. 중력 배율 기반 충격량을 제거했고 S를 유지하면 최고점부터 중력으로 하강한다. `JumpPathDisplay.cs`는 실제 현재 속도에서 궤적을 계산한다.
- [이동 검증 코드](Artifacts/PlayerMovement/MovementChecks.cs)의 .NET 대역 검사 28/28을 통과했다. 해제 시 6에서 -4.5로 바뀌는 경우와 중복 적용 방지, 최고점과 하강 중 속도 보존을 확인했다. Unity 플레이 테스트는 생략했다.

### 2026-11-11 — 점프 하강 중력 1.75배 적용

- `PlayerBase.cs`에서 최고점이나 S 해제로 하강이 시작되면 직전 중력 배율을 1.75배로 적용하고 착지 시 복원한다. 상승 중 S 해제의 Y 속도 배율은 -0.75다. `JumpPathDisplay.cs`는 궤적 계산 중 하강 전환 시 같은 중력 배율을 적용한다.
- [이동 검증 코드](Artifacts/PlayerMovement/MovementChecks.cs)의 .NET 대역 검사 37/37을 통과했다. 상승 9.8, 최고점·조기 해제 후 하강 17.15, 착지와 비활성화 시 9.8 복원을 확인했다. Unity 플레이 테스트는 생략했다.

### 2026-11-16 — 기능 구현 범위 명시

- 요청/목적: 요청에 없는 기능을 임의로 구현하지 않도록 개발 규칙을 명시한다.
- 변경 파일: `Readme.md`. 2절에 추가 기능이 필요하다고 판단되면 이유를 설명하고 사용자 승인 후 구현한다는 원칙을 추가했다.
- 검증·인계: 기존 산출물 범위 규칙과 일치함을 확인했다. 문서만 변경했으며 이후 기능 구현에 적용한다.

### 2026-11-16 — 근접 공격 명중 연출

- `PlayerBase.cs`에서 공격자를 `EnemyBase.ApplyPlayerDamage`에 전달하도록 연결했다. `EnemyWhiteFlash.cs`는 최소 3프레임·0.01초를 유지하도록 조정했다. `GameHitStop.cs`는 공격자와 피격 적의 `Animator` 속도를 0.01초 동안 정지하고, `AttackCameraShake.cs`는 직교 메인 카메라 렌더 중 상하좌우로 약 5픽셀의 오프셋을 0.005초 적용한다. [Unity Animator 재생 속도](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/animator/speed), [Unity 카메라 렌더링 콜백](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rendering/renderpipelinemanager/begincamerarendering)
- Unity 6000.6.6f1 명령줄 PlayMode에서 실제 근접 판정, URP 카메라 오프셋 5픽셀, `Animator` 속도 정지, 예약값 및 종료 조건을 직접 입력했을 때의 복원을 21/21 확인했다. 기존 두 테스트 씬의 임시 외형에는 `Animator`가 없어 정지 검증에는 임시 `Animator`를 사용했다. 실제 프레임 경과에 따른 체감과 캐릭터 애니메이션 클립 재생은 후속 확인 항목이다. [검증 결과](Artifacts/EnemyWhiteFlash/hit_effects_verification.txt)

### 2026-11-16 — WhiteFlash 0.03초와 A 점프·S 공격

- `EnemyWhiteFlash.cs`에서 최소 3프레임 조건을 제거하고 실제 시간 0.03초가 지나면 복원하도록 조정했다. `PlayerBase.cs`에서 A에 점프 시작·유지 입력을, S에 공격 입력을 연결했다. `PlayTestDisplay.cs` 조작 안내와 `MovementChecks.cs`의 점프 입력도 갱신했다.
- Unity 6000.6.6f1 명령줄 PlayMode에서 가상 키 입력으로 A의 점프 요청과 유지·해제, S의 실제 피해 적용, WhiteFlash의 0.03초 예약 및 종료 조건에서 머티리얼 복원을 확인했다. 명중 연출 검사 25개를 통과했다. [검증 결과](Artifacts/EnemyWhiteFlash/key_mapping_and_duration_verification.txt)

### 2026-11-17 — 드롭 조각을 공격으로 수집

- `PlayerBase.cs`에서 접촉 수집을 제거하고 S 근접 공격의 `OverlapBox` 결과로 조각 수집을 시도한다. 조회 시점 이후에 생성된 조각은 새 공격에 맞아야 수집된다. `PlayTestDisplay.cs`의 조작 안내와 `UnityTestDoubles.cs`의 Collider2D 대역을 맞췄다. [Unity PhysicsScene2D.OverlapBox](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/physicsscene2d/overlapbox), [Unity ContactFilter2D.useTriggers](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/contactfilter2d/usetriggers)
- Unity 6000.6.6f1 명령줄 PlayMode에서 가상 S 입력과 기존 B 조각 프리팹을 사용해 별도 공격 수집, 접촉과 반대 방향 공격의 수집 방지, 중복 수집 방지, 특수 소진 후 재공격 수집을 15/15 확인했다. Trigger 조각은 프로젝트의 `Queries Hit Triggers`가 꺼진 조건에서 공격 쿼리에 잡히지 않았으며, 저장된 설정은 활성화 상태다. [검증 결과](Artifacts/FragmentAttack/verification.txt)

### 2026-11-18 — 착지 직전 점프 입력 버퍼

- `PlayerBase.cs`에 0.01초의 점프 입력 만료 시각을 추가했다. 물리 단계 안에서 착지한 시점을 고려해 첫 접지 판정에서 입력을 한 번 소비한다. `MovementChecks.cs` 및 `UnityTestDoubles.cs`에서 입력 시각과 고정 시각을 따로 진행하도록 했으며 기존 하강 중력 배율의 기대값도 소스의 1.55에 맞췄다. [Unity 고정 시각](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/time/fixedtimeasdouble), [Unity 2D 물리 갱신 순서](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SimulationMode2D.FixedUpdate.html)
- Unity 명령줄 PlayMode에서 가상 A 입력과 수동 물리 단계로 실제 바닥 접촉 뒤 점프 시작 속도 35와 입력 소비를 7/7 확인했다. C# 대역 검사에서는 0.02초의 물리 갱신 사이에 착지 0.01초 전 입력이 들어온 경우, 만료와 중복 소비 방지, 코요테 타임을 포함해 42/42를 확인했다. [검증 결과](Artifacts/JumpInputBuffer/verification.txt)

### 2026-11-19 — 드롭 조각 수명과 단계별 흐림

- `FragmentPickup.cs`에서 드롭 초기화 시 게임 시각과 자식 스프라이트의 원래 Alpha를 기록한다. 0~10초에는 원래 모습을 표시하고 10~13초에는 0.3초, 13~15초에는 0.1초마다 흐림과 선명함을 한 번 왕복한다. 15초에 오브젝트를 제거하고 만료 이후의 수집을 거부한다. 이동 검사의 `UnityTestDoubles.cs`에도 사용한 렌더러·투명도·왕복 계산 API를 추가했다. [Unity Mathf.PingPong](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/mathf/pingpong), [Unity 게임 시각](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/time/timeasdouble)
- Unity 6000.6.6f1 명령줄 PlayMode에서 경과 시각을 지정해 투명도 주기와 전환 경계, 원래 색 보존, 수명 직전 수집 및 만료 수집 거부를 검사했다. 만료 처리 후 프레임을 진행해 실제 오브젝트 제거까지 25/25를 확인했다. 이동 대역 검사도 42/42를 통과했다. [검증 코드](Artifacts/FragmentLifetime/verify_fragment_lifetime.cs), [검증 결과](Artifacts/FragmentLifetime/verification.txt), [이동 검사](Artifacts/FragmentLifetime/movement_regression.txt)

### 2026-11-19 — 지상 보스의 패턴 보상 조각

- `BossEnemy.cs`에서 돌진이 벽에 충돌하면 1개, 내려찍기를 마치면 1개, 광역 공격을 마치면 2개의 조각을 드롭한다. 보스의 드롭 설정을 우선 사용하고 비어 있으면 소환 조각 적의 설정을 사용해 현재 테스트 보스에는 B 조각이 나온다. `EnemyBase.cs`의 조각 생성·초기화를 공통 메서드로 추출해 패턴 보상에도 기존 수집과 15초 수명을 적용했다.
- Unity 6000.6.6f1 명령줄 PlayMode에서 보스의 실제 패턴과 2D 벽 충돌로 수량, 벽 반대쪽 드롭, 중복 드롭 방지, 지정 조각 우선 적용, 저장된 보스 프리팹의 B 조각 수집을 포함해 32/32를 확인했다. 검증에서 패턴 대기는 앞당겼으며 종료 후 3초·5초의 예약값도 확인했다. 기존 이동 대역 검사는 42/42를 통과했다. [검증 코드](Artifacts/BossPatterns/verify_boss_patterns.cs), [검증 결과](Artifacts/BossPatterns/pattern_fragment_verification.txt)

### 2026-11-19 — A/B/C 10종 조합 특수 공격

- `FragmentSpecialEffect.cs`에 AAA~CCC의 기본 조합 선택과 조정 가능한 초기 수치를 두고 `FragmentSpecialDefinition.cs`로 레시피·장비의 효과 선택을 연결했다. `SpecialHitQuery.cs`, `SpecialDash.cs`, `SpecialProjectile.cs`에서 단일·부채꼴·원형 범위, 짧은 돌진, 관통·단일·폭발 투사체를 처리한다. `PlayerBase.cs`는 기본 조합을 D 입력에 연결하고 돌진을 기존 물리 갱신과 함께 진행한다.
- `SpecialRecipe.cs`와 `SpecialEquipment.cs`에서 기본 조합에도 효과 교체와 보정을 적용한다. 기존 등록 레시피가 해당 조합에 있으면 우선 사용하고 시전 성공 시 한 번만 차감한다. 초기 피해 배수와 사거리는 위의 조합 표에 기록했으며, 시각 이펙트·투사체 외형과 애니메이션은 후속 연결 대상이다.
- Unity 6000.6.6f1 명령줄 PlayMode에서 실제 2D 쿼리와 수동 물리 단계로 10종 조합의 피해·범위, 돌진과 관통, 벽과 폭발, 장비 2발 보정·기능 교체·조합 제한, 등록 레시피 우선, 3회 사용과 가상 D 입력을 114/114 확인했다. 기존 이동 검사는 새 Unity 전용 공격 컴포넌트를 대역으로 분리해 42/42를 통과했다. [검증 코드](Artifacts/FragmentSpecials/verify_fragment_specials.cs), [검증 결과](Artifacts/FragmentSpecials/verification.txt), [이동 검사](Artifacts/FragmentSpecials/movement_regression.txt)

### 2026-11-19 — 특수 공격 10종의 시각 효과와 조각 HUD

- `SpecialEffectVisual.cs`와 URP 셰이더에서 기본 도형과 색으로 베기·돌진·파동의 범위, 날아가는 투사체, 명중점 폭발을 표시한다. `FragmentSpecialEffect.cs`, `SpecialDash.cs`, `SpecialProjectile.cs`가 기존 판정에 사용한 위치·방향과 범위로 도형을 표시한다. 캐릭터의 공격 모션은 기존 상태로 재생한다.
- `PlayTestDisplay.cs`는 미조합 A/B/C 개수와 활성 조합·남은 횟수를 표시한다. 테스트 버튼은 `PlayTestScene.unity`에서 활성화했으며 실제 보유량을 사용하지 않고 선택한 기본 공격을 즉시 실행한다.
- Unity 6000.6.6f1의 PlayTestScene 복사본에서 HUD 상태 문구·10종 테스트 진입점, 투사체 이동, 효과의 범위와 실제 렌더 픽셀을 46/46 확인했다. 기존 조합 공격 기능 114/114와 이동 대역 검사 42/42를 재실행했다. 미리보기는 효과를 별도 카메라로 촬영했으며, 왼쪽부터 행 순서대로 AAA~CCC다. [검증 코드](Artifacts/SpecialEffectVisual/verify_special_effect_visuals.cs), [검증 결과](Artifacts/SpecialEffectVisual/verification.txt), [효과 미리보기](Artifacts/SpecialEffectVisual/preview.png)

### 2026-11-19 — 왼쪽 아래 조각 및 특수 슬롯

- `PlayTestDisplay.cs`의 기존 HP·조작 안내·조각 집계·테스트 버튼을 제거하고 두 테스트 씬에 정사각형 네 칸을 표시한다. 첫 세 칸은 획득 순서의 조각, 마지막 칸은 장비 교체를 반영한 특수 공격 이름과 남은 횟수다.
- `PlayerBase.cs`와 `PlayerCheckpointState.cs`에 최대 3개인 조각 큐와 저장 순서를 적용했다. 세 번째 시전 성공 시 큐를 비운다. 순서 필드가 없는 구버전 저장은 A, B, C 순서로 복원한다. `SpecialRecipe.cs`는 실제 시전과 HUD에서 효과 선택 규칙을 공유한다.
- 키보드·마우스 조작 없이 Unity 6000.6.6f1 명령줄 PlayMode의 PlayTestScene에서 큐의 순서·용량, 3회 사용, JSON 복원·구버전 호환, 장비 교체 이름과 두 해상도의 슬롯 위치를 30/30 확인했다. 특수 공격 회귀 검사 114/114, 이동 대역 검사 42/42를 통과했다. 게임 화면의 실제 렌더링은 별도로 검증하지 않았다. [검증 코드](Artifacts/FragmentSlots/verify_fragment_slots.cs), [검증 결과](Artifacts/FragmentSlots/verification.txt)

### 2026-11-19 — PlayTestScene 조각 HUD 렌더 검증

- 현재 프로젝트의 복사본에서 Unity 6000.6.6f1로 macOS 플레이어를 만들어 포커스를 가져오지 않고 실행했다. 검증 코드로 플레이어를 적 앞에 배치하고 실제 근접 공격 판정을 호출했다. C·A·B 적을 순서대로 처치하고, 드롭된 조각을 별도 공격으로 수집했다. 실제 특수 시전 메서드도 세 번 호출했다. 원본 씬과 게임 코드는 수정하지 않았다.
- 1280×720 게임 프레임 일곱 장에서 1~3번의 C→A→B 표시, 4번의 `Explosive Thrust`와 3→2→1회 표시, 세 번째 시전 후 네 칸의 내용이 비워지는 것을 확인했다. 근접 처치·별도 공격 수집·시전·슬롯 테두리·글자 픽셀 검사는 106/106 통과했다. 배치 모드에서는 프레임 캡처가 되지 않아 macOS 백그라운드 실행으로 전환했고, 공격 전에는 위치 이동이 반영되도록 한 물리 갱신을 기다렸다. 도중에 URP 후처리 셰이더 스트리핑 경고가 기록됐다. [검증 코드](Artifacts/FragmentSlots/verify_fragment_slots_render.cs), [검증 결과](Artifacts/FragmentSlots/PlayTestScene/verification.txt), [게임 화면](Artifacts/FragmentSlots/PlayTestScene/03-cab.png), [단계별 HUD](Artifacts/FragmentSlots/PlayTestScene/hud_sequence.png)

### 2026-11-20 — 공통 카메라 크기·추적·경계

- `PlayerCameraFollow.cs`가 씬 로드 시 메인 직교 카메라에 공통 설정을 적용한다. 두 테스트 씬의 수평 `PositionConstraint`를 0.3초 보간으로 교체했고 타이틀과 2D 씬 템플릿에도 기본 직교 크기 10을 저장했다. 카메라가 따라가는 축과 가로 오프셋은 기존 테스트 씬과 같게 설정했다. [Unity Vector2.SmoothDamp](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Vector2.SmoothDamp.html), [Unity Camera.orthographicSize](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-orthographicSize.html)
- 사각 경계를 활성화하면 화면 모서리와 명중 흔들림을 제한한다. 경계가 화면보다 좁은 경우 뷰포트와 직교 투영을 함께 조정해 배율을 유지한다. Unity 6000.6.6f1 실행에서는 뷰포트 변경 시 보고되는 `Camera.aspect`도 바뀌어, 원래 비율을 픽셀 뷰포트로 복원하도록 수정했다. 기존 테스트 씬은 경계를 지정하면 사용할 수 있다.
- 키보드·마우스 없이 macOS 빌드에서 타이틀 자동 적용과 두 테스트 씬의 실제 추적 메서드·3개 종횡비·경계·흔들림·크기 재설정을 61/61 확인했다. 1280×720 렌더 프레임에서 좁은 경계 바깥의 월드가 가려지고 HUD와 오브젝트의 배율이 유지되는 것도 확인했다. [검증 코드](Artifacts/PlayerCamera/VerifyPlayerCamera.cs), [검증 결과](Artifacts/PlayerCamera/verification.txt), [기본 화면](Artifacts/PlayerCamera/wide.png), [좁은 경계 화면](Artifacts/PlayerCamera/cropped.png)

### 2026-11-21 — AAB 늘어나는 칼날과 ABC 파편 베기

- `FragmentSpecialEffect.cs`의 AAB와 ABC 기본 조합에 `ExtendingBlade`와 `ShardSlash`를 연결했다. 기존 효과 유형의 직렬화 번호와 명시적으로 선택하는 레시피·장비 효과는 호환성을 위해 유지했다. 첫 피해와 후속 피해의 기본 배수 및 판정 범위는 위의 조합 표에 기록했다.
- `SpecialHitQuery.cs`는 칼날의 박스 캐스트로 가장 먼저 닿는 적과 지형을 비교한다. 파편은 근접 명중점부터 전방으로 검사하며 첫 적과 이미 피해를 받은 적은 제외한다. `SpecialEffectVisual.cs`는 실제로 뻗은 칼날과 밝은 끝부분, 명중점의 전방 파편을 표시한다. [Unity PhysicsScene2D.BoxCast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PhysicsScene2D.BoxCast.html)
- Unity 6000.6.6f1 명령줄 PlayMode에서 실제 2D 판정으로 양방향 명중·칼끝 보너스·벽과 범위 차단·파편 중복 제거·처치 직후 파편·빗나감, 시각 효과 메시와 HUD 이름, 기존 조합과 장비 동작을 142/142 확인했다. 박스 캐스트 거리 검사는 계산한 기하 거리와 엔진 관측값 차이 0.005를 반영해 0.01 허용 오차로 재실행했다. 이동 대역 검사는 42/42를 통과했다. 새 시각 효과 검증은 메시와 위치까지 수행했다. [검증 코드](Artifacts/FragmentSpecials/verify_fragment_specials.cs), [검증 결과](Artifacts/FragmentSpecials/verification_aab_abc.txt)

### 2026-11-21 — C# 접근 제한자 정리

- `Assets/Function/`과 `Assets/Scenes/TestScene/JumpPathDisplay.cs`에서 명시적 `internal` 88곳을 제거했다. 최상위 보조 클래스는 선언을 생략해 기본 접근 범위를 사용한다. 클래스 간 호출에 필요한 멤버는 `public`으로 바꾸고, 공개된 체크포인트 API가 사용하는 `PlayerCheckpointState`도 `public`으로 맞췄다. [C# 접근 제한자](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers)
- `Artifacts/`의 이동 대역과 저장·접촉·특수 공격 검증 코드를 새 멤버 접근 범위에 맞췄다. 클래스 이름과 저장 필드 및 기존 직렬화 참조를 유지했다.
- `Assets/`, `Packages/`, `Artifacts/`의 관리 대상 C# 소스에서 `internal` 표기가 0건임을 확인했다. Unity 6000.6.6f1의 임시 프로젝트에서 컴파일, 조각 저장·복원 30/30, 특수 공격 142/142를 확인했다. 이동 대역 검사는 42/42를 통과했다. [검증 결과](Artifacts/AccessModifiers/verification.txt)

### 2026-11-23 — 방향키 무기 조합표 작성

- 요청에 따라 `Readme.md`에 방향키 세 번으로 만드는 10종 무기의 기획표를 추가했다. 기존 무기 대응은 조합 기호를 기준으로 삼고, ABC·BBB·BBC·BCC의 효과는 최신 기획을 따랐다.
- 기획표의 조합과 방향키 대응을 대조했다. 변경 범위는 문서이며, 후속 결정 사항은 ABC의 파편 세부 동작과 BBB의 발사 방식이다.

### 2026-11-23 — 무기 조합 입력과 감속 규칙 정리

- 요청에 따라 `Readme.md`의 무기표 앞에 파편 게이지, 조합 중 이동 제한, 약 15% 속도와 실제 5초 제한, 버튼 해제 시 취소·획득 조건을 기록했다. 아티팩트 강화와 보스 패턴별 무기 선택도 함께 정리했다.
- 무기표의 방향키 대응과 대조하고 `Assets/Function/Player/PlayerBase.cs`의 기존 D 입력·특수 사용 경로를 확인했다. 문서 변경으로 Unity 실행 검증은 생략했다. 게이지 수치와 재진입 시 감속 처리 등 미정 항목은 ‘게이지와 조합 조작’ 절에서 이어간다.

### 2026-11-23 — 파편 충전과 집중 종료 규칙 갱신

- `Readme.md`에 적 3회 명중과 드롭 파편 1개 획득의 충전량을 기록했다. 집중 만료 후에도 조합을 이어가던 이전 기획을 수정해, 실제 시간 5초 만료 또는 무기 확정 시 집중을 끝내고 3초간 조합을 제한하도록 정리했다.
- 드롭 파편의 게임 시간 기준 수명 15초는 `Assets/Function/Player/FragmentPickup.cs`, 기존 특수 사용 횟수 3회는 `Assets/Function/Player/PlayerBase.cs`와 대조했다. 문서만 변경했다. 새 무기 사용 횟수 2회는 시험 제안이며, 게이지 차감과 취소·만료 시 세부 처리는 후속 결정 사항이다.

### 2026-11-23 — 방향키 무기 조합과 파편 게이지 구현

- `WeaponFocus.cs`, `PlayerBase.cs`, `PlayerCheckpointState.cs`와 테스트 HUD에 게이지·누적 명중, D 집중, 최근 방향 세 개, 무기 확정과 2회 시전을 연결했다. 시험 규칙으로 확정 시 세 칸을 차감하며, 미완성 조기 해제는 재진입을 허용하고 만료 시에는 입력을 폐기한다. 새 저장은 게이지와 명중 진행도·무기·남은 횟수를 보관한다. 이전 저장은 같은 조합 기호를 적용하고 남은 횟수를 최대 2회로 변환한다.
- 적의 공격·사망 경로에 `FragmentDrop.cs`를 연결했다. 관통 투창은 적마다 드롭 한 개를 생성한다. 5발 사격은 전방 20° 동시 직선탄으로, 산탄은 전방 10° 동시 세 발로 설정했다. 기존 `SpecialRecipe`의 장비 효과 선택·보정을 사용하며 아티팩트별 강화 수치와 장착 화면은 후속 작업이다.
- Unity 6000.6.6f1 명령줄 PlayMode에서 PlayTestScene의 가상 키 입력·충전·드롭·저장 변환을 59/59, 실제 2D 물리를 사용한 10종 무기와 장비 보정을 133/133 확인했다. 5초 만료와 3초 쿨타임은 실제 시각 기반 예약값과 경계 갱신을 검사했다. 이동 대역 42/42도 통과했다. HUD는 씬 연결까지 확인했다. [집중 검증 코드](Artifacts/WeaponFocus/VerifyWeaponFocus.cs), [무기 검증 코드](Artifacts/FragmentSpecials/verify_fragment_specials.cs), [검증 결과](Artifacts/WeaponFocus/verification.txt)

### 2026-11-23 — 방향키 조준과 공중 하향 공격

- `PlayerBase.cs`가 상·하·좌·우 입력을 근접 쿼리와 시전의 조준 벡터로 전달한다. `SpecialContext.cs`, 특수 판정·투사체 생성 및 이펙트는 시전 시점의 방향을 사용한다. `PlayTestDisplay.cs`는 실제 근접 판정의 중심·회전·크기에 맞춰 표시한다. 상하를 우선하고 무입력에서는 마지막 수평 방향을 따른다. [Unity PhysicsScene2D.OverlapBox](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PhysicsScene2D.OverlapBox.html)
- Unity 6000.6.6f1 명령줄 PlayMode에서 가상 키보드로 실제 점프 뒤 ↓ 근접 타격, 범위 표시의 회전과 크기, 상·하·좌·우 및 집중 입력 분리, 투창의 방향 고정과 관통, 산탄 각도를 32/32 확인했다. 기존 특수 공격 133/133과 이동 대역 42/42도 통과했다. [검증 코드](Artifacts/AttackDirection/VerifyAttackDirection.cs), [검증 결과](Artifacts/AttackDirection/verification.txt)
