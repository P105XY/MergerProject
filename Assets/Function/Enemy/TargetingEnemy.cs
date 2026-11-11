using UnityEngine;

namespace Mech.Function
{
    public abstract class TargetingEnemy : EnemyBase
    {
        [SerializeField] private PlayerBase targetPlayer = null;

        private PlayerBase foundPlayer;
        private double nextTargetSearchTime;

        protected bool CanAct => !IsDefeated && CurrentState != EnemyState.Dead && CurrentState != EnemyState.Groggy;

        // 지정된 플레이어를 우선 사용하고, 미지정이면 살아 있는 플레이어를 찾아 기억한다.
        protected PlayerBase GetTargetPlayer()
        {
            if (targetPlayer != null) {
                return targetPlayer.isActiveAndEnabled && targetPlayer.CurrentHealth > 0f ? targetPlayer : null;
            }

            if (foundPlayer != null && foundPlayer.isActiveAndEnabled && foundPlayer.CurrentHealth > 0f) {
                return foundPlayer;
            }

            if (Time.fixedTimeAsDouble < nextTargetSearchTime) {
                return null;
            }

            nextTargetSearchTime = Time.fixedTimeAsDouble + 0.5;
            foundPlayer = null;

            foreach (PlayerBase player in FindObjectsByType<PlayerBase>(FindObjectsSortMode.None)) {
                if (player.isActiveAndEnabled && player.CurrentHealth > 0f && player.gameObject.scene == gameObject.scene) {
                    foundPlayer = player;
                    break;
                }
            }

            return foundPlayer;
        }
    }
}
