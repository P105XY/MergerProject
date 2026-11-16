// 일시 정지한 플레이 테스트 씬에서 실제 물리 경로와 화면 표시 데이터를 비교한다.
if (!EditorApplication.isPlaying || !EditorApplication.isPaused) {
    throw new System.InvalidOperationException("일시 정지한 PlayTestScene이 필요합니다.");
}

var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
var display = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
var body = player.GetComponent<Rigidbody2D>();
var scene = UnityEngine.PhysicsSceneExtensions2D.GetPhysicsScene2D(player.gameObject.scene);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var playerType = typeof(Mech.Function.PlayerBase);
var fixedUpdate = playerType.GetMethod("FixedUpdate", flags);
var resetInput = playerType.GetMethod("OnDisable", flags);
var moveInput = playerType.GetField("movementInput", flags);
var jumpRequest = playerType.GetField("jumpRequested", flags);
var jumpHold = playerType.GetField("jumpHeld", flags);
var updateDisplay = typeof(PlayTestDisplay).GetMethod("LateUpdate", flags);
var path = typeof(PlayTestDisplay).GetField("jumpPath", flags).GetValue(display);
var pathType = path.GetType();
var predicted = (System.Collections.Generic.List<Vector2>)pathType.GetField("predicted", flags).GetValue(path);
var traveled = (System.Collections.Generic.List<Vector2>)pathType.GetField("traveled", flags).GetValue(path);
var tracking = pathType.GetField("tracking", flags);
var results = new System.Collections.Generic.List<object>();
var previousSimulation = Physics2D.simulationMode;
var wall = new GameObject("Temporary jump prediction wall");
wall.SetActive(false);
wall.transform.position = new Vector3(-2.65f, 2f, 0f);
wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 5f);
float dt = Time.fixedDeltaTime;

// 예상 조건과 실제값을 기록하고 결과가 다르면 검증을 중단한다.
System.Action<string, string, object, bool> check = (input, expected, actual, passed) => {
    Debug.Log(string.Format("[Jump Path] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual));
    results.Add(new { input, expected, actual, passed });

    if (!passed) {
        throw new System.InvalidOperationException("점프 경로 불일치: " + input);
    }
};

// 실제 플레이어 물리 콜백과 표시 갱신을 고정 시간으로 한 단계 진행한다.
System.Action<int, bool, bool> step = (direction, pressed, held) => {
    moveInput.SetValue(player, (float)direction);
    jumpRequest.SetValue(player, pressed);
    jumpHold.SetValue(player, held);
    fixedUpdate.Invoke(player, null);

    if (!scene.Simulate(dt)) {
        throw new System.InvalidOperationException("2D 물리 실행에 실패했습니다.");
    }

    updateDisplay.Invoke(display, null);
};

// 같은 지면에서 매번 새로운 점프를 시작할 수 있도록 플레이어를 놓는다.
System.Action reset = () => {
    resetInput.Invoke(player, null);
    var position = new Vector2(-5f, 0.41f);
    body.position = position;
    player.transform.position = position;
    body.linearVelocity = Vector2.zero;
    body.WakeUp();
    Physics2D.SyncTransforms();

    for (int index = 0; index < 10; index++) {
        step(0, false, false);
    }
};

// 현재 예상 점들에서 최고점을 구한다.
System.Func<float> highestPrediction = () => {
    float highest = float.NegativeInfinity;

    foreach (Vector2 point in predicted) {
        highest = Mathf.Max(highest, point.y);
    }

    return highest;
};

try {
    Physics2D.simulationMode = SimulationMode2D.Script;

    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Mech.Function.EnemyBase>()) {
        enemy.gameObject.SetActive(false);
    }

    reset();
    float groundY = body.position.y;
    step(1, true, true);
    // 첫 유효 점프는 실제 이륙점과 나아가는 방향의 예측 점을 만든다.
    check("첫 점프의 실제 및 예측 표시", "실제 2점 이상, 예측 3점 이상", new { traveled = traveled.Count, predicted = predicted.Count }, traveled.Count >= 2 && predicted.Count >= 3);
    Vector2 origin = traveled[0];
    float fullPredicted = highestPrediction();
    float fullLandingX = predicted[predicted.Count - 1].x;
    check("계속 오른쪽으로 이동하는 점프의 끝점", "이륙점보다 오른쪽", fullLandingX, fullLandingX > origin.x + 1f);
    step(1, true, true);
    check("공중 재입력", "같은 이륙점과 누적 경로", traveled.Count, traveled[0] == origin && traveled.Count >= 3);
    float actualApex = body.position.y;

    for (int index = 0; index < 80 && (bool)tracking.GetValue(path); index++) {
        step(1, false, true);
        actualApex = Mathf.Max(actualApex, body.position.y);
    }

    // 실제 지면 착지에서 예상 점이 사라지고 누적 실선이 남는다.
    check("유지 점프의 최고점 오차", "0.08 유닛 이내", Mathf.Abs(actualApex - fullPredicted), Mathf.Abs(actualApex - fullPredicted) < 0.08f);
    check("착지 X 오차", "0.15 유닛 이내", Mathf.Abs(body.position.x - fullLandingX), Mathf.Abs(body.position.x - fullLandingX) < 0.15f);
    check("착지한 뒤 남는 경로", "추적 종료, 예측 없음, 실제 이동 3점 이상", new { tracking = tracking.GetValue(path), traveled = traveled.Count, predicted = predicted.Count }, !(bool)tracking.GetValue(path) && traveled.Count >= 3 && predicted.Count == 0);

    reset();
    step(1, true, true);

    for (int index = 0; index < 3; index++) {
        step(1, false, true);
    }

    float beforeRelease = highestPrediction();
    step(-1, false, false);
    float afterRelease = highestPrediction();
    float reversedLandingX = predicted[predicted.Count - 1].x;
    // 버튼 조기 해제와 방향 반전은 같은 점프의 남은 궤적에 반영된다.
    check("상승 중 버튼 해제", "예상 최고점 0.3 유닛 이상 감소", beforeRelease - afterRelease, beforeRelease - afterRelease > 0.3f);
    check("상승 중 왼쪽 방향으로 전환", "예상 착지점이 오른쪽 유지보다 왼쪽", reversedLandingX, reversedLandingX < fullLandingX - 1f);
    check("조기 해제 예상 높이", "이륙점에서 0.5~2.2 유닛", afterRelease - groundY, afterRelease - groundY > 0.5f && afterRelease - groundY < 2.2f);

    reset();
    wall.SetActive(true);
    Physics2D.SyncTransforms();
    step(1, true, true);
    float blockedX = predicted[predicted.Count - 1].x;
    // 앞쪽 벽과 맞닿는 지점에서 예상 경로가 멈춰야 한다.
    check("앞쪽 장애물", "벽 앞 -3.15 부근에서 예측 정지", blockedX, Mathf.Abs(blockedX + 3.15f) < 0.12f);
    wall.SetActive(false);
    reset();
    step(1, true, true);

    for (int index = 0; index < 9; index++) {
        step(1, false, true);
    }

    return new { passed = results.Count, results, fullPredictedHeight = fullPredicted - groundY, actualHeight = actualApex - groundY };
} finally {
    wall.SetActive(false);
    Physics2D.simulationMode = previousSimulation;
}
