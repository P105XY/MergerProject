// 타이틀 플레이 모드에서 중요 아이템·3분 간격·지역 이동·종료 자동 저장을 검사한다.
if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 0) {
    throw new System.InvalidOperationException("타이틀 플레이 모드가 필요합니다.");
}

const string stagePath = "Assets/Scenes/TestScene/PlayTestScene.unity";
const string destinationPath = "Assets/Scenes/SampleScene.unity";
const string itemId = "playtest.important-key";
const string arrivalId = "autosave-arrival";
var progress = typeof(Mech.Function.CheckpointProgress);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var directory = System.IO.Path.Combine("/private/tmp", "merger-autosave-triggers-" + System.Guid.NewGuid().ToString("N"));
var savePath = System.IO.Path.Combine(directory, "checkpoint.json");
var storeType = progress.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var store = System.Activator.CreateInstance(storeType, flags, null, new object[] { directory }, null);
progress.GetField("saveStore", flags).SetValue(null, store);
int passed = 0;
int phase = 0;
double deadline = EditorApplication.timeSinceStartup + 25.0;
EditorApplication.CallbackFunction observe = null;

// 검증 입력과 기대값·실제값을 출력하고 차이가 있으면 중단한다.
System.Action<string, object, object> check = (input, expected, actual) => {
    string message = string.Format("[Autosave Triggers] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual);
    Debug.Log(message);
    UnityEngine.Assertions.Assert.AreEqual(expected, actual, message);

    if (!object.Equals(expected, actual)) {
        throw new System.InvalidOperationException(message);
    }

    passed++;
};

// 현재 슬롯을 읽고 아직 저장되지 않았다면 빈 문자열을 반환한다.
System.Func<string> read = () => System.IO.File.Exists(savePath) ? System.IO.File.ReadAllText(savePath) : "";
System.Func<Mech.Function.PlayerBase> player = () => UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();

// 플레이어를 지점으로 옮겨 실제 2D 물리 트리거를 밟는다.
System.Action<float> move = x => {
    var target = new Vector2(x, 0.4f);
    var body = player().GetComponent<Rigidbody2D>();
    player().transform.position = target;
    body.position = target;
    body.linearVelocity = Vector2.zero;
    body.WakeUp();
    Physics2D.SyncTransforms();
};

// 두 번째 지역의 시작 위치와 도착 체크포인트를 임시로 구성한다.
UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode> prepareArrival = (scene, mode) => {
    if (scene.path == destinationPath) {
        var destinationPlayer = new GameObject("Temporary destination player");
        destinationPlayer.transform.position = new Vector3(20f, 50f, 0f);
        destinationPlayer.AddComponent<BoxCollider2D>();
        destinationPlayer.AddComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        destinationPlayer.AddComponent<Mech.Function.PlayerBase>();

        var arrival = new GameObject("Temporary arrival checkpoint");
        arrival.transform.position = new Vector3(22f, 50f, 0f);
        arrival.AddComponent<BoxCollider2D>().isTrigger = true;
        var checkpoint = arrival.AddComponent<Mech.Function.Checkpoint>();
        typeof(Mech.Function.Checkpoint).GetField("checkpointId", flags).SetValue(checkpoint, arrivalId);
    }
};
UnityEngine.SceneManagement.SceneManager.sceneLoaded += prepareArrival;

// 타이머 경계와 실제 획득·이동 뒤의 저장 파일을 순서대로 확인한다.
observe = () => {
    bool done = false;
    try {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline) {
            throw new System.InvalidOperationException("검증 시간 초과 또는 플레이 종료. 단계 " + phase);
        }

        string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        switch (phase) {
            case 0:
                // 타이틀에서 시간을 흘려도 저장을 만들지 않아야 한다.
                progress.GetMethod("TickAutosave", flags).Invoke(null, new object[] { 180f });
                check("타이틀에서는 180초가 지나도 저장 없음", "", read());
                check("새 게임 시작", true, Mech.Function.CheckpointProgress.TryStartNewGame(stagePath));
                phase++;
                break;
            case 1:
                if (scenePath != stagePath || Mech.Function.CheckpointProgress.IsLoading) {
                    return;
                }

                // 179초에는 저장하지 않고 180초에 시작 위치와 현재 상태를 기록한다.
                var tick = progress.GetMethod("TickAutosave", flags);
                progress.GetField("autosaveElapsed", flags).SetValue(null, 0f);
                player().TryCollectFragment(Mech.Function.CollectibleType.A);
                tick.Invoke(null, new object[] { 179f });
                check("게임 플레이 179초에는 저장 없음", "", read());
                tick.Invoke(null, new object[] { 1f });
                check("180초에 지역 시작 위치 저장", true, read().Contains("\"CheckpointId\":\"$scene-entry\""));
                check("180초에 당시 조각 저장", true, read().Contains("\"CollectedA\":1"));
                string timedSave = read();
                player().TryCollectFragment(Mech.Function.CollectibleType.B);
                float originalTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                try {
                    tick.Invoke(null, new object[] { 180f });
                    check("일시 정지에서는 주기 저장 없음", timedSave, read());
                } finally {
                    Time.timeScale = originalTimeScale;
                }

                // 중요 아이템은 보상 이벤트에서 받은 마지막 조각까지 반영해 저장해야 한다.
                var item = new GameObject("Temporary important item");
                item.SetActive(false);
                item.transform.position = new Vector3(-7f, 0.4f, 0f);
                item.AddComponent<BoxCollider2D>().isTrigger = true;
                var pickup = item.AddComponent<Mech.Function.OneTimePickup>();
                var pickupType = typeof(Mech.Function.OneTimePickup);
                pickupType.GetField("itemId", flags).SetValue(pickup, itemId);
                pickupType.GetField("autoSaveOnCollect", flags).SetValue(pickup, true);
                var reward = (UnityEngine.Events.UnityEvent<Mech.Function.PlayerBase>)pickupType.GetField("onCollected", flags).GetValue(pickup);
                reward.AddListener(target => target.TryCollectFragment(Mech.Function.CollectibleType.C));
                item.SetActive(true);
                move(-7f);
                phase++;
                break;
            case 2:
                if (!Mech.Function.CheckpointProgress.HasCollectedItem(itemId)) {
                    return;
                }

                check("중요 아이템 ID 즉시 저장", true, read().Contains(itemId));
                check("중요 아이템 보상 후 특수 횟수 저장", true, read().Contains("\"SpecialUsesRemaining\":3"));
                move(-3.5f);
                phase++;
                break;
            case 3:
                if (!read().Contains("playtest-start")) {
                    return;
                }

                // 실제 체크포인트를 지나면 복귀 지점이 바뀌고 지역 이동은 도착 지점에서 저장한다.
                check("체크포인트 자동 저장", true, read().Contains("playtest-start"));
                player().TryUseSpecial();
                player().ApplyDamage(25f);
                var portal = new GameObject("Temporary region exit");
                portal.SetActive(false);
                portal.transform.position = new Vector3(-9f, 0.4f, 0f);
                portal.AddComponent<BoxCollider2D>().isTrigger = true;
                var transition = portal.AddComponent<Mech.Function.RegionTransition>();
                var transitionType = typeof(Mech.Function.RegionTransition);
                transitionType.GetField("destinationScenePath", flags).SetValue(transition, destinationPath);
                transitionType.GetField("destinationCheckpointId", flags).SetValue(transition, arrivalId);
                portal.SetActive(true);
                move(-9f);
                phase++;
                break;
            case 4:
                if (scenePath != destinationPath || Mech.Function.CheckpointProgress.IsLoading) {
                    return;
                }

                check("지역 이동의 목적지 씬 저장", true, read().Contains(destinationPath));
                check("지역 이동의 도착 체크포인트 저장", true, read().Contains(arrivalId));
                check("도착 체크포인트 X", 22f, player().transform.position.x);
                check("이동한 플레이어 체력 유지", 75f, player().CurrentHealth);
                check("지역 이동 시 중요 아이템 획득 이력 유지", true, read().Contains(itemId));
                check("지역 이동 때 특수 횟수 저장", true, read().Contains("\"SpecialUsesRemaining\":2"));
                player().TryUseSpecial();
                // 편집기에서 플레이 모드를 정상 종료하면 종료 콜백이 1회를 저장해야 한다.
                check("종료 직전에만 바뀐 특수 횟수", 1, player().SpecialUsesRemaining);
                check("종료 전 슬롯에는 지역 이동 시점 횟수", true, read().Contains("\"SpecialUsesRemaining\":2"));
                done = true;
                break;
        }
    } catch (System.Exception error) {
        Debug.LogException(error);
        done = true;
    } finally {
        if (done) {
            EditorApplication.update -= observe;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= prepareArrival;
            Debug.Log("[Autosave Triggers] 종료 직전 | 통과 " + passed);
        }
    }
};
EditorApplication.update += observe;
return new { directory, unityVersion = Application.unityVersion };
