using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    sealed class SpecialHitQuery
    {
        private readonly SpecialContext context;
        private readonly HashSet<EnemyBase> hitEnemies = new();
        private readonly List<Collider2D> overlaps = new();
        private readonly List<RaycastHit2D> sightHits = new();

        // 한 번의 타격에서 공유할 시전자와 적별 중복 타격 기록을 준비한다.
        public SpecialHitQuery(in SpecialContext context)
        {
            this.context = context;
        }

        // 현재 시전으로 아직 타격하지 않은 살아 있는 적인지 확인한다.
        public bool CanHit(EnemyBase enemy)
        {
            return enemy != null && enemy.isActiveAndEnabled && !enemy.IsDefeated
                && enemy.CurrentState != EnemyState.Dead && !hitEnemies.Contains(enemy);
        }

        // 같은 적의 여러 충돌체에 중복 피해를 주지 않고 플레이어 명중 연출을 적용한다.
        public bool HitEnemy(EnemyBase enemy, float damage)
        {
            if (context.Caster == null || !context.Caster.isActiveAndEnabled || context.Caster.IsDead || !CanHit(enemy)) {
                return false;
            }

            hitEnemies.Add(enemy);
            float previousHealth = enemy.CurrentHealth;
            enemy.ApplyPlayerDamage(damage, context.Caster);
            return enemy.CurrentHealth < previousHealth;
        }

        // 칼날의 첫 지형 및 대상까지의 거리를 비교해 첫 타격과 칼끝 피해를 판정한다.
        public float HitFirstAlongBlade(float range, float width, float damage, float tipStart, float tipDamage)
        {
            const float castHalfThickness = 0.01f;
            Vector2 direction = context.AttackDirection;
            LayerMask attackLayers = context.Caster.SpecialAttackLayers;
            LayerMask terrainLayers = context.Caster.SpecialEnvironmentLayers;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(attackLayers.value | terrainLayers.value);
            var size = new Vector2(castHalfThickness * 2f, width);
            Vector2 start = context.Origin - direction * castHalfThickness;
            context.Caster.gameObject.scene.GetPhysicsScene2D().BoxCast(start, size, context.AttackAngle, direction, range, filter, sightHits);
            EnemyBase firstEnemy = null;
            float firstEnemyDistance = float.PositiveInfinity;
            float firstWallDistance = float.PositiveInfinity;

            foreach (RaycastHit2D hit in sightHits) {
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                bool targetLayer = (attackLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

                if (targetLayer && hit.distance < firstEnemyDistance && CanHit(enemy)) {
                    firstEnemy = enemy;
                    firstEnemyDistance = hit.distance;
                } else if (EnemyDash.IsTerrain(hit.collider, terrainLayers)) {
                    firstWallDistance = Mathf.Min(firstWallDistance, hit.distance);
                }
            }

            if (firstEnemy != null && firstEnemyDistance < firstWallDistance) {
                HitEnemy(firstEnemy, firstEnemyDistance >= tipStart ? tipDamage : damage);
                return firstEnemyDistance;
            }

            return Mathf.Min(range, firstWallDistance);
        }

        // 부채꼴이나 원 범위에서 벽으로 가려지지 않은 적에게 가까운 순서로 피해를 준다.
        public bool HitArea(Vector2 center, float radius, float arcAngle, float damage,
            bool singleTarget, out Vector2 impact, bool forwardOnly = false)
        {
            impact = center;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(context.Caster.SpecialAttackLayers);
            context.Caster.gameObject.scene.GetPhysicsScene2D().OverlapCircle(center, radius, filter, overlaps);
            overlaps.Sort((left, right) => (left.ClosestPoint(center) - center).sqrMagnitude
                .CompareTo((right.ClosestPoint(center) - center).sqrMagnitude));
            bool hitAny = false;

            foreach (Collider2D collider in overlaps) {
                EnemyBase enemy = collider.GetComponentInParent<EnemyBase>();

                if (!CanHit(enemy)) {
                    continue;
                }

                Vector2 point = collider.ClosestPoint(center);
                Vector2 offset = point - center;

                if ((forwardOnly && Vector2.Dot(offset, context.AttackDirection) <= 0f)
                    || (arcAngle < 360f && Vector2.Angle(context.AttackDirection, offset) > arcAngle * 0.5f)
                    || !HasLineOfSight(center, point) || !HitEnemy(enemy, damage)) {
                    continue;
                }

                if (!hitAny) {
                    impact = point;
                }

                hitAny = true;

                if (singleTarget) {
                    break;
                }
            }

            return hitAny;
        }

        // 효과 중심과 적 사이의 고체 지형을 검사해 벽 너머 타격을 차단한다.
        private bool HasLineOfSight(Vector2 origin, Vector2 point)
        {
            Vector2 offset = point - origin;
            float distance = offset.magnitude;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(context.Caster.SpecialEnvironmentLayers);
            context.Caster.gameObject.scene.GetPhysicsScene2D().Raycast(origin, offset.normalized, distance, filter, sightHits);

            foreach (RaycastHit2D hit in sightHits) {
                if (EnemyDash.IsTerrain(hit.collider, context.Caster.SpecialEnvironmentLayers) && hit.distance < distance - 0.01f) {
                    return false;
                }
            }

            return true;
        }
    }
}
