using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    public enum FragmentSpecialType
    {
        HeavyStrike,
        DashThrust,
        WideSlash,
        PiercingWave,
        ExplosiveThrust,
        SpinningSlash,
        FocusedShot,
        ExplosiveShot,
        SpreadingShot,
        RadialWave,
        ExtendingBlade,
        ShardSlash,
        PiercingSpear,
        FiveShot,
        Buckshot
    }

    public sealed class FragmentSpecialEffect : ISpecialEffect
    {
        public FragmentSpecialType Type { get; }
        public float DamageMultiplier { get; set; }
        public float Range { get; set; }
        public float ArcAngle { get; set; } = 60f;
        public float DashDistance { get; set; } = 3f;
        public float DashDuration { get; set; } = 0.15f;
        public float ProjectileSpeed { get; set; } = 18f;
        public float ProjectileRadius { get; set; } = 0.25f;
        public int ProjectileCount { get; set; } = 1;
        public float ProjectileSpreadAngle { get; set; } = 10f;
        public bool PierceEnemies { get; set; }
        public float ExplosionRadius { get; set; }
        public float ExplosionDamageMultiplier { get; set; }
        public float BladeWidth { get; set; } = 0.55f;
        public float TipStartFraction { get; set; } = 0.75f;
        public float TipDamageMultiplier { get; set; } = 1.5f;
        public float ShardRange { get; set; } = 3f;
        public float ShardArcAngle { get; set; } = 90f;
        public float ShardDamageMultiplier { get; set; } = 2f;

        // 조합 효과의 기본 피해량과 사거리 및 공격 형태를 준비한다.
        public FragmentSpecialEffect(FragmentSpecialType type)
        {
            Type = type;

            switch (type) {
                case FragmentSpecialType.HeavyStrike:
                    DamageMultiplier = 6f;
                    Range = 1.8f;
                    break;
                case FragmentSpecialType.DashThrust:
                    DamageMultiplier = 3f;
                    Range = 1.6f;
                    break;
                case FragmentSpecialType.WideSlash:
                    DamageMultiplier = 3f;
                    Range = 3.2f;
                    ArcAngle = 120f;
                    break;
                case FragmentSpecialType.PiercingWave:
                    DamageMultiplier = 3f;
                    Range = 14f;
                    PierceEnemies = true;
                    break;
                case FragmentSpecialType.ExplosiveThrust:
                    DamageMultiplier = 2.5f;
                    Range = 1.6f;
                    DashDistance = 2.5f;
                    ExplosionRadius = 1.5f;
                    ExplosionDamageMultiplier = 2f;
                    break;
                case FragmentSpecialType.SpinningSlash:
                    DamageMultiplier = 3f;
                    Range = 2.5f;
                    ArcAngle = 360f;
                    break;
                case FragmentSpecialType.FocusedShot:
                    DamageMultiplier = 5f;
                    Range = 20f;
                    ProjectileSpeed = 24f;
                    break;
                case FragmentSpecialType.ExplosiveShot:
                    DamageMultiplier = 3f;
                    Range = 16f;
                    ExplosionRadius = 1.8f;
                    ExplosionDamageMultiplier = 3f;
                    break;
                case FragmentSpecialType.SpreadingShot:
                    DamageMultiplier = 2.5f;
                    Range = 16f;
                    ProjectileSpeed = 16f;
                    ExplosionRadius = 3.5f;
                    ExplosionDamageMultiplier = 2.5f;
                    break;
                case FragmentSpecialType.RadialWave:
                    DamageMultiplier = 2.5f;
                    Range = 6f;
                    ArcAngle = 360f;
                    break;
                case FragmentSpecialType.ExtendingBlade:
                    DamageMultiplier = 3f;
                    Range = 4f;
                    break;
                case FragmentSpecialType.ShardSlash:
                    DamageMultiplier = 2.5f;
                    Range = 1.8f;
                    ArcAngle = 90f;
                    break;
                case FragmentSpecialType.PiercingSpear:
                    DamageMultiplier = 2.5f;
                    Range = 18f;
                    ProjectileSpeed = 32f;
                    PierceEnemies = true;
                    break;
                case FragmentSpecialType.FiveShot:
                    DamageMultiplier = 1f;
                    Range = 20f;
                    ProjectileSpeed = 24f;
                    ProjectileCount = 5;
                    ProjectileSpreadAngle = 5f;
                    break;
                case FragmentSpecialType.Buckshot:
                    DamageMultiplier = 1f;
                    Range = 16f;
                    ProjectileSpeed = 16f;
                    ProjectileCount = 3;
                    ProjectileSpreadAngle = 5f;
                    break;
            }
        }

        // 조각의 순서와 무관하게 A, B, C 개수로 기본 효과 유형을 선택한다.
        private static FragmentSpecialType? GetDefaultType(FragmentCombination combination)
        {
            switch (combination.ACount, combination.BCount, combination.CCount) {
                case (3, 0, 0): return FragmentSpecialType.HeavyStrike;
                case (2, 1, 0): return FragmentSpecialType.ExtendingBlade;
                case (2, 0, 1): return FragmentSpecialType.WideSlash;
                case (1, 2, 0): return FragmentSpecialType.PiercingWave;
                case (1, 1, 1): return FragmentSpecialType.PiercingSpear;
                case (1, 0, 2): return FragmentSpecialType.SpinningSlash;
                case (0, 3, 0): return FragmentSpecialType.FiveShot;
                case (0, 2, 1): return FragmentSpecialType.ExplosiveShot;
                case (0, 1, 2): return FragmentSpecialType.Buckshot;
                case (0, 0, 3): return FragmentSpecialType.RadialWave;
                default: return null;
            }
        }

        // 조합에 해당하는 기본 공격의 새 시전 인스턴스를 만든다.
        public static FragmentSpecialEffect Create(FragmentCombination combination)
        {
            FragmentSpecialType? type = GetDefaultType(combination);
            return type.HasValue ? new FragmentSpecialEffect(type.Value) : null;
        }

        // 조합에 해당하는 기본 공격의 표시 이름을 반환한다.
        public static string GetDefaultName(FragmentCombination combination)
        {
            FragmentSpecialType? type = GetDefaultType(combination);
            return type.HasValue ? GetDisplayName(type.Value) : "";
        }

        // 조합 효과의 유형을 HUD에 표시할 이름으로 바꾼다.
        public static string GetDisplayName(FragmentSpecialType type)
        {
            return type switch {
                FragmentSpecialType.HeavyStrike => "Heavy Strike",
                FragmentSpecialType.DashThrust => "Dash Thrust",
                FragmentSpecialType.WideSlash => "Wide Slash",
                FragmentSpecialType.PiercingWave => "Piercing Wave",
                FragmentSpecialType.ExplosiveThrust => "Explosive Thrust",
                FragmentSpecialType.SpinningSlash => "Spinning Slash",
                FragmentSpecialType.FocusedShot => "Focused Shot",
                FragmentSpecialType.ExplosiveShot => "Explosive Shot",
                FragmentSpecialType.SpreadingShot => "Spreading Shot",
                FragmentSpecialType.RadialWave => "Radial Wave",
                FragmentSpecialType.ExtendingBlade => "Extending Blade",
                FragmentSpecialType.ShardSlash => "Shard Slash",
                FragmentSpecialType.PiercingSpear => "Piercing Spear",
                FragmentSpecialType.FiveShot => "Five Shot",
                FragmentSpecialType.Buckshot => "Buckshot",
                _ => ""
            };
        }

        // 장비 보정이 적용된 수치로 조합에 해당하는 공격을 실행한다.
        public bool TryExecute(in SpecialContext context)
        {
            if (context.Caster == null || !context.Caster.isActiveAndEnabled || context.Caster.IsDead
                || !IsPositiveFinite(DamageMultiplier) || !IsPositiveFinite(Range)
                || !IsPositiveFinite(context.Caster.AttackPower * DamageMultiplier)
                || !IsPositiveFinite(ArcAngle) || ArcAngle > 360f
                || ExplosionRadius < 0f || !float.IsFinite(ExplosionRadius)
                || (ExplosionRadius > 0f && (!IsPositiveFinite(ExplosionDamageMultiplier)
                    || !IsPositiveFinite(context.Caster.AttackPower * ExplosionDamageMultiplier)))) {
                return false;
            }

            switch (Type) {
                case FragmentSpecialType.ExtendingBlade:
                    return ExtendBlade(context);
                case FragmentSpecialType.ShardSlash:
                    return SlashWithShards(context);
                case FragmentSpecialType.DashThrust:
                case FragmentSpecialType.ExplosiveThrust:
                    return IsPositiveFinite(DashDistance) && IsPositiveFinite(DashDuration)
                        && IsPositiveFinite(DashDistance / DashDuration)
                        && context.Caster.TryBeginSpecialDash(context, this);
                case FragmentSpecialType.PiercingWave:
                case FragmentSpecialType.FocusedShot:
                case FragmentSpecialType.ExplosiveShot:
                case FragmentSpecialType.SpreadingShot:
                case FragmentSpecialType.PiercingSpear:
                case FragmentSpecialType.FiveShot:
                case FragmentSpecialType.Buckshot:
                    return FireProjectiles(context);
                default:
                    SpecialEffectVisual.ShowArea(context, Type, context.Origin, Range, ArcAngle, false);
                    new SpecialHitQuery(context).HitArea(context.Origin, Range, ArcAngle,
                        context.Caster.AttackPower * DamageMultiplier, Type == FragmentSpecialType.HeavyStrike, out _);
                    return true;
            }
        }

        // 제자리에서 칼날을 뻗어 첫 적을 찌르고 칼끝 구간의 명중에는 추가 피해를 준다.
        private bool ExtendBlade(in SpecialContext context)
        {
            float damage = context.Caster.AttackPower * DamageMultiplier;
            float tipDamage = damage * TipDamageMultiplier;

            if (!IsPositiveFinite(BladeWidth) || !IsPositiveFinite(TipStartFraction) || TipStartFraction > 1f
                || TipDamageMultiplier < 1f || !IsPositiveFinite(tipDamage)) {
                return false;
            }

            float tipStart = Range * TipStartFraction;
            var hits = new SpecialHitQuery(context);
            float length = hits.HitFirstAlongBlade(Range, BladeWidth, damage, tipStart, tipDamage);
            SpecialEffectVisual.ShowBlade(context, length, BladeWidth, tipStart);
            return true;
        }

        // 가까운 적 한 명을 벤 뒤 명중점의 전방으로 파편 피해를 퍼뜨린다.
        private bool SlashWithShards(in SpecialContext context)
        {
            float shardDamage = context.Caster.AttackPower * ShardDamageMultiplier;

            if (!IsPositiveFinite(ShardRange) || !IsPositiveFinite(ShardArcAngle) || ShardArcAngle > 180f
                || !IsPositiveFinite(shardDamage)) {
                return false;
            }

            var hits = new SpecialHitQuery(context);
            SpecialEffectVisual.ShowArea(context, Type, context.Origin, Range, ArcAngle, false);
            float damage = context.Caster.AttackPower * DamageMultiplier;

            if (hits.HitArea(context.Origin, Range, ArcAngle, damage, true, out Vector2 impact)) {
                SpecialEffectVisual.ShowArea(context, Type, impact, ShardRange, ShardArcAngle, true);
                hits.HitArea(impact, ShardRange, ShardArcAngle, shardDamage, false, out _, forwardOnly: true);
            }

            return true;
        }

        // 시전 방향을 중심으로 보정된 개수와 간격에 맞춰 투사체를 생성한다.
        private bool FireProjectiles(in SpecialContext context)
        {
            if (!IsPositiveFinite(ProjectileSpeed) || !IsPositiveFinite(ProjectileRadius)
                || ProjectileCount < 1 || !float.IsFinite(ProjectileSpreadAngle)) {
                return false;
            }

            Vector2 forward = context.AttackDirection;
            Vector2 side = Vector2.Perpendicular(forward);

            for (int index = 0; index < ProjectileCount; index++) {
                float angle = (index - (ProjectileCount - 1) * 0.5f) * ProjectileSpreadAngle * Mathf.Deg2Rad;
                Vector2 direction = forward * Mathf.Cos(angle) + side * Mathf.Sin(angle);
                var instance = new GameObject($"Special {Type}");
                SceneManager.MoveGameObjectToScene(instance, context.Caster.gameObject.scene);
                instance.transform.position = context.Origin;
                instance.AddComponent<SpecialProjectile>().Initialize(context, this, direction);
            }

            return true;
        }

        // 물리 쿼리에 사용할 양수 유한 수치인지 확인한다.
        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && float.IsFinite(value);
        }
    }
}
