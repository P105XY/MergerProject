using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

public sealed class VerifyFragmentSlotsRender : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string ScenePath = "Assets/Scenes/TestScene/PlayTestScene.unity";
    private static readonly MethodInfo Attack = typeof(PlayerBase).GetMethod("Attack", PrivateInstance);
    private string output;
    private int checks;

#if UNITY_EDITOR
    // 복사본에서 테스트 씬만 포함하고 저장 경로가 분리된 macOS 플레이어를 만든다.
    public static void Build()
    {
        string output = RequireEnvironment("MERGER_HUD_CAPTURE_DIR");
        Directory.CreateDirectory(output);
        PlayerSettings.productName = "Merger HUD Render Verification";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.merger.verification.fragmenthud");
        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.SplashScreen.show = false;
        BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(output, "HudVerification.app"),
            target = BuildTarget.StandaloneOSX
        });

        if (result.summary.result != BuildResult.Succeeded) {
            throw new InvalidOperationException($"플레이어 빌드 실패: {result.summary.result}");
        }
    }
#endif

    // 테스트 플레이어가 시작하면 게임 씬에서 프레임 검사 코루틴을 실행한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartVerification()
    {
        string output = Environment.GetEnvironmentVariable("MERGER_HUD_CAPTURE_DIR");

        if (string.IsNullOrEmpty(output)) {
            return;
        }

        var verifier = new GameObject("HUD render verification").AddComponent<VerifyFragmentSlotsRender>();
        verifier.output = output;
        verifier.StartCoroutine(verifier.Run());
    }

    // 테스트 로그와 캡처 디렉터리에 필요한 환경 변수 값을 읽는다.
    private static string RequireEnvironment(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"환경 변수 누락: {key}");
    }

    // 프레임 사이에 검사 코드를 실행하고 실패나 성공을 종료 코드에 반영한다.
    private IEnumerator Run()
    {
        IEnumerator checks = Verify();

        while (true) {
            object next;
            bool running;

            try {
                running = checks.MoveNext();
                next = running ? checks.Current : null;
            } catch (Exception error) {
                Debug.LogException(error);
                Application.Quit(1);
                yield break;
            }

            if (!running) {
                break;
            }

            yield return next;
        }

        Debug.Log($"[FragmentHudRender] PASS {this.checks}/{this.checks}");
        Application.Quit(0);
    }

    // 씬에 있는 C·A·B 적을 공격해 조각을 얻고 특수 능력을 세 번 시전한다.
    private IEnumerator Verify()
    {
        yield return null;
        PlayerBase player = FindAnyObjectByType<PlayerBase>();
        Check("실행 씬", ScenePath, player.gameObject.scene.path);
        Check("PlayTestDisplay 가동", true, FindAnyObjectByType<PlayTestDisplay>().isActiveAndEnabled);

        yield return new WaitForEndOfFrame();
        Capture(player, "00-empty", "", 0);

        string expectedOrder = "";
        var order = new[] { CollectibleType.C, CollectibleType.A, CollectibleType.B };
        var enemies = FindObjectsByType<TestEnemy>();
        var body = player.GetComponent<Rigidbody2D>();
        float groundHeight = body.position.y;

        foreach (CollectibleType type in order) {
            TestEnemy enemy = enemies.Single(candidate => candidate.HeldObjectType == type);
            GameObject drop = null;
            enemy.FragmentDropped += (_, _, dropped) => drop = dropped;
            var strikePosition = new Vector2(enemy.transform.position.x - 1.1f, groundHeight);
            player.transform.position = strikePosition;
            body.position = strikePosition;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            // 입력은 체력 30인 적을 피해 10의 근접 공격으로 세 번 타격하는 것이며 매번 체력 10 감소를 기대한다.
            for (int hit = 0; hit < 3; hit++) {
                Attack.Invoke(player, null);
                Check($"{type} 적 근접 공격 {hit + 1}회 후 체력", 20f - hit * 10f, enemy.CurrentHealth);
                yield return new WaitForSeconds(0.06f);
            }

            Check($"{type} 적의 정상 공격 처치와 드롭", true, enemy.IsDefeated && drop != null);
            Attack.Invoke(player, null);
            yield return null;
            Check($"{type} 조각의 별도 공격 수집", true, drop == null);
            expectedOrder += type.ToString();
            int uses = expectedOrder.Length == 3 ? 3 : 0;
            yield return new WaitForEndOfFrame();
            Capture(player, $"0{expectedOrder.Length}-{expectedOrder.ToLowerInvariant()}", expectedOrder, uses);
        }

        for (int uses = 2; uses >= 0; uses--) {
            Check($"특수 시전 {3 - uses}회 성공", true, player.TryUseSpecial());
            yield return new WaitForEndOfFrame();
            Capture(player, $"0{6 - uses}-remaining-{uses}", uses == 0 ? "" : "CAB", uses);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 실제 렌더 프레임에 슬롯 테두리와 점유 칸의 글자 픽셀이 나타나는지 확인한다.
    private void Capture(PlayerBase player, string filename, string expectedOrder, int uses)
    {
        Check($"{filename} 조각 큐", expectedOrder, string.Concat(player.QueuedFragments));
        Check($"{filename} 남은 횟수", uses, player.SpecialUsesRemaining);

        if (uses > 0) {
            Check($"{filename} 활성 능력", "Explosive Thrust", player.GetActiveSpecialName());
        }

        Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();

        if (frame == null) {
            throw new InvalidOperationException("게임 프레임을 캡처하지 못했습니다.");
        }

        try {
            Check($"{filename} 캡처 유효 크기", true, frame.width >= 388 && frame.height >= 216);

            for (int slot = 0; slot < 4; slot++) {
                int x = 16 + slot * 89;
                Color32 fill = frame.GetPixel(x + 5, 21);
                Color32 border = frame.GetPixel(x + 10, 16);
                Check($"{filename} {slot + 1}번 칸 테두리", true, ChannelDistance(fill, border) >= 15);

                bool expectedText = slot < expectedOrder.Length || (slot == 3 && uses > 0);
                Check($"{filename} {slot + 1}번 칸 글자", expectedText, CountGlyphPixels(frame, x, 21, 93, fill) > 8);

                if (slot == 3 && uses > 0) {
                    Check($"{filename} 능력명 줄 픽셀", true, CountGlyphPixels(frame, x, 43, 93, fill) > 8);
                    Check($"{filename} 남은 횟수 줄 픽셀", true, CountGlyphPixels(frame, x, 21, 38, fill) > 8);
                }
            }

            File.WriteAllBytes(Path.Combine(output, $"{filename}.png"), frame.EncodeToPNG());
        } finally {
            Destroy(frame);
        }
    }

    // 배경색과의 차이가 큰 슬롯 내부 픽셀 수를 센다.
    private static int CountGlyphPixels(Texture2D frame, int x, int bottom, int top, Color32 fill)
    {
        int pixels = 0;

        for (int y = bottom; y < top; y++) {
            for (int column = x + 4; column < x + 78; column++) {
                if (ChannelDistance(fill, frame.GetPixel(column, y)) >= 65) {
                    pixels++;
                }
            }
        }

        return pixels;
    }

    // 두 픽셀의 RGB 채널 중 가장 큰 차이를 구한다.
    private static int ChannelDistance(Color32 left, Color32 right)
    {
        return Math.Max(Math.Abs(left.r - right.r), Math.Max(Math.Abs(left.g - right.g), Math.Abs(left.b - right.b)));
    }

    // 검사 입력과 기대값 및 실제값을 기록하고 차이가 있으면 실패 처리한다.
    private void Check<T>(string input, T expected, T actual)
    {
        string line = $"[FragmentHudRender] 입력 {input} | 기대 [{expected}] | 실제 [{actual}]";
        Debug.Log(line);

        if (!Equals(expected, actual)) {
            throw new InvalidOperationException(line);
        }

        checks++;
    }
}
