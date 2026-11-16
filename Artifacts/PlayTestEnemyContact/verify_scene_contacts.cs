// PlayTestScene 플레이 모드에서 배치된 적에 실제로 닿았을 때의 피해를 검증한다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

if (!EditorApplication.isPlaying || EditorApplication.isPaused || scene.name != "PlayTestScene") {
    throw new System.InvalidOperationException("실행 중인 PlayTestScene이 필요합니다.");
}

var player = UnityEngine.Object.FindAnyObjectByType<Mech.Function.PlayerBase>();
var body = player.GetComponent<Rigidbody2D>();
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var progress = typeof(Mech.Function.CheckpointProgress);
var storeType = progress.Assembly.GetType("Mech.Function.CheckpointSaveStore", true);
var directory = System.IO.Path.Combine("/private/tmp", "merger-scene-contact-" + System.Guid.NewGuid().ToString("N"));
var store = System.Activator.CreateInstance(storeType, flags, null, new object[] { directory }, null);
progress.GetField("saveStore", flags).SetValue(null, store);
int phase = 0;
int passed = 0;
float phaseStart = Time.time;
double deadline = EditorApplication.timeSinceStartup + 15.0;
EditorApplication.CallbackFunction observe = null;

// 기대 체력과 실제 체력을 출력하고 값이 다르면 중단한다.
System.Action<string, float, float> check = (input, expected, actual) => {
    string message = string.Format("[Scene Enemy Contact] 입력 {0} | 기대 {1} | 실제 {2}", input, expected, actual);
    Debug.Log(message);

    if (!Mathf.Approximately(expected, actual)) {
        throw new System.InvalidOperationException(message);
    }

    passed++;
};

// 씬의 실제 적 옆으로 이동해 콜라이더 접촉 상태를 만든다.
System.Action<float> move = x => {
    var position = new Vector2(x, 0.4f);
    player.transform.position = position;
    body.position = position;
    body.linearVelocity = Vector2.zero;
    body.WakeUp();
    Physics2D.SyncTransforms();
    phaseStart = Time.time;
};
System.Action<string> touch = enemyName => {
    var enemy = GameObject.Find(enemyName).GetComponent<Mech.Function.TestEnemy>();
    move(enemy.transform.position.x - 0.65f);
};

// 실제 게임 시간이 지나면서 최초 피격, 재피격, 접촉 해제와 나머지 적을 확인한다.
observe = () => {
    bool done = false;

    try {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline) {
            throw new System.InvalidOperationException("검증 시간 초과 또는 플레이 종료. 단계 " + phase);
        }

        switch (phase) {
            case 0:
                check("시작 체력", 100f, player.CurrentHealth);
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                touch("Test Enemy A");
                phase++;
                break;
            case 1:
                if (Time.time - phaseStart < 0.2f) {
                    return;
                }

                check("A에 닿은 뒤 첫 피해 및 재피격 대기", 90f, player.CurrentHealth);
                phase++;
                break;
            case 2:
                if (Time.time - phaseStart < 0.9f) {
                    return;
                }

                check("A에 계속 닿아 있으면 0.75초 뒤 피해", 80f, player.CurrentHealth);
                move(-5f);
                phase++;
                break;
            case 3:
                if (Time.time - phaseStart < 0.85f) {
                    return;
                }

                check("접촉이 끝나면 추가 피해 없음", 80f, player.CurrentHealth);
                touch("Test Enemy B");
                phase++;
                break;
            case 4:
                if (Time.time - phaseStart < 0.2f) {
                    return;
                }

                check("B에 닿으면 피해", 70f, player.CurrentHealth);
                move(-5f);
                phase++;
                break;
            case 5:
                if (Time.time - phaseStart < 0.85f) {
                    return;
                }

                touch("Test Enemy C");
                phase++;
                break;
            case 6:
                if (Time.time - phaseStart < 0.2f) {
                    return;
                }

                check("C에 닿으면 피해", 60f, player.CurrentHealth);
                EditorApplication.isPaused = true;
                done = true;
                break;
        }
    } catch (System.Exception error) {
        Debug.LogException(error);
        done = true;
    } finally {
        if (done) {
            EditorApplication.update -= observe;
            Debug.Log("[Scene Enemy Contact] 종료 | 통과 " + passed);
        }
    }
};
EditorApplication.update += observe;
return new { directory, unityVersion = Application.unityVersion };
