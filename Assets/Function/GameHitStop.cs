using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-2)]
    sealed class GameHitStop : MonoBehaviour
    {
        private const double Duration = 0.01;
        private readonly List<(Animator Animator, float Speed)> pausedAnimators = new();
        private double endsAt;
        private int hitFrame;

        // 공격자 또는 피격 대상의 Animator를 잠시 정지한다.
        public static void Play(MonoBehaviour owner)
        {
            if (owner == null || !owner.isActiveAndEnabled) {
                return;
            }

            var hitStop = owner.GetComponent<GameHitStop>();

            if (hitStop == null) {
                hitStop = owner.gameObject.AddComponent<GameHitStop>();
            }

            hitStop.Pause(owner);
        }

        // 원래 재생 속도를 한 번 기록하고 연속 명중 시 종료 시각을 갱신한다.
        private void Pause(MonoBehaviour owner)
        {
            if (pausedAnimators.Count == 0) {
                var enemy = owner.GetComponent<EnemyBase>();
                var player = owner.GetComponent<PlayerBase>();

                foreach (Animator animator in GetComponentsInChildren<Animator>()) {
                    if (!animator.isActiveAndEnabled
                        || animator.GetComponentInParent<EnemyBase>() != enemy
                        || animator.GetComponentInParent<PlayerBase>() != player) {
                        continue;
                    }

                    pausedAnimators.Add((animator, animator.speed));
                    animator.speed = 0f;
                }
            }

            endsAt = Time.unscaledTimeAsDouble + Duration;
            hitFrame = Time.frameCount;
            enabled = pausedAnimators.Count > 0;
        }

        // 정지 시간이 끝나면 애니메이션 갱신 전에 재생을 재개한다.
        private void Update()
        {
            if (Time.frameCount != hitFrame && Time.unscaledTimeAsDouble >= endsAt) {
                enabled = false;
            }
        }

        // 정지 종료나 오브젝트 비활성화 시 원래 재생 속도를 복원한다.
        private void OnDisable()
        {
            foreach (var entry in pausedAnimators) {
                if (entry.Animator != null) {
                    entry.Animator.speed = entry.Speed;
                }
            }

            pausedAnimators.Clear();
        }
    }
}
