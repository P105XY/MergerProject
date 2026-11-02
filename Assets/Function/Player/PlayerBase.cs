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
        [SerializeField, Min(0f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float attackPower = 10f;
        [SerializeField, Min(0f)] private float attacksPerSecond = 1f;
        [SerializeField, Min(0f)] private float movementSpeed = 5f;
        [SerializeField, Min(0f)] private float jumpSpeed = 8f;
        [SerializeField, Min(0f)] private float attackReach = 1.1f;
        [SerializeField] private Vector2 attackSize = new Vector2(1.2f, 1f);
        [SerializeField] private LayerMask attackLayers = ~0;
        [SerializeField] private LayerMask groundLayers = ~0;
        private const int RequiredFragmentCount = 3;
        private const int SpecialUseCount = 3;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
        private Rigidbody2D playerBody;
        private float movementInput;
        private float nextAttackTime;
        private bool jumpRequested;
        private int facingDirection = 1;
        private readonly List<Collider2D> attackHits = new List<Collider2D>();
        private readonly HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();
        private int typeACount;
        private int typeBCount;
        private int typeCCount;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
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
        public event Action<PlayerBase> SpecialRequested;
        public event Action<PlayerBase, FragmentCombination> SpecialUsed;

        // 플레이어의 물리 컴포넌트와 초기 체력을 준비한다.
        protected virtual void Awake()
        {
            playerBody = GetComponent<Rigidbody2D>();
            CurrentHealth = Mathf.Max(0f, maxHealth);
        }

        // 키보드 입력을 읽고 공격과 특수 기능 요청을 전달한다.
        protected virtual void Update()
        {
            Keyboard keyboard = Keyboard.current;
            movementInput = 0f;

            if (keyboard == null) {
                return;
            }

            movementInput = (keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.leftArrowKey.isPressed ? 1f : 0f);

            if (movementInput != 0f) {
                facingDirection = movementInput > 0f ? 1 : -1;
            }

            if (keyboard.aKey.isPressed && attacksPerSecond > 0f && Time.time >= nextAttackTime) {
                nextAttackTime = Time.time + 1f / attacksPerSecond;
                Attack();
                AttackRequested?.Invoke(this);
            }

            if (keyboard.sKey.wasPressedThisFrame) {
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
                    enemy.ApplyDamage(attackPower);
                }
            }
        }

        // 겹친 조각을 수집하고 이미 사용된 조각은 무시한다.
        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            FragmentPickup fragment = other.GetComponentInParent<FragmentPickup>();

            if (fragment != null) {
                fragment.TryCollect(this);
            }
        }

        // 조각을 종류별로 모으고 세 개가 되면 조합을 준비한다.
        public bool TryCollectFragment(CollectibleType fragmentType)
        {
            if (IsSpecialAvailable) {
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

        // 준비된 조합의 특수 기능을 한 번 사용하고 남은 횟수를 줄인다.
        public bool TryUseSpecial()
        {
            if (!IsSpecialAvailable) {
                return false;
            }

            FragmentCombination combination = ActiveCombination;
            SpecialUsesRemaining--;

            if (SpecialUsesRemaining == 0) {
                ActiveCombination = default;
            }

            SpecialUsed?.Invoke(this, combination);
            SpecialRequested?.Invoke(this);
            return true;
        }

        // 물리 갱신에서 좌우 이동과 지상 점프를 적용한다.
        protected virtual void FixedUpdate()
        {
            Vector2 velocity = playerBody.linearVelocity;
            velocity.x = movementInput * Mathf.Max(0f, movementSpeed);

            if (jumpRequested && IsGrounded()) {
                velocity.y = Mathf.Max(0f, jumpSpeed);
            }

            jumpRequested = false;
            playerBody.linearVelocity = velocity;
        }

        // 비활성화할 때 남은 이동과 점프 입력을 지운다.
        protected virtual void OnDisable()
        {
            movementInput = 0f;
            jumpRequested = false;
        }

        // 양수 피해를 적용하고 현재 체력을 0 이상으로 유지한다.
        public void ApplyDamage(float damage)
        {
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
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
