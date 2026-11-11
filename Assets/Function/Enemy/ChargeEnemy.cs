using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class ChargeEnemy : TargetingEnemy
    {
        [SerializeField, Min(0f)] private float detectionRange = 12f;
        [SerializeField, Min(0.01f)] private float chargeInterval = 5f;
        [SerializeField, Min(0f)] private float windupDuration = 0.5f;
        [SerializeField, Min(0f)] private float chargeDistance = 300f;
        [SerializeField, Min(0.01f)] private float chargeDuration = 1f;

        private const float WallClearance = 0.01f;
        private const double PhysicsTimeTolerance = 0.0001;
        private readonly List<RaycastHit2D> chargeHits = new List<RaycastHit2D>();
        private Rigidbody2D body;
        private PlayerBase detectedPlayer;
        private double nextWindupTime;
        private double launchTime;
        private float elapsedChargeTime;
        private Vector2 chargeDirection;

        // 돌진 적이 탐지 전까지 고정 높이에서 움직이지 않도록 바디를 준비한다.
        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        // 플레이어를 탐지하고 준비·돌진·재대기 순서로 상태를 진행한다.
        private void FixedUpdate()
        {
            if (!CanAct) {
                ResetCharge();
                return;
            }

            if (CurrentState == EnemyState.Attack && chargeDirection != Vector2.zero) {
                MoveCharge();
                return;
            }

            PlayerBase player = GetTargetPlayer();

            if (player != detectedPlayer) {
                ResetCharge();
                TryChangeState(EnemyState.Idle);
            }

            if (player == null) {
                return;
            }

            Vector2 offset = (Vector2)player.transform.position - body.position;

            if (detectedPlayer == null) {
                if (offset.sqrMagnitude > detectionRange * detectionRange) {
                    return;
                }

                detectedPlayer = player;
            }

            double now = Time.fixedTimeAsDouble;

            if (CurrentState == EnemyState.Windup) {
                if (now + PhysicsTimeTolerance < launchTime) {
                    return;
                }

                chargeDirection = offset.x < 0f ? Vector2.left : Vector2.right;
                elapsedChargeTime = 0f;
                TryChangeState(EnemyState.Attack);
                MoveCharge();
            } else if (now + PhysicsTimeTolerance >= nextWindupTime) {
                launchTime = now + windupDuration;
                nextWindupTime = now + Mathf.Max(chargeInterval, windupDuration + chargeDuration);
                TryChangeState(EnemyState.Windup);
            }
        }

        // 돌진 경로에 있는 플레이어에게 접촉 피해를 주고 벽 앞에서 정지한다.
        private void MoveCharge()
        {
            float duration = Mathf.Max(0.01f, chargeDuration);
            float nextElapsedTime = Mathf.Min(duration, elapsedChargeTime + Time.fixedDeltaTime);

            if (duration - nextElapsedTime < PhysicsTimeTolerance) {
                nextElapsedTime = duration;
            }

            float distance = chargeDistance * (nextElapsedTime - elapsedChargeTime) / duration;
            elapsedChargeTime = nextElapsedTime;
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            bool hitWall = false;

            if (distance > 0f) {
                body.Cast(chargeDirection, filter, chargeHits, distance);

                foreach (RaycastHit2D hit in chargeHits) {
                    Collider2D collider = hit.collider;
                    PlayerBase player = collider.GetComponentInParent<PlayerBase>();

                    if (player != null) {
                        player.TryReceiveContactDamage(AttackPower);
                        continue;
                    }

                    if (!collider.isTrigger && Vector2.Dot(hit.normal, chargeDirection) < -0.01f) {
                        distance = Mathf.Max(0f, hit.distance - WallClearance);
                        hitWall = true;
                        break;
                    }
                }

                body.position += chargeDirection * distance;
            }

            if (hitWall || elapsedChargeTime >= duration) {
                chargeDirection = Vector2.zero;
                TryChangeState(EnemyState.Idle);
            }
        }

        // 비활성화되면 탐지와 돌진을 초기화한다.
        private void OnDisable()
        {
            ResetCharge();

            if (CanAct) {
                TryChangeState(EnemyState.Idle);
            }
        }

        // 저장된 대상과 돌진 방향 및 다음 준비 시간을 초기화한다.
        private void ResetCharge()
        {
            detectedPlayer = null;
            chargeDirection = Vector2.zero;
            nextWindupTime = 0;
        }
    }
}
