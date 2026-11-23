using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Mech.Function;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

public sealed class VerifyPlayerCamera : MonoBehaviour
{
    private const string OutputEnvironment = "MERGER_CAMERA_VERIFICATION_DIR";
    private const string TitlePath = "Assets/Scenes/SampleScene.unity";
    private const string PlayPath = "Assets/Scenes/TestScene/PlayTestScene.unity";
    private const string FlyingPath = "Assets/Scenes/TestScene/FlyingBossTestScene.unity";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly MethodInfo UpdateView = typeof(PlayerCameraFollow).GetMethod("UpdateView", Private);
    private static int passed;

#if UNITY_EDITOR
    // 현재 씬들을 포함하고 저장 경로를 분리한 검증 플레이어를 빌드한다.
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable(OutputEnvironment);
        PlayerSettings.productName = "Merger Camera Verification";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.merger.verification.camera");
        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.SplashScreen.show = false;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { TitlePath, PlayPath, FlyingPath },
            target = BuildTarget.StandaloneOSX,
            locationPathName = Path.Combine(output, "CameraVerification.app")
        });

        if (report.summary.result != BuildResult.Succeeded) {
            throw new InvalidOperationException($"검증 빌드 실패: {report.summary.result}");
        }
    }
#endif

    // 검증 전용 실행에서 씬이 바뀌어도 테스트 오브젝트를 유지한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartVerification()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputEnvironment))) {
            return;
        }

        var host = new GameObject("Camera verification");
        DontDestroyOnLoad(host);
        host.AddComponent<VerifyPlayerCamera>().StartCoroutine(Run());
    }

    // 프레임 사이에 검증을 실행하고 실패 시 종료 코드에 반영한다.
    private static IEnumerator Run()
    {
        IEnumerator verification = Verify();

        while (true) {
            object instruction;

            try {
                if (!verification.MoveNext()) {
                    break;
                }

                instruction = verification.Current;
            } catch (Exception error) {
                Debug.LogException(error);
                Application.Quit(1);
                yield break;
            }

            yield return instruction;
        }

        Debug.Log($"[PlayerCamera] PASS {passed}/{passed}");
        Application.Quit(0);
    }

    // 타이틀과 두 테스트 씬에서 공통 설정과 화면 경계를 확인한다.
    private static IEnumerator Verify()
    {
        yield return null;
        Camera title = Camera.main;
        Check("타이틀에 공통 컴포넌트 자동 연결", true, title.TryGetComponent<PlayerCameraFollow>(out _));
        CheckNear("타이틀의 기본 크기", 10f, title.orthographicSize);

        yield return SceneManager.LoadSceneAsync(PlayPath);
        yield return null;
        VerifyScene(Camera.main);

        yield return new WaitForEndOfFrame();
        Capture("wide.png");

        Camera camera = Camera.main;
        var follow = camera.GetComponent<PlayerCameraFollow>();
        var narrowBounds = new Rect(-6f, -1f, 12f, 6f);
        float uncroppedAspect = camera.aspect;
        Set(follow, "worldBounds", narrowBounds);
        Step(follow, 0.02f, snap: true);

        // 입력은 12×6의 작은 경계이며 크기 10 및 원래 화면 배율 보존을 기대한다.
        CheckNear("좁은 경계에서도 직교 크기 보존", 10f, camera.orthographicSize);
        CheckNear("좁은 경계의 표시 영역 높이", 0.3f, camera.rect.height);
        CheckNear("좁은 경계의 표시 영역 폭", 12f / (20f * uncroppedAspect), camera.rect.width);
        CheckEdges(camera, narrowBounds);
        yield return null;
        yield return new WaitForEndOfFrame();
        CheckNear("다음 렌더 프레임에서도 표시 영역 폭 유지", 12f / (20f * uncroppedAspect), camera.rect.width);
        Capture("cropped.png");

        Set(follow, "limitToBounds", false);
        Set(follow, "orthographicSize", 7f);
        Step(follow, 0.02f);
        CheckNear("명시한 크기 7 적용", 7f, camera.orthographicSize);
        CheckNear("경계 해제 후 전체 뷰포트 복원", 1f, camera.rect.height);
        CheckNear("경계 해제 후 투영 복원", 1f / 7f, camera.projectionMatrix.m11);

        yield return SceneManager.LoadSceneAsync(FlyingPath);
        yield return null;
        VerifyScene(Camera.main);
    }

    // 플레이어 추적과 여러 화면 비율에서의 경계 및 흔들림을 검사한다.
    private static void VerifyScene(Camera camera)
    {
        string scene = camera.gameObject.scene.name;
        var follow = camera.GetComponent<PlayerCameraFollow>();
        var player = FindAnyObjectByType<PlayerBase>();
        var target = (Transform)typeof(PlayerCameraFollow).GetField("target", Private).GetValue(follow);
        Check($"{scene}의 플레이어 연결", player.transform, target);
        CheckNear($"{scene}의 기본 크기", 10f, camera.orthographicSize);
        CheckNear($"{scene}의 보간 시간", 0.3f, (float)typeof(PlayerCameraFollow).GetField("smoothTime", Private).GetValue(follow));

        // 입력은 플레이어의 X +10 및 Y +8 이동이며 첫 스텝의 X 지연과 고정 Y를 기대한다.
        Vector3 start = camera.transform.position;
        player.transform.position += new Vector3(10f, 8f, 0f);
        Step(follow, 0.02f);
        float firstStep = camera.transform.position.x - start.x;
        Check($"{scene} 첫 프레임에 즉시 도착하지 않고 이동", true, firstStep > 0f && firstStep < 10f);
        CheckNear($"{scene} 수평 추적 중 Y 고정", start.y, camera.transform.position.y);

        for (int frame = 1; frame < 15; frame++) {
            Step(follow, 0.02f);
        }

        float dampedDelta = camera.transform.position.x - start.x;
        Check($"{scene} 0.3초 보간 이동량", true, dampedDelta > 5f && dampedDelta < 7f);

        // 입력은 양 축 경계 밖의 대상이며 어떤 프레임에서도 화면 모서리가 밖으로 나가지 않아야 한다.
        var bounds = new Rect(-40f, -20f, 80f, 40f);
        Set(follow, "followVertical", true);
        Set(follow, "worldBounds", bounds);
        Set(follow, "limitToBounds", true);
        player.transform.position = new Vector3(100f, 100f, 0f);

        foreach (float aspect in new[] { 16f / 9f, 9f / 16f, 2.5f }) {
            camera.aspect = aspect;
            Step(follow, 0.02f, snap: true);
            CheckEdges(camera, bounds);
            CheckNear($"{scene} 비율 {aspect} 오른쪽 끝", bounds.xMax - 10f * aspect, camera.transform.position.x);
            CheckNear($"{scene} 비율 {aspect} 위쪽 끝", bounds.yMax - 10f, camera.transform.position.y);
        }

        Vector3 edge = camera.transform.position;
        AttackCameraShake.Play();
        var shake = camera.GetComponent<AttackCameraShake>();
        typeof(AttackCameraShake).GetMethod("BeginCameraRendering", Private)
            .Invoke(shake, new object[] { default(ScriptableRenderContext), camera });
        CheckNear($"{scene} 경계 밖을 향한 흔들림 제한", edge.x, camera.transform.position.x);
        typeof(AttackCameraShake).GetMethod("EndCameraRendering", Private)
            .Invoke(shake, new object[] { default(ScriptableRenderContext), camera });
        CheckNear($"{scene} 렌더 종료 후 위치 복원", edge.x, camera.transform.position.x);

        // 입력은 반대 모서리 밖의 대상이며 왼쪽 및 아래쪽 경계까지 이동한다.
        player.transform.position = new Vector3(-100f, -100f, 0f);
        Step(follow, 0.02f, snap: true);
        CheckEdges(camera, bounds);
        CheckNear($"{scene} 왼쪽 끝", bounds.xMin + 10f * camera.aspect, camera.transform.position.x);
        CheckNear($"{scene} 아래쪽 끝", bounds.yMin + 10f, camera.transform.position.y);

        camera.ResetAspect();
        player.transform.position = new Vector3(0f, 0.4f, 0f);
        player.GetComponent<Rigidbody2D>().position = player.transform.position;
        Step(follow, 0.02f, snap: true);
    }

    // 카메라의 실제 투영을 역변환해 화면의 양쪽 모서리가 경계 안에 있는지 확인한다.
    private static void CheckEdges(Camera camera, Rect bounds)
    {
        Vector3 low = camera.projectionMatrix.inverse.MultiplyPoint(new Vector3(-1f, -1f, 0f)) + camera.transform.position;
        Vector3 high = camera.projectionMatrix.inverse.MultiplyPoint(new Vector3(1f, 1f, 0f)) + camera.transform.position;
        const float tolerance = 0.001f;
        Check($"{camera.gameObject.scene.name} 화면 하단 및 좌측", true,
            low.x >= bounds.xMin - tolerance && low.y >= bounds.yMin - tolerance);
        Check($"{camera.gameObject.scene.name} 화면 상단 및 우측", true,
            high.x <= bounds.xMax + tolerance && high.y <= bounds.yMax + tolerance);
    }

    // 검증용 Inspector 값을 바꾼다.
    private static void Set(PlayerCameraFollow follow, string field, object value)
    {
        typeof(PlayerCameraFollow).GetField(field, Private).SetValue(follow, value);
    }

    // 고정된 시간 간격으로 실제 추적 메서드를 한 번 실행한다.
    private static void Step(PlayerCameraFollow follow, float time, bool snap = false)
    {
        UpdateView.Invoke(follow, new object[] { time, snap });
    }

    // 최종 렌더 프레임을 PNG에 저장한다.
    private static void Capture(string filename)
    {
        Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
        Check($"{filename} 렌더 프레임 생성", true, frame != null);
        File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable(OutputEnvironment), filename), frame.EncodeToPNG());
        Destroy(frame);
    }

    // 실수 검사의 입력과 기대값 및 실제값을 출력한다.
    private static void CheckNear(string input, float expected, float actual)
    {
        string line = $"[PlayerCamera] 입력 {input} | 기대 [{expected:F5}] | 실제 [{actual:F5}]";
        Debug.Log(line);

        if (!Mathf.Approximately(expected, actual) && !(Mathf.Abs(expected - actual) < 0.001f)) {
            throw new InvalidOperationException(line);
        }

        passed++;
    }

    // 입력과 기대값 및 실제값을 기록하고 다르면 검증을 중단한다.
    private static void Check<T>(string input, T expected, T actual)
    {
        string line = $"[PlayerCamera] 입력 {input} | 기대 [{expected}] | 실제 [{actual}]";
        Debug.Log(line);

        if (!Equals(expected, actual)) {
            throw new InvalidOperationException(line);
        }

        passed++;
    }
}
