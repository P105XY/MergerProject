using System;
using UnityEngine;

namespace Mech.Function
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Groggy,
        Dead,
        Windup
    }

    public enum CollectibleType
    {
        A,
        B,
        C
    }

    public class EnemyBase : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float groggyValue = 0f;
        [SerializeField, Min(0f)] private float groggyResistance = 0f;
        [SerializeField, Min(0f)] private float attackPower = 10f;
        [SerializeField, Min(0f)] private float attackSpeed = 1f;
        [SerializeField] private CollectibleType heldObjectType = CollectibleType.A;
        [SerializeField] private GameObject heldObject = null;
        private EnemyWhiteFlash whiteFlash;

        public float MaxHealth
        {
            get => maxHealth;
            protected set => maxHealth = value;
        }
        public float CurrentHealth { get; private set; }
        public float GroggyValue => groggyValue;
        public float GroggyResistance => groggyResistance;
        public float AttackPower => attackPower;
        public float AttackSpeed => attackSpeed;
        public CollectibleType HeldObjectType => heldObjectType;
        public GameObject HeldObject => heldObject;
        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
        public bool IsDefeated { get; private set; }
        internal bool IsWhiteFlashing => whiteFlash != null && whiteFlash.IsFlashing;

        public event Action<EnemyState, EnemyState> StateChanged;
        public event Action<EnemyBase, CollectibleType, GameObject> FragmentDropped;
        public event Action<EnemyBase> Defeated;

        // 적의 현재 체력을 최대 체력으로 초기화한다.
        protected virtual void Awake()
        {
            CurrentHealth = Mathf.Max(0f, maxHealth);
        }

        // 양수 피해를 적용하고 현재 체력을 0 이상으로 유지한다.
        public void ApplyDamage(float damage)
        {
            ApplyDamage(damage, false);
        }

        // 공격 주체가 생략된 플레이어 피해에 적의 피격 연출을 적용한다.
        public void ApplyPlayerDamage(float damage)
        {
            ApplyPlayerDamage(damage, null);
        }

        // 플레이어 피해를 적용하고 공격자와 적에 명중 연출을 연결한다.
        public void ApplyPlayerDamage(float damage, PlayerBase attacker)
        {
            ApplyDamage(damage, true, attacker);
        }

        // 유효한 피해로 체력을 줄이고 플레이어 공격이면 명중 연출을 시작한다.
        private void ApplyDamage(float damage, bool fromPlayer, PlayerBase attacker = null)
        {
            if (IsDefeated || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

            if (fromPlayer && isActiveAndEnabled) {
                if (whiteFlash == null) {
                    whiteFlash = gameObject.AddComponent<EnemyWhiteFlash>();
                }

                whiteFlash.Play(this);
                GameHitStop.Play(this);
                GameHitStop.Play(attacker);
                AttackCameraShake.Play();
            }

            if (CurrentHealth <= 0f) {
                Defeat();
            }
        }

        // 적을 한 번만 처치 처리하고 지정된 조각을 생성한다.
        private void Defeat()
        {
            IsDefeated = true;
            GameObject droppedFragment = null;

            if (heldObject != null) {
                droppedFragment = Instantiate(heldObject, transform.position, Quaternion.identity);
                FragmentPickup pickup = droppedFragment.GetComponent<FragmentPickup>();

                if (pickup == null) {
                    pickup = droppedFragment.AddComponent<FragmentPickup>();
                }

                pickup.Initialize(heldObjectType);
            }

            if (heldObject == null) {
                Debug.LogWarning("적에게 드롭할 조각이 지정되지 않았습니다.", this);
            }

            TryChangeState(EnemyState.Dead);

            if (droppedFragment != null) {
                FragmentDropped?.Invoke(this, heldObjectType, droppedFragment);
            }

            Defeated?.Invoke(this);
        }

        // 유효한 새 상태로 전환하고 이전 상태와 새 상태를 알린다.
        public bool TryChangeState(EnemyState nextState)
        {
            if (!Enum.IsDefined(typeof(EnemyState), nextState) || CurrentState == nextState || (IsDefeated && nextState != EnemyState.Dead)) {
                return false;
            }

            EnemyState previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, nextState);
            return true;
        }
    }
}
