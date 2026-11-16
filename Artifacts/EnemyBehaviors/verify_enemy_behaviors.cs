// 플레이 모드에서 Unity MCP execute_code에 이 메서드 본문을 전달한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused) {
    throw new System.InvalidOperationException("일시 정지하지 않은 플레이 모드가 필요합니다.");
}

var root = new GameObject("Enemy Behavior Verification");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var targetField = typeof(Mech.Function.TargetingEnemy).GetField("targetPlayer", flags);
var rateField = typeof(Mech.Function.EnemyBase).GetField("attackSpeed", flags);
var checks = new System.Collections.Generic.List<string>();

// 분리된 물리 시험 위치에 오브젝트를 생성한다.
System.Func<string, float, float, GameObject> makeObject = (name, x, y) => {
    var item = new GameObject(name);
    item.transform.SetParent(root.transform);
    item.transform.position = new Vector3(x, y, 0f);
    return item;
};

// 입력 없이 지정된 위치에 머무는 실제 플레이어를 만든다.
System.Func<float, float, Mech.Function.PlayerBase> makePlayer = (x, y) => {
    var item = makeObject("Player", x, y);
    item.AddComponent<BoxCollider2D>();
    var body = item.AddComponent<Rigidbody2D>();
    body.constraints = RigidbodyConstraints2D.FreezeAll;
    var player = item.AddComponent<Mech.Function.PlayerBase>();
    body.gravityScale = 0f;
    return player;
};

// 플레이어 바디를 검증에서 지정한 좌표로 옮긴다.
System.Action<Mech.Function.PlayerBase, float, float> movePlayer = (player, x, y) => {
    player.GetComponent<Rigidbody2D>().position = new Vector2(x, y);
};

// 충돌체와 바디를 갖춘 추적 또는 돌진 적을 만든다.
System.Func<System.Type, Mech.Function.PlayerBase, float, float, Mech.Function.TargetingEnemy> makeGroundEnemy =
    (type, player, x, y) => {
        var item = makeObject(type.Name, x, y);
        var collider = item.AddComponent<BoxCollider2D>();
        collider.isTrigger = type == typeof(Mech.Function.MeleeEnemy);
        var body = item.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        var enemy = (Mech.Function.TargetingEnemy)item.AddComponent(type);
        targetField.SetValue(enemy, player);
        return enemy;
    };

// 발사 방향을 비교하는 동안 추가 발사가 없도록 첫 발 이후 간격을 10초로 둔다.
System.Action<Mech.Function.PlayerBase, float, float> makeFlying = (player, x, y) => {
    var item = makeObject("Flying enemy", x, y);
    item.AddComponent<BoxCollider2D>();
    var enemy = item.AddComponent<Mech.Function.FlyingEnemy>();
    targetField.SetValue(enemy, player);
    rateField.SetValue(enemy, 0.1f);
};

// 기대값과 실제값을 기록하고 허용 오차를 벗어나면 검증을 실패시킨다.
System.Action<string, double, double, double> check = (input, expected, actual, tolerance) => {
    string message = string.Format("[Enemy Behavior Check] 입력: {0}; 기대: {1:F4}; 실제: {2:F4}; 허용 오차: {3:F4}", input, expected, actual, tolerance);
    Debug.Log(message);
    checks.Add(message);
    bool passed = System.Math.Abs(expected - actual) <= tolerance;
    UnityEngine.Assertions.Assert.IsTrue(passed, message);

    if (!passed) {
        throw new System.InvalidOperationException(message);
    }
};

var meleeTarget = makePlayer(1002f, 1000f);
var melee = makeGroundEnemy(typeof(Mech.Function.MeleeEnemy), meleeTarget, 1000f, 1000f);
var meleeBody = melee.GetComponent<Rigidbody2D>();

var chargeTarget = makePlayer(2020f, 1002f);
var charge = makeGroundEnemy(typeof(Mech.Function.ChargeEnemy), chargeTarget, 2000f, 1000f);
var chargeBody = charge.GetComponent<Rigidbody2D>();
var floor = makeObject("Floor at feet", 2150f, 999.25f).AddComponent<BoxCollider2D>();
floor.size = new Vector2(450f, 0.5f);
var changes = new System.Collections.Generic.List<System.Tuple<Mech.Function.EnemyState, double, float>>();
// 상태 전환 순간의 실제 물리 시간과 위치를 저장한다.
charge.StateChanged += (previous, next) => changes.Add(System.Tuple.Create(next, Time.fixedTimeAsDouble, chargeBody.position.x));

var sweptTarget = makePlayer(3003f, 1000f);
var wallCharge = makeGroundEnemy(typeof(Mech.Function.ChargeEnemy), sweptTarget, 3000f, 1000f);
var wall = makeObject("Thin charge wall", 3016f, 1000f).AddComponent<BoxCollider2D>();
wall.size = new Vector2(0.2f, 3f);

var dodgingTarget = makePlayer(4060f, 970f);
makeFlying(dodgingTarget, 4000f, 1000f);
var shotTarget = makePlayer(5006f, 998f);
makeFlying(shotTarget, 5000f, 1000f);
var shieldedTarget = makePlayer(6006f, 998f);
makeFlying(shieldedTarget, 6000f, 1000f);
makeObject("Projectile wall", 6003f, 999f).AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 3f);
Physics2D.SyncTransforms();

double startTime = Time.fixedTimeAsDouble;
Mech.Function.EnemyProjectile dodgedShot = null;
Vector2 shotDirection = new Vector2(10f, -5f).normalized;
Vector2 sampledPosition = Vector2.zero;
double sampledTime = 0;
bool checkedShotMotion = false;
bool movedChargeTarget = false;
bool checkedWindup = false;
bool checkedFirstPhase = false;
bool checkedFirstCharge = false;
bool checkedMeleeTurn = false;
bool finished = false;
EditorApplication.CallbackFunction observe = null;

// 물리 시간이 진행되는 동안 공격과 충돌의 결과를 확인하고 검증 오브젝트를 정리한다.
observe = () => {
    try {
        if (!EditorApplication.isPlaying) {
            finished = true;
            return;
        }

        double now = Time.fixedTimeAsDouble;
        double elapsed = now - startTime;

        if (elapsed > 7.5) {
            throw new System.InvalidOperationException("적 행동 검증 시간 초과, 전환 횟수: " + changes.Count);
        }

        if (dodgedShot == null && sampledTime == 0 && elapsed >= 0.04) {
            int shots = 0;

            foreach (var projectile in UnityEngine.Object.FindObjectsByType<Mech.Function.EnemyProjectile>(FindObjectsSortMode.None)) {
                if (Mathf.Abs(projectile.transform.position.x - 4000f) < 160f) {
                    dodgedShot = projectile;
                    shots++;
                }
            }

            // 첫 공격에는 투사체 한 발이 초기 대각선 방향으로 생성되어야 한다.
            check("공중 적의 첫 공격에서 발사한 수", 1, shots, 0);
            check("최초 위치를 향하는 대각선 발사 방향", 1, Vector2.Dot(dodgedShot.transform.right, shotDirection), 0.0001);
            sampledPosition = dodgedShot.transform.position;
            sampledTime = now;
            movePlayer(dodgingTarget, 4060f, 1030f);
        }

        if (!checkedShotMotion && sampledTime > 0 && now - sampledTime >= 0.08) {
            // 플레이어가 옮겨도 방향은 고정되고 실제 비행 속도는 50이어야 한다.
            Vector2 velocity = ((Vector2)dodgedShot.transform.position - sampledPosition) / (float)(now - sampledTime);
            check("비행 중 플레이어 이동 후 투사체 속도", 50, velocity.magnitude, 0.05);
            check("비행 중 플레이어 이동 후 처음 방향과의 일치", 1, Vector2.Dot(velocity.normalized, shotDirection), 0.0001);
            checkedShotMotion = true;
        }

        if (!movedChargeTarget && elapsed >= 0.3) {
            // 탐지 범위 밖의 돌진 적은 정지하고, 근접 적은 속도 3으로 접근한다.
            check("플레이어가 탐지 범위 밖인 돌진 적의 이동 거리", 0, chargeBody.position.x - 2000f, 0.001);
            check("플레이어가 탐지 범위 밖인 돌진 적의 상태", (int)Mech.Function.EnemyState.Idle, (int)charge.CurrentState, 0);
            check("근접 적의 플레이어 방향 속도", 3, meleeBody.linearVelocityX, 0.02);
            movePlayer(chargeTarget, 2009f, 1002f);
            movedChargeTarget = true;
        }

        if (!checkedWindup && changes.Count == 1 && now - changes[0].Item2 >= 0.2) {
            // 인지 후 0.5초가 지나기 전까지는 준비 상태에서 정지해야 한다.
            check("인지 후 준비 중 돌진 적의 상태", (int)Mech.Function.EnemyState.Windup, (int)charge.CurrentState, 0);
            check("인지 후 준비 중 돌진 적의 이동 거리", 0, chargeBody.position.x - 2000f, 0.001);
            checkedWindup = true;
        }

        if (!checkedFirstPhase && elapsed >= 0.95) {
            // 근접 적 접촉과 맞은 투사체는 각 10 피해, 회피 및 벽 뒤는 피해가 없다.
            check("근접 적과 첫 접촉 뒤 플레이어 체력", 90, meleeTarget.CurrentHealth, 0);
            check("직선 투사체를 맞은 플레이어 체력", 90, shotTarget.CurrentHealth, 0);
            check("발사 후 이동해 회피한 플레이어 체력", 100, dodgingTarget.CurrentHealth, 0);
            check("투사체 경로의 벽 뒤 플레이어 체력", 100, shieldedTarget.CurrentHealth, 0);
            check("돌진 한 물리 단계 사이에 있는 플레이어 체력", 90, sweptTarget.CurrentHealth, 0);
            check("돌진 적이 얇은 벽 앞에서 멈춘 수평 위치", 3015.39, wallCharge.GetComponent<Rigidbody2D>().position.x, 0.03);
            movePlayer(meleeTarget, 999f, 1000f);
            checkedFirstPhase = true;
        }

        if (!checkedMeleeTurn && checkedFirstPhase && elapsed >= 1.15) {
            // 플레이어가 뒤로 옮기면 근접 적은 좌측으로 추적해야 한다.
            check("플레이어가 뒤로 이동한 뒤 근접 적의 수평 속도", -3, meleeBody.linearVelocityX, 0.02);
            checkedMeleeTurn = true;
        }

        if (!checkedFirstCharge && changes.Count >= 3) {
            // 0.5초 준비 뒤 장애물 없는 수평 경로에서 1초 동안 300 이동해야 한다.
            check("첫 돌진 준비 시간", 0.5, changes[1].Item2 - changes[0].Item2, 0.002);
            check("첫 돌진 물리 이동 시간", 1, changes[2].Item2 - changes[1].Item2 + Time.fixedDeltaTime, 0.002);
            check("첫 돌진 수평 거리", 300, changes[2].Item3 - changes[1].Item3, 0.01);
            movePlayer(chargeTarget, 2280f, 1002f);
            checkedFirstCharge = true;
        }

        if (changes.Count >= 5) {
            // 탐지 범위를 벗어나도 같은 대상에게 5초 주기로 다시 돌진하고 방향을 새로 고른다.
            check("첫 돌진과 둘째 돌진의 시작 간격", 5, changes[4].Item2 - changes[1].Item2, 0.002);
            check("둘째 돌진이 현재 플레이어 쪽으로 이동하는 방향", 1, chargeBody.position.x < changes[4].Item3 ? 1 : 0, 0);
            check("준비 중 정지 검증 실행 여부", 1, checkedWindup ? 1 : 0, 0);
            finished = true;
        }
    } catch (System.Exception exception) {
        Debug.LogException(exception);
        finished = true;
    } finally {
        if (finished) {
            EditorApplication.update -= observe;

            foreach (var projectile in UnityEngine.Object.FindObjectsByType<Mech.Function.EnemyProjectile>(FindObjectsSortMode.None)) {
                if (projectile.transform.position.x > 3900f && projectile.transform.position.y > 700f) {
                    UnityEngine.Object.Destroy(projectile.gameObject);
                }
            }

            if (root != null) {
                UnityEngine.Object.Destroy(root);
            }

            Debug.Log("[Enemy Behavior Check] 종료; 기록 건수: " + checks.Count);
        }
    }
};
EditorApplication.update += observe;
return new { unityVersion = Application.unityVersion, timeLimitSeconds = 7.5 };
