using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1)]
    sealed class EnemyWhiteFlash : MonoBehaviour
    {
        private const double Duration = 0.05;
        private readonly List<(SpriteRenderer Renderer, Material Original)> originalMaterials = new();
        private Material flashMaterial;
        private double endsAt;

        public bool IsFlashing => originalMaterials.Count != 0;

        // 적에 속한 스프라이트를 밝게 표시하고 연속 피격 시 종료 시각을 갱신한다.
        public void Play(EnemyBase owner)
        {
            if (!IsFlashing) {
                foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>()) {
                    if (!sprite.enabled || sprite.sprite == null || sprite.GetComponentInParent<EnemyBase>() != owner) {
                        continue;
                    }

                    if (flashMaterial == null) {
                        Shader shader = Resources.Load<Shader>("EnemyWhiteFlash");

                        if (shader == null || !shader.isSupported) {
                            return;
                        }

                        flashMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                    }

                    originalMaterials.Add((sprite, sprite.sharedMaterial));
                    sprite.sharedMaterial = flashMaterial;
                }
            }

            endsAt = Time.unscaledTimeAsDouble + Duration;
            enabled = IsFlashing;
        }

        // 표시 시간이 지나면 원래 머티리얼을 복원한다.
        private void Update()
        {
            if (Time.unscaledTimeAsDouble >= endsAt) {
                enabled = false;
            }
        }

        // 비활성화될 때 효과가 적용된 스프라이트의 머티리얼을 돌려놓는다.
        private void OnDisable()
        {
            foreach (var entry in originalMaterials) {
                if (entry.Renderer != null && entry.Renderer.sharedMaterial == flashMaterial) {
                    entry.Renderer.sharedMaterial = entry.Original;
                }
            }

            originalMaterials.Clear();
        }

        // 적과 함께 런타임 효과 머티리얼을 해제한다.
        private void OnDestroy()
        {
            if (flashMaterial != null) {
                Destroy(flashMaterial);
            }
        }
    }
}
