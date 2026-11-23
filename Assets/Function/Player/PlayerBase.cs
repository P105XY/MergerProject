using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    public readonly struct FragmentCombination
    {
        public int ACount { get; }
        public int BCount { get; }
        public int CCount { get; }

        // A, B, C 조각의 개수로 조합 정보를 만든다.
        public FragmentCombination(int aCount, int bCount, int cCount)
        {
            ACount = aCount;
            BCount = bCount;
            CCount = cCount;
        }

        // 최근 방향 입력을 순서와 무관한 방향별 개수로 바꾼다.
        public static FragmentCombination FromDirections(IEnumerable<CollectibleType> directions)
        {
            int left = 0;
            int down = 0;
            int right = 0;

            foreach (CollectibleType direction in directions) {
                switch (direction) {
                    case CollectibleType.A: left++; break;
                    case CollectibleType.B: down++; break;
                    case CollectibleType.C: right++; break;
                }
            }

            return new FragmentCombination(left, down, right);
        }
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerBase : MonoBehaviour
    {
        private float maxHealth = 100f;
        private float contactDamageCooldown = 0.75f;
        private float attackPower = 10f;
        private float attacksPerSecond = 1f;
        private float maximumMovementSpeed = 5f;
        private float movementAcceleration = 120f;
        private float movementDeceleration = 180f;
        private float jumpForce = 35.0f;
        private float gravityScale = 9.8f;
        private float movementFriction = 0f;
        private float maximumJumpHeight = 30f;
        private float coyoteTime = 0.12f;
        private float jumpInputBufferTime = 0.01f;
        private float jumpFallGravityMultiplier = 1.55f;
        private float attackReach = 1.1f;
        private Vector2 attackSize = new Vector2(1.2f, 1f);
        private LayerMask attackLayers = ~0;
        private LayerMask groundLayers = ~0;
        private SpecialRecipe[] specialRecipes = Array.Empty<SpecialRecipe>();
        private SpecialEquipment[] equippedSpecialEquipment = Array.Empty<SpecialEquipment>();
        public const int FragmentSlotCount = WeaponFocus.DirectionCount;
        public const int WeaponGaugeCapacity = WeaponFocus.Capacity;
        private const int SpecialUseCount = 2;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
        private readonly List<Collider2D> contactColliders = new List<Collider2D>();
        private Rigidbody2D playerBody;
        private PhysicsMaterial2D runtimeMovementMaterial;
        private float movementInput;
        private float nextAttackTime;
        private float nextContactDamageTime;
        private double jumpInputExpiresAt = double.NegativeInfinity;
        private bool jumpHeld;
        private bool jumpInProgress;
        private bool jumpReleaseApplied;
        private bool jumpDescending;
        private float gravityScaleBeforeFall;
        private float coyoteTimeRemaining;
        private float jumpStartHeight;
        private int facingDirection = 1;
        private readonly List<Collider2D> attackHits = new List<Collider2D>();
        private readonly HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();
        private readonly WeaponFocus weaponFocus = new();
        private FragmentCombination activeCombination;
        private bool specialUseInProgress;
        private SpecialDash specialDash;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        public bool IsJumping => jumpInProgress && isActiveAndEnabled && !IsDead;
        public bool IsJumpDescending => jumpDescending;
        public float JumpFallGravityScale => jumpDescending ? playerBody.gravityScale : playerBody.gravityScale * jumpFallGravityMultiplier;
        public LayerMask JumpGroundLayers => groundLayers;
        public LayerMask SpecialAttackLayers => attackLayers;
        public LayerMask SpecialEnvironmentLayers => Physics2D.GetLayerCollisionMask(gameObject.layer);
        public float AttackPower => attackPower;
        public float AttacksPerSecond => attacksPerSecond;
        public int FacingDirection => facingDirection;
        public Vector2 LastAttackCenter { get; private set; }
        public float LastAttackAngle { get; private set; }
        public Vector2 AttackSize => new Vector2(Mathf.Max(0f, attackSize.x), Mathf.Max(0f, attackSize.y));
        public int WeaponGauge => weaponFocus.Gauge;
        public int HitsTowardFragment => weaponFocus.HitProgress;
        public bool IsFocusingWeapon => weaponFocus.IsActive;
        public bool CanFocusWeapon => !IsSpecialAvailable && weaponFocus.CanBegin(Time.unscaledTimeAsDouble);
        public IReadOnlyList<CollectibleType> CombinationInputs => weaponFocus.Directions;
        public int SpecialUsesRemaining { get; private set; }
        public bool IsSpecialAvailable => SpecialUsesRemaining > 0;
        public FragmentCombination ActiveCombination => activeCombination;

        public event Action<PlayerBase> AttackRequested;
        public event Action JumpStarted;
        public event Action<PlayerBase> SpecialRequested;
        public event Action<PlayerBase, FragmentCombination> SpecialUsed;
        public static event Action<PlayerBase> Died;

        // 플레이어의 물리 컴포넌트와 초기 체력을 준비한다.
        protected virtual void Awake()
        {
            playerBody = GetComponent<Rigidbody2D>();
            playerBody.gravityScale = Mathf.Max(0f, gravityScale);
            PhysicsMaterial2D originalMovementMaterial = playerBody.sharedMaterial;
            runtimeMovementMaterial = new PhysicsMaterial2D("Player movement") {
                friction = Mathf.Max(0f, movementFriction),
                bounciness = originalMovementMaterial != null ? originalMovementMaterial.bounciness : 0f,
                hideFlags = HideFlags.DontSave
            };
            playerBody.sharedMaterial = runtimeMovementMaterial;
            CurrentHealth = Mathf.Max(0f, maxHealth);
        }

        // 플레이어 전용으로 생성한 물리 재질을 해제한다.
        protected virtual void OnDestroy()
        {
            weaponFocus.Cancel();

            if (runtimeMovementMaterial != null) {
                Destroy(runtimeMovementMaterial);
            }
        }

        // 키보드 입력을 읽고 공격과 특수 기능 요청을 전달한다.
        protected virtual void Update()
        {
            if (IsDead) {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            movementInput = 0f;
            jumpHeld = false;
            double now = Time.unscaledTimeAsDouble;
            weaponFocus.Tick(now);

            if (keyboard == null) {
                weaponFocus.Cancel();
                return;
            }

            bool composing = ReadWeaponInput(keyboard, now);

            if (!composing) {
                movementInput = (keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
            }

            jumpHeld = keyboard.aKey.isPressed;

            if (movementInput != 0f) {
                facingDirection = movementInput > 0f ? 1 : -1;
            }

            if (keyboard.sKey.isPressed && attacksPerSecond > 0f && Time.time >= nextAttackTime) {
                nextAttackTime = Time.time + 1f / attacksPerSecond;
                Attack();
                AttackRequested?.Invoke(this);
            }

            if (keyboard.aKey.wasPressedThisFrame) {
                jumpInputExpiresAt = Time.timeAsDouble + jumpInputBufferTime;
            }

            if (!composing && keyboard.dKey.wasPressedThisFrame && IsSpecialAvailable) {
                TryUseSpecial();
            }
        }

        // D로 집중을 시작하고 집중 중의 방향 입력을 확정한다.
        private bool ReadWeaponInput(Keyboard keyboard, double now)
        {
            if (keyboard.dKey.wasPressedThisFrame && !IsSpecialAvailable) {
                weaponFocus.TryBegin(now);
            }

            if (!weaponFocus.IsActive) {
                return false;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame) {
                weaponFocus.AddDirection(CollectibleType.A);
            }

            if (keyboard.downArrowKey.wasPressedThisFrame) {
                weaponFocus.AddDirection(CollectibleType.B);
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame) {
                weaponFocus.AddDirection(CollectibleType.C);
            }

            if (!keyboard.dKey.isPressed && weaponFocus.TryComplete(now, out FragmentCombination result)) {
                activeCombination = result;
                SpecialUsesRemaining = SpecialUseCount;
            }

            return true;
        }

        // 누른 방향키로 공격 방향을 정하고 입력이 없으면 마지막 좌우 방향을 사용한다.
        private Vector2 GetAttackDirection()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && !weaponFocus.IsActive) {
                if (keyboard.upArrowKey.isPressed != keyboard.downArrowKey.isPressed) {
                    return keyboard.upArrowKey.isPressed ? Vector2.up : Vector2.down;
                }

                if (keyboard.leftArrowKey.isPressed != keyboard.rightArrowKey.isPressed) {
                    return keyboard.leftArrowKey.isPressed ? Vector2.left : Vector2.right;
                }
            }

            return Vector2.right * facingDirection;
        }

        // 조준 방향으로 회전한 근접 판정에 닿은 조각을 수집하고 적에게 피해를 준다.
        private void Attack()
        {
            Vector2 direction = GetAttackDirection();
            LastAttackCenter = (Vector2)transform.position + direction * Mathf.Max(0f, attackReach);
            LastAttackAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(attackLayers);
            filter.useTriggers = true;
            attackHits.Clear();
            hitEnemies.Clear();
            gameObject.scene.GetPhysicsScene2D().OverlapBox(LastAttackCenter, AttackSize, LastAttackAngle, filter, attackHits);

            foreach (Collider2D hit in attackHits) {
                FragmentPickup fragment = hit.GetComponentInParent<FragmentPickup>();

                if (fragment != null && fragment.isActiveAndEnabled) {
                    fragment.TryCollect(this);
                    continue;
                }

                if (hit.isTrigger) {
                    continue;
                }

                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();

                if (enemy != null && enemy.isActiveAndEnabled && hitEnemies.Add(enemy)) {
                    enemy.ApplyPlayerDamage(attackPower, this);
                }
            }
        }

        // 일반 충돌이 시작되면 접촉 피해를 확인한다.
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            RegisterContact(collision.collider);
        }

        // 일반 충돌이 계속되는 동안 접촉 목록을 복구한다.
        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            RegisterContact(collision.collider);
        }

        // 일반 충돌이 끝난 콜라이더를 접촉 목록에서 제거한다.
        protected virtual void OnCollisionExit2D(Collision2D collision)
        {
            contactColliders.Remove(collision.collider);
        }

        // 트리거에 진입하면 접촉 피해를 확인한다.
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            RegisterContact(other);
        }

        // 트리거 접촉 목록을 복구하고 접촉 피해를 확인한다.
        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            RegisterContact(other);
        }

        // 벗어난 트리거를 접촉 목록에서 제거한다.
        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            contactColliders.Remove(other);
        }

        // 새 콜라이더를 접촉 목록에 등록하고 첫 피해를 확인한다.
        private void RegisterContact(Collider2D other)
        {
            if (other == null || !isActiveAndEnabled || contactColliders.Contains(other)) {
                return;
            }

            contactColliders.Add(other);
            TryApplyContactDamage(other);
        }

        // 물리 바디가 잠들어도 접촉 목록에서 재피격을 확인한다.
        private void ApplyOngoingContactDamage()
        {
            if (CurrentHealth <= 0f || Time.time < nextContactDamageTime) {
                return;
            }

            for (int index = contactColliders.Count - 1; index >= 0; index--) {
                Collider2D other = contactColliders[index];

                if (other == null || !other.isActiveAndEnabled) {
                    contactColliders.RemoveAt(index);
                    continue;
                }

                TryApplyContactDamage(other);

                if (Time.time < nextContactDamageTime) {
                    break;
                }
            }
        }

        // 살아 있는 적이나 위험 장애물의 피해를 공통 재피격 간격에 맞춰 적용한다.
        private void TryApplyContactDamage(Collider2D other)
        {
            if (other == null) {
                return;
            }

            float damage;
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

            if (enemy != null) {
                if (!enemy.isActiveAndEnabled || enemy.IsDefeated || enemy.CurrentState == EnemyState.Dead) {
                    return;
                }

                damage = enemy.AttackPower;
            } else {
                DamagedObstacle obstacle = other.GetComponentInParent<DamagedObstacle>();

                if (obstacle == null || !obstacle.isActiveAndEnabled) {
                    return;
                }

                damage = obstacle.Damage;
            }

            float previousHealth = CurrentHealth;
            TryReceiveContactDamage(damage);

            if (enemy != null && enemy.CurrentState != EnemyState.Attack && CurrentHealth < previousHealth) {
                enemy.DropAttackFragment();
            }
        }

        // 충돌이나 돌진으로 인한 피해에 동일한 재피격 간격을 적용한다.
        public void TryReceiveContactDamage(float damage)
        {
            float now = Time.time;

            if (!isActiveAndEnabled || CurrentHealth <= 0f || now < nextContactDamageTime) {
                return;
            }

            float previousHealth = CurrentHealth;
            ApplyDamage(damage);

            if (CurrentHealth < previousHealth) {
                nextContactDamageTime = now + Mathf.Max(0.02f, contactDamageCooldown);
            }
        }

        // 월드에서 수집한 파편 한 개로 무기 게이지를 한 칸 충전한다.
        public bool TryCollectFragment(CollectibleType fragmentType)
        {
            return !IsDead && isActiveAndEnabled
                && fragmentType is (CollectibleType.A or CollectibleType.B or CollectibleType.C)
                && weaponFocus.TryCharge();
        }

        // 적에게 실제로 피해를 준 명중을 파편 게이지 진행도에 기록한다.
        public void RegisterWeaponHit()
        {
            if (!IsDead && isActiveAndEnabled) {
                weaponFocus.RegisterHit();
            }
        }

        // 시전할 때 참조하는 장착 장비 목록을 교체한다.
        public void SetSpecialEquipment(params SpecialEquipment[] equipment)
        {
            equippedSpecialEquipment = equipment == null
                ? Array.Empty<SpecialEquipment>()
                : (SpecialEquipment[])equipment.Clone();
        }

        // 활성 조합의 특수 효과를 실행하고 성공했을 때만 사용 횟수를 줄인다.
        public bool TryUseSpecial()
        {
            if (IsDead || !IsSpecialAvailable || specialUseInProgress) {
                return false;
            }

            specialUseInProgress = true;

            try {
                FragmentCombination combination = ActiveCombination;
                SpecialContext context = new SpecialContext(this, combination, GetAttackDirection());

                if (!TryExecuteSpecial(context)) {
                    return false;
                }

                SpecialUsesRemaining--;

                if (SpecialUsesRemaining == 0) {
                    activeCombination = default;
                }

                SpecialUsed?.Invoke(this, combination);
                SpecialRequested?.Invoke(this);
                return true;
            } finally {
                specialUseInProgress = false;
            }
        }

        // 활성 조합과 일치하는 첫 레시피를 찾아 현재 장비로 시전한다.
        private bool TryExecuteSpecial(in SpecialContext context)
        {
            SpecialRecipe recipe = FindSpecialRecipe(context.Combination);
            return recipe != null ? recipe.TryExecute(context, equippedSpecialEquipment)
                : SpecialRecipe.TryExecuteDefault(context, equippedSpecialEquipment);
        }

        // 활성 조합에 장비 효과 교체까지 반영한 공격 이름을 표시용으로 돌려준다.
        public string GetActiveSpecialName()
        {
            FragmentCombination combination = ActiveCombination;
            return SpecialRecipe.GetEffectName(FindSpecialRecipe(combination), combination, equippedSpecialEquipment);
        }

        // 등록 순서에서 지정한 조합의 첫 번째 레시피를 찾는다.
        private SpecialRecipe FindSpecialRecipe(FragmentCombination combination)
        {
            foreach (SpecialRecipe recipe in specialRecipes) {
                if (recipe != null && recipe.Matches(combination)) {
                    return recipe;
                }
            }

            return null;
        }

        // 장비 보정된 특수 돌진을 중복 실행 없이 시작한다.
        public bool TryBeginSpecialDash(in SpecialContext context, FragmentSpecialEffect effect)
        {
            if (specialDash == null) {
                specialDash = gameObject.AddComponent<SpecialDash>();
            }

            return specialDash.TryBegin(context, effect);
        }

        // 물리 갱신에서 지속 접촉 피해와 이동·점프를 처리한다.
        protected virtual void FixedUpdate()
        {
            ApplyOngoingContactDamage();

            if (IsDead) {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            Vector2 velocity = playerBody.linearVelocity;
            velocity.x = specialDash != null && specialDash.IsRunning ? 0f : NextHorizontalSpeed(velocity.x, deltaTime);
            bool grounded = velocity.y <= 0f && IsGrounded();

            if (grounded) {
                coyoteTimeRemaining = Mathf.Max(0f, coyoteTime);
                jumpInProgress = false;
                SetJumpDescending(false);
            } else {
                coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - deltaTime);
            }

            // 접지는 직전 물리 단계의 결과이며, 그 단계 안에서 착지한 시점에는 한 단계의 오차가 있다.
            double previousStepStart = Time.fixedTimeAsDouble - 2d * deltaTime;
            bool hasBufferedJump = jumpInputExpiresAt >= previousStepStart;
            bool startedJump = hasBufferedJump && (grounded || coyoteTimeRemaining > 0f) && jumpForce > 0f && maximumJumpHeight > 0f;

            if (startedJump) {
                velocity.y = jumpForce;
                jumpStartHeight = playerBody.position.y;
                jumpInProgress = true;
                jumpReleaseApplied = false;
                coyoteTimeRemaining = 0f;
            }

            if (jumpInProgress && !jumpHeld && !jumpReleaseApplied && velocity.y > 0f) {
                SetJumpDescending(true);
                jumpReleaseApplied = true;
            }

            if (jumpInProgress && velocity.y > 0f) {
                velocity.y = LimitJumpRise(velocity.y, playerBody.position.y, deltaTime);
            }

            if (jumpInProgress && velocity.y <= 0f) {
                SetJumpDescending(true);
            }

            if (startedJump || !hasBufferedJump) {
                jumpInputExpiresAt = double.NegativeInfinity;
            }

            playerBody.linearVelocity = velocity;
            specialDash?.Step(deltaTime);

            if (startedJump) {
                JumpStarted?.Invoke();
            }
        }

        // 현재 입력으로 한 물리 단계 뒤에 사용할 수평 속도를 구한다.
        private float NextHorizontalSpeed(float speed, float deltaTime)
        {
            float targetSpeed = movementInput * Mathf.Max(0f, maximumMovementSpeed);
            float speedChange = movementInput == 0f ? movementDeceleration : movementAcceleration;
            return Mathf.MoveTowards(speed, targetSpeed, Mathf.Max(0f, speedChange) * deltaTime);
        }

        // 점프 시작점 기준으로 다음 물리 단계의 상승 속도를 제한한다.
        private float LimitJumpRise(float speed, float height, float deltaTime)
        {
            float remainingHeight = Mathf.Max(0f, maximumJumpHeight - (height - jumpStartHeight));
            return Mathf.Min(speed, remainingHeight / deltaTime);
        }

        // 점프 하강을 시작할 때 중력을 높이고 하강이 끝나면 이전 배율을 복원한다.
        private void SetJumpDescending(bool descending)
        {
            if (jumpDescending == descending) {
                return;
            }

            if (descending) {
                gravityScaleBeforeFall = playerBody.gravityScale;
                playerBody.gravityScale = JumpFallGravityScale;
            } else {
                playerBody.gravityScale = gravityScaleBeforeFall;
            }

            jumpDescending = descending;
        }

        // 현재 입력이 이어질 때 수평 이동과 점프 높이 상한을 반영한 속도를 예측한다.
        public Vector2 PredictJumpVelocity(Vector2 position, Vector2 velocity, float deltaTime)
        {
            velocity.x = NextHorizontalSpeed(velocity.x, deltaTime);

            if (velocity.y > 0f) {
                velocity.y = LimitJumpRise(velocity.y, position.y, deltaTime);
            }

            return velocity;
        }

        // 비활성화할 때 접촉 목록과 남은 이동·점프 입력을 지운다.
        protected virtual void OnDisable()
        {
            weaponFocus.Cancel();
            contactColliders.Clear();
            movementInput = 0f;
            jumpInputExpiresAt = double.NegativeInfinity;
            jumpHeld = false;
            jumpInProgress = false;
            jumpReleaseApplied = false;
            coyoteTimeRemaining = 0f;
            SetJumpDescending(false);
            specialDash?.Cancel();
        }

        // 양수 피해로 체력이 처음 0이 되면 이동을 멈추고 사망을 알린다.
        public void ApplyDamage(float damage)
        {
            if (IsDead || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

            if (IsDead) {
                weaponFocus.Cancel();
                jumpInputExpiresAt = double.NegativeInfinity;
                specialDash?.Cancel();
                SetJumpDescending(false);
                playerBody.linearVelocity = Vector2.zero;
                Died?.Invoke(this);
            }
        }

        // 무기 게이지, 누적 명중, 제작한 무기와 남은 시전 횟수를 저장한다.
        public PlayerCheckpointState CaptureCheckpointState()
        {
            FragmentCombination active = ActiveCombination;
            return new PlayerCheckpointState {
                WeaponSystemVersion = PlayerCheckpointState.CurrentWeaponSystemVersion,
                WeaponGauge = WeaponGauge,
                HitsTowardFragment = HitsTowardFragment,
                ActiveA = active.ACount,
                ActiveB = active.BCount,
                ActiveC = active.CCount,
                SpecialUsesRemaining = SpecialUsesRemaining
            };
        }

        // 저장 형식에 따라 게이지와 무기 사용 횟수 또는 기존 조각 수량을 검증한다.
        public static bool IsValidCheckpointState(PlayerCheckpointState state)
        {
            if (state == null || state.CollectedA < 0 || state.CollectedB < 0 || state.CollectedC < 0
                || state.ActiveA < 0 || state.ActiveB < 0 || state.ActiveC < 0
                || state.SpecialUsesRemaining < 0) {
                return false;
            }

            long collected = (long)state.CollectedA + state.CollectedB + state.CollectedC;
            long active = (long)state.ActiveA + state.ActiveB + state.ActiveC;
            bool hasWeapon = state.SpecialUsesRemaining > 0;

            if (active != (hasWeapon ? FragmentSlotCount : 0)) {
                return false;
            }

            if (state.WeaponSystemVersion == PlayerCheckpointState.CurrentWeaponSystemVersion) {
                return state.SpecialUsesRemaining <= SpecialUseCount && collected == 0
                    && (state.FragmentOrder == null || state.FragmentOrder.Length == 0)
                    && state.WeaponGauge is >= 0 and <= WeaponGaugeCapacity
                    && state.HitsTowardFragment is >= 0 and < WeaponFocus.HitsPerFragment;
            }

            if (state.WeaponSystemVersion != 0 || state.SpecialUsesRemaining > 3
                || collected >= FragmentSlotCount || (hasWeapon && collected != 0)) {
                return false;
            }

            if (state.FragmentOrder == null || state.FragmentOrder.Length == 0) {
                return true;
            }

            FragmentCombination orderCounts = FragmentCombination.FromDirections(state.FragmentOrder);
            return state.FragmentOrder.Length == active + collected
                && orderCounts.ACount == state.ActiveA + state.CollectedA
                && orderCounts.BCount == state.ActiveB + state.CollectedB
                && orderCounts.CCount == state.ActiveC + state.CollectedC;
        }

        // 새 씬의 플레이어를 배치하고 무기와 게이지 및 지정된 체력을 복원한다.
        public void RestoreCheckpointState(PlayerCheckpointState state, Vector3 position, float? currentHealth = null)
        {
            bool legacy = state.WeaponSystemVersion == 0;
            int gauge = legacy ? state.CollectedA + state.CollectedB + state.CollectedC : state.WeaponGauge;
            weaponFocus.Restore(gauge, legacy ? 0 : state.HitsTowardFragment);
            activeCombination = new FragmentCombination(state.ActiveA, state.ActiveB, state.ActiveC);
            SpecialUsesRemaining = Math.Min(SpecialUseCount, state.SpecialUsesRemaining);
            CurrentHealth = Mathf.Max(0f, Mathf.Min(currentHealth ?? maxHealth, maxHealth));
            jumpInputExpiresAt = double.NegativeInfinity;
            specialDash?.Cancel();
            transform.position = position;
            playerBody.position = (Vector2)position;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        // 플레이어 아래의 지면과 접촉 중인지 확인한다.
        private bool IsGrounded()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int hitCount = playerBody.Cast(Vector2.down, filter, groundHits, 0.05f);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++) {
                if (groundHits[hitIndex].normal.y > 0.5f) {
                    return true;
                }
            }

            return false;
        }

    }
}
