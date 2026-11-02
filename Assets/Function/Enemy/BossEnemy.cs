using UnityEngine;

namespace Mech.Function
{
    public class BossEnemy : EnemyBase
    {
        [SerializeField] private FragmentEnemy fragmentEnemyPrefab = null;
        [SerializeField] private Transform fragmentSpawnPoint = null;
        [SerializeField, Min(0f)] private float fragmentSpawnInterval = 5f;

        private float spawnElapsedTime;

        // 살아 있는 동안 조각 적 생성 주기를 진행하고 보스 패턴을 갱신한다.
        protected virtual void Update()
        {
            if (IsDefeated || CurrentState == EnemyState.Dead) {
                return;
            }

            UpdateBossPattern();

            if (IsDefeated || CurrentState == EnemyState.Dead || fragmentEnemyPrefab == null || fragmentSpawnInterval <= 0f) {
                return;
            }

            spawnElapsedTime += Time.deltaTime;

            if (spawnElapsedTime >= fragmentSpawnInterval) {
                spawnElapsedTime = 0f;
                Transform spawnTransform = fragmentSpawnPoint != null ? fragmentSpawnPoint : transform;
                Instantiate(fragmentEnemyPrefab, spawnTransform.position, spawnTransform.rotation);
            }
        }

        // 파생 보스가 고유 패턴을 갱신할 수 있도록 제공한다.
        protected virtual void UpdateBossPattern()
        {
        }
    }
}
