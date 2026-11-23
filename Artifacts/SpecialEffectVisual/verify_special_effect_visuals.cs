using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class VerifySpecialEffectVisuals
{
    private const string Running = "VerifySpecialEffectVisuals.Running";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int Width = 360;
    private const int Height = 210;
    private static readonly Vector3 origin = new Vector3(-40f, 30f, 0f);
    private static int checks;

    // 도메인 재로드 이후 검증을 이어서 실행한다.
    static VerifySpecialEffectVisuals()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 저장된 PlayTestScene의 HUD로 렌더 검증을 시작한다.
    public static void Run()
    {
        PlayerSettings.productName = "Merger Special Visual Verification";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/PlayTestScene.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 입력과 기대값 및 실제값을 기록하고 일치 여부를 확인한다.
    private static void Check(string input, object expected, object actual, bool passed)
    {
        string line = $"[SpecialVisuals] 입력 {input} | 기대 {expected} | 실제 {actual}";
        Debug.Log(line);

        if (!passed) {
            throw new InvalidOperationException(line);
        }

        checks++;
    }

    // 머티리얼에 그려지는 10종 효과와 HUD 테스트 진입점을 검증한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        try {
            Physics2D.simulationMode = SimulationMode2D.Script;
            var hud = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var dashboard = typeof(PlayTestDisplay).GetMethod("GetSpecialStatus", Private);
            var practice = typeof(PlayTestDisplay).GetMethod("TryPracticeAttack", Private);
            var tick = typeof(PlayerBase).GetMethod("FixedUpdate", Private);
            var view = new GameObject("Verification Camera").AddComponent<Camera>();
            view.enabled = false;
            view.orthographic = true;
            view.orthographicSize = 6.6f;
            view.aspect = Width / (float)Height;
            view.transform.position = origin + new Vector3(1.7f, 0f, -10f);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.035f, 0.054f, 0.09f);
            view.allowHDR = false;
            const int previewLayer = 30;
            view.cullingMask = 1 << previewLayer;
            var tile = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(Width * 2, Height * 5, TextureFormat.RGBA32, false);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);

            // 입력: 조각 수집 후 활성화 → 기대: HUD가 미조합 수와 활성 조합·사용 횟수를 표시.
            player.TryCollectFragment(CollectibleType.A);
            player.TryCollectFragment(CollectibleType.B);
            Check("HUD 미조합 조각 수", "Collected 2/3    D: Special", dashboard.Invoke(hud, null),
                (string)dashboard.Invoke(hud, null) == "Collected 2/3    D: Special");
            player.TryCollectFragment(CollectibleType.C);
            Check("HUD 활성 조합과 횟수", "D: ABC    Uses 3/3", dashboard.Invoke(hud, null),
                (string)dashboard.Invoke(hud, null) == "D: ABC    Uses 3/3");
            string[] keys = { "AAA", "AAB", "AAC", "ABB", "ABC", "ACC", "BBB", "BBC", "BCC", "CCC" };
            float[] radius = { 1.8f, 1.6f, 3.2f, 0f, 1.5f, 2.5f, 0f, 1.8f, 3.5f, 6f };
            var shots = new[] { 3, 6, 7, 8 };

            for (int index = 0; index < keys.Length; index++) {
                foreach (MonoBehaviour instance in Find<MonoBehaviour>("SpecialEffectVisual", "SpecialProjectile")) {
                    if (instance != null) {
                        UnityEngine.Object.DestroyImmediate(instance.gameObject);
                    }
                }

                body.position = origin;
                player.transform.position = origin;
                Physics2D.SyncTransforms();
                GameObject wall = null;

                if (index == 4 || index == 7 || index == 8) {
                    wall = new GameObject("Verification impact target");
                    wall.transform.position = origin + Vector3.right * (index == 4 ? 3f : 4f);
                    wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 1f);

                    if (index == 4) {
                        wall.AddComponent<TestEnemy>();
                    }

                    Physics2D.SyncTransforms();
                }

                // 입력: 각 조합 버튼 → 기대: 현재 자원을 보존하며 조합 효과가 실행됨.
                bool started = (bool)practice.Invoke(hud, new object[] { index });
                Check(keys[index] + " 테스트 진입점", true, started, started);
                Check(keys[index] + " 실제 조합 횟수 유지", 3, player.SpecialUsesRemaining, player.SpecialUsesRemaining == 3);

                if (shots.Contains(index)) {
                    var projectile = Find<MonoBehaviour>("SpecialProjectile").Single();
                    var shotTick = projectile.GetType().GetMethod("FixedUpdate", Private);
                    Vector3 before = projectile.transform.position;
                    shotTick.Invoke(projectile, null);
                    Check(keys[index] + " 투사체 위치·외형", "이동·자식 렌더러", projectile.transform.position,
                        projectile.transform.position.x > before.x && projectile.GetComponentsInChildren<MeshRenderer>().Length == 3);

                    for (int step = 0; step < (index >= 7 ? 16 : 6); step++) {
                        shotTick.Invoke(projectile, null);
                    }
                } else if (index == 1 || index == 4) {
                    for (int step = 0; step < (index == 4 ? 8 : 4); step++) {
                        tick.Invoke(player, null);
                        Physics2D.Simulate(Time.fixedDeltaTime);
                    }
                }

                if (radius[index] > 0f) {
                    string name = index == 4 || index == 7 || index == 8 ? "Special burst" : "Special attack area";
                    var area = Find<MonoBehaviour>("SpecialEffectVisual").Single(item => item.name == name);
                    float size = area.transform.localScale.x;
                    var fill = area.GetComponentInChildren<MeshFilter>();
                    float maxX = fill.sharedMesh.vertices.Max(point => point.x);
                    Check(keys[index] + " 판정 반경과 메시 경계", radius[index], size * maxX,
                        Mathf.Abs(size * maxX - radius[index]) < 0.025f);
                }

                // 입력: 현재 카메라 렌더링 → 기대: 배경 위에서 효과 색 픽셀이 확인됨.
                foreach (MonoBehaviour visual in Find<MonoBehaviour>("SpecialEffectVisual")) {
                    foreach (MeshRenderer renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
                        renderer.gameObject.layer = previewLayer;
                    }
                }

                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var previous = RenderTexture.active;

                try {
                    RenderTexture.active = target;
                    tile.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    Color32[] pixels = tile.GetPixels32();
                    int impactPixels = pixels.Count(pixel => Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > 175);
                    Check(keys[index] + " 렌더 픽셀", "밝은 픽셀 20개 초과", impactPixels, impactPixels > 20);

                    if (index == 2 || index == 9) {
                        Vector3 excluded = index == 2 ? new Vector3(-1.5f, 1.5f, 0f) : new Vector3(5f, 5f, 0f);
                        Vector3 viewport = view.WorldToViewportPoint(origin + excluded);
                        Color32 outside = tile.GetPixel((int)(viewport.x * Width), (int)(viewport.y * Height));
                        Color32 background = tile.GetPixel(0, 0);
                        Check(keys[index] + " 도형 바깥 픽셀", background, outside, outside.Equals(background));
                    }

                    sheet.SetPixels32(index % 2 * Width, (4 - index / 2) * Height, Width, Height, pixels);
                } finally {
                    RenderTexture.active = previous;
                }

                if (index == 1) {
                    for (int step = 0; step < 8; step++) {
                        tick.Invoke(player, null);
                        Physics2D.Simulate(Time.fixedDeltaTime);
                    }
                }

                if (wall != null) {
                    UnityEngine.Object.DestroyImmediate(wall);
                }
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "special_effect_preview.png"), sheet.EncodeToPNG());
            Debug.Log($"[SpecialVisuals] PASS {checks}/{checks}");
            SessionState.EraseBool(Running);
            EditorApplication.Exit(0);
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    // 렌더 검증에 쓰인 런타임 효과를 내부 타입 이름으로 찾는다.
    private static T[] Find<T>(params string[] typeNames) where T : MonoBehaviour
    {
        return UnityEngine.Object.FindObjectsByType<T>().Where(item => typeNames.Contains(item.GetType().Name)).ToArray();
    }
}
