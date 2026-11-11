using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float radius = 0.12f;
        [SerializeField, Min(0.01f)] private float lifetime = 4f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private Vector2 velocity;
        private float gravity;
        private float damage;
        private float timeRemaining;
        private bool isFlying;

        // 발사 방향과 속도, 피해량 및 선택적 낙하 가속도로 투사체를 초기화한다.
        internal void Initialize(Vector2 launchDirection, float launchSpeed, float attackDamage, float fallAcceleration = 0f)
        {
            velocity = launchDirection * launchSpeed;
            gravity = Mathf.Max(0f, fallAcceleration);
            damage = attackDamage;
            timeRemaining = lifetime;
            isFlying = true;
        }

        // 이번 물리 단계의 비행 경로를 검사해 첫 충돌 또는 수명 만료를 처리한다.
        private void FixedUpdate()
        {
            if (!isFlying) {
                return;
            }

            float elapsed = Mathf.Min(Time.fixedDeltaTime, timeRemaining);
            Vector2 movement = velocity * elapsed + Vector2.down * (0.5f * gravity * elapsed * elapsed);
            float distance = movement.magnitude;
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(hitLayers);
            gameObject.scene.GetPhysicsScene2D().CircleCast(transform.position, radius, movement.normalized, distance, filter, hits);

            foreach (RaycastHit2D hit in hits) {
                Collider2D collider = hit.collider;

                if (collider.transform.IsChildOf(transform) || collider.GetComponentInParent<EnemyBase>() != null) {
                    continue;
                }

                PlayerBase player = collider.GetComponentInParent<PlayerBase>();

                if (player != null) {
                    if (player.isActiveAndEnabled && player.CurrentHealth > 0f) {
                        player.ApplyDamage(damage);
                        RemoveProjectile();
                        return;
                    }

                    continue;
                }

                if (!collider.isTrigger) {
                    RemoveProjectile();
                    return;
                }
            }

            transform.position += (Vector3)movement;
            velocity.y -= gravity * elapsed;
            timeRemaining -= elapsed;

            if (timeRemaining <= 0f) {
                RemoveProjectile();
            }
        }

        // 투사체의 판정을 즉시 끝내고 오브젝트를 제거한다.
        private void RemoveProjectile()
        {
            isFlying = false;
            Destroy(gameObject);
        }
    }
}
