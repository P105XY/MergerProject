using System;
using System.Collections.Generic;
using System.Reflection;
using Mech.Function;
using UnityEngine;

namespace MergerSpecialChecks
{
    public static class SpecialAbilityChecks
    {
        private const BindingFlags PrivateField = BindingFlags.Instance | BindingFlags.NonPublic;

        // 플레이 모드에서 실제 PlayerBase와 임시 효과로 특수 시전 규칙을 검증한다.
        public static string[] Run()
        {
            var objects = new List<UnityEngine.Object>();
            var checks = new List<string>();

            // 임시 에셋을 만들고 검증이 끝난 뒤 정리할 목록에 추가한다.
            T Asset<T>() where T : ScriptableObject
            {
                var asset = ScriptableObject.CreateInstance<T>();
                objects.Add(asset);
                return asset;
            }

            // Inspector가 저장하는 설정 필드에 테스트 입력을 지정한다.
            void Set(object target, string field, object value)
            {
                target.GetType().GetField(field, PrivateField).SetValue(target, value);
            }

            // 입력과 기대값 및 실제값을 기록하고 결과가 일치하는지 확인한다.
            void Check<T>(string input, T expected, T actual)
            {
                string result = $"입력 {input} | 기대 {expected} | 실제 {actual}";
                Debug.Log("[SpecialAbility] " + result);

                if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
                    throw new InvalidOperationException(result);
                }

                checks.Add(result);
            }

            // 조합 개수와 기본 효과를 지정한 임시 레시피를 만든다.
            SpecialRecipe Recipe(int a, int b, SpecialEffectDefinition effect)
            {
                var recipe = Asset<SpecialRecipe>();
                Set(recipe, "aCount", a);
                Set(recipe, "bCount", b);
                Set(recipe, "baseEffect", effect);
                return recipe;
            }

            // 적용 조합과 교체 효과, 우선순위 및 보정을 가진 임시 장비를 만든다.
            SpecialEquipment Equipment(SpecialRecipe target, SpecialEffectDefinition effect, int priority,
                params SpecialModifierDefinition[] modifiers)
            {
                var item = Asset<SpecialEquipment>();
                Set(item, "targetRecipe", target);
                Set(item, "replacementEffect", effect);
                Set(item, "replacementPriority", priority);
                Set(item, "modifiers", modifiers);
                return item;
            }

            try {
                var host = new GameObject("SpecialAbilityChecks");
                objects.Add(host);
                host.transform.position = new Vector3(1000f, 20f, 0f);
                host.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                host.AddComponent<BoxCollider2D>();
                var player = host.AddComponent<PlayerBase>();
                var projectile = Asset<ProbeEffectDefinition>();
                projectile.Label = "Projectile";
                projectile.IsProjectile = true;
                var beam = Asset<ProbeEffectDefinition>();
                beam.Label = "Beam";
                var failed = Asset<ProbeEffectDefinition>();
                failed.Succeeds = false;
                var recipe = Recipe(2, 1, projectile);
                var otherRecipe = Recipe(1, 2, beam);
                var extraShot = Asset<ExtraShotProbe>();
                var addShot = Equipment(null, null, 0, extraShot);
                var lowerPriority = Equipment(null, projectile, 0);
                var higherPriority = Equipment(recipe, beam, 5);
                var equalPriority = Equipment(null, projectile, 5);
                var unrelated = Equipment(otherRecipe, projectile, 100);
                var rejectCast = Equipment(null, failed, 100);
                int usedEvents = 0;
                int requestedEvents = 0;
                int lastA = 0;
                bool nestedCast = true;
                player.SpecialUsed += (_, combo) => {
                    usedEvents++;
                    lastA = combo.ACount;
                    nestedCast = player.TryUseSpecial();
                };
                player.SpecialRequested += _ => requestedEvents++;

                // 입력은 조각 0개와 A→B→A이며, 준비 전 거부하고 수집 후 3회를 가진다.
                Check("준비 전 시전", false, player.TryUseSpecial());
                player.TryCollectFragment(CollectibleType.A);
                player.TryCollectFragment(CollectibleType.B);
                player.TryCollectFragment(CollectibleType.A);
                Check("A→B→A 수집", 3, player.SpecialUsesRemaining);

                // 일치하는 레시피가 없거나 효과가 실행을 거부하면 3회와 이벤트 0회를 유지한다.
                Set(player, "specialRecipes", new[] { otherRecipe });
                Check("다른 개수만 등록", false, player.TryUseSpecial());
                Set(player, "specialRecipes", new[] { otherRecipe, recipe });
                player.SetSpecialEquipment(rejectCast);
                Check("교체 효과 실행 거부", false, player.TryUseSpecial());
                Check("실패 뒤 남은 횟수", 3, player.SpecialUsesRemaining);
                Check("실패 뒤 성공 알림 합계", 0, usedEvents + requestedEvents);

                // 투사체 보정을 적용하면 2발이 되고, 다음 시전에서 장비를 빼면 기본 1발로 돌아간다.
                player.SetSpecialEquipment(addShot);
                Check("A2·B1 기본 효과 시전", true, player.TryUseSpecial());
                Check("+1발 장비", "Projectile:2", projectile.LastCast);
                Check("2발 시전의 남은 횟수", 2, player.SpecialUsesRemaining);
                Check("시전 당시 위치", (Vector2)host.transform.position, projectile.LastOrigin);
                Check("시전 당시 방향", 1, projectile.LastFacingDirection);
                Check("성공 이벤트 중 중첩 시전", false, nestedCast);
                player.SetSpecialEquipment();
                Check("장비 해제 후 시전", true, player.TryUseSpecial());
                Check("새 시전의 기본 수량", "Projectile:1", projectile.LastCast);

                // 적용 대상과 우선순위로 광선을 선택하고, 광선에 투사체 보정을 적용하지 않는다.
                player.SetSpecialEquipment(lowerPriority, higherPriority, equalPriority, unrelated, addShot);
                Check("높은 우선순위와 동점 장비 동시 장착", true, player.TryUseSpecial());
                Check("다른 조합 전용 우선순위 100은 제외, 동점은 앞의 광선", "Beam", beam.LastCast);
                Check("광선 시전 시 직전 투사체 기록 유지", "Projectile:1", projectile.LastCast);
                Check("세 번째 성공 뒤 남은 횟수", 0, player.SpecialUsesRemaining);
                Check("세 번째 성공 뒤 활성 조합 A", 0, player.ActiveCombination.ACount);
                Check("성공 알림에 전달한 조합 A", 2, lastA);
                Check("SpecialUsed 발행 횟수", 3, usedEvents);
                Check("SpecialRequested 발행 횟수", 3, requestedEvents);
                Check("소진 후 추가 시전", false, player.TryUseSpecial());
                return checks.ToArray();
            } finally {
                foreach (UnityEngine.Object instance in objects) {
                    UnityEngine.Object.Destroy(instance);
                }
            }
        }
    }

    public sealed class ProbeEffectDefinition : SpecialEffectDefinition
    {
        public string Label;
        public bool IsProjectile;
        public bool Succeeds = true;
        public string LastCast;
        public Vector2 LastOrigin;
        public int LastFacingDirection;

        // 투사체 또는 광선으로 동작하는 새 검증용 효과를 만든다.
        public override ISpecialEffect CreateCast(in SpecialContext context)
        {
            return IsProjectile ? new ProjectileProbe { Definition = this } : new ProbeCast { Definition = this };
        }
    }

    public class ProbeCast : ISpecialEffect
    {
        public ProbeEffectDefinition Definition;
        protected virtual string Result => Definition.Label;

        // 효과를 시작한 결과와 전달받은 시전 정보를 기록한다.
        public bool TryExecute(in SpecialContext context)
        {
            Definition.LastCast = Result;
            Definition.LastOrigin = context.Origin;
            Definition.LastFacingDirection = context.FacingDirection;
            return Definition.Succeeds;
        }
    }

    public sealed class ProjectileProbe : ProbeCast
    {
        public int Count = 1;
        protected override string Result => $"{Definition.Label}:{Count}";
    }

    public sealed class ExtraShotProbe : SpecialModifierDefinition
    {
        // 투사체를 지원하는 효과의 발사 수를 한 발 늘린다.
        public override void Apply(ISpecialEffect effect)
        {
            if (effect is ProjectileProbe projectile) {
                projectile.Count++;
            }
        }
    }
}
