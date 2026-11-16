using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyPlayerJumpHalf
{
    private const string Running = "VerifyPlayerJumpHalf.Running";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // 플레이 모드 진입 시 도메인이 다시 로드되어도 검증을 연결한다.
    static VerifyPlayerJumpHalf()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 지정한 씬에서 저장된 점프력 25를 확인하고 플레이를 시작한다.
    public static void Run()
    {
        string scene = Environment.GetCommandLineArgs().Contains("-verify-flying-scene") ? "FlyingBossTestScene" : "PlayTestScene";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + scene + ".unity");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
        float force = new SerializedObject(player).FindProperty("jumpForce").floatValue;
        Check(scene + " 저장된 점프력", "25", force, force == 25f);
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= Verify;
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // S를 유지한 상태의 실제 물리 스텝에서 천장 여유 또는 입구 벽 통과를 확인한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exitCode = 0;

        try {
            var store = typeof(CheckpointProgress).GetField("saveStore", Private);
            string directory = Path.Combine(Path.GetTempPath(), "merger-jump-half-" + Guid.NewGuid().ToString("N"));
            store.SetValue(null, Activator.CreateInstance(store.FieldType, Private | BindingFlags.Public, null,
                new object[] { directory }, null));
            Physics2D.simulationMode = SimulationMode2D.Script;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            var playerCollider = player.GetComponent<Collider2D>();
            var ceiling = GameObject.Find("Ceiling");
            bool entrance = ceiling == null;
            var tick = typeof(PlayerBase).GetMethod("FixedUpdate", Private);

            if (entrance) {
                body.position = new Vector2(11.08f, 0.41f);
                player.transform.position = new Vector3(11.08f, 0.41f, 0f);
                typeof(PlayerBase).GetField("movementInput", Private).SetValue(player, 1f);
            }

            Physics2D.SyncTransforms();
            float startY = body.position.y;
            float peak = startY;
            float peakHead = playerCollider.bounds.max.y;
            typeof(PlayerBase).GetField("jumpRequested", Private).SetValue(player, true);
            typeof(PlayerBase).GetField("jumpHeld", Private).SetValue(player, true);
            tick.Invoke(player, null);
            Check("점프 시작 수직 속도", "25", body.linearVelocity.y, body.linearVelocity.y == 25f);

            // 기본 물리 간격으로 60스텝 동안 최고점과 충돌체 위쪽 경계를 측정한다.
            for (int step = 0; step < 60; step++) {
                Physics2D.Simulate(Time.fixedDeltaTime);
                peak = Mathf.Max(peak, body.position.y);
                peakHead = Mathf.Max(peakHead, playerCollider.bounds.max.y);
                tick.Invoke(player, null);
            }

            float rise = peak - startY;
            Check("S 유지 점프 상승 높이", "2.5~3.3유닛", rise.ToString("F3"), rise > 2.5f && rise < 3.3f);

            if (entrance) {
                float wallRight = GameObject.Find("Entrance Wall").GetComponent<Collider2D>().bounds.max.x;
                Check("보스 구역 입구 진입", "플레이어 충돌체가 벽 오른쪽", playerCollider.bounds.min.x, playerCollider.bounds.min.x > wallRight);
            } else {
                float clearance = ceiling.GetComponent<Collider2D>().bounds.min.y - peakHead;
                Check("비행 보스 씬 천장 여유", "플레이어 충돌체가 천장 아래", clearance.ToString("F3"), clearance > 0f);
            }

            Debug.Log("[JumpHalf] PASS 4/4");
        } catch (Exception error) {
            Debug.LogError("[JumpHalf] " + error);
            exitCode = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exitCode);
    }

    // 입력과 기대값·실제값을 기록하고 불일치하면 중단한다.
    private static void Check(string input, string expected, object actual, bool passed)
    {
        string line = "[JumpHalf] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!passed) {
            throw new InvalidOperationException(line);
        }
    }
}
