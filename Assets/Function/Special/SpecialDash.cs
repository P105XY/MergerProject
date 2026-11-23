using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    sealed class SpecialDash : MonoBehaviour
    {
        private readonly List<RaycastHit2D> hits = new();
        private SpecialContext context;
        private SpecialHitQuery hitQuery;
        private Rigidbody2D body;
        private float remainingDistance;
        private float speed;
        private float reach;
        private float arcAngle;
        private float damage;
        private float explosionRadius;
        private float explosionDamage;
        private FragmentSpecialType type;
        private SpecialEffectVisual visual;

        public bool IsRunning { get; private set; }

        // 진행 중인 돌진이 없으면 이번 찌르기의 이동과 피해 수치를 복사한다.
        public bool TryBegin(in SpecialContext context, FragmentSpecialEffect effect)
        {
            if (IsRunning) {
                return false;
            }

            this.context = context;
            body = GetComponent<Rigidbody2D>();
            hitQuery = new SpecialHitQuery(context);
            remainingDistance = effect.DashDistance;
            speed = effect.DashDistance / effect.DashDuration;
            reach = effect.Range;
            arcAngle = effect.ArcAngle;
            damage = context.Caster.AttackPower * effect.DamageMultiplier;
            explosionRadius = effect.ExplosionRadius;
            explosionDamage = context.Caster.AttackPower * effect.ExplosionDamageMultiplier;
            type = effect.Type;
            visual = SpecialEffectVisual.ShowArea(context, type, body.position, reach, arcAngle, false, followsDash: true);
            IsRunning = true;
            return true;
        }

        // 플레이어 물리 갱신 안에서 벽과 적을 지나치지 않도록 돌진 찌르기를 진행한다.
        public void Step(float deltaTime)
        {
            if (!IsRunning) {
                return;
            }

            if (hitQuery.HitArea(body.position, reach, arcAngle, damage, true, out Vector2 point)) {
                FinishHit(point);
                return;
            }

            Vector2 direction = context.AttackDirection;
            float distance = Mathf.Min(speed * deltaTime, remainingDistance);
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(context.Caster.SpecialAttackLayers.value | context.Caster.SpecialEnvironmentLayers.value);
            body.Cast(direction, filter, hits, distance + 0.01f);
            hits.Sort((left, right) => left.distance.CompareTo(right.distance));
            bool blocked = false;
            EnemyBase target = null;
            Vector2 impact = default;

            foreach (RaycastHit2D hit in hits) {
                if (Vector2.Dot(hit.normal, direction) >= -0.01f) {
                    continue;
                }

                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                bool targetLayer = (context.Caster.SpecialAttackLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

                if ((targetLayer && hitQuery.CanHit(enemy)) || EnemyDash.IsTerrain(hit.collider, context.Caster.SpecialEnvironmentLayers)) {
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.01f));
                    target = targetLayer && hitQuery.CanHit(enemy) ? enemy : null;
                    impact = hit.point;
                    blocked = true;
                    break;
                }
            }

            body.position += direction * distance;
            remainingDistance -= distance;

            if (visual != null) {
                visual.transform.position = body.position;
            }

            Physics2D.SyncTransforms();

            if (target != null && hitQuery.HitEnemy(target, damage)) {
                FinishHit(impact);
                return;
            }

            if (!blocked && hitQuery.HitArea(body.position, reach, arcAngle, damage, true, out point)) {
                FinishHit(point);
                return;
            }

            if (blocked || remainingDistance <= 0f) {
                Cancel();
            }
        }

        // 찌르기가 명중하면 지정된 반경으로 폭발 피해를 적용하고 돌진을 끝낸다.
        private void FinishHit(Vector2 point)
        {
            if (explosionRadius > 0f) {
                SpecialEffectVisual.ShowArea(context, type, point, explosionRadius, 360f, true);
                new SpecialHitQuery(context).HitArea(point, explosionRadius, 360f, explosionDamage, false, out _);
            }

            Cancel();
        }

        // 비활성화나 사망 또는 돌진 완료 시 남은 이동을 취소한다.
        public void Cancel()
        {
            IsRunning = false;

            if (visual != null) {
                visual.Fade(0.18f);
                visual = null;
            }
        }
    }
}
