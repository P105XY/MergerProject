using System;
using System.IO;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyPlayerGravityTuning
{
    private const string Running = "VerifyPlayerGravityTuning.Running";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

    // 도메인이 재로드되면 검증용 플레이 모드 이벤트를 다시 구독한다.
    static VerifyPlayerGravityTuning()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
    }

    // 두 씬에서 Inspector 값을 읽은 뒤 저장된 PlayTestScene을 실행한다.
    public static void Run()
    {
        foreach (string scene in new[] { "FlyingBossTestScene", "PlayTestScene" }) {
            EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + scene + ".unity");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var settings = new SerializedObject(player);

            // 두 씬의 플레이어는 기존 최대 속도 5와 새 점프 설정을 저장한다.
            Check(scene + " 최대 이동 속도", 5f, settings.FindProperty("maximumMovementSpeed").floatValue);
            Check(scene + " 점프 속도", 50f, settings.FindProperty("jumpForce").floatValue);
            Check(scene + " 높이 상한", 30f, settings.FindProperty("maximumJumpHeight").floatValue);
            Check(scene + " 해제 배율", 1f, settings.FindProperty("jumpReleaseMultiplier").floatValue);
            Check(scene + " 중력 배율", 9.8f, settings.FindProperty("gravityScale").floatValue);
            Check(scene + " 바디 중력 배율", 9.8f, player.GetComponent<Rigidbody2D>().gravityScale);
        }

        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.EnterPlaymode();
    }

    // 실제 PlayMode에서 점프의 시작 속도와 중력 적용, 버튼 해제 동작을 검사한다.
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exitCode = 0;

        try {
            var field = typeof(CheckpointProgress).GetField("saveStore", Private);
            string directory = Path.Combine(Path.GetTempPath(), "merger-gravity-check-" + Guid.NewGuid().ToString("N"));
            field.SetValue(null, Activator.CreateInstance(field.FieldType, Private | BindingFlags.Public, null,
                new object[] { directory }, null));
            Physics2D.simulationMode = SimulationMode2D.Script;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            var step = typeof(PlayerBase).GetMethod("FixedUpdate", Private);
            var jumpHeld = typeof(PlayerBase).GetField("jumpHeld", Private);
            typeof(PlayerBase).GetField("jumpRequested", Private).SetValue(player, true);
            jumpHeld.SetValue(player, true);
            Physics2D.SyncTransforms();
            float start = body.position.y;
            step.Invoke(player, null);

            // 점프 요청 시 속도 50이 적용되고 한 스텝 뒤에는 월드 중력에 배율 9.8이 곱해진다.
            Check("PlayMode 점프 시작 수직 속도", 50f, body.linearVelocity.y);
            Physics2D.Simulate(Time.fixedDeltaTime);
            float afterGravity = body.linearVelocity.y;
            Check("PlayMode 첫 물리 스텝 후 수직 속도", 50f + Physics2D.gravity.y * 9.8f * Time.fixedDeltaTime, afterGravity);
            jumpHeld.SetValue(player, false);
            step.Invoke(player, null);

            // 상승 중 버튼 해제 배율 1은 중력이 적용되기 전 수직 속도를 보존한다.
            Check("PlayMode 상승 중 해제 직후 수직 속도", afterGravity, body.linearVelocity.y);
            float peak = body.position.y;

            for (int tick = 0; tick < 45; tick++) {
                Physics2D.Simulate(Time.fixedDeltaTime);
                peak = Mathf.Max(peak, body.position.y);
                step.Invoke(player, null);
            }

            float rise = peak - start;
            Debug.Log("[GravityTuning] 입력 높이 상한 30 | 기대 실제 상승 12~13.5 | 실제 " + rise.ToString("F3"));

            if (rise <= 12f || rise >= 13.5f) {
                throw new InvalidOperationException("점프 최고점이 기대 범위를 벗어났습니다.");
            }

            Debug.Log("[GravityTuning] PASS 16/16");
        } catch (Exception error) {
            Debug.LogError("[GravityTuning] " + error);
            exitCode = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exitCode);
    }

    // 입력과 기대값 및 실제값을 기록하고 불일치하면 중단한다.
    private static void Check(string input, float expected, float actual)
    {
        string line = "[GravityTuning] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (Mathf.Abs(expected - actual) > 0.001f) {
            throw new InvalidOperationException(line);
        }
    }
}
