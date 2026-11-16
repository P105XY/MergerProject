using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;

[InitializeOnLoad]
public static class VerifyBossArena
{
    private const string Running = "VerifyBossArena.Running";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
    private static int checks;

    // 도메인을 다시 불러온 뒤에도 플레이 시작 이벤트에 연결한다.
    static VerifyBossArena()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += OnPlayStateChanged;
        }
    }

    // 저장된 PlayTestScene을 열어 실제 프리팹 인스턴스로 검증한다.
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestScene/PlayTestScene.unity", OpenSceneMode.Single);

        if (scene.isDirty) {
            throw new InvalidOperationException("저장된 보스 테스트 씬을 열 수 없습니다.");
        }

        var boss = UnityEngine.Object.FindAnyObjectByType<BossEnemy>();
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(boss.gameObject);
        SessionState.SetString("VerifyBossArena.PrefabPath", prefabPath);
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= OnPlayStateChanged;
        EditorApplication.playModeStateChanged += OnPlayStateChanged;
        EditorApplication.EnterPlaymode();
    }

    // 플레이 모드에서 실제 2D 물리와 프리팹 연결을 검증한다.
    private static void OnPlayStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exit = 0;

        try {
            Physics2D.simulationMode = SimulationMode2D.Script;
            Time.fixedDeltaTime = 0.02f;
            var storeField = typeof(CheckpointProgress).GetField("saveStore", Private);
            var directory = Path.Combine(Path.GetTempPath(), "merger-boss-arena-" + Guid.NewGuid().ToString("N"));
            storeField.SetValue(null, Activator.CreateInstance(storeField.FieldType, Private | BindingFlags.Public,
                null, new object[] { directory }, null));
            Verify();
            Debug.Log("[BossArenaCheck] PASS " + checks + "/" + checks);
        } catch (Exception error) {
            Debug.LogError("[BossArenaCheck] " + error);
            exit = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exit);
    }

    // 카메라와 진입 경로, 네 패턴 및 파편 수집을 저장된 씬에서 확인한다.
    private static void Verify()
    {
        var boss = UnityEngine.Object.FindAnyObjectByType<BossEnemy>();
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
        var body = player.GetComponent<Rigidbody2D>();
        var hitbox = player.GetComponent<Collider2D>();
        var left = GameObject.Find("Entrance Wall").GetComponent<BoxCollider2D>();
        var right = GameObject.Find("Far Wall").GetComponent<BoxCollider2D>();
        var floor = GameObject.Find("Arena Floor").GetComponent<BoxCollider2D>();
        var checkpoint = GameObject.Find("Boss Arena Checkpoint").GetComponent<Checkpoint>();
        var cameraFollow = Camera.main.GetComponent<PositionConstraint>();
        string prefabPath = SessionState.GetString("VerifyBossArena.PrefabPath", "");

        // 저장된 보스 인스턴스와 메인 카메라가 기존 플레이어에 연결된다.
        Check("저장된 보스 프리팹과 체력", "TestBoss.prefab / 2250", prefabPath + " / " + boss.CurrentHealth,
            prefabPath.EndsWith("/TestBoss.prefab", StringComparison.Ordinal) && boss.CurrentHealth == 2250f);
        Check("카메라 제약", "플레이어 X축", cameraFollow.translationAxis,
            cameraFollow.constraintActive && cameraFollow.translationAxis == Axis.X && cameraFollow.GetSource(0).sourceTransform == player.transform);

        // 입구 체크포인트는 기존 저장 시스템에 새 복귀 위치를 등록한다.
        MovePlayer(player, 6.5f, 0.41f);
        Physics2D.Simulate(Time.fixedDeltaTime);
        typeof(Checkpoint).GetMethod("LateUpdate", Private).Invoke(checkpoint, null);
        bool saved = CheckpointProgress.HasSave();
        Check("입구 트리거 통과", "playtest-boss-entrance / 저장 있음", checkpoint.CheckpointId + " / " + saved,
            checkpoint.CheckpointId == "playtest-boss-entrance" && saved);

        // 실제 플레이어의 기존 점프와 좌우 이동을 물리 스텝으로 실행해 입구 벽을 넘는다.
        MovePlayer(player, 11.08f, 0.41f);
        typeof(PlayerBase).GetField("movementInput", Private).SetValue(player, 1f);
        typeof(PlayerBase).GetField("jumpRequested", Private).SetValue(player, true);
        typeof(PlayerBase).GetField("jumpHeld", Private).SetValue(player, true);
        var playerStep = typeof(PlayerBase).GetMethod("FixedUpdate", Private);

        for (int step = 0; step < 40; step++) {
            playerStep.Invoke(player, null);
            Physics2D.Simulate(Time.fixedDeltaTime);
        }

        Check("실제 점프로 입구 벽 통과", "플레이어가 벽 오른쪽", player.transform.position.x,
            hitbox.bounds.min.x > left.bounds.max.x && floor.bounds.min.x < left.bounds.min.x);

        // 지정 위치에서 네 패턴을 독립적으로 관찰하도록 검증용 플레이어 바디를 고정한다.
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        hitbox.isTrigger = true;
        MovePlayer(player, 17f, 0.41f);
        float hp = player.CurrentHealth;
        var bossStep = typeof(BossEnemy).GetMethod("FixedUpdate", Private);
        var deadline = typeof(BossEnemy).GetField("nextPatternTime", Private);

        // 대기만 앞당긴 뒤 보스의 실제 고정 갱신과 2D 물리를 한 번 실행한다.
        void TickBoss(bool begin)
        {
            if (begin) {
                deadline.SetValue(boss, Time.fixedTimeAsDouble - 1);
            }

            Physics2D.SyncTransforms();
            bossStep.Invoke(boss, null);
            Physics2D.Simulate(Time.fixedDeltaTime);
        }

        // 돌진이 왼쪽 벽에 멈추며 경로 안 플레이어에게 피해를 준다.
        TickBoss(true);

        for (int step = 0; step < 100 && boss.CurrentState == EnemyState.Attack; step++) {
            TickBoss(false);
        }

        float leftGap = boss.GetComponent<BoxCollider2D>().bounds.min.x - left.bounds.max.x;
        Check("왼쪽 돌진과 벽", "벽 간격 0~0.05, 피해 10", leftGap + " / " + (hp - player.CurrentHealth),
            boss.CurrentState == EnemyState.Idle && leftGap >= 0f && leftGap < 0.05f && hp - player.CurrentHealth == 10f);

        // 보스가 벽에 붙은 상태에서 가까운 전방에 내려찍기를 맞춘다.
        MovePlayer(player, 16.5f, 0.41f);
        hp = player.CurrentHealth;
        TickBoss(true);
        Check("내려찍기", "피해 10", hp - player.CurrentHealth, hp - player.CurrentHealth == 10f);

        // 두 공격 뒤 배치된 프리팹에서 고정된 B 조각 적 세 마리가 나온다.
        MovePlayer(player, 26f, 1.6f);
        TickBoss(true);
        var summoned = UnityEngine.Object.FindObjectsByType<FragmentEnemy>();
        Check("두 공격 뒤 조각 적 소환", "고정된 B 조각 적 3마리", summoned.Length,
            summoned.Length == 3 && summoned.All(enemy => enemy.HeldObjectType == CollectibleType.B
                && enemy.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Static
                && Mathf.Abs(enemy.GetComponent<Collider2D>().bounds.min.y - floor.bounds.max.y) < 0.025f));

        // 실제 접촉 피해 후 두 번 타격하면 B 조각을 드롭하고 플레이어가 수집한다.
        var fragment = summoned[0];
        MovePlayer(player, fragment.transform.position.x, fragment.transform.position.y);
        hp = player.CurrentHealth;
        Physics2D.Simulate(Time.fixedDeltaTime);
        Check("조각 적 접촉", "피해 10", hp - player.CurrentHealth, hp - player.CurrentHealth == 10f);
        GameObject drop = null;
        fragment.FragmentDropped += (_, __, instance) => drop = instance;
        fragment.ApplyPlayerDamage(player.AttackPower);
        Check("조각 적 첫 타격", "HP 10", fragment.CurrentHealth, fragment.CurrentHealth == 10f);
        fragment.ApplyPlayerDamage(player.AttackPower);
        bool collected = drop != null && drop.GetComponent<FragmentPickup>().TryCollect(player);
        Check("두 번째 타격과 B 조각", "처치, B 조각 1개", player.TypeBCount, fragment.IsDefeated && collected && player.TypeBCount == 1);

        // 내려찍기 범위 밖의 높은 전방 위치에서 광역 공격에 맞는다.
        MovePlayer(player, 20f, 1.6f);
        hp = player.CurrentHealth;
        TickBoss(true);
        Check("광역 공격", "피해 10", hp - player.CurrentHealth, hp - player.CurrentHealth == 10f);

        // 반대 방향 돌진이 오른쪽 벽에서도 멈춘다.
        TickBoss(true);

        for (int step = 0; step < 100 && boss.CurrentState == EnemyState.Attack; step++) {
            TickBoss(false);
        }

        float rightGap = right.bounds.min.x - boss.GetComponent<BoxCollider2D>().bounds.max.x;
        Check("오른쪽 돌진과 벽", "벽 간격 0~0.05", rightGap,
            boss.CurrentState == EnemyState.Idle && rightGap >= 0f && rightGap < 0.05f);
    }

    // 이전 접촉 피해 재사용 대기를 지우고 플레이어를 검증 지점에 배치한다.
    private static void MovePlayer(PlayerBase player, float x, float y)
    {
        var body = player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, y);
        body.linearVelocity = Vector2.zero;
        player.transform.position = new Vector3(x, y, 0f);
        typeof(PlayerBase).GetField("nextContactDamageTime", Private).SetValue(player, Time.time - 1f);
        Physics2D.SyncTransforms();
    }

    // 입력과 기대값 및 실제값을 기록하고 불일치 시 중단한다.
    private static void Check(string input, string expected, object actual, bool passed)
    {
        string line = "[BossArenaCheck] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!passed) {
            throw new InvalidOperationException(line);
        }

        checks++;
    }
}
