using UnityEngine;

namespace Mech.Function
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class MeleeEnemy : TargetingEnemy
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3f;

        private Rigidbody2D body;

        // 기본 체력을 초기화하고 이동에 사용할 물리 바디를 기억한다.
        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody2D>();
        }

        // 공격 동작 없이 플레이어의 좌우 위치를 계속 추적한다.
        private void FixedUpdate()
        {
            PlayerBase player = CanAct ? GetTargetPlayer() : null;
            float horizontalSpeed = 0f;

            if (player != null) {
                float offset = player.transform.position.x - body.position.x;
                horizontalSpeed = Mathf.Clamp(offset / Time.fixedDeltaTime, -moveSpeed, moveSpeed);
            }

            body.linearVelocityX = horizontalSpeed;

            if (CanAct) {
                TryChangeState(player != null ? EnemyState.Chase : EnemyState.Idle);
            }
        }

        // 컴포넌트가 꺼지면 남은 수평 이동을 정지한다.
        private void OnDisable()
        {
            body.linearVelocityX = 0f;
        }
    }
}
