using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyJumpDescentImpulse
{
    private const string Running = "VerifyJumpDescentImpulse.Running";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // 도메인 재로드 뒤에도 검증용 플레이 모드 이벤트를 복원한다.
    static VerifyJumpDescentImpulse()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 명령줄에서 고른 테스트 씬을 열어 플레이 모드를 시작한다.
    public static void Run()
    {
        string scene = Environment.GetCommandLineArgs().Contains("-verify-flying-scene") ? "FlyingBossTestScene" : "PlayTestScene";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + scene + ".unity");
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= Verify;
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 실제 물리의 해제·최고점 충격량과 궤적 예측을 비교한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exitCode = 0;

        try {
            var store = typeof(CheckpointProgress).GetField("saveStore", Private);
            string directory = Path.Combine(Path.GetTempPath(), "merger-jump-impulse-" + Guid.NewGuid().ToString("N"));
            store.SetValue(null, Activator.CreateInstance(store.FieldType, Private | BindingFlags.Public, null,
                new object[] { directory }, null));
            Physics2D.simulationMode = SimulationMode2D.Script;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            var settings = new SerializedObject(player);
            float boost = settings.FindProperty("jumpDescentSpeedBoost").floatValue;
            float multiplier = settings.FindProperty("jumpReleaseMultiplier").floatValue;
            body.mass = 2.5f;
            float gravityScale = body.gravityScale;
            Vector2 gravity = Physics2D.gravity;
            Vector2 origin = body.position;
            var tick = typeof(PlayerBase).GetMethod("FixedUpdate", Private);
            var held = typeof(PlayerBase).GetField("jumpHeld", Private);
            var requested = typeof(PlayerBase).GetField("jumpRequested", Private);
            var clear = typeof(PlayerBase).GetMethod("OnDisable", Private);

            // 바닥의 시작 위치에서 버튼을 유지한 점프를 요청한다.
            void Begin()
            {
                clear.Invoke(player, null);
                body.position = origin;
                body.linearVelocity = Vector2.zero;
                player.transform.position = new Vector3(origin.x, origin.y, 0f);
                Physics2D.SyncTransforms();
                held.SetValue(player, true);
                requested.SetValue(player, true);
            }

            // 실제 중력으로 상승을 끝낸 직후의 수직 속도와 위치를 읽는다.
            void ReachApex()
            {
                for (int step = 0; step < 60 && body.linearVelocity.y > 0f; step++) {
                    tick.Invoke(player, null);
                    Physics2D.Simulate(Time.fixedDeltaTime);
                }

                if (body.position.y <= origin.y || body.linearVelocity.y > 0f) {
                    throw new InvalidOperationException("공중에서 최고점에 도달하지 못했습니다.");
                }
            }

            // 입력: 씬의 하강 보정 1.5, 0.06초 상승 뒤 해제 → 기대: 기존 배율 적용 후 추가로 1.5 감소.
            Check("씬에 저장된 하강 보정", 1.5f, boost);
            Begin();

            for (int step = 0; step < 3; step++) {
                tick.Invoke(player, null);
                Physics2D.Simulate(Time.fixedDeltaTime);
            }

            float beforeRelease = body.linearVelocity.y;
            held.SetValue(player, false);
            tick.Invoke(player, null);
            Check("해제 직후 수직 속도", beforeRelease * multiplier - boost, body.linearVelocity.y);
            ReachApex();
            float releasedApex = body.linearVelocity.y;
            tick.Invoke(player, null);
            Check("해제로 이미 적용한 뒤 최고점", releasedApex, body.linearVelocity.y);

            // 입력: 버튼을 유지하며 자연 최고점 도달 → 기대: 예측과 실제 모두 한 번만 아래로 1.5 추가.
            Begin();
            tick.Invoke(player, null);
            Physics2D.Simulate(Time.fixedDeltaTime);
            ReachApex();
            Vector2 apexVelocity = body.linearVelocity;
            var prediction = typeof(PlayerBase).GetMethod("PredictJumpVelocity");
            object[] predictionArguments = { body.position, apexVelocity, Time.fixedDeltaTime, true };
            var expected = (Vector2)prediction.Invoke(player, predictionArguments);
            tick.Invoke(player, null);
            Check("버튼 유지 최고점의 충격량", apexVelocity.y - boost, body.linearVelocity.y);
            Check("최고점 예측 수직 속도", body.linearVelocity.y, expected.y);
            float firstDescent = body.linearVelocity.y;
            held.SetValue(player, false);
            tick.Invoke(player, null);
            Check("최고점 이후 해제 시 추가 충격량 없음", firstDescent, body.linearVelocity.y);
            Check("플레이어 중력 배율 유지", gravityScale, body.gravityScale);
            Check("월드 중력 유지", gravity.y, Physics2D.gravity.y);
            Debug.Log("[JumpImpulse] PASS 8/8");
        } catch (Exception error) {
            Debug.LogError("[JumpImpulse] " + error);
            exitCode = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exitCode);
    }

    // 입력과 기대값 및 실제값을 기록하고 차이가 크면 검증을 중단한다.
    private static void Check(string input, float expected, float actual)
    {
        string line = $"[JumpImpulse] 입력 {input} | 기대 {expected:F5} | 실제 {actual:F5}";
        Debug.Log(line);

        if (Mathf.Abs(expected - actual) > 0.001f) {
            throw new InvalidOperationException(line);
        }
    }
}
