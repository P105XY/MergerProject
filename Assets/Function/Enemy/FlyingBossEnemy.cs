using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class FlyingBossEnemy : TargetingEnemy
    {
        private enum Pattern { FragmentSummon, ChargeSummon, Dive, ArcShot, RadialShot }

        [SerializeField] private BoxCollider2D arenaBounds = null;
        [SerializeField] private LayerMask terrainLayers = ~0;
        [SerializeField] private FragmentEnemy fragmentEnemyPrefab = null;
        [SerializeField] private BossChargeMinion chargeMinionPrefab = null;
        [SerializeField] private EnemyProjectile projectilePrefab = null;
        [SerializeField, Min(0f)] private float fallbackHalfWidth = 8f;
        [SerializeField, Min(0f)] private float hoverAmplitude = 0.35f;
        [SerializeField, Min(0.01f)] private float flightSpeed = 6f;
        [SerializeField, Min(0.01f)] private float diveSpeed = 18f;
        [SerializeField, Min(0f)] private float patternDelay = 3f;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
        [SerializeField, Min(0.01f)] private float projectileGravity = 12f;
        [SerializeField, Min(0.15f)] private float fragmentFloorClearance = 0.55f;

        private const float BossHealth = 3400f;
        private const double ChargeSummonInterval = 2;
        private const double RadialInterval = 1.5;
        private const double TimeTolerance = 0.0001;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private Rigidbody2D body;
        private BoxCollider2D bodyCollider;
        private Vector2 placementCenter;
        private Vector2 diveDirection;
        private int flightDirection = 1;
        private float hoverTime;
        private Pattern nextPattern;
        private Pattern? activePattern;
        private double nextActionTime;
        private int chargeSummonsRemaining;

        // 새로 추가한 컴포넌트의 Inspector 체력을 지정한다.
        private void Reset()
        {
            MaxHealth = BossHealth;
        }

        // 고정 체력과 비행에 사용할 물리 바디를 준비한다.
        protected override void Awake()
        {
            MaxHealth = BossHealth;
            base.Awake();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<BoxCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            placementCenter = body.position;
        }

        // 활성화 직후 다음 패턴까지 대기한다.
        private void OnEnable()
        {
            nextActionTime = Time.fixedTimeAsDouble + patternDelay;
        }

        // 비활성화 시 아직 발사하거나 소환하지 않은 패턴을 취소한다.
        private void OnDisable()
        {
            FinishPattern(Time.fixedTimeAsDouble);
        }

        // 보스의 공중 이동과 패턴 타이머를 물리 프레임에 맞춰 진행한다.
        private void FixedUpdate()
        {
            double now = Time.fixedTimeAsDouble;

            if (!CanAct) {
                activePattern = null;
                return;
            }

            if (activePattern == Pattern.Dive) {
                if (EnemyDash.Step(body, diveDirection, Mathf.Max(0.01f, diveSpeed) * Time.fixedDeltaTime, terrainLayers, AttackPower, hits)) {
                    FinishPattern(now);
                }

                return;
            }

            if (now + TimeTolerance < nextActionTime) {
                Fly();
                return;
            }

            PlayerBase player = GetTargetPlayer();

            if (player == null) {
                Fly();

                if (activePattern.HasValue) {
                    FinishPattern(now);
                }

                return;
            }

            if (!activePattern.HasValue) {
                activePattern = nextPattern;
                nextPattern = nextPattern == Pattern.RadialShot ? Pattern.FragmentSummon : nextPattern + 1;
                chargeSummonsRemaining = 3;
                TryChangeState(EnemyState.Attack);

                if (CanAct && isActiveAndEnabled) {
                    BeginPattern(player, now);

                    if (activePattern != Pattern.Dive) {
                        Fly();
                    }
                }

                return;
            }

            Fly();

            if (activePattern == Pattern.ChargeSummon) {
                SummonCharger(player, now);
            } else {
                FireRadial(false);
                FinishPattern(now);
            }
        }

        // 다섯 패턴 중 선택된 패턴의 첫 동작을 시작한다.
        private void BeginPattern(PlayerBase player, double now)
        {
            switch (activePattern) {
                case Pattern.FragmentSummon:
                    SummonFragments();
                    FinishPattern(now);
                    break;
                case Pattern.ChargeSummon:
                    SummonCharger(player, now);
                    break;
                case Pattern.Dive:
                    diveDirection = AimAt(player);
                    DropAttackFragment();
                    break;
                case Pattern.ArcShot:
                    FireArc(player);
                    FinishPattern(now);
                    break;
                case Pattern.RadialShot:
                    FireRadial(true);
                    nextActionTime = now + RadialInterval;
                    break;
            }
        }

        // 패턴을 정리하고 다음 패턴까지 공중 순찰 상태로 돌아간다.
        private void FinishPattern(double now)
        {
            activePattern = null;
            nextActionTime = now + patternDelay;

            if (CanAct) {
                TryChangeState(EnemyState.Idle);
            }
        }

        // 설정한 경기장의 가로 중앙선에서 좌우로 이동하며 높이를 조금씩 바꾼다.
        private void Fly()
        {
            GetFlightBounds(out Vector2 center, out float halfWidth);
            hoverTime += Time.fixedDeltaTime;
            float edge = center.x + flightDirection * halfWidth;
            float y = center.y + Mathf.Sin(hoverTime * 2.5f) * hoverAmplitude;
            float step = Mathf.Max(0.01f, flightSpeed) * Time.fixedDeltaTime;
            Vector2 position = body.position;
            var destination = new Vector2(Mathf.MoveTowards(position.x, edge, step), Mathf.MoveTowards(position.y, y, step));
            body.MovePosition(destination);

            if (Mathf.Abs(destination.x - edge) < 0.02f) {
                flightDirection = -flightDirection;
            }
        }

        // 경기장 경계 또는 배치 위치에서 비행 중심과 좌우 한계를 구한다.
        private void GetFlightBounds(out Vector2 center, out float halfWidth)
        {
            if (arenaBounds != null && arenaBounds.isActiveAndEnabled) {
                Bounds bounds = arenaBounds.bounds;
                center = bounds.center;
                halfWidth = Mathf.Max(0f, bounds.extents.x - bodyCollider.bounds.extents.x - 0.25f);
            } else {
                center = placementCenter;
                halfWidth = fallbackHalfWidth;
            }
        }

        // 현재 플레이어 위치에 대한 단발 조준 방향을 계산한다.
        private Vector2 AimAt(PlayerBase player)
        {
            Vector2 offset = (Vector2)player.transform.position - body.position;
            return offset.sqrMagnitude > 0.000001f ? offset.normalized : Vector2.right;
        }

        // 바닥 위에 세 마리에서 다섯 마리까지 조각 적을 띄운다.
        private void SummonFragments()
        {
            if (fragmentEnemyPrefab == null) {
                Debug.LogWarning("비행 보스의 조각 적 프리팹을 연결해야 합니다.", this);
                return;
            }

            GetFlightBounds(out Vector2 center, out float halfWidth);
            int count = Random.Range(3, 6);
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(terrainLayers);
            var physicsScene = gameObject.scene.GetPhysicsScene2D();

            for (int index = 0; index < count; index++) {
                float fraction = (index + 1f) / (count + 1f);
                float x = Mathf.Lerp(center.x - halfWidth, center.x + halfWidth, fraction);
                physicsScene.Raycast(new Vector2(x, center.y), Vector2.down, 40f, filter, hits);

                foreach (RaycastHit2D hit in hits) {
                    if (hit.normal.y > 0.5f && EnemyDash.IsTerrain(hit.collider, terrainLayers)) {
                        var minion = Instantiate(fragmentEnemyPrefab, new Vector3(x, hit.point.y, transform.position.z), Quaternion.identity);
                        MoveIntoScene(minion.gameObject);
                        minion.HoverAboveFloor(hit.point.y, fragmentFloorClearance);
                        break;
                    }
                }
            }
        }

        // 각 생성 시점에 조준한 돌진 적을 소환하고 다음 소환까지 2초를 예약한다.
        private void SummonCharger(PlayerBase player, double now)
        {
            if (chargeMinionPrefab == null) {
                Debug.LogWarning("비행 보스의 돌진 적 프리팹을 연결해야 합니다.", this);
                FinishPattern(now);
                return;
            }

            var minion = Instantiate(chargeMinionPrefab, LaunchOrigin(), Quaternion.identity);
            MoveIntoScene(minion.gameObject);
            minion.Launch(AimAt(player), terrainLayers);
            chargeSummonsRemaining--;

            if (chargeSummonsRemaining > 0) {
                nextActionTime = now + ChargeSummonInterval;
            } else {
                FinishPattern(now);
            }
        }

        // 중앙 포물선이 조준 위치를 지나도록 계산하고 좌우 15도에 한 발씩 더 쏜다.
        private void FireArc(PlayerBase player)
        {
            DropAttackFragment();
            Vector2 offset = (Vector2)player.transform.position - body.position;
            float speed = Mathf.Max(0.01f, projectileSpeed);
            float gravity = Mathf.Max(0.01f, projectileGravity);
            float travel = Mathf.Max(0.2f, offset.magnitude / speed);
            Vector2 velocity = offset / travel + Vector2.up * (0.5f * gravity * travel);

            for (int index = -1; index <= 1; index++) {
                Vector2 spread = Quaternion.Euler(0f, 0f, index * 15f) * velocity;
                LaunchProjectile(spread, gravity);
            }
        }

        // X자 대각선 또는 십자 직선 방향으로 네 발을 발사한다.
        private void FireRadial(bool diagonal)
        {
            DropAttackFragment();
            float startAngle = diagonal ? 45f : 0f;

            for (int index = 0; index < 4; index++) {
                Vector2 direction = Quaternion.Euler(0f, 0f, startAngle + index * 90f) * Vector2.right;
                LaunchProjectile(direction * Mathf.Max(0.01f, projectileSpeed), 0f);
            }
        }

        // 투사체 프리팹을 생성하고 직선 또는 포물선 운동을 시작한다.
        private void LaunchProjectile(Vector2 velocity, float gravity)
        {
            Vector3 origin = LaunchOrigin();
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            EnemyProjectile projectile;

            if (projectilePrefab != null) {
                projectile = Instantiate(projectilePrefab, origin, rotation);
            } else {
                var instance = new GameObject(nameof(EnemyProjectile));
                instance.transform.SetPositionAndRotation(origin, rotation);
                projectile = instance.AddComponent<EnemyProjectile>();
            }

            MoveIntoScene(projectile.gameObject);
            projectile.Initialize(velocity.normalized, velocity.magnitude, AttackPower, gravity);
        }

        // 현재 물리 위치를 발사와 소환의 원점으로 사용한다.
        private Vector3 LaunchOrigin()
        {
            return new Vector3(body.position.x, body.position.y, transform.position.z);
        }

        // 생성된 오브젝트를 보스와 같은 물리 씬으로 옮긴다.
        private void MoveIntoScene(GameObject instance)
        {
            if (instance.scene != gameObject.scene) {
                SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
            }
        }
    }
}
