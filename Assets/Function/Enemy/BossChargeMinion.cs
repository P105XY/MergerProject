using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class BossChargeMinion : EnemyBase
    {
        [SerializeField, Min(0.01f)] private float moveSpeed = 12f;
        [SerializeField, Min(0.1f)] private float lifetime = 6f;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private Rigidbody2D body;
        private Vector2 direction;
        private LayerMask terrainLayers;
        private float remainingLife;

        // 발사 방향대로만 이동할 키네마틱 바디를 준비한다.
        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        // 생성 순간의 돌진 방향과 지형 레이어를 지정한다.
        public void Launch(Vector2 launchDirection, LayerMask collisionLayers)
        {
            direction = launchDirection;
            terrainLayers = collisionLayers;
            remainingLife = lifetime;
            TryChangeState(EnemyState.Attack);
            DropAttackFragment();
        }

        // 정해진 방향으로 진행하고 지형에 닿거나 수명이 끝나면 제거한다.
        private void FixedUpdate()
        {
            if (IsDefeated || direction == Vector2.zero) {
                return;
            }

            float step = Mathf.Min(Time.fixedDeltaTime, remainingLife);
            remainingLife -= step;

            if (EnemyDash.Step(body, direction, Mathf.Max(0.01f, moveSpeed) * step, terrainLayers, AttackPower, hits)
                || remainingLife <= 0f) {
                direction = Vector2.zero;
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}
