using System;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyFragmentLifetime
{
    private const string Running = "VerifyFragmentLifetime.Running";
    private const string WaitingForDestruction = "VerifyFragmentLifetime.WaitingForDestruction";
    private const string ExpiredId = "VerifyFragmentLifetime.ExpiredId";
    private const string PassedCount = "VerifyFragmentLifetime.PassedCount";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int passed;
    private static GameObject expiredObject;
    private static double destructionDeadline;

    // 도메인 재로드 후에도 플레이 모드 검증을 연결한다.
    static VerifyFragmentLifetime()
    {
        if (SessionState.GetBool(WaitingForDestruction, false)) {
            ulong rawId = ulong.Parse(SessionState.GetString(ExpiredId, "0"));
            expiredObject = EditorUtility.EntityIdToObject(EntityId.FromULong(rawId)) as GameObject;
            passed = SessionState.GetInt(PassedCount, 0);
            destructionDeadline = EditorApplication.timeSinceStartup + 15d;
            EditorApplication.update += VerifyDestruction;
        } else if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 프로젝트 복사본의 임시 장면에서 수명 검증을 시작한다.
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPaused = false;
        SessionState.EraseBool(WaitingForDestruction);
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 입력과 기대값 및 실제값을 출력하고 허용 오차를 확인한다.
    private static void Check(string input, double expected, double actual)
    {
        string message = $"[FragmentLifetime] 입력 {input} | 기대 {expected:F5} | 실제 {actual:F5}";
        Debug.Log(message);

        if (double.IsNaN(actual) || Math.Abs(expected - actual) > 0.001d) {
            throw new InvalidOperationException(message);
        }

        passed++;
    }

    // 실제 드롭 프리팹에서 경과 시각별 투명도와 수집 가능 여부를 검사한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        try {
            Physics2D.simulationMode = SimulationMode2D.Script;
            var hero = new GameObject("Player");
            hero.AddComponent<Rigidbody2D>();
            hero.AddComponent<BoxCollider2D>();
            var player = hero.AddComponent<PlayerBase>();
            var source = new GameObject("Drop source").AddComponent<TestEnemy>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayTest/BossFragmentB.prefab");
            typeof(EnemyBase).GetField("heldObject", Private).SetValue(source, prefab);
            typeof(EnemyBase).GetField("heldObjectType", Private).SetValue(source, CollectibleType.B);
            GameObject dropped = null;
            source.FragmentDropped += (_, _, item) => dropped = item;
            source.ApplyDamage(source.CurrentHealth);
            var fragment = dropped.GetComponent<FragmentPickup>();
            var sprite = dropped.GetComponentInChildren<SpriteRenderer>();
            var update = typeof(FragmentPickup).GetMethod("Update", Private);
            var droppedAt = typeof(FragmentPickup).GetField("droppedAt", Private);
            Color original = sprite.color;

            // 지정한 경과 시각으로 기존 업데이트를 실행하고 투명도를 확인한다.
            void Sample(double age, double expectedAlpha)
            {
                droppedAt.SetValue(fragment, Time.timeAsDouble - age);
                update.Invoke(fragment, null);
                Check($"드롭 후 {age:F3}초 Alpha", original.a * expectedAlpha, sprite.color.a);
            }

            // 입력: 0~10초 → 기대: 원래 투명도 유지.
            Sample(0d, 1d);
            Sample(9.999d, 1d);
            Sample(10d, 1d);
            // 입력: 10초 이후 0.3초의 한 주기 → 기대: 원래 Alpha의 1, 0.5, 0, 0.5, 1.
            Sample(10.075d, 0.5d);
            Sample(10.15d, 0d);
            Sample(10.225d, 0.5d);
            Sample(10.3d, 1d);
            // 입력: 13초 이후 0.1초의 한 주기 → 기대: 원래 Alpha의 1, 0.5, 0, 0.5, 1.
            Sample(13d, 1d);
            Sample(13.025d, 0.5d);
            Sample(13.05d, 0d);
            Sample(13.075d, 0.5d);
            Sample(13.1d, 1d);
            Check("흐림 중 원래 R 채널", original.r, sprite.color.r);
            Check("흐림 중 원래 G 채널", original.g, sprite.color.g);
            Check("흐림 중 원래 B 채널", original.b, sprite.color.b);

            // 입력: 특수 준비 중 14.95초의 조각을 수집 시도 → 기대: 조각을 보존하고 수명도 보존.
            player.TryCollectFragment(CollectibleType.A);
            player.TryCollectFragment(CollectibleType.A);
            player.TryCollectFragment(CollectibleType.A);
            Sample(14.95d, 0d);
            double beforeAttempt = (double)droppedAt.GetValue(fragment);
            Check("특수 준비 중 수집 거부", 0d, fragment.TryCollect(player) ? 1d : 0d);
            Check("실패한 수집 시도가 수명에 미치는 영향", beforeAttempt, (double)droppedAt.GetValue(fragment));

            // 입력: 특수를 소진하고 14.999초인 조각을 수집 → 기대: B 한 개 획득.
            typeof(PlayerBase).GetProperty("SpecialUsesRemaining").SetValue(player, 0);
            droppedAt.SetValue(fragment, Time.timeAsDouble - 14.999d);
            Check("수명 직전 수집 성공", 1d, fragment.TryCollect(player) ? 1d : 0d);
            Check("수명 직전 수집한 B 개수", 1d, player.TypeBCount);

            // 입력: 반투명 자식 SpriteRenderer가 있는 조각 → 기대: 원래 Alpha에 흐림 비율을 곱함.
            expiredObject = UnityEngine.Object.Instantiate(prefab);
            var expired = expiredObject.GetComponent<FragmentPickup>();
            var dimSprite = expiredObject.GetComponentInChildren<SpriteRenderer>();
            Color dimColor = dimSprite.color;
            dimColor.a = 0.6f;
            dimSprite.color = dimColor;
            typeof(FragmentPickup).GetMethod("Initialize").Invoke(expired, new object[] { CollectibleType.B });
            droppedAt.SetValue(expired, Time.timeAsDouble - 10.075d);
            update.Invoke(expired, null);
            Check("원래 Alpha 0.6의 반절 흐림", 0.3d, dimSprite.color.a);

            // 입력: 15초에 Update보다 먼저 수집 시도 → 기대: 획득 거부 및 B 개수 유지.
            droppedAt.SetValue(expired, Time.timeAsDouble - 15d);
            Check("15초 이후 수집 거부", 0d, expired.TryCollect(player) ? 1d : 0d);
            Check("만료된 조각의 B 수집 개수", 1d, player.TypeBCount);
            SessionState.SetString(ExpiredId, EntityId.ToULong(expiredObject.GetEntityId()).ToString());
            SessionState.SetInt(PassedCount, passed);
            SessionState.SetBool(WaitingForDestruction, true);
            update.Invoke(expired, null);
            destructionDeadline = EditorApplication.timeSinceStartup + 15d;
            EditorApplication.update += VerifyDestruction;
            EditorApplication.QueuePlayerLoopUpdate();
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    // Update가 요청한 실제 오브젝트 제거가 완료되는지 확인한다.
    private static void VerifyDestruction()
    {
        if (EditorApplication.isPaused) {
            EditorApplication.isPaused = false;
        }

        if (expiredObject != null && EditorApplication.timeSinceStartup < destructionDeadline) {
            EditorApplication.QueuePlayerLoopUpdate();
            return;
        }

        EditorApplication.update -= VerifyDestruction;

        try {
            Check("제거 검증 중 플레이 모드 유지", 1d, EditorApplication.isPlaying ? 1d : 0d);
            Check("15초 경과 후 조각 오브젝트 제거", 1d, expiredObject == null ? 1d : 0d);
            Debug.Log($"[FragmentLifetime] PASS {passed}/{passed}");
            SessionState.EraseBool(Running);
            SessionState.EraseBool(WaitingForDestruction);
            EditorApplication.Exit(0);
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
