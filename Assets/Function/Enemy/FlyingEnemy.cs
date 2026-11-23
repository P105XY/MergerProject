using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    public sealed class FlyingEnemy : TargetingEnemy
    {
        [SerializeField] private Transform projectileSpawnPoint = null;
        [SerializeField] private EnemyProjectile projectilePrefab = null;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 50f;

        private double nextFireTime;

        // 살아 있는 플레이어를 향해 공격 주기마다 직선 투사체 한 발을 발사한다.
        private void FixedUpdate()
        {
            if (!CanAct) {
                return;
            }

            PlayerBase player = GetTargetPlayer();

            if (player == null || AttackSpeed <= 0f) {
                TryChangeState(EnemyState.Idle);
                return;
            }

            double now = Time.fixedTimeAsDouble;

            if (now < nextFireTime) {
                return;
            }

            Vector3 origin = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
            Vector2 offset = (Vector2)(player.transform.position - origin);

            if (offset.sqrMagnitude <= 0.000001f) {
                return;
            }

            LaunchProjectile(origin, offset.normalized);
            DropAttackFragment();
            nextFireTime = now + 1.0 / AttackSpeed;
            TryChangeState(EnemyState.Attack);
        }

        // 발사 위치와 방향을 고정한 투사체를 적과 같은 씬에 만든다.
        private void LaunchProjectile(Vector3 origin, Vector2 direction)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            EnemyProjectile projectile;

            if (projectilePrefab != null) {
                projectile = Instantiate(projectilePrefab, origin, rotation);
            } else {
                GameObject projectileObject = new GameObject(nameof(EnemyProjectile));
                projectileObject.transform.SetPositionAndRotation(origin, rotation);
                projectile = projectileObject.AddComponent<EnemyProjectile>();
            }

            if (projectile.gameObject.scene != gameObject.scene) {
                SceneManager.MoveGameObjectToScene(projectile.gameObject, gameObject.scene);
            }

            projectile.Initialize(direction, projectileSpeed, AttackPower);
        }
    }
}
