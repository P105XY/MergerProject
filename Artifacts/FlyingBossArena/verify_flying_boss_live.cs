using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEngine;

public sealed class VerifyFlyingBossLive : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly string[] Expected = { "FragmentSummon", "ChargeSummon", "Dive", "ArcShot", "RadialShot" };
    private readonly HashSet<BossChargeMinion> chargers = new HashSet<BossChargeMinion>();
    private readonly HashSet<EnemyProjectile> projectiles = new HashSet<EnemyProjectile>();
    private readonly List<double> chargerTimes = new List<double>();
    private FlyingBossEnemy boss;
    private PlayerBase player;
    private FragmentEnemy hoverSample;
    private float fragmentInitialHeight;
    private bool fragmentHovered;
    private bool floorDive;
    private BossChargeMinion firstCharger;
    private Vector2 firstDirection;
    private Vector2 firstChargerPosition;
    private Vector2 initialBossPosition;
    private float low = float.PositiveInfinity;
    private float high = float.NegativeInfinity;
    private float xRange;
    private double startedAt;
    private double diagonalTime;
    private double collisionDeadline;
    private int patternIndex = -1;
    private int arcCount;
    private int diagonalCount;
    private int cardinalCount;
    private int checks;
    private float firstArcY;
    private EnemyProjectile firstArc;
    private EnemyProjectile floorProbe;
    private EnemyProjectile wallProbe;
    private bool gravityObserved;
    private bool directionObserved;
    private bool done;

    // 독립 빌드의 실제 게임 루프에 검증 관찰자만 추가한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        var host = new GameObject("Flying Boss Verification");
        DontDestroyOnLoad(host);
        host.AddComponent<VerifyFlyingBossLive>();
    }

    // 저장된 보스 아레나의 대상과 임시 저장 슬롯을 준비한다.
    private void Start()
    {
        startedAt = Time.realtimeSinceStartupAsDouble;
        floorDive = Environment.GetCommandLineArgs().Contains("-verify-floor-dive");
        var directory = Path.Combine(Path.GetTempPath(), "flying-boss-check-" + Guid.NewGuid().ToString("N"));
        typeof(CheckpointProgress).GetField("saveStore", Private).SetValue(null, new CheckpointSaveStore(directory));
        boss = FindAnyObjectByType<FlyingBossEnemy>();
        player = FindAnyObjectByType<PlayerBase>();
        initialBossPosition = boss.GetComponent<Rigidbody2D>().position;
        Check("시작 체력", "3400", boss.CurrentHealth, boss.CurrentHealth == 3400f);
        boss.StateChanged += OnStateChanged;
    }

    // 실제 패턴 상태 이벤트에서 순서와 비행 및 직접 돌진 결과를 확인한다.
    private void OnStateChanged(EnemyState previous, EnemyState current)
    {
        if (done) {
            return;
        }

        if (current == EnemyState.Attack) {
            patternIndex++;
            string actual = typeof(FlyingBossEnemy).GetField("activePattern", Private).GetValue(boss).ToString();
            Check("패턴 " + (patternIndex + 1), Expected[patternIndex], actual, actual == Expected[patternIndex]);

            if (patternIndex == 0) {
                Check("경기장 중앙선 좌우 이동·부유", "가로 이동 5 이상, 높이차 0.5 이상",
                    xRange.ToString("F3") + " / " + (high - low).ToString("F3"),
                    xRange > 5f && high - low > 0.5f && low > initialBossPosition.y - 0.5f && high < initialBossPosition.y + 0.5f);
            }

            if (patternIndex == 2 && floorDive) {
                PlacePlayer(boss.GetComponent<Rigidbody2D>().position.x);
            }
        } else if (previous == EnemyState.Attack && current == EnemyState.Idle) {
            if (patternIndex == 0) {
                var spawned = FindObjectsByType<FragmentEnemy>();
                Check("조각 적 소환", "3~5체 및 바닥 위 부유", spawned.Length,
                    spawned.Length >= 3 && spawned.Length <= 5 && spawned.All(enemy =>
                        enemy.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Kinematic
                        && enemy.GetComponent<Collider2D>().bounds.min.y > -0.1f));

                if (spawned.Length > 0) {
                    hoverSample = spawned[0];
                    fragmentInitialHeight = hoverSample.GetComponent<Rigidbody2D>().position.y;
                }
            } else if (patternIndex == 2) {
                Vector2 position = boss.GetComponent<Rigidbody2D>().position;
                bool stoppedAtFloor = Mathf.Abs(position.y - 0.225f) < 0.08f;
                bool stoppedAtWall = Mathf.Abs(Mathf.Abs(position.x) - 13f) < 0.08f;
                Check("보스 직접 돌진 종료", floorDive ? "바닥 앞 정지" : "벽 앞 정지", position,
                    floorDive ? stoppedAtFloor : stoppedAtWall);
                PlacePlayer(-11f);
            }
        }
    }

    // 시간을 조작하지 않고 자연 생성된 적과 투사체를 프레임마다 기록한다.
    private void Update()
    {
        if (done) {
            return;
        }

        double now = Time.fixedTimeAsDouble;

        if (Time.realtimeSinceStartupAsDouble - startedAt > 45) {
            Check("실행 시간 제한", "45초 이내 완료", patternIndex, false);
            return;
        }

        if (patternIndex < 0) {
            Vector2 position = boss.GetComponent<Rigidbody2D>().position;
            low = Mathf.Min(low, position.y);
            high = Mathf.Max(high, position.y);
            xRange = Mathf.Max(xRange, Mathf.Abs(position.x - initialBossPosition.x));
        }

        if (!fragmentHovered && hoverSample != null
            && Mathf.Abs(hoverSample.GetComponent<Rigidbody2D>().position.y - fragmentInitialHeight) > 0.08f) {
            fragmentHovered = true;
        }

        ObserveChargers(now);
        ObserveShots(now);
    }

    // 세 돌진 적의 소환 간격과 플레이어 이동 뒤 첫 적의 방향 고정을 확인한다.
    private void ObserveChargers(double now)
    {
        foreach (BossChargeMinion charger in FindObjectsByType<BossChargeMinion>()) {
            if (!chargers.Add(charger)) {
                continue;
            }

            chargerTimes.Add(now);

            if (chargerTimes.Count == 1) {
                firstCharger = charger;
                firstDirection = (Vector2)typeof(BossChargeMinion).GetField("direction", Private).GetValue(charger);
                firstChargerPosition = charger.GetComponent<Rigidbody2D>().position;
                PlacePlayer(11f);
            } else {
                double gap = now - chargerTimes[chargerTimes.Count - 2];
                Check("돌진 적 " + chargerTimes.Count + "번째 소환 간격", "2.000초", gap.ToString("F3"), Math.Abs(gap - 2) <= 0.025);
            }
        }

        if (!directionObserved && firstCharger != null) {
            Vector2 movement = firstCharger.GetComponent<Rigidbody2D>().position - firstChargerPosition;

            if (movement.sqrMagnitude > 0.02f) {
                Vector2 current = (Vector2)typeof(BossChargeMinion).GetField("direction", Private).GetValue(firstCharger);
                directionObserved = true;
                Check("플레이어가 반대편으로 이동한 뒤 첫 돌진 적", "발사 방향 고정", current,
                    current == firstDirection && Vector2.Dot(movement.normalized, firstDirection) > 0.99f);
            }
        }
    }

    // 포물선, 대각선, 십자 발사 수와 발사 간격을 확인한다.
    private void ObserveShots(double now)
    {
        if (collisionDeadline > 0) {
            if (now >= collisionDeadline) {
                Check("포물선의 바닥·벽 충돌", "수명 4초 이전에 두 발 제거", (floorProbe == null) + " / " + (wallProbe == null),
                    floorProbe == null && wallProbe == null);

                if (!done) {
                    done = true;
                    Debug.Log("[FlyingBossLive] PASS " + checks + "/" + checks);
                    Application.Quit(0);
                }
            }

            return;
        }

        var gravity = typeof(EnemyProjectile).GetField("gravity", Private);
        var velocity = typeof(EnemyProjectile).GetField("velocity", Private);

        foreach (EnemyProjectile shot in FindObjectsByType<EnemyProjectile>()) {
            if (!projectiles.Add(shot)) {
                continue;
            }

            Vector2 motion = (Vector2)velocity.GetValue(shot);

            if ((float)gravity.GetValue(shot) > 0f) {
                arcCount++;

                if (firstArc == null && !gravityObserved) {
                    firstArc = shot;
                    firstArcY = motion.y;
                }
            } else if (Mathf.Abs(Mathf.Abs(motion.x) - Mathf.Abs(motion.y)) < 0.001f) {
                diagonalCount++;
                diagonalTime = now;
            } else {
                if (cardinalCount == 0) {
                    double gap = now - diagonalTime;
                    Check("X자 → 십자 발사 간격", "1.500초", gap.ToString("F3"), Math.Abs(gap - 1.5) <= 0.025);
                }

                Check("십자 발사 축", "가로 또는 세로", motion, Mathf.Abs(motion.x) < 0.001f || Mathf.Abs(motion.y) < 0.001f);
                cardinalCount++;
            }
        }

        if (!gravityObserved && firstArc != null && ((Vector2)velocity.GetValue(firstArc)).y < firstArcY - 0.1f) {
            gravityObserved = true;
        }

        if (cardinalCount >= 4) {
            Check("발사 수", "포물선 3, X자 4, 십자 4", arcCount + " / " + diagonalCount + " / " + cardinalCount,
                arcCount == 3 && diagonalCount == 4 && cardinalCount == 4);
            Check("포물선의 수직 속도", "시간에 따라 감소", gravityObserved, gravityObserved);
            Check("돌진 적 총 소환 수", "3체 및 방향 고정 확인", chargerTimes.Count, chargerTimes.Count == 3 && directionObserved);
            Check("조각 적의 부유 운동", "초기 높이에서 0.08 이상 변화", fragmentHovered, fragmentHovered);
            StartTerrainChecks(now);
        }
    }

    // 실제 투사체를 지형 가까이 발사해 수명 만료 전 제거를 검사한다.
    private void StartTerrainChecks(double now)
    {
        var prefab = (EnemyProjectile)typeof(FlyingBossEnemy).GetField("projectilePrefab", Private).GetValue(boss);
        floorProbe = Instantiate(prefab, new Vector3(11f, 0.5f, 0f), Quaternion.identity);
        wallProbe = Instantiate(prefab, new Vector3(13f, 2f, 0f), Quaternion.identity);
        floorProbe.Initialize(Vector2.down, 12f, 0f, 12f);
        wallProbe.Initialize(Vector2.right, 12f, 0f, 12f);
        collisionDeadline = now + 0.35;
    }

    // 검증을 위한 플레이어 위치만 설정한다.
    private void PlacePlayer(float x)
    {
        var body = player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, 0.41f);
        player.transform.position = new Vector3(x, 0.41f, player.transform.position.z);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    // 입력과 기대값·실제값을 기록하고 불일치하면 실패 상태로 종료한다.
    private void Check(string input, string expected, object actual, bool pass)
    {
        Debug.Log("[FlyingBossLive] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual);

        if (pass) {
            checks++;
        } else {
            done = true;
            Debug.LogError("[FlyingBossLive] FAIL " + input);
            Application.Quit(1);
        }
    }
}
