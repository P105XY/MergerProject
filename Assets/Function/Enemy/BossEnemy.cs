using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class BossEnemy : TargetingEnemy
    {
        private enum AttackPattern { Charge, Slam, Sweep }

        private const float BossHealth = 2250f;
        private const float AttackDelay = 3f;
        private const float SummonDelay = 5f;
        private const int AttacksBeforeSummon = 2;
        private const int SummonCount = 3;
        private const int SpawnSearchSteps = 9;
        private const float GroundProbeHeight = 0.5f;
        private const float WallClearance = 0.01f;
        private const double PhysicsTimeTolerance = 0.0001;

        [SerializeField] private FragmentEnemy fragmentEnemyPrefab = null;
        [SerializeField, Min(0.1f)] private float fragmentSpawnSpacing = 1.5f;
        [SerializeField] private LayerMask environmentLayers = ~0;
        [SerializeField, Min(0.01f)] private float chargeSpeed = 20f;
        [SerializeField] private Vector2 slamSize = new Vector2(2f, 2.5f);
        [SerializeField] private Vector2 sweepSize = new Vector2(6f, 2.5f);

        private readonly List<RaycastHit2D> chargeHits = new List<RaycastHit2D>();
        private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>();
        private readonly List<Collider2D> overlapHits = new List<Collider2D>();
        private Rigidbody2D body;
        private BoxCollider2D bodyCollider;
        private BoxCollider2D attackTrigger;
        private AttackPattern nextAttack;
        private int attacksSinceSummon;
        private int chargeDirection;
        private double nextPatternTime;

        public int FacingDirection { get; private set; } = 1;

        // 컴포넌트 추가 시 Inspector에 보스 체력을 설정한다.
        private void Reset()
        {
            MaxHealth = BossHealth;
        }

        // 보스 체력과 고정 높이의 물리 바디 및 전방 공격 트리거를 준비한다.
        protected override void Awake()
        {
            MaxHealth = BossHealth;
            base.Awake();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<BoxCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            var triggerObject = new GameObject("Boss Attack Trigger");
            triggerObject.layer = gameObject.layer;
            triggerObject.transform.SetParent(transform, false);
            attackTrigger = triggerObject.AddComponent<BoxCollider2D>();
            attackTrigger.isTrigger = true;
            attackTrigger.enabled = false;
        }

        // 활성화 후 첫 패턴까지 대기한다.
        private void OnEnable()
        {
            nextPatternTime = Time.fixedTimeAsDouble + AttackDelay;
        }

        // 비활성화 시 진행 중인 돌진을 종료한다.
        private void OnDisable()
        {
            if (chargeDirection != 0 && CanAct) {
                FinishPattern(AttackDelay);
            }

            chargeDirection = 0;
        }

        // 물리 프레임마다 행동 가능한 보스의 패턴을 진행한다.
        private void FixedUpdate()
        {
            if (CanAct) {
                UpdateBossPattern();
            } else {
                chargeDirection = 0;
            }
        }

        // 돌진을 이어가거나 대기가 끝나면 다음 공격 또는 강제 소환을 실행한다.
        protected virtual void UpdateBossPattern()
        {
            if (chargeDirection != 0) {
                MoveCharge();
                return;
            }

            if (Time.fixedTimeAsDouble + PhysicsTimeTolerance < nextPatternTime) {
                return;
            }

            PlayerBase player = GetTargetPlayer();

            if (player == null) {
                return;
            }

            float offset = player.transform.position.x - body.position.x;

            if (offset != 0f) {
                FacingDirection = offset < 0f ? -1 : 1;
            }

            TryChangeState(EnemyState.Attack);

            if (!CanAct || !isActiveAndEnabled) {
                return;
            }

            if (attacksSinceSummon >= AttacksBeforeSummon) {
                attacksSinceSummon = 0;
                SummonFragments();
                FinishPattern(SummonDelay);
                return;
            }

            AttackPattern attack = nextAttack;
            nextAttack = attack == AttackPattern.Sweep ? AttackPattern.Charge : attack + 1;

            if (attack == AttackPattern.Charge) {
                chargeDirection = FacingDirection;
                MoveCharge();
            } else {
                Strike(attack == AttackPattern.Slam ? slamSize : sweepSize);
                attacksSinceSummon++;
                FinishPattern(AttackDelay);
            }
        }

        // 패턴이 끝난 물리 시각부터 다음 패턴까지 대기한다.
        private void FinishPattern(float delay)
        {
            chargeDirection = 0;
            nextPatternTime = Time.fixedTimeAsDouble + delay;

            if (CanAct) {
                TryChangeState(EnemyState.Idle);
            }
        }

        // 벽까지 돌진하고 벽 앞의 플레이어에게 기존 접촉 피해를 적용한다.
        private void MoveCharge()
        {
            Vector2 direction = Vector2.right * chargeDirection;
            float distance = Mathf.Max(0.01f, chargeSpeed) * Time.fixedDeltaTime;
            var filter = new ContactFilter2D { useTriggers = true };
            body.Cast(direction, filter, chargeHits, distance + WallClearance);
            float wallDistance = float.PositiveInfinity;

            foreach (RaycastHit2D hit in chargeHits) {
                if (IsEnvironment(hit.collider) && Vector2.Dot(hit.normal, direction) < -0.1f
                    && hit.collider.bounds.max.y > bodyCollider.bounds.min.y + WallClearance) {
                    wallDistance = Mathf.Min(wallDistance, hit.distance);
                }
            }

            float moveDistance = Mathf.Min(distance, Mathf.Max(0f, wallDistance - WallClearance));

            foreach (RaycastHit2D hit in chargeHits) {
                if (hit.distance <= moveDistance && hit.collider.GetComponentInParent<PlayerBase>() is PlayerBase player) {
                    player.TryReceiveContactDamage(AttackPower);
                }
            }

            body.position += direction * moveDistance;

            if (wallDistance <= distance + WallClearance) {
                attacksSinceSummon++;
                FinishPattern(AttackDelay);
            }
        }

        // 바라보는 방향의 바닥부터 지정 크기로 공격 트리거를 열어 한 번 타격한다.
        private void Strike(Vector2 size)
        {
            float localDirection = FacingDirection * Mathf.Sign(transform.TransformVector(Vector3.right).x);
            attackTrigger.size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
            attackTrigger.offset = bodyCollider.offset + new Vector2(
                localDirection * (bodyCollider.size.x + attackTrigger.size.x) * 0.5f,
                (attackTrigger.size.y - bodyCollider.size.y) * 0.5f);
            attackTrigger.enabled = true;
            Physics2D.SyncTransforms();
            attackTrigger.Overlap(new ContactFilter2D { useTriggers = true }, overlapHits);
            attackTrigger.enabled = false;

            foreach (Collider2D hit in overlapHits) {
                if (hit.GetComponentInParent<PlayerBase>() is PlayerBase player) {
                    player.TryReceiveContactDamage(AttackPower);
                }
            }
        }

        // 보스 발밑 높이에서 전후의 빈 바닥을 찾아 조각 적 세 마리를 배치한다.
        private void SummonFragments()
        {
            if (fragmentEnemyPrefab == null) {
                Debug.LogWarning("보스에게 소환할 조각 적 프리팹이 지정되지 않았습니다.", this);
                return;
            }

            Bounds bounds = bodyCollider.bounds;
            int spawned = 0;
            float spacing = Mathf.Max(0.1f, fragmentSpawnSpacing);
            var physicsScene = gameObject.scene.GetPhysicsScene2D();
            var filter = new ContactFilter2D { useTriggers = false };

            for (int step = 0; step < SpawnSearchSteps && spawned < SummonCount; step++) {
                float distance = bounds.extents.x + spacing * (step + 0.5f);

                if (TrySpawnFragment(physicsScene, filter, bounds, spacing, distance * FacingDirection)) {
                    spawned++;
                }

                if (spawned < SummonCount && TrySpawnFragment(physicsScene, filter, bounds, spacing, -distance * FacingDirection)) {
                    spawned++;
                }
            }

            if (spawned < SummonCount) {
                Debug.LogWarning($"보스 주변 바닥에 조각 적을 배치할 자리가 부족합니다 ({spawned}/{SummonCount}).", this);
            }
        }

        // 벽 너머와 점유된 자리를 피하고 보스 발밑 높이에 가까운 바닥에 조각 적을 생성한다.
        private bool TrySpawnFragment(PhysicsScene2D physicsScene, ContactFilter2D filter, Bounds bounds, float spacing, float offset)
        {
            Vector2 center = bounds.center;
            physicsScene.Raycast(center, Vector2.right * Mathf.Sign(offset), Mathf.Abs(offset), filter, groundHits);

            foreach (RaycastHit2D hit in groundHits) {
                if (IsEnvironment(hit.collider)) {
                    return false;
                }
            }

            float x = center.x + offset;
            physicsScene.Raycast(new Vector2(x, bounds.min.y + GroundProbeHeight), Vector2.down, 2f * GroundProbeHeight, filter, groundHits);

            foreach (RaycastHit2D hit in groundHits) {
                if (hit.normal.y <= 0.5f || !IsEnvironment(hit.collider)) {
                    continue;
                }

                float groundClearance = Physics2D.defaultContactOffset * 2f + WallClearance;
                physicsScene.OverlapBox(hit.point + Vector2.up * (GroundProbeHeight + groundClearance),
                    new Vector2(spacing * 0.8f, 2f * GroundProbeHeight), 0f, filter, overlapHits);

                if (overlapHits.Count != 0) {
                    return false;
                }

                FragmentEnemy fragment = Instantiate(fragmentEnemyPrefab, new Vector3(x, hit.point.y, transform.position.z), Quaternion.identity);

                if (fragment.gameObject.scene != gameObject.scene) {
                    SceneManager.MoveGameObjectToScene(fragment.gameObject, gameObject.scene);
                }

                fragment.PlaceAsObstacle(hit.point.y);
                Physics2D.SyncTransforms();
                return true;
            }

            return false;
        }

        // 플레이어나 적이 아닌 지정 레이어의 단단한 지형만 벽과 바닥으로 취급한다.
        private bool IsEnvironment(Collider2D collider)
        {
            return !collider.isTrigger && (environmentLayers.value & (1 << collider.gameObject.layer)) != 0
                && collider.GetComponentInParent<EnemyBase>() == null && collider.GetComponentInParent<PlayerBase>() == null;
        }
    }
}
