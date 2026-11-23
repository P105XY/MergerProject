using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    public class FragmentPickup : MonoBehaviour
    {
        private const double Lifetime = 15d;
        private const double SlowFadeStart = 10d;
        private const double FastFadeStart = 13d;
        private const float SlowFadePeriod = 0.3f;
        private const float FastFadePeriod = 0.1f;
        private readonly List<(SpriteRenderer Renderer, float Alpha)> originalAlphas = new();
        private CollectibleType fragmentType;
        private bool isInitialized;
        private bool isCollected;
        private double droppedAt;

        // 드롭한 조각의 종류와 생성 시각 및 원래 스프라이트 투명도를 기록한다.
        public void Initialize(CollectibleType type)
        {
            fragmentType = type;
            droppedAt = Time.timeAsDouble;
            originalAlphas.Clear();

            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true)) {
                if (sprite.GetComponentInParent<FragmentPickup>() == this) {
                    originalAlphas.Add((sprite, sprite.color.a));
                }
            }

            isInitialized = true;
        }

        // 드롭 후 경과 시간에 따라 흐림 주기를 바꾸고 15초가 되면 조각을 제거한다.
        private void Update()
        {
            if (!isInitialized || isCollected) {
                return;
            }

            double elapsed = Time.timeAsDouble - droppedAt;

            if (elapsed >= Lifetime) {
                Destroy(gameObject);
                return;
            }

            if (elapsed < SlowFadeStart) {
                return;
            }

            bool fastFade = elapsed >= FastFadeStart;
            float phaseTime = (float)(elapsed - (fastFade ? FastFadeStart : SlowFadeStart));
            float period = fastFade ? FastFadePeriod : SlowFadePeriod;
            float opacity = 1f - Mathf.PingPong(2f * phaseTime / period, 1f);

            foreach (var entry in originalAlphas) {
                if (entry.Renderer != null) {
                    Color color = entry.Renderer.color;
                    color.a = entry.Alpha * opacity;
                    entry.Renderer.color = color;
                }
            }
        }

        // 플레이어가 조각을 받을 수 있으면 한 번만 수집한다.
        public bool TryCollect(PlayerBase player)
        {
            if (!isInitialized || isCollected || Time.timeAsDouble - droppedAt >= Lifetime
                || player == null || !player.isActiveAndEnabled) {
                return false;
            }

            isCollected = true;

            if (!player.TryCollectFragment(fragmentType)) {
                isCollected = false;
                return false;
            }

            Destroy(gameObject);
            return true;
        }
    }
}
