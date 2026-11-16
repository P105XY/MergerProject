// 시작 씬의 플레이 모드에서 실제 UI Toolkit 버튼과 씬 전환을 검증한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused) {
    throw new System.InvalidOperationException("실행 중인 타이틀 플레이 모드가 필요합니다.");
}

const string titlePath = "Assets/Scenes/SampleScene.unity";
const string stagePath = "Assets/Scenes/TestScene/PlayTestScene.unity";
const string prefsKey = "Mech.GameOptions.v1";
var progress = typeof(Mech.Function.CheckpointProgress);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var directory = System.IO.Path.Combine("/private/tmp", "merger-title-verification-" + System.Guid.NewGuid().ToString("N"));
var storeType = progress.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var isolatedStore = System.Activator.CreateInstance(storeType, flags, null, new object[] { directory }, null);
progress.GetField("saveStore", flags).SetValue(null, isolatedStore);
int passed = 0;

// 입력과 기대값 및 실제값을 남기고 불일치 시 실행을 중단한다.
System.Action<string, object, object> check = (input, expected, actual) => {
    string message = string.Format("[Title Verification] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual);
    Debug.Log(message);
    bool success = expected is float && actual is float
        ? Mathf.Abs((float)expected - (float)actual) < 0.04f
        : object.Equals(expected, actual);
    UnityEngine.Assertions.Assert.IsTrue(success, message);

    if (!success) {
        throw new System.InvalidOperationException(message);
    }

    passed++;
};

// 현재 타이틀의 문서와 이름이 지정된 버튼을 조회한다.
System.Func<UnityEngine.UIElements.VisualElement> root = () =>
    UnityEngine.Object.FindAnyObjectByType<UnityEngine.UIElements.UIDocument>().rootVisualElement;
System.Func<string, UnityEngine.UIElements.Button> button = name =>
    UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root(), name, (string)null);

// 실제 키보드 조작 없이 버튼에 UI Toolkit의 제출 이벤트를 보낸다.
System.Action<string> submit = name => {
    var element = button(name);

    using (var evt = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()) {
        evt.target = element;
        element.SendEvent(evt);
    }
};

var priorOptions = Mech.Function.GameOptions.GetCurrent();
bool hadPrefs = PlayerPrefs.HasKey(prefsKey);
string priorPrefs = PlayerPrefs.GetString(prefsKey, "");

try {
    // 옵션 버튼은 편집 화면을 열고 뒤로는 변경을 적용하지 않는다.
    submit("open-options");
    var options = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root(), "options-menu", (string)null);
    check("옵션 버튼으로 패널 열기", false, options.ClassListContains("is-hidden"));
    var volume = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Slider>(root(), "master-volume", (string)null);
    volume.value = 0.42f;
    submit("back-options");
    check("뒤로 버튼으로 패널 닫기", true, options.ClassListContains("is-hidden"));
    check("옵션 취소 뒤 전체 볼륨", priorOptions.MasterVolume, AudioListener.volume);
    check("빈 슬롯의 이어하기 버튼", false, button("continue-game").enabledInHierarchy);
    submit("continue-game");
    check("비활성 이어하기의 씬 로드 여부", false, Mech.Function.CheckpointProgress.IsLoading);
    // 적용 버튼은 볼륨 값을 엔진과 저장에 반영한다.
    submit("open-options");
    volume.value = 0.42f;
    submit("apply-options");
    check("옵션 적용 뒤 전체 볼륨", 0.42f, AudioListener.volume);
    var savedOptions = JsonUtility.FromJson<Mech.Function.GameOptionValues>(PlayerPrefs.GetString(prefsKey));
    check("옵션 파일에 변경값 기록", 0.42f, savedOptions.MasterVolume);
    check("옵션 적용 뒤 타이틀 복귀", true, options.ClassListContains("is-hidden"));
} finally {
    if (hadPrefs) {
        PlayerPrefs.SetString(prefsKey, priorPrefs);
    } else {
        PlayerPrefs.DeleteKey(prefsKey);
    }

    PlayerPrefs.Save();
    Mech.Function.GameOptions.Apply(priorOptions);
}

// 중간 체크포인트와 특수 2회를 가진 기존 진행으로 타이틀의 로드 경로를 확인한다.
string sample = "{\"Version\":1,\"ScenePath\":\"" + stagePath + "\",\"CheckpointId\":\"playtest-middle\","
    + "\"Player\":{\"ActiveA\":1,\"ActiveB\":1,\"ActiveC\":1,\"SpecialUsesRemaining\":2}}";
var dataType = progress.Assembly.GetType("Mech.Function.CheckpointSaveData", true);
storeType.GetMethod("Write", flags).Invoke(isolatedStore, new[] { JsonUtility.FromJson(sample, dataType) });
submit("open-options");
submit("back-options");
check("유효한 슬롯의 이어하기 버튼", true, button("continue-game").enabledInHierarchy);
submit("continue-game");
check("이어하기 버튼이 씬 전환 시작", true, Mech.Function.CheckpointProgress.IsLoading);

double start = EditorApplication.timeSinceStartup;
int phase = 0;
EditorApplication.CallbackFunction observe = null;

// 비동기 씬 전환 완료 후 이어하기와 새 게임의 서로 다른 진입 상태를 확인한다.
observe = () => {
    bool done = false;
    try {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - start > 25.0) {
            throw new System.InvalidOperationException("타이틀 UI 검증 제한 시간 초과. 단계 " + phase);
        }

        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        if (phase == 0 && scene == stagePath && !Mech.Function.CheckpointProgress.IsLoading) {
            var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
            check("이어하기 버튼으로 복원된 체크포인트 X", 0.5f, player.transform.position.x);
            check("이어하기 버튼으로 복원된 특수 횟수", 2, player.SpecialUsesRemaining);
            check("이어하기 버튼으로 복원된 체력", player.MaxHealth, player.CurrentHealth);
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
            phase++;
        } else if (phase == 1 && scene == titlePath) {
            check("타이틀 재진입 시 기존 슬롯 표시", true, button("continue-game").enabledInHierarchy);
            submit("new-game");
            check("새 게임 버튼이 씬 전환 시작", true, Mech.Function.CheckpointProgress.IsLoading);
            phase++;
        } else if (phase == 2 && scene == stagePath && !Mech.Function.CheckpointProgress.IsLoading) {
            var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
            check("새 게임 시작 X", -5f, player.transform.position.x);
            check("새 게임 특수 횟수", 0, player.SpecialUsesRemaining);
            check("새 게임 이전 슬롯 보존", true, System.IO.File.ReadAllText(System.IO.Path.Combine(directory, "checkpoint.json")).Contains("playtest-middle"));
            player.ApplyDamage(player.MaxHealth);
            check("새 게임 첫 체크포인트 전 사망은 이전 슬롯 로드 없음", true, player.IsDead && !Mech.Function.CheckpointProgress.IsLoading);
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
            phase++;
        } else if (phase == 3 && scene == titlePath) {
            submit("new-game");
            phase++;
        } else if (phase == 4 && scene == stagePath && !Mech.Function.CheckpointProgress.IsLoading) {
            // 새 게임에서 첫 체크포인트에 닿으면 현재 진행이 기존 슬롯을 갱신한다.
            var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(-3.5f, 0.4f);
            player.transform.position = body.position;
            body.WakeUp();
            Physics2D.SyncTransforms();
            phase++;
        } else if (phase == 5 && System.IO.File.ReadAllText(System.IO.Path.Combine(directory, "checkpoint.json")).Contains("playtest-start")) {
            var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
            check("새 게임 첫 체크포인트가 단일 슬롯을 갱신", true, Mech.Function.CheckpointProgress.HasSave());
            player.ApplyDamage(player.MaxHealth);
            check("새 게임 첫 저장 이후 사망 시 현재 슬롯 로드", true, Mech.Function.CheckpointProgress.IsLoading);
            phase++;
        } else if (phase == 6 && scene == stagePath && !Mech.Function.CheckpointProgress.IsLoading) {
            var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
            check("새 게임에서 저장한 시작 체크포인트 복원", -3.5f, player.transform.position.x);
            done = true;
        }
    } catch (System.Exception error) {
        Debug.LogException(error);
        done = true;
    } finally {
        if (done) {
            EditorApplication.update -= observe;
            Debug.Log("[Title Verification] 종료 | 통과 " + passed);
        }
    }
};
EditorApplication.update += observe;
return new { directory, unityVersion = Application.unityVersion };
