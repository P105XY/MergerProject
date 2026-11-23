// PlayTestScene의 플레이 모드에서 키보드 입력 없이 단일 슬롯과 실제 씬 복원을 검증한다.
if (!EditorApplication.isPlaying || EditorApplication.isPaused) {
    throw new System.InvalidOperationException("실행 중인 플레이 모드가 필요합니다.");
}

const string scenePath = "Assets/Scenes/TestScene/PlayTestScene.unity";
const string titlePath = "Assets/Scenes/SampleScene.unity";
var progress = typeof(Mech.Function.CheckpointProgress);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var directory = System.IO.Path.Combine("/private/tmp", "merger-checkpoint-verification-" + System.Guid.NewGuid().ToString("N"));
var storeType = progress.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var isolatedStore = System.Activator.CreateInstance(storeType, new object[] { directory });
progress.GetField("saveStore", flags).SetValue(null, isolatedStore);
var primary = System.IO.Path.Combine(directory, "checkpoint.json");
var backup = primary + ".bak";
int passed = 0;
int phase = 0;
int loadResults = 0;
bool loadSuccess = false;
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction observe = null;

// 입력과 기대값 및 실제값을 기록하고 비교에 실패하면 시험을 중단한다.
System.Action<string, object, object> check = (input, expected, actual) => {
    string message = string.Format("[Checkpoint Verification] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual);
    Debug.Log(message);
    bool success = expected is float && actual is float
        ? Mathf.Abs((float)expected - (float)actual) < 0.035f
        : object.Equals(expected, actual);
    UnityEngine.Assertions.Assert.IsTrue(success, message);

    if (!success) {
        throw new System.InvalidOperationException(message);
    }

    passed++;
};

// 씬이 열릴 때마다 이전 오브젝트를 재사용하지 않고 새 플레이어와 적을 조회한다.
System.Func<Mech.Function.PlayerBase> player = () => UnityEngine.Object.FindFirstObjectByType<Mech.Function.PlayerBase>();
System.Func<Mech.Function.TestEnemy> enemy = () => GameObject.Find("Test Enemy A").GetComponent<Mech.Function.TestEnemy>();

// 실제 플레이어 충돌체를 체크포인트에 옮겨 물리 진입 이벤트를 발생시킨다.
System.Action<float> move = x => {
    var current = player();
    var position = new Vector2(x, 0.4f);
    var body = current.GetComponent<Rigidbody2D>();
    current.transform.position = position;
    body.position = position;
    body.linearVelocity = Vector2.zero;
    body.WakeUp();
    Physics2D.SyncTransforms();
};

// 이어하기와 사망 후 다시하기 모두 중간 체크포인트의 같은 상태로 돌아와야 한다.
System.Action<string> verifyMiddle = action => {
    var restored = player();
    var revivedEnemy = enemy();
    check(action + " 체크포인트 X", 0.5f, restored.transform.position.x);
    check(action + " 체크포인트 Y", 0.4f, restored.transform.position.y);
    check(action + " 체력 전부 회복", restored.MaxHealth, restored.CurrentHealth);
    check(action + " 조합에 포함된 A/B/C", "1/1/1", string.Format("{0}/{1}/{2}", restored.ActiveCombination.ACount, restored.ActiveCombination.BCount, restored.ActiveCombination.CCount));
    check(action + " 남은 특수 횟수", 2, restored.SpecialUsesRemaining);
    check(action + " 조합 밖 조각 합계", 0, restored.TypeACount + restored.TypeBCount + restored.TypeCCount);
    check(action + " 일반 적이 살아 있음", false, revivedEnemy.IsDefeated);
    check(action + " 일반 적 체력", revivedEnemy.MaxHealth, revivedEnemy.CurrentHealth);
};

// 비동기 복원 결과를 다음 에디터 갱신에서 검사할 수 있도록 기록한다.
System.Action<bool> finished = ok => {
    loadResults++;
    loadSuccess = ok;
};

// 저장과 씬 전환이 실제 프레임에서 완료되면 다음 검증으로 진행한다.
observe = () => {
    bool done = false;
    try {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - start > 25.0) {
            throw new System.InvalidOperationException("플레이 종료 또는 검증 제한 시간 초과. 단계 " + phase);
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        switch (phase) {
            case 0:
                // 빈 슬롯은 이어하기가 거부되고 체력이 0이 되면 재로드를 시작하지 않는다.
                check("빈 슬롯 확인", false, Mech.Function.CheckpointProgress.HasSave());
                check("빈 슬롯 이어하기", false, Mech.Function.CheckpointProgress.TryContinue());
                var objectWithoutSave = new GameObject("No save death probe");
                objectWithoutSave.transform.position = new Vector3(1000f, 1000f, 0f);
                objectWithoutSave.AddComponent<BoxCollider2D>();
                objectWithoutSave.AddComponent<Rigidbody2D>();
                var withoutSave = objectWithoutSave.AddComponent<Mech.Function.PlayerBase>();
                withoutSave.ApplyDamage(withoutSave.MaxHealth);
                check("저장 전 사망 상태", true, withoutSave.IsDead);
                check("저장 전 사망 시 로드 여부", false, Mech.Function.CheckpointProgress.IsLoading);
                UnityEngine.Object.Destroy(objectWithoutSave);
                player().TryCollectFragment(Mech.Function.CollectibleType.A);
                player().TryCollectFragment(Mech.Function.CollectibleType.B);
                move(-3.5f);
                phase++;
                break;
            case 1:
                if (!System.IO.File.Exists(primary)) {
                    return;
                }

                // 첫 체크포인트의 물리 진입은 미조합 A/B를 단일 파일에 기록한다.
                check("첫 트리거 진입으로 저장 가능", true, Mech.Function.CheckpointProgress.HasSave());
                string first = System.IO.File.ReadAllText(primary);
                check("첫 저장 체크포인트 ID", true, first.Contains("playtest-start"));
                check("첫 저장의 미조합 A/B", true, first.Contains("\"CollectedA\":1,\"CollectedB\":1"));
                player().TryCollectFragment(Mech.Function.CollectibleType.C);
                player().TryUseSpecial();
                move(0.5f);
                phase++;
                break;
            case 2:
                string second = System.IO.File.ReadAllText(primary);

                if (!second.Contains("playtest-middle")) {
                    return;
                }

                // 두 번째 체크포인트가 같은 슬롯을 갱신하며 이전 정상 저장본은 복구용으로 남는다.
                check("둘째 트리거의 특수 잔여 2회 저장", true, second.Contains("\"SpecialUsesRemaining\":2"));
                check("직전 정상 저장본 보존", true, System.IO.File.Exists(backup) && System.IO.File.ReadAllText(backup).Contains("playtest-start"));
                enemy().ApplyDamage(enemy().MaxHealth);
                player().ApplyDamage(25f);
                check("이어하기 전 적 처치", true, enemy().IsDefeated);
                check("이어하기 전 플레이어 피해", 75f, player().CurrentHealth);
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
                phase++;
                break;
            case 3:
                if (currentScene != titlePath) {
                    return;
                }

                // 다른 씬의 이어하기에서 저장 씬을 열고 연속 요청은 한 건만 처리한다.
                check("다른 씬에서 이어하기 요청", true, Mech.Function.CheckpointProgress.TryContinue());
                check("로드 중 중복 이어하기", false, Mech.Function.CheckpointProgress.TryContinue());
                phase++;
                break;
            case 4:
                if (loadResults < 1) {
                    return;
                }

                check("이어하기 로드 성공", true, loadSuccess && currentScene == scenePath);
                verifyMiddle("이어하기");
                // 저장 이후 특수 사용과 처치를 다시 바꾼 뒤 실제 사망 경로를 호출한다.
                player().TryUseSpecial();
                check("사망 전 변경한 특수 횟수", 1, player().SpecialUsesRemaining);
                enemy().ApplyDamage(enemy().MaxHealth);
                var dead = player();
                dead.ApplyDamage(dead.MaxHealth);
                check("사망에서 자동으로 복원 시작", true, dead.IsDead && Mech.Function.CheckpointProgress.IsLoading);
                check("사망 중 특수 사용 거부", false, dead.TryUseSpecial());
                phase++;
                break;
            case 5:
                if (loadResults < 2) {
                    return;
                }

                check("사망 후 다시하기 로드 성공", true, loadSuccess && currentScene == scenePath);
                verifyMiddle("사망 후 다시하기");
                // 주 저장이 깨지면 처음 체크포인트의 정상 백업으로 이어갈 수 있어야 한다.
                System.IO.File.WriteAllText(primary, "{unclosed");
                check("손상된 주 저장의 백업 이어하기", true, Mech.Function.CheckpointProgress.TryContinue());
                phase++;
                break;
            case 6:
                if (loadResults < 3) {
                    return;
                }

                var recovered = player();
                check("백업 복원 성공", true, loadSuccess && currentScene == scenePath);
                check("백업 체크포인트 X", -3.5f, recovered.transform.position.x);
                check("백업 미조합 A/B/C 복원", "1/1/0", string.Format("{0}/{1}/{2}", recovered.TypeACount, recovered.TypeBCount, recovered.TypeCCount));
                check("백업 특수 사용 횟수 복원", 0, recovered.SpecialUsesRemaining);
                check("백업 체력 전부 회복", recovered.MaxHealth, recovered.CurrentHealth);
                done = true;
                break;
        }
    } catch (System.Exception error) {
        Debug.LogException(error);
        done = true;
    } finally {
        if (done) {
            EditorApplication.update -= observe;
            Mech.Function.CheckpointProgress.LoadFinished -= finished;
            Debug.Log("[Checkpoint Verification] 종료 | 통과 " + passed);
        }
    }
};
Mech.Function.CheckpointProgress.LoadFinished += finished;
EditorApplication.update += observe;
return new { directory, unityVersion = Application.unityVersion };
