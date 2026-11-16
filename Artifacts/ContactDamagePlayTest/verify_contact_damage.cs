// 저장된 PlayTestScene의 플레이 모드에서 Unity MCP execute_code에 메서드 본문을 전달한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused) {
    throw new System.InvalidOperationException("일시 정지하지 않은 플레이 모드가 필요합니다.");
}

var root = new GameObject("Contact Damage Verification");
root.transform.position = new Vector3(100f, 100f, 0f);
var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var physicsScene = UnityEngine.PhysicsSceneExtensions2D.GetPhysicsScene2D(activeScene);
var previousMode = Physics2D.simulationMode;
var privateInstance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var attackPower = typeof(Mech.Function.EnemyBase).GetField("attackPower", privateInstance);
var obstacleDamage = typeof(Mech.Function.DamagedObstacle).GetField("damage", privateInstance);
var checks = new System.Collections.Generic.List<string>();

// 독립적인 충돌을 위해 플레이어와 물리 바디를 정해진 위치에 만든다.
System.Func<int, Mech.Function.PlayerBase> makePlayer = slot => {
    var playerObject = new GameObject("Player " + slot);
    playerObject.transform.SetParent(root.transform, false);
    playerObject.transform.localPosition = Vector3.right * (slot * 6f);
    playerObject.AddComponent<BoxCollider2D>();
    var body = playerObject.AddComponent<Rigidbody2D>();
    body.constraints = RigidbodyConstraints2D.FreezeAll;
    var player = playerObject.AddComponent<Mech.Function.PlayerBase>();
    body.gravityScale = 0f;
    return player;
};

// 피해 컴포넌트를 루트에 붙일 수 있도록 충돌 영역을 자식으로 만든다.
System.Func<int, bool, float, GameObject> makeSource = (slot, trigger, offset) => {
    var source = new GameObject("Source " + slot);
    source.transform.SetParent(root.transform, false);
    source.transform.localPosition = Vector3.right * (slot * 6f + offset);
    var hitbox = new GameObject("Child collider");
    hitbox.transform.SetParent(source.transform, false);
    hitbox.AddComponent<BoxCollider2D>().isTrigger = trigger;
    return source;
};

// 입력과 기대값 및 실제값을 기록하고 차이가 있으면 검증을 실패시킨다.
System.Action<string, float, float> check = (input, expected, actual) => {
    string message = string.Format("[Contact Damage Check] 입력: {0}; 기대: {1}; 실제: {2}", input, expected, actual);
    Debug.Log(message);
    checks.Add(message);
    UnityEngine.Assertions.Assert.IsTrue(Mathf.Approximately(expected, actual), message);
    if (!Mathf.Approximately(expected, actual)) {
        throw new System.InvalidOperationException(message);
    }
};

try {
    Physics2D.simulationMode = SimulationMode2D.Script;
    var enemySolid = makePlayer(0);
    attackPower.SetValue(makeSource(0, false, 0.75f).AddComponent<Mech.Function.EnemyBase>(), 16f);
    var enemyTrigger = makePlayer(1);
    attackPower.SetValue(makeSource(1, true, 0.75f).AddComponent<Mech.Function.EnemyBase>(), 23f);
    var hazardSolid = makePlayer(2);
    obstacleDamage.SetValue(makeSource(2, false, 0.75f).AddComponent<Mech.Function.DamagedObstacle>(), 12f);
    var hazardTrigger = makePlayer(3);
    makeSource(3, true, 0.75f).AddComponent<Mech.Function.DamagedObstacle>();
    var normal = makePlayer(4);
    makeSource(4, false, 0.75f);
    makeSource(4, true, -0.75f);
    var deadEnemy = makePlayer(5);
    makeSource(5, false, 0.75f).AddComponent<Mech.Function.EnemyBase>().TryChangeState(Mech.Function.EnemyState.Dead);
    var disabledHazard = makePlayer(6);
    makeSource(6, true, 0.75f).AddComponent<Mech.Function.DamagedObstacle>().enabled = false;
    var simultaneous = makePlayer(7);
    makeSource(7, false, 0.75f).AddComponent<Mech.Function.EnemyBase>();
    makeSource(7, true, -0.75f).AddComponent<Mech.Function.DamagedObstacle>();
    var clamped = makePlayer(8);
    clamped.ApplyDamage(95f);
    makeSource(8, true, 0.75f).AddComponent<Mech.Function.DamagedObstacle>();
    var fragmentPlayer = makePlayer(9);
    makeSource(9, true, -0.75f).AddComponent<Mech.Function.DamagedObstacle>();
    var pickup = makeSource(9, true, 0.75f).AddComponent<Mech.Function.FragmentPickup>();
    typeof(Mech.Function.FragmentPickup).GetMethod("Initialize", privateInstance).Invoke(pickup, new object[] { Mech.Function.CollectibleType.B });
    var leaver = makePlayer(10);
    var leftHazard = makeSource(10, true, 0.75f);
    leftHazard.AddComponent<Mech.Function.DamagedObstacle>();

    Physics2D.SyncTransforms();
    physicsScene.Simulate(Time.fixedDeltaTime);

    // 적 공격력 16과 23을 각각 일반·트리거 충돌의 피해로 기대한다.
    check("공격력 16인 적의 일반 자식 콜라이더", 84f, enemySolid.CurrentHealth);
    check("공격력 23인 적의 트리거 자식 콜라이더", 77f, enemyTrigger.CurrentHealth);
    // 장애물의 지정 피해 12와 기본 피해 10을 각각 기대한다.
    check("피해량 12인 일반 장애물", 88f, hazardSolid.CurrentHealth);
    check("기본 피해량 10인 트리거 장애물", 90f, hazardTrigger.CurrentHealth);
    // 일반 콜라이더, 사망 상태 적, 꺼진 장애물은 체력을 줄이지 않아야 한다.
    check("일반 지형과 피해 없는 트리거 동시 접촉", 100f, normal.CurrentHealth);
    check("사망 상태 적의 일반 콜라이더", 100f, deadEnemy.CurrentHealth);
    check("비활성화된 장애물 컴포넌트의 트리거", 100f, disabledHazard.CurrentHealth);
    // 두 위험 원천에 동시에 닿으면 피해 10 한 번만 받고, 체력 하한은 0이다.
    check("피해량 10인 적과 장애물의 동시 접촉", 90f, simultaneous.CurrentHealth);
    check("체력 5에서 장애물 피해 10", 0f, clamped.CurrentHealth);
    check("곧 벗어날 트리거에 첫 접촉", 90f, leaver.CurrentHealth);

    leftHazard.transform.localPosition += Vector3.right * 2f;
    Physics2D.SyncTransforms();
    physicsScene.Simulate(Time.fixedDeltaTime);

    // 게임 시간이 그대로일 때 접촉이 이어져도 피해를 더 받지 않고 조각은 수집한다.
    check("같은 게임 시각에 일반 충돌 유지", 88f, hazardSolid.CurrentHealth);
    check("같은 게임 시각에 트리거 충돌 유지", 90f, hazardTrigger.CurrentHealth);
    check("접촉 피해 대기 중 B 조각 트리거", 1f, fragmentPlayer.TypeBCount);

    foreach (Transform child in root.transform) {
        Vector3 position = child.localPosition;

        if (position.x < 11f || (position.x > 19f && position.x < 59f)) {
            child.gameObject.SetActive(false);
        }
    }

    hazardTrigger.GetComponent<Rigidbody2D>().Sleep();
    float startTime = Time.time;
    EditorApplication.CallbackFunction observe = null;

    // 실제 게임 시간이 0.75초를 지나면 지속 접촉 피해와 임시 물체를 확인한다.
    observe = () => {
        if (EditorApplication.isPlaying && Time.time - startTime < 0.9f) {
            return;
        }

        EditorApplication.update -= observe;
        try {
            if (EditorApplication.isPlaying) {
                string elapsed = (Time.time - startTime).ToString("F3");
                check("일반 장애물에 " + elapsed + " 게임 초 동안 접촉 유지", 76f, hazardSolid.CurrentHealth);
                check("물리 바디를 잠재운 상태로 트리거에 " + elapsed + " 게임 초 동안 접촉 유지", 80f, hazardTrigger.CurrentHealth);
                check("트리거를 벗어나 " + elapsed + " 게임 초 대기", 90f, leaver.CurrentHealth);
            }
        } finally {
            if (root != null) {
                UnityEngine.Object.Destroy(root);
            }
        }
    };
    EditorApplication.update += observe;
    return new { unityVersion = Application.unityVersion, immediateChecks = checks, timedChecksPending = 3 };
} catch {
    UnityEngine.Object.Destroy(root);
    throw;
} finally {
    Physics2D.simulationMode = previousMode;
}
