using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    sealed class SpecialProjectile : MonoBehaviour
    {
        private readonly List<RaycastHit2D> hits = new();
        private SpecialContext context;
        private SpecialHitQuery hitQuery;
        private Vector2 direction;
        private float speed;
        private float radius;
        private float remainingRange;
        private float damage;
        private float explosionRadius;
        private float explosionDamage;
        private bool pierces;
        private bool active;
        private FragmentSpecialType type;

        // 보정된 시전 수치를 복사하고 투사체의 한 방향 비행을 시작한다.
        public void Initialize(in SpecialContext context, FragmentSpecialEffect effect, Vector2 direction)
        {
            this.context = context;
            this.direction = direction;
            hitQuery = new SpecialHitQuery(context);
            speed = effect.ProjectileSpeed;
            radius = effect.ProjectileRadius;
            remainingRange = effect.Range;
            damage = context.Caster.AttackPower * effect.DamageMultiplier;
            explosionRadius = effect.ExplosionRadius;
            explosionDamage = context.Caster.AttackPower * effect.ExplosionDamageMultiplier;
            pierces = effect.PierceEnemies;
            type = effect.Type;
            SpecialEffectVisual.AttachProjectile(transform, type, direction, radius);
            active = true;
        }

        // 이동 경로의 적과 지형을 거리순으로 검사해 관통이나 폭발을 처리한다.
        private void FixedUpdate()
        {
            if (!active) {
                return;
            }

            if (context.Caster == null || !context.Caster.isActiveAndEnabled || context.Caster.IsDead) {
                Remove();
                return;
            }

            float distance = Mathf.Min(speed * Time.fixedDeltaTime, remainingRange);
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(context.Caster.SpecialAttackLayers.value | context.Caster.SpecialEnvironmentLayers.value);
            context.Caster.gameObject.scene.GetPhysicsScene2D().CircleCast(transform.position, radius, direction, distance, filter, hits);
            hits.Sort((left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit2D hit in hits) {
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                bool targetLayer = (context.Caster.SpecialAttackLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

                if (targetLayer && hitQuery.CanHit(enemy)) {
                    if (explosionRadius > 0f) {
                        Explode(hit.point);
                        Remove();
                        return;
                    }

                    if (hitQuery.HitEnemy(enemy, damage)) {
                        SpecialEffectVisual.ShowArea(context, type, hit.point, radius * 2f, 360f, true);

                        if (type == FragmentSpecialType.PiercingSpear) {
                            var scene = context.Caster.gameObject.scene;
                            Vector3 dropPosition = FragmentDrop.FindFloor(hit.point, scene);
                            FragmentDrop.Spawn(null, CollectibleType.A, dropPosition, scene);
                        }

                        if (!pierces) {
                            Remove();
                            return;
                        }
                    }
                }

                if (EnemyDash.IsTerrain(hit.collider, context.Caster.SpecialEnvironmentLayers)) {
                    Explode((Vector2)transform.position + direction * Mathf.Max(0f, hit.distance - 0.01f));
                    Remove();
                    return;
                }
            }

            transform.position += (Vector3)(direction * distance);
            remainingRange -= distance;

            if (remainingRange <= 0f) {
                Remove();
            }
        }

        // 폭발형 투사체의 명중 지점에서 주위 적에게 한 번씩 피해를 준다.
        private void Explode(Vector2 center)
        {
            if (explosionRadius > 0f) {
                SpecialEffectVisual.ShowArea(context, type, center, explosionRadius, 360f, true);
                new SpecialHitQuery(context).HitArea(center, explosionRadius, 360f, explosionDamage, false, out _);
            }
        }

        // 투사체의 판정을 즉시 종료하고 런타임 오브젝트를 제거한다.
        private void Remove()
        {
            active = false;
            Destroy(gameObject);
        }
    }
}
