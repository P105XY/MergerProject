using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyPlayerJumpRetune
{
    private const string Running = "VerifyPlayerJumpRetune.Running";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // 도메인이 다시 로드돼도 검증용 플레이 모드 이벤트를 연결한다.
    static VerifyPlayerJumpRetune()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 명령줄에서 고른 저장 씬을 열고 플레이 모드를 시작한다.
    public static void Run()
    {
        string scene = Environment.GetCommandLineArgs().Contains("-verify-flying-scene") ? "FlyingBossTestScene" : "PlayTestScene";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + scene + ".unity");
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= Verify;
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 실제 2D 물리에서 이전과 현재의 점프 높이와 해제 속도를 비교한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exitCode = 0;

        try {
            var store = typeof(CheckpointProgress).GetField("saveStore", Private);
            string directory = Path.Combine(Path.GetTempPath(), "merger-jump-retune-" + Guid.NewGuid().ToString("N"));
            store.SetValue(null, Activator.CreateInstance(store.FieldType, Private | BindingFlags.Public, null,
                new object[] { directory }, null));
            Physics2D.simulationMode = SimulationMode2D.Script;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            var hitbox = player.GetComponent<Collider2D>();
            var settings = new SerializedObject(player);
            float force = settings.FindProperty("jumpForce").floatValue;
            float release = settings.FindProperty("jumpReleaseMultiplier").floatValue;
            Vector2 start = body.position;
            Check("씬의 점프 시작 속도와 해제 배율", "27.82 / 0.704", force + " / " + release,
                Mathf.Abs(force - 27.82f) < 0.0001f && Mathf.Abs(release - 0.704f) < 0.0001f);
            var fixedStep = typeof(PlayerBase).GetMethod("FixedUpdate", Private);
            var clearState = typeof(PlayerBase).GetMethod("OnDisable", Private);
            var held = typeof(PlayerBase).GetField("jumpHeld", Private);
            var requested = typeof(PlayerBase).GetField("jumpRequested", Private);
            var movement = typeof(PlayerBase).GetField("movementInput", Private);
            var jumpForce = typeof(PlayerBase).GetField("jumpForce", Private);
            var releaseMultiplier = typeof(PlayerBase).GetField("jumpReleaseMultiplier", Private);

            // 시작점으로 복귀시킨 뒤 해제 시점과 점프 설정을 지정해 한 번의 궤적을 측정한다.
            (float Rise, float Head, float ReleaseSpeed) Measure(Vector2 origin, float speed, float multiplier, int releaseTick, float move = 0f)
            {
                clearState.Invoke(player, null);
                body.position = origin;
                body.linearVelocity = Vector2.zero;
                player.transform.position = new Vector3(origin.x, origin.y, 0f);
                Physics2D.SyncTransforms();
                jumpForce.SetValue(player, speed);
                releaseMultiplier.SetValue(player, multiplier);
                requested.SetValue(player, true);
                movement.SetValue(player, move);
                float peak = origin.y;
                float top = hitbox.bounds.max.y;
                float releaseSpeed = 0f;

                for (int step = 0; step < 60; step++) {
                    held.SetValue(player, step < releaseTick);
                    fixedStep.Invoke(player, null);

                    if (step == releaseTick) {
                        releaseSpeed = body.linearVelocity.y;
                    }

                    Physics2D.Simulate(Time.fixedDeltaTime);
                    peak = Mathf.Max(peak, body.position.y);
                    top = Mathf.Max(top, hitbox.bounds.max.y);
                }

                return (peak - origin.y, top, releaseSpeed);
            }

            // 이전 설정 25/-1과 실제 씬 설정을 같은 물리 조건에서 비교한다.
            var oldLong = Measure(start, 25f, -1f, 60);
            var newLong = Measure(start, force, release, 60);
            var oldShort = Measure(start, 25f, -1f, 3);
            var newShort = Measure(start, force, release, 3);
            float longRatio = newLong.Rise / oldLong.Rise;
            float shortRatio = newShort.Rise / oldShort.Rise;
            Check("유지 점프 높이", "1.25배", $"{oldLong.Rise:F3} → {newLong.Rise:F3} ({longRatio:F3}배)", Mathf.Abs(longRatio - 1.25f) < 0.01f);
            Check("0.06초 입력 점프 높이", "2배", $"{oldShort.Rise:F3} → {newShort.Rise:F3} ({shortRatio:F3}배)", Mathf.Abs(shortRatio - 2f) < 0.02f);
            Check("0.06초 해제 직후 수직 속도", "하강 대신 상승 유지", $"{oldShort.ReleaseSpeed:F3} → {newShort.ReleaseSpeed:F3}",
                oldShort.ReleaseSpeed < 0f && newShort.ReleaseSpeed > 0f);

            // 점프 요청과 해제가 같은 물리 스텝에 도착해도 짧은 상승이 생긴다.
            var instantTap = Measure(start, force, release, 0);
            Check("즉시 탭 점프", "1유닛 이상 상승, 유지 점프보다 낮음", instantTap.Rise.ToString("F3"),
                instantTap.Rise > 1f && instantTap.Rise < newLong.Rise);
            var ceiling = GameObject.Find("Ceiling");

            if (ceiling != null) {
                float gap = ceiling.GetComponent<Collider2D>().bounds.min.y - newLong.Head;
                Check("유지 점프 최고점의 천장 여유", "양의 간격", gap.ToString("F3"), gap > 0f);
            } else {
                Measure(new Vector2(11.08f, 0.41f), force, release, 60, 1f);
                float wallRight = GameObject.Find("Entrance Wall").GetComponent<Collider2D>().bounds.max.x;
                Check("지상 보스 구역 진입", "입구 벽 오른쪽", hitbox.bounds.min.x, hitbox.bounds.min.x > wallRight);
            }

            Debug.Log("[JumpRetune] PASS 6/6");
        } catch (Exception error) {
            Debug.LogError("[JumpRetune] " + error);
            exitCode = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exitCode);
    }

    // 입력과 기대값 및 실제값을 기록하고 불일치하면 중단한다.
    private static void Check(string input, string expected, object actual, bool passed)
    {
        string line = "[JumpRetune] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!passed) {
            throw new InvalidOperationException(line);
        }
    }
}
