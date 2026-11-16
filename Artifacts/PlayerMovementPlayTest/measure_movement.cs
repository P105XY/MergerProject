// MCP execute_code 메서드 본문으로 실행한다. OS 입력 장치나 에셋 저장을 사용하지 않는다.
if (!UnityEditor.EditorApplication.isPlaying || !UnityEditor.EditorApplication.isPaused) {
    throw new System.InvalidOperationException("Paused PlayMode required");
}
var player = UnityEngine.Object.FindFirstObjectByType<Mech.Function.PlayerBase>();
var body = player.GetComponent<UnityEngine.Rigidbody2D>();
var physics = UnityEngine.PhysicsSceneExtensions2D.GetPhysicsScene2D(player.gameObject.scene);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Mech.Function.PlayerBase);
var fixedMethod = type.GetMethod("FixedUpdate", flags);
var resetMethod = type.GetMethod("OnDisable", flags);
var groundMethod = type.GetMethod("IsGrounded", flags);
var inputField = type.GetField("movementInput", flags);
var requestField = type.GetField("jumpRequested", flags);
var heldField = type.GetField("jumpHeld", flags);
var oldMode = UnityEngine.Physics2D.simulationMode;
var wasEnabled = player.enabled;
var oldPosition = body.position;
var oldVelocity = body.linearVelocity;
var disabledEnemies = new System.Collections.Generic.List<UnityEngine.GameObject>();
var checks = new System.Collections.Generic.List<object>();
float dt = UnityEngine.Time.fixedDeltaTime;
float maximumSpeed = (float)type.GetField("maximumMovementSpeed", flags).GetValue(player);
var ledge = new UnityEngine.GameObject("Temporary movement ledge");
ledge.transform.position = new UnityEngine.Vector3(30f, -0.5f, 0f);
ledge.AddComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(4f, 1f);
// 입력과 기대 조건 및 실제값을 출력하고 실패 시 검증을 중단한다.
System.Action<string, string, object, bool> check = (input, expected, actual, passed) => {
    checks.Add(new { input, expected, actual, passed });
    UnityEngine.Debug.Log("입력: " + input + ", 기대: " + expected + ", 실제: " + actual);

    if (!passed) {
        throw new System.Exception("Assertion failed: " + input);
    }
};
// 실제 PlayerBase 이동 콜백과 Unity 2D 물리를 한 고정 간격 진행한다.
System.Action<int, bool, bool> step = (direction, requested, held) => {
    inputField.SetValue(player, (float)direction);
    requestField.SetValue(player, requested);
    heldField.SetValue(player, held);
    fixedMethod.Invoke(player, null);

    if (!physics.Simulate(dt)) {
        throw new System.Exception("Physics simulation failed");
    }
};
// 같은 플레이어를 초기 위치로 옮기고 실제 지면에 안정시킨다.
System.Action<UnityEngine.Vector2> reset = position => {
    resetMethod.Invoke(player, null);
    body.position = position;
    body.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();

    for (int index = 0; index < 12; index++) {
        step(0, false, false);
    }
};
try {
    UnityEngine.Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
    player.enabled = false;

    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Mech.Function.EnemyBase>(UnityEngine.FindObjectsSortMode.None)) {
        if (enemy.gameObject.activeSelf) {
            disabledEnemies.Add(enemy.gameObject);
            enemy.gameObject.SetActive(false);
        }
    }

    var origin = new UnityEngine.Vector2(-5f, 0.41f);
    reset(origin);
    // 입력: 우측 이동 → 기대: 속도가 최대 목표의 95%에 도달하고 과속하지 않는다.
    int accelerationTicks = 0;
    float highestSpeed = 0f;

    for (int tick = 0; tick < 25; tick++) {
        step(1, false, false);
        highestSpeed = UnityEngine.Mathf.Max(highestSpeed, body.linearVelocity.x);

        if (accelerationTicks == 0 && body.linearVelocity.x >= maximumSpeed * 0.95f) {
            accelerationTicks = tick + 1;
        }
    }

    check("우측 0.5초", "최대 속도 도달, 초과 없음", highestSpeed, accelerationTicks > 0 && highestSpeed <= maximumSpeed + 0.01f);
    float stopStart = body.position.x;
    int stopTicks = 0;

    for (int tick = 0; tick < 30; tick++) {
        step(0, false, false);

        if (UnityEngine.Mathf.Abs(body.linearVelocity.x) <= 0.01f) {
            stopTicks = tick + 1;
            break;
        }
    }

    float stopDistance = body.position.x - stopStart;
    check("이동키 해제", "0.6초 안에 정지", stopTicks * dt, stopTicks > 0);
    // 입력: 최대 속도에서 반대 방향 → 기대: 반대 최대 속도까지 전환.
    reset(origin);

    for (int tick = 0; tick < 25; tick++) {
        step(1, false, false);
    }

    int reversalTicks = 0;

    for (int tick = 0; tick < 40; tick++) {
        step(-1, false, false);

        if (body.linearVelocity.x <= -maximumSpeed * 0.95f) {
            reversalTicks = tick + 1;
            break;
        }
    }

    check("반대 방향 입력", "0.8초 안에 반대 최대 속도", reversalTicks * dt, reversalTicks > 0);
    // 입력: 유지 점프와 0.08초 뒤 해제 → 기대: 높이 차이, 공중 재점프 없음, 최대 높이 준수.
    float[] heights = new float[2];
    float[] apexTimes = new float[2];
    float[] flightTimes = new float[2];

    for (int variant = 0; variant < 2; variant++) {
        reset(origin);
        float startHeight = body.position.y;
        step(0, true, true);
        float jumpVelocity = body.linearVelocity.y;
        step(0, true, true);
        check("점프 중 재요청 " + variant, "상승 속도 증가 없음", body.linearVelocity.y, body.linearVelocity.y <= jumpVelocity + 0.01f);

        for (int tick = 2; tick < 180; tick++) {
            step(0, false, variant == 0 || tick < 4);
            float height = body.position.y - startHeight;

            if (height > heights[variant]) {
                heights[variant] = height;
                apexTimes[variant] = (tick + 1) * dt;
            }

            if ((bool)groundMethod.Invoke(player, null) && body.linearVelocity.y <= 0f) {
                flightTimes[variant] = (tick + 1) * dt;
                break;
            }
        }

        check("점프 착지 " + variant, "3.6초 안에 착지", flightTimes[variant], flightTimes[variant] > 0f);
        check("최대 높이 " + variant, "3.02 이하", heights[variant], heights[variant] <= 3.02f);
        step(0, false, true);
        check("착지 후 계속 누름 " + variant, "자동 재점프 없음", body.linearVelocity.y, body.linearVelocity.y <= 0.01f);
    }

    check("점프 버튼 조기 해제", "유지 점프보다 낮음", heights[1], heights[1] < heights[0] * 0.8f);
    // 입력: 실제 발판 이탈 뒤 0.06초 및 0.18초에 점프 → 기대: 각각 허용과 거부.
    bool[] coyoteResults = new bool[2];

    for (int variant = 0; variant < 2; variant++) {
        reset(new UnityEngine.Vector2(31.8f, 0.91f));
        int walked = 0;

        while ((bool)groundMethod.Invoke(player, null) && walked < 40) {
            step(1, false, false);
            walked++;
        }

        check("발판 이탈 " + variant, "실제 접지 상실", walked, walked > 0 && walked < 40);
        int delayTicks = variant == 0 ? 3 : 9;

        for (int tick = 0; tick < delayTicks; tick++) {
            step(1, false, false);
        }

        step(1, true, true);
        coyoteResults[variant] = body.linearVelocity.y > 0f;
        check("발판 이탈 후 " + delayTicks * dt + "초", variant == 0 ? "점프 허용" : "점프 거부", body.linearVelocity.y, coyoteResults[variant] == (variant == 0));
    }

    return new {
        mode = "Paused PlayMode, real PhysicsScene2D.Simulate; movement fields supplied by test, no keyboard device",
        fixedDeltaTime = dt,
        accelerationTime = accelerationTicks * dt,
        stopTime = stopTicks * dt,
        stopDistance,
        reversalTime = reversalTicks * dt,
        fullJumpHeight = heights[0],
        shortJumpHeight = heights[1],
        fullJumpApexTime = apexTimes[0],
        shortJumpApexTime = apexTimes[1],
        fullJumpFlightTime = flightTimes[0],
        shortJumpFlightTime = flightTimes[1],
        assertions = checks
    };
} finally {
    UnityEngine.Object.DestroyImmediate(ledge);
    body.position = oldPosition;
    body.linearVelocity = oldVelocity;
    resetMethod.Invoke(player, null);
    player.enabled = wasEnabled;

    foreach (var enemyObject in disabledEnemies) {
        enemyObject.SetActive(true);
    }

    UnityEngine.Physics2D.simulationMode = oldMode;
    UnityEngine.Physics2D.SyncTransforms();
}
