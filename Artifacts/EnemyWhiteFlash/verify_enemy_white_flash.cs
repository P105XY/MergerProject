// Unity MCP PlayMode에서 근접 공격, 실제 URP 픽셀과 피격 상태 복원을 검사한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PlayTestScene") {
    throw new InvalidOperationException("실행 중인 PlayTestScene이 필요합니다.");
}

var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
var progress = typeof(Mech.Function.CheckpointProgress);
var storeType = progress.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var directory = System.IO.Path.Combine("/private/tmp", "merger-whiteflash-" + Guid.NewGuid().ToString("N"));
var store = Activator.CreateInstance(storeType, new object[] { directory });
progress.GetField("saveStore", flags).SetValue(null, store);
var flashProperty = typeof(Mech.Function.EnemyBase).GetProperty("IsWhiteFlashing");
var objects = new List<UnityEngine.Object>();
int checks = 0;

// 입력과 기대값, 실제값을 기록하고 실패 시 검증을 중단한다.
Action<string, string, object, bool> check = (input, expected, actual, passed) => {
    string line = "[EnemyWhiteFlash] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
    Debug.Log(line);

    if (!passed) {
        throw new InvalidOperationException(line);
    }

    checks++;
};

// 런타임에 만든 검증용 오브젝트와 렌더링 리소스를 제거한다.
Action cleanup = () => {
    foreach (var instance in objects) {
        UnityEngine.Object.Destroy(instance);
    }
};

try {
    var center = new Vector3(1000f, 20f, 0f);
    var enemyObject = new GameObject("WhiteFlash verification enemy");
    objects.Add(enemyObject);
    enemyObject.transform.position = center;
    enemyObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.8f;
    var enemy = enemyObject.AddComponent<Mech.Function.TestEnemy>();
    var sprite = enemyObject.AddComponent<SpriteRenderer>();
    var texture = new Texture2D(3, 1, TextureFormat.RGBA32, false);
    objects.Add(texture);
    texture.filterMode = FilterMode.Point;
    texture.SetPixels32(new[] { new Color32(15, 35, 50, 255), new Color32(15, 35, 50, 128), new Color32(15, 35, 50, 0) });
    texture.Apply();
    sprite.sprite = Sprite.Create(texture, new Rect(0, 0, 3, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
    objects.Add(sprite.sprite);
    var original = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
    objects.Add(original);
    sprite.sharedMaterial = original;

    var playerObject = new GameObject("WhiteFlash verification player");
    objects.Add(playerObject);
    playerObject.transform.position = center - Vector3.right * 1.1f;
    playerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    playerObject.AddComponent<BoxCollider2D>().enabled = false;
    var player = playerObject.AddComponent<Mech.Function.PlayerBase>();
    Physics2D.SyncTransforms();

    var cameraObject = new GameObject("WhiteFlash verification camera");
    objects.Add(cameraObject);
    cameraObject.transform.position = center - Vector3.forward * 10f;
    var camera = cameraObject.AddComponent<Camera>();
    camera.enabled = false;
    camera.orthographic = true;
    camera.orthographicSize = 0.5f;
    camera.aspect = 3f;
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = Color.black;
    camera.allowHDR = false;
    var target = new RenderTexture(96, 32, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
    objects.Add(target);
    var readback = new Texture2D(96, 32, TextureFormat.RGBA32, false);
    objects.Add(readback);

    // URP로 그린 화면을 읽고 불투명, 반투명, 투명 영역의 색을 반환한다.
    Func<Color32[]> render = () => {
        var previous = RenderTexture.active;

        try {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, 96, 32), 0, 0);
            var pixels = readback.GetPixels32();
            return new[] { pixels[16 * 96 + 16], pixels[16 * 96 + 48], pixels[16 * 96 + 80] };
        } finally {
            RenderTexture.active = previous;
        }
    };

    // 일반 피해와 유효하지 않은 플레이어 피해는 기존 머티리얼을 유지한다.
    enemy.ApplyDamage(1f);
    enemy.ApplyPlayerDamage(0f);
    check("일반 피해 1과 유효하지 않은 플레이어 피해 0", "HP 99, 플래시 없음", enemy.CurrentHealth + ", " + flashProperty.GetValue(enemy),
        enemy.CurrentHealth == 99f && !(bool)flashProperty.GetValue(enemy));
    var before = render()[0];
    check("플래시 전 어두운 스프라이트", "RGB 15, 35, 50 ±2", before, Math.Abs(before.r - 15) <= 2 && Math.Abs(before.g - 35) <= 2 && Math.Abs(before.b - 50) <= 2);

    // 실제 PlayerBase 근접 판정은 피해를 주고 즉시 0.05초 플래시를 시작한다.
    double hitTime = Time.unscaledTimeAsDouble;
    typeof(Mech.Function.PlayerBase).GetMethod("Attack", flags).Invoke(player, null);
    check("근접 공격 10", "HP 89, 플래시 활성", enemy.CurrentHealth + ", " + flashProperty.GetValue(enemy),
        enemy.CurrentHealth == 89f && (bool)flashProperty.GetValue(enemy));
    var controller = enemyObject.GetComponent(progress.Assembly.GetType("Mech.Function.EnemyWhiteFlash", true));
    var endField = controller.GetType().GetField("endsAt", flags);
    double configured = (double)endField.GetValue(controller) - hitTime;
    check("플래시 종료 시각", "0.05초 뒤", configured, Math.Abs(configured - 0.05) < 0.00001);
    var during = render();
    check("플래시 불투명 픽셀", "RGB 230, 230, 230 ±1", during[0],
        Math.Abs(during[0].r - 230) <= 1 && Math.Abs(during[0].g - 230) <= 1 && Math.Abs(during[0].b - 230) <= 1);
    check("반투명 픽셀", "RGB 169 ±2", during[1], Math.Abs(during[1].r - 169) <= 2 && during[1].r == during[1].g && during[1].r == during[1].b);
    check("투명 픽셀", "RGB 0, 0, 0", during[2], during[2].r == 0 && during[2].g == 0 && during[2].b == 0);

    // 종료까지 0.01초 남은 시점을 재현하고 재피격 후 종료 시간이 0.05초로 늘어나는지 확인한다.
    endField.SetValue(controller, hitTime + 0.01);
    enemy.ApplyPlayerDamage(1f);
    configured = (double)endField.GetValue(controller) - hitTime;
    check("플래시 중 재피격", "0.05초로 갱신", configured, Math.Abs(configured - 0.05) < 0.00001);

    // 실제 프레임을 기다린 뒤 원래 머티리얼과 픽셀을 확인하고 배치된 적의 치명타도 검사한다.
    var sceneEnemy = GameObject.Find("Test Enemy A").GetComponent<Mech.Function.TestEnemy>();
    var display = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
    var hitbox = sceneEnemy.GetComponent<Collider2D>();
    int phase = 0;
    double timedHit = 0;
    int timedFrame = 0;
    double deadline = EditorApplication.timeSinceStartup + 6.0;
    EditorApplication.CallbackFunction observe = null;
    observe = () => {
        try {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline) {
                throw new InvalidOperationException("플래시 복원 검증이 시간 초과되었습니다.");
            }

            if (phase == 0) {
                if ((bool)flashProperty.GetValue(enemy)) {
                    return;
                }

                bool restored = sprite.sharedMaterial == original && render()[0].Equals(before);
                check("플래시 종료 뒤 머티리얼과 픽셀", "원래 값 복원", restored, restored);
                enemy.ApplyPlayerDamage(1f);
                enemyObject.SetActive(false);
                check("플래시 중 오브젝트 비활성화", "머티리얼 즉시 복원", sprite.sharedMaterial == original, sprite.sharedMaterial == original);
                phase = 1;
                return;
            }

            if (phase == 1) {
                // GPU 읽기 작업과 다른 프레임에서 플래시를 시작해 실제 복원 시간을 잰다.
                enemyObject.SetActive(true);
                timedHit = Time.unscaledTimeAsDouble;
                timedFrame = Time.frameCount;
                enemy.ApplyPlayerDamage(1f);
                phase = 2;
                return;
            }

            if (phase == 2) {
                if ((bool)flashProperty.GetValue(enemy)) {
                    return;
                }

                double elapsed = Time.unscaledTimeAsDouble - timedHit;
                check("별도 피격에서 실제 프레임 경과", "0.05초 이상", elapsed.ToString("F4") + "초, " + (Time.frameCount - timedFrame) + "프레임", elapsed >= 0.05);
                sceneEnemy.ApplyPlayerDamage(sceneEnemy.CurrentHealth);
                typeof(PlayTestDisplay).GetMethod("Update", flags).Invoke(display, null);
                check("배치된 적 치명타", "처치·플래시 활성·충돌 해제", sceneEnemy.IsDefeated + ", " + flashProperty.GetValue(sceneEnemy) + ", " + hitbox.enabled,
                    sceneEnemy.IsDefeated && sceneEnemy.gameObject.activeSelf && (bool)flashProperty.GetValue(sceneEnemy) && !hitbox.enabled);
                phase = 3;
                return;
            }

            if (sceneEnemy.gameObject.activeSelf) {
                return;
            }

            check("치명타 플래시 종료", "플래시 해제 및 적 숨김", !sceneEnemy.gameObject.activeSelf, !(bool)flashProperty.GetValue(sceneEnemy));
            Debug.Log("[EnemyWhiteFlash] PASS " + checks + "/13");
        } catch (Exception error) {
            Debug.LogError("[EnemyWhiteFlash] " + error);
        }

        EditorApplication.update -= observe;
        cleanup();
    };
    EditorApplication.update += observe;
    return "WhiteFlash frame checks scheduled.";
} catch {
    cleanup();
    throw;
}
