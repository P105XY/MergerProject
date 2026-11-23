// SampleScene의 플레이 모드에서 실제 아이템과 체크포인트의 트리거를 검증한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused) {
    throw new System.InvalidOperationException("실행 중인 플레이 모드가 필요합니다.");
}

const string stagePath = "Assets/Scenes/TestScene/PlayTestScene.unity";
const string titlePath = "Assets/Scenes/SampleScene.unity";
const string firstId = "playtest.test_key";
const string secondId = "playtest.test_charm";
var progressType = typeof(Mech.Function.CheckpointProgress);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
string directory = System.IO.Path.Combine("/private/tmp", "merger-pickup-verification-" + System.Guid.NewGuid().ToString("N"));
string primary = System.IO.Path.Combine(directory, "checkpoint.json");
var storeType = progressType.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var isolatedStore = System.Activator.CreateInstance(storeType, new object[] { directory });
progressType.GetField("saveStore", flags).SetValue(null, isolatedStore);
var idField = typeof(Mech.Function.OneTimePickup).GetField("itemId", flags);
var eventField = typeof(Mech.Function.OneTimePickup).GetField("onCollected", flags);
int collectedEvents = 0;
int loads = 0;
int phase = 0;
int passed = 0;
bool loadSucceeded = false;
GameObject firstPickup = null;
GameObject secondPickup = null;
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction observe = null;

// 기대값과 실제값을 남기고 불일치하면 검증을 중단한다.
System.Action<string, object, object> check = (input, expected, actual) => {
    string message = string.Format("[One-Time Pickup] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual);
    Debug.Log(message);
    UnityEngine.Assertions.Assert.AreEqual(expected, actual, message);

    if (!object.Equals(expected, actual)) {
        throw new System.InvalidOperationException(message);
    }

    passed++;
};

// 새로 로드된 스테이지에 테스트 아이템을 배치하고 보상 이벤트 횟수를 센다.
System.Func<UnityEngine.SceneManagement.Scene, string, float, GameObject> place = (scene, id, x) => {
    var item = new GameObject(id);
    item.SetActive(false);

    if (item.scene != scene) {
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item, scene);
    }

    item.transform.position = new Vector3(x, 0.4f, 0f);
    var trigger = item.AddComponent<BoxCollider2D>();
    trigger.isTrigger = true;
    trigger.size = new Vector2(0.6f, 1f);
    var pickup = item.AddComponent<Mech.Function.OneTimePickup>();
    idField.SetValue(pickup, id);
    var onCollected = (UnityEngine.Events.UnityEvent<Mech.Function.PlayerBase>)eventField.GetValue(pickup);
    onCollected.AddListener(player => collectedEvents++);
    item.SetActive(true);
    return item;
};

// 실제 복원 콜백보다 먼저 씬에 테스트 오브젝트를 만든다.
UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode> prepare =
    (scene, mode) => {
        if (scene.path == stagePath) {
            firstPickup = place(scene, firstId, -7f);
            secondPickup = place(scene, secondId, -9f);
        }
    };

// 비동기 복원 결과를 다음 에디터 갱신에서 읽을 수 있도록 기록한다.
System.Action<bool> finished = succeeded => {
    loadSucceeded = succeeded;
    loads++;
};

// 현재 씬의 플레이어를 찾아 물리 트리거를 밟을 위치로 옮긴다.
System.Func<Mech.Function.PlayerBase> getPlayer = () => UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
System.Action<float> move = x => {
    var player = getPlayer();
    var body = player.GetComponent<Rigidbody2D>();
    var target = new Vector2(x, 0.4f);
    player.transform.position = target;
    body.position = target;
    body.linearVelocity = Vector2.zero;
    body.WakeUp();
    Physics2D.SyncTransforms();
};

// 이전 버전의 JSON으로 시작하고 수집, 저장, 재시작, 이어하기와 새 게임을 차례로 확인한다.
observe = () => {
    bool done = false;
    try {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - start > 25.0) {
            throw new System.InvalidOperationException("검증 시간 초과 또는 플레이 종료. 단계 " + phase);
        }

        string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        switch (phase) {
            case 0:
                // 획득 필드가 없는 기존 세이브로도 이어할 수 있어야 한다.
                check("기존 JSON의 이어하기 시작", true, Mech.Function.CheckpointProgress.TryContinue());
                phase++;
                break;
            case 1:
                if (loads < 1) {
                    return;
                }

                check("기존 세이브 복원", true, loadSucceeded && scenePath == stagePath);
                check("기존 플레이어 조각 복원", 1, getPlayer().TypeBCount);
                check("기존 세이브의 아이템 초기 표시", true, firstPickup.activeSelf && secondPickup.activeSelf);
                move(-7f);
                phase++;
                break;
            case 2:
                if (!Mech.Function.CheckpointProgress.HasCollectedItem(firstId)) {
                    return;
                }

                // 실제 접촉으로 획득한 아이템은 숨기고 보상은 한 번만 호출한다.
                check("첫 아이템 획득 후 비활성", false, firstPickup.activeSelf);
                check("첫 아이템 보상 호출", 1, collectedEvents);
                firstPickup.SetActive(true);
                check("이미 획득한 아이템 다시 활성화", false, firstPickup.activeSelf);
                check("다시 활성화해도 보상 중복 없음", 1, collectedEvents);
                move(0.5f);
                phase++;
                break;
            case 3:
                string saved = System.IO.File.ReadAllText(primary);

                if (!saved.Contains("playtest-middle")) {
                    return;
                }

                // 중간 체크포인트는 먼저 주운 아이템의 ID만 저장한다.
                check("체크포인트가 첫 아이템 ID 저장", true, saved.Contains("\"CollectedItemIds\":[\"" + firstId + "\"]"));
                move(-9f);
                phase++;
                break;
            case 4:
                if (!Mech.Function.CheckpointProgress.HasCollectedItem(secondId)) {
                    return;
                }

                // 두 번째 아이템은 저장 전에 주웠으므로 사망 시 롤백된다.
                check("둘째 아이템 획득 후 비활성", false, secondPickup.activeSelf);
                check("체크포인트 이후의 ID는 파일에 없음", false, System.IO.File.ReadAllText(primary).Contains(secondId));
                getPlayer().ApplyDamage(getPlayer().MaxHealth);
                check("사망으로 체크포인트 복원 요청", true, Mech.Function.CheckpointProgress.IsLoading);
                phase++;
                break;
            case 5:
                if (loads < 2) {
                    return;
                }

                check("사망 후 로드 성공", true, loadSucceeded);
                check("사망 후 저장된 아이템 비활성", false, firstPickup.activeSelf);
                check("사망 후 미저장 아이템 복귀", true, secondPickup.activeSelf);
                check("사망 후 획득 목록이 체크포인트 상태로 복귀", false, Mech.Function.CheckpointProgress.HasCollectedItem(secondId));
                check("복원 시 보상 재호출 없음", 2, collectedEvents);
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
                phase++;
                break;
            case 6:
                if (scenePath != titlePath) {
                    return;
                }

                // 타이틀에서 이어하기도 같은 저장 상태를 복원한다.
                check("타이틀에서 이어하기 시작", true, Mech.Function.CheckpointProgress.TryContinue());
                phase++;
                break;
            case 7:
                if (loads < 3) {
                    return;
                }

                check("이어하기 후 저장된 아이템 비활성", false, firstPickup.activeSelf);
                check("이어하기 후 미저장 아이템 활성", true, secondPickup.activeSelf);
                check("새 게임 시작 요청", true, Mech.Function.CheckpointProgress.TryStartNewGame(stagePath));
                phase++;
                break;
            case 8:
                if (Mech.Function.CheckpointProgress.IsLoading) {
                    return;
                }

                // 새 게임의 획득 목록은 비고 기존 슬롯은 첫 저장까지 남는다.
                check("새 게임 아이템 표시", true, firstPickup.activeSelf && secondPickup.activeSelf);
                check("새 게임 기존 획득 상태 제거", false, Mech.Function.CheckpointProgress.HasCollectedItem(firstId));
                check("새 게임 첫 체크포인트 전 슬롯 유지", true, System.IO.File.ReadAllText(primary).Contains(firstId));
                done = true;
                break;
        }
    } catch (System.Exception error) {
        Debug.LogException(error);
        done = true;
    } finally {
        if (done) {
            EditorApplication.update -= observe;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= prepare;
            Mech.Function.CheckpointProgress.LoadFinished -= finished;
            Debug.Log("[One-Time Pickup] 종료 | 통과 " + passed);
        }
    }
};
System.IO.Directory.CreateDirectory(directory);
System.IO.File.WriteAllText(primary, "{\"Version\":1,\"ScenePath\":\"" + stagePath
    + "\",\"CheckpointId\":\"playtest-start\",\"Player\":{\"CollectedB\":1}}");
UnityEngine.SceneManagement.SceneManager.sceneLoaded += prepare;
Mech.Function.CheckpointProgress.LoadFinished += finished;
EditorApplication.update += observe;
return new { directory, unityVersion = Application.unityVersion };
