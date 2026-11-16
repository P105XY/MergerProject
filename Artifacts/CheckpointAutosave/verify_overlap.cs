// 타이틀의 플레이 모드에서 임시 슬롯과 겹친 아이템·체크포인트 트리거를 검사한다.
if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 0) {
    throw new System.InvalidOperationException("타이틀 플레이 모드가 필요합니다.");
}

const string itemId = "playtest.autosave-overlap";
var progress = typeof(Mech.Function.CheckpointProgress);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var directory = System.IO.Path.Combine("/private/tmp", "merger-autosave-overlap-" + System.Guid.NewGuid().ToString("N"));
var savePath = System.IO.Path.Combine(directory, "checkpoint.json");
var storeType = progress.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var store = System.Activator.CreateInstance(storeType, flags, null, new object[] { directory }, null);
progress.GetField("saveStore", flags).SetValue(null, store);
int passed = 0;

// 기대값과 실제값을 출력하고 값이 다르면 검사에 실패한다.
System.Action<string, bool, bool> check = (input, expected, actual) => {
    string message = string.Format("[Autosave Overlap] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual);
    Debug.Log(message);
    UnityEngine.Assertions.Assert.AreEqual(expected, actual, message);

    if (expected != actual) {
        throw new System.InvalidOperationException(message);
    }

    passed++;
};

// 스테이지가 열리면 가운데 체크포인트와 같은 위치에 검증용 아이템을 놓는다.
var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1);
load.completed += _ => {
    var item = new GameObject("Autosave overlap probe");
    item.SetActive(false);
    item.transform.position = new Vector3(0.5f, 0.4f, 0f);
    item.AddComponent<BoxCollider2D>().isTrigger = true;
    var pickup = item.AddComponent<Mech.Function.OneTimePickup>();
    typeof(Mech.Function.OneTimePickup).GetField("itemId", flags).SetValue(pickup, itemId);
    item.SetActive(true);

    var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
    var body = player.GetComponent<Rigidbody2D>();
    body.position = new Vector2(0.5f, 0.4f);
    player.transform.position = body.position;
    body.linearVelocity = Vector2.zero;
    body.WakeUp();
    Physics2D.SyncTransforms();

    double deadline = EditorApplication.timeSinceStartup + 10.0;
    EditorApplication.CallbackFunction observe = null;

    // 실제 물리 접촉이 끝나면 자동 저장에 같은 프레임의 획득 ID가 있는지 확인한다.
    observe = () => {
        bool ready = Mech.Function.CheckpointProgress.HasCollectedItem(itemId) && System.IO.File.Exists(savePath);

        if (!ready && EditorApplication.timeSinceStartup < deadline) {
            return;
        }

        EditorApplication.update -= observe;

        try {
            check("동시 접촉에서 아이템 획득 및 체크포인트 저장", true, ready);
            check("획득한 아이템 숨김", false, item.activeSelf);
            string json = System.IO.File.ReadAllText(savePath);
            check("중간 체크포인트 ID", true, json.Contains("playtest-middle"));
            check("같은 프레임에 획득한 아이템 ID 저장", true, json.Contains(itemId));
        } catch (System.Exception error) {
            Debug.LogException(error);
        } finally {
            Debug.Log("[Autosave Overlap] 종료 | 통과 " + passed);
        }
    };
    EditorApplication.update += observe;
};
return new { directory, unityVersion = Application.unityVersion };
