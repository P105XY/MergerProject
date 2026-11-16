using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyJumpReleaseDive
{
    private const string Running = "VerifyJumpReleaseDive.Running";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // 도메인이 다시 로드되면 검증용 플레이 모드 이벤트를 복구한다.
    static VerifyJumpReleaseDive()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 명령줄에서 지정한 저장된 씬을 열고 플레이 모드를 시작한다.
    public static void Run()
    {
        string scene = Environment.GetCommandLineArgs().Contains("-verify-flying-scene") ? "FlyingBossTestScene" : "PlayTestScene";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + scene + ".unity");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
        float serialized = new SerializedObject(player).FindProperty("jumpReleaseMultiplier").floatValue;
        Check(scene + " 저장된 해제 배율", "-1", serialized, serialized == -1f);
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= Verify;
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 입력 상태만 지정하고 실제 Unity 2D 물리에서 상승과 해제 직후를 비교한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exitCode = 0;

        try {
            var store = typeof(CheckpointProgress).GetField("saveStore", Private);
            string directory = Path.Combine(Path.GetTempPath(), "merger-jump-release-" + Guid.NewGuid().ToString("N"));
            store.SetValue(null, Activator.CreateInstance(store.FieldType, Private | BindingFlags.Public, null,
                new object[] { directory }, null));
            Physics2D.simulationMode = SimulationMode2D.Script;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            var step = typeof(PlayerBase).GetMethod("FixedUpdate", Private);
            var held = typeof(PlayerBase).GetField("jumpHeld", Private);
            typeof(PlayerBase).GetField("jumpRequested", Private).SetValue(player, true);
            held.SetValue(player, true);
            Physics2D.SyncTransforms();
            float groundY = body.position.y;

            // 세 번 물리를 진행할 동안 버튼을 유지하면 플레이어는 공중에서 상승한다.
            for (int tick = 0; tick < 3; tick++) {
                step.Invoke(player, null);
                Physics2D.Simulate(Time.fixedDeltaTime);
            }

            float upward = body.linearVelocity.y;
            float height = body.position.y;
            Check("유지 후 실제 상승", "공중, 양의 수직 속도", upward, height > groundY && upward > 0f);

            // 공중에서 버튼을 해제한 한 스텝은 중력이 추가되기 전에 수직 속도를 반전한다.
            held.SetValue(player, false);
            step.Invoke(player, null);
            float released = body.linearVelocity.y;
            Check("해제 직후 속도", (-upward).ToString("F5"), released, Mathf.Abs(released + upward) < 0.001f);

            // 다음 물리 스텝부터 높이가 내려가고 해제 상태를 유지해도 다시 상승하지 않는다.
            Physics2D.Simulate(Time.fixedDeltaTime);
            Check("해제 뒤 한 물리 스텝", "이전보다 낮은 위치", body.position.y, body.position.y < height);
            float falling = body.linearVelocity.y;
            step.Invoke(player, null);
            Check("해제 유지 중 수직 속도", falling.ToString("F5"), body.linearVelocity.y,
                falling < 0f && Mathf.Abs(body.linearVelocity.y - falling) < 0.001f);
            Debug.Log("[JumpReleaseDive] PASS 5/5");
        } catch (Exception error) {
            Debug.LogError("[JumpReleaseDive] " + error);
            exitCode = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exitCode);
    }

    // 입력·기대값·실제값을 출력하고 불일치하면 검증을 중단한다.
    private static void Check(string input, string expected, object actual, bool passed)
    {
        string line = "[JumpReleaseDive] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!passed) {
            throw new InvalidOperationException(line);
        }
    }
}
