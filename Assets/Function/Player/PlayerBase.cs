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
        private float jumpFallGravityMultiplier = 1.55f;
        private float attackReach = 1.1f;
        private Vector2 attackSize = new Vector2(1.2f, 1f);
        private LayerMask attackLayers = ~0;
        private LayerMask groundLayers = ~0;
        private SpecialRecipe[] specialRecipes = Array.Empty<SpecialRecipe>();
        private SpecialEquipment[] equippedSpecialEquipment = Array.Empty<SpecialEquipment>();
        private const int RequiredFragmentCount = 3;
        private const int SpecialUseCount = 3;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
        private readonly List<Collider2D> contactColliders = new List<Collider2D>();
        private Rigidbody2D playerBody;
        private PhysicsMaterial2D runtimeMovementMaterial;
        private float movementInput;
        private float nextAttackTime;
        private float nextContactDamageTime;
        private bool jumpRequested;
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
        private int typeACount;
        private int typeBCount;
        private int typeCCount;
        private bool specialUseInProgress;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        internal bool IsJumping => jumpInProgress && isActiveAndEnabled && !IsDead;
        internal bool IsJumpDescending => jumpDescending;
        internal float JumpFallGravityScale => jumpDescending ? playerBody.gravityScale : playerBody.gravityScale * jumpFallGravityMultiplier;
        internal LayerMask JumpGroundLayers => groundLayers;
        public float AttackPower => attackPower;
        public float AttacksPerSecond => attacksPerSecond;
        public int FacingDirection => facingDirection;
        public int TypeACount => typeACount;
        public int TypeBCount => typeBCount;
        public int TypeCCount => typeCCount;
        public int SpecialUsesRemaining { get; private set; }
        public bool IsSpecialAvailable => SpecialUsesRemaining > 0;
        public FragmentCombination ActiveCombination { get; private set; }

        public event Action<PlayerBase> AttackRequested;
        internal event Action JumpStarted;
        public event Action<PlayerBase> SpecialRequested;
        public event Action<PlayerBase, FragmentCombination> SpecialUsed;
        internal static event Action<PlayerBase> Died;

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

            if (keyboard == null) {
                return;
            }

            movementInput = (keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
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
                jumpRequested = true;
            }

            if (keyboard.dKey.wasPressedThisFrame) {
                TryUseSpecial();
            }
        }

        // 바라보는 방향의 공격 범위에 있는 적에게 피해를 한 번씩 적용한다.
        private void Attack()
        {
            Vector2 center = (Vector2)transform.position + Vector2.right * (facingDirection * Mathf.Max(0f, attackReach));
            Vector2 size = new Vector2(Mathf.Max(0f, attackSize.x), Mathf.Max(0f, attackSize.y));
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(attackLayers);
            filter.useTriggers = false;
            attackHits.Clear();
            hitEnemies.Clear();
            gameObject.scene.GetPhysicsScene2D().OverlapBox(center, size, 0f, filter, attackHits);

            foreach (Collider2D hit in attackHits) {
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

        // 트리거 접촉 목록을 복구하고 수집 가능한 조각을 줍는다.
        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            RegisterContact(other);
            FragmentPickup fragment = other.GetComponentInParent<FragmentPickup>();

            if (fragment != null) {
                fragment.TryCollect(this);
            }
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

            TryReceiveContactDamage(damage);
        }

        // 충돌이나 돌진으로 인한 피해에 동일한 재피격 간격을 적용한다.
        internal void TryReceiveContactDamage(float damage)
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

        // 조각을 종류별로 모으고 세 개가 되면 조합을 준비한다.
        public bool TryCollectFragment(CollectibleType fragmentType)
        {
            if (IsDead || IsSpecialAvailable) {
                return false;
            }

            switch (fragmentType) {
                case CollectibleType.A:
                    typeACount++;
                    break;
                case CollectibleType.B:
                    typeBCount++;
                    break;
                case CollectibleType.C:
                    typeCCount++;
                    break;
                default:
                    return false;
            }

            if (typeACount + typeBCount + typeCCount == RequiredFragmentCount) {
                ActiveCombination = new FragmentCombination(typeACount, typeBCount, typeCCount);
                typeACount = 0;
                typeBCount = 0;
                typeCCount = 0;
                SpecialUsesRemaining = SpecialUseCount;
            }

            return true;
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
                SpecialContext context = new SpecialContext(this, combination);

                if (!TryExecuteSpecial(context)) {
                    return false;
                }

                SpecialUsesRemaining--;

                if (SpecialUsesRemaining == 0) {
                    ActiveCombination = default;
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
            foreach (SpecialRecipe recipe in specialRecipes) {
                if (recipe != null && recipe.Matches(context.Combination)) {
                    return recipe.TryExecute(context, equippedSpecialEquipment);
                }
            }

            return false;
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
            velocity.x = NextHorizontalSpeed(velocity.x, deltaTime);
            bool grounded = velocity.y <= 0f && IsGrounded();

            if (grounded) {
                coyoteTimeRemaining = Mathf.Max(0f, coyoteTime);
                jumpInProgress = false;
                SetJumpDescending(false);
            } else {
                coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - deltaTime);
            }

            bool startedJump = jumpRequested && (grounded || coyoteTimeRemaining > 0f) && jumpForce > 0f && maximumJumpHeight > 0f;

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

            jumpRequested = false;
            playerBody.linearVelocity = velocity;

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
        internal Vector2 PredictJumpVelocity(Vector2 position, Vector2 velocity, float deltaTime)
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
            contactColliders.Clear();
            movementInput = 0f;
            jumpRequested = false;
            jumpHeld = false;
            jumpInProgress = false;
            jumpReleaseApplied = false;
            coyoteTimeRemaining = 0f;
            SetJumpDescending(false);
        }

        // 양수 피해로 체력이 처음 0이 되면 이동을 멈추고 사망을 알린다.
        public void ApplyDamage(float damage)
        {
            if (IsDead || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

            if (IsDead) {
                SetJumpDescending(false);
                playerBody.linearVelocity = Vector2.zero;
                Died?.Invoke(this);
            }
        }

        // 미조합 조각과 활성 조합 및 남은 특수 사용 횟수를 저장용으로 복사한다.
        internal PlayerCheckpointState CaptureCheckpointState()
        {
            return new PlayerCheckpointState {
                CollectedA = typeACount,
                CollectedB = typeBCount,
                CollectedC = typeCCount,
                ActiveA = ActiveCombination.ACount,
                ActiveB = ActiveCombination.BCount,
                ActiveC = ActiveCombination.CCount,
                SpecialUsesRemaining = SpecialUsesRemaining
            };
        }

        // 저장 상태가 게임의 조각 조합과 특수 횟수 규칙에 맞는지 확인한다.
        internal static bool IsValidCheckpointState(PlayerCheckpointState state)
        {
            if (state == null || state.CollectedA < 0 || state.CollectedB < 0 || state.CollectedC < 0
                || state.ActiveA < 0 || state.ActiveB < 0 || state.ActiveC < 0
                || state.SpecialUsesRemaining < 0 || state.SpecialUsesRemaining > SpecialUseCount) {
                return false;
            }

            long collected = (long)state.CollectedA + state.CollectedB + state.CollectedC;
            long active = (long)state.ActiveA + state.ActiveB + state.ActiveC;
            return state.SpecialUsesRemaining == 0
                ? active == 0 && collected < RequiredFragmentCount
                : active == RequiredFragmentCount && collected == 0;
        }

        // 새 씬의 플레이어를 배치하고 조각과 특수 횟수 및 지정된 체력을 복원한다.
        internal void RestoreCheckpointState(PlayerCheckpointState state, Vector3 position, float? currentHealth = null)
        {
            typeACount = state.CollectedA;
            typeBCount = state.CollectedB;
            typeCCount = state.CollectedC;
            ActiveCombination = new FragmentCombination(state.ActiveA, state.ActiveB, state.ActiveC);
            SpecialUsesRemaining = state.SpecialUsesRemaining;
            CurrentHealth = Mathf.Max(0f, Mathf.Min(currentHealth ?? maxHealth, maxHealth));
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
