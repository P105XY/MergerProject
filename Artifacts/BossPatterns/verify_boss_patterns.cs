using System;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BossPatternChecks
{
    private const string RunningKey = "BossPatternChecks.Running";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo NextPattern = typeof(BossEnemy).GetField("nextPatternTime", PrivateInstance);
    private static readonly MethodInfo FixedStep = typeof(BossEnemy).GetMethod("FixedUpdate", PrivateInstance);
    private static int checks;

    // 도메인 재로딩 뒤에도 플레이 시작 이벤트에 검증을 연결한다.
    static BossPatternChecks()
    {
        if (SessionState.GetBool(RunningKey, false)) {
            EditorApplication.playModeStateChanged += OnPlayStateChanged;
        }
    }

    // 임시 프로젝트의 빈 씬에서 플레이 모드를 시작한다.
    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/BossVerificationTemporary.unity");
        SessionState.SetBool(RunningKey, true);
        EditorApplication.playModeStateChanged -= OnPlayStateChanged;
        EditorApplication.playModeStateChanged += OnPlayStateChanged;
        EditorApplication.EnterPlaymode();
    }

    // 플레이 모드에서 실제 2D 물리를 수동으로 진행하고 결과를 종료 코드로 전달한다.
    private static void OnPlayStateChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exitCode = 0;

        try {
            Physics2D.simulationMode = SimulationMode2D.Script;
            Time.fixedDeltaTime = 0.02f;
            Verify();
            Debug.Log("[BossPatterns] PASS " + checks + "/" + checks);
        } catch (Exception error) {
            Debug.LogError("[BossPatterns] " + error);
            exitCode = 1;
        }

        SessionState.EraseBool(RunningKey);
        EditorApplication.Exit(exitCode);
    }

    // 네 패턴과 소환 적 피해, 조각 드롭 및 보스 사망을 실제 충돌체로 검사한다.
    private static void Verify()
    {
        Solid("Floor", new Vector2(0f, -0.5f), new Vector2(60f, 1f));
        Solid("Left Wall", new Vector2(-14f, 2f), new Vector2(1f, 4f));
        Solid("Right Wall", new Vector2(14f, 2f), new Vector2(1f, 4f));
        var dropTemplate = new GameObject("Boss Check Fragment Drop");
        dropTemplate.transform.position = new Vector3(200f, 20f);
        dropTemplate.AddComponent<CircleCollider2D>().isTrigger = true;
        var templateHitbox = Solid("Boss Check Fragment", new Vector2(100f, 5f), Vector2.one);
        templateHitbox.gameObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var template = templateHitbox.gameObject.AddComponent<FragmentEnemy>();
        typeof(EnemyBase).GetField("heldObject", PrivateInstance).SetValue(template, dropTemplate);
        typeof(EnemyBase).GetField("heldObjectType", PrivateInstance).SetValue(template, CollectibleType.B);

        var playerHitbox = Solid("Boss Check Player", new Vector2(9f, 1f), Vector2.one * 0.5f);
        playerHitbox.isTrigger = true;
        var playerBody = playerHitbox.gameObject.AddComponent<Rigidbody2D>();
        playerBody.bodyType = RigidbodyType2D.Kinematic;
        playerBody.useFullKinematicContacts = true;
        var player = playerHitbox.gameObject.AddComponent<PlayerBase>();
        var bossHitbox = Solid("Boss Check Boss", new Vector2(0f, 1f), Vector2.one * 2f);
        var boss = bossHitbox.gameObject.AddComponent<BossEnemy>();
        typeof(BossEnemy).GetField("fragmentEnemyPrefab", PrivateInstance).SetValue(boss, template);
        typeof(TargetingEnemy).GetField("targetPlayer", PrivateInstance).SetValue(boss, player);

        // 시작 체력은 2250이며 첫 3초가 지나기 전에는 정지한다.
        Check("보스 초기 체력", "2250 / 2250", boss.CurrentHealth + " / " + boss.MaxHealth,
            boss.CurrentHealth == 2250f && boss.MaxHealth == 2250f);
        CheckDelay(boss, 3, "첫 패턴");

        // 돌진은 지정 방향 벽에서 끝나며 경로에 있는 플레이어를 한 번 타격한다.
        BeginPattern(boss);

        for (int step = 0; step < 100 && boss.CurrentState == EnemyState.Attack; step++) {
            Tick(boss);
        }

        Check("오른쪽 돌진 정지 X", "12.49", boss.transform.position.x,
            boss.CurrentState == EnemyState.Idle && Mathf.Abs(boss.transform.position.x - 12.49f) < 0.025f);
        Check("오른쪽 돌진 경로의 플레이어 HP", "90", player.CurrentHealth, player.CurrentHealth == 90f);
        CheckDelay(boss, 3, "돌진 후");

        // 가까운 전방 플레이어는 내려찍기에 맞고 소환은 다음 패턴까지 발생하지 않는다.
        MovePlayer(player, 10.1f, 1f);
        BeginPattern(boss);
        Check("내려찍기 플레이어 HP", "80", player.CurrentHealth, player.CurrentHealth == 80f);
        int beforeSummon = Fragments(template).Length;
        Check("공격 2회 직후 소환 수", "0", beforeSummon, beforeSummon == 0);
        CheckDelay(boss, 3, "내려찍기 후");

        // 세 번째 패턴에서 벽 안쪽 바닥에 고정된 B 조각 적 세 마리가 등장한다.
        MovePlayer(player, 3f, 1f);
        BeginPattern(boss);
        var firstWave = Fragments(template);
        Check("두 공격 뒤 강제 소환", "3마리", firstWave.Length, firstWave.Length == 3);
        bool onGround = firstWave.All(fragment => Mathf.Abs(fragment.GetComponent<Collider2D>().bounds.min.y) < 0.025f
            && fragment.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Static
            && fragment.transform.position.x < 13.5f && fragment.HeldObjectType == CollectibleType.B);
        Check("첫 소환 위치·고정·조각 종류", "바닥 안쪽, Static, B", onGround, onGround);
        CheckDelay(boss, 5, "소환 후");

        // 소환 적과 실제 물리 접촉하면 피해를 받고, 처치하면 B 조각을 하나 받는다.
        var contactFragment = firstWave[0];
        MovePlayer(player, contactFragment.transform.position.x, contactFragment.transform.position.y);
        Tick(boss);
        Check("소환 적 접촉 후 HP", "70", player.CurrentHealth, player.CurrentHealth == 70f);
        int drops = 0;
        contactFragment.FragmentDropped += (_, type, drop) => {
            if (type == CollectibleType.B && drop != null) {
                drops++;
            }
        };
        contactFragment.ApplyPlayerDamage(contactFragment.CurrentHealth);
        bool colliderEnabled = contactFragment.GetComponent<Collider2D>().enabled;
        Check("소환 적 처치", "B 조각 1개, 충돌 해제", drops + " / " + colliderEnabled, drops == 1 && !colliderEnabled);

        // 내려찍기보다 먼 위치를 광역 공격이 타격하며, 다음 돌진은 반대편 벽에서 끝난다.
        MovePlayer(player, 6.5f, 2f);
        BeginPattern(boss);
        Check("광역 공격의 먼 전방 표적 HP", "60", player.CurrentHealth, player.CurrentHealth == 60f);
        CheckDelay(boss, 3, "광역 후");
        BeginPattern(boss);

        for (int step = 0; step < 100 && boss.CurrentState == EnemyState.Attack; step++) {
            Tick(boss);
        }

        Check("왼쪽 돌진 정지 X", "-12.49", boss.transform.position.x,
            boss.CurrentState == EnemyState.Idle && Mathf.Abs(boss.transform.position.x + 12.49f) < 0.025f);
        CheckDelay(boss, 3, "두 번째 돌진 후");

        // 다음 두 공격 뒤 소환이 반복되고 보스를 처치하면 기한이 지나도 패턴이 멈춘다.
        MovePlayer(player, 0f, 2f);
        BeginPattern(boss);
        int secondWave = Fragments(template).Length;
        Check("두 번째 강제 소환", "생존 5마리", secondWave, secondWave == 5);
        boss.ApplyDamage(2250f);
        BeginPattern(boss);
        Check("보스 처치 후 패턴 정지", "Dead, 생존 5마리", boss.CurrentState + " / " + Fragments(template).Length,
            boss.IsDefeated && boss.CurrentState == EnemyState.Dead && Fragments(template).Length == 5);
    }

    // 다음 패턴의 대기값을 확인하고 기한이 오기 전 움직임과 상태가 유지되는지 검사한다.
    private static void CheckDelay(BossEnemy boss, double seconds, string label)
    {
        double delay = (double)NextPattern.GetValue(boss) - Time.fixedTimeAsDouble;
        Vector3 position = boss.transform.position;
        Tick(boss);
        Check(label + " 대기", seconds + "초, 정지", delay.ToString("F3") + "초 / " + boss.CurrentState,
            Math.Abs(delay - seconds) < 0.001 && boss.CurrentState == EnemyState.Idle && boss.transform.position == position);
    }

    // 백그라운드 검증에서 패턴의 대기 기한을 만료시키고 실제 패턴을 호출한다.
    private static void BeginPattern(BossEnemy boss)
    {
        NextPattern.SetValue(boss, Time.fixedTimeAsDouble - 1);
        Tick(boss);
    }

    // 실제 보스의 물리 갱신 한 번과 2D 물리 시뮬레이션 한 번을 실행한다.
    private static void Tick(BossEnemy boss)
    {
        Physics2D.SyncTransforms();
        FixedStep.Invoke(boss, null);
        Physics2D.Simulate(Time.fixedDeltaTime);
    }

    // 플레이어를 검증 위치로 옮기고 이전 패턴의 재피격 대기를 만료시킨다.
    private static void MovePlayer(PlayerBase player, float x, float y)
    {
        player.transform.position = new Vector3(x, y, 0f);
        typeof(PlayerBase).GetField("nextContactDamageTime", PrivateInstance).SetValue(player, Time.time - 1f);
        Physics2D.SyncTransforms();
    }

    // 검증용 사각 충돌체를 생성한다.
    private static BoxCollider2D Solid(string name, Vector2 position, Vector2 size)
    {
        var instance = new GameObject(name);
        instance.transform.position = position;
        var collider = instance.AddComponent<BoxCollider2D>();
        collider.size = size;
        return collider;
    }

    // 실제 소환된 살아 있는 조각 적을 찾는다.
    private static FragmentEnemy[] Fragments(FragmentEnemy template)
    {
        return UnityEngine.Object.FindObjectsByType<FragmentEnemy>()
            .Where(fragment => fragment != template && !fragment.IsDefeated).ToArray();
    }

    // 검증 입력과 기대값 및 실제값을 기록하고 불일치 시 실패시킨다.
    private static void Check(string input, string expected, object actual, bool passed)
    {
        string line = "[BossPatterns] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!passed) {
            throw new InvalidOperationException(line);
        }

        checks++;
    }
}
