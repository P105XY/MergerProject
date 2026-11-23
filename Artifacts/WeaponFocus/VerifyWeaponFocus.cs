using System;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class VerifyWeaponFocus
{
    private const string Running = "VerifyWeaponFocus.Running";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static PlayerBase player;
    private static Keyboard keyboard;
    private static int passed;

    // Unity 도메인 재로드 후 검증을 다시 연결한다.
    static VerifyWeaponFocus()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 현재 플레이 테스트 씬을 격리된 저장 위치로 불러온다.
    public static void Run()
    {
        PlayerSettings.productName = "Merger Weapon Focus Verification";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/PlayTestScene.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 실제 입력과 2D 판정 및 저장 코드로 무기 집중의 전 과정을 검사한다.
    private static void Verify(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        try {
            var sceneHud = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
            player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            Check("플레이 테스트 씬에서 HUD 연결", true, sceneHud != null && player != null);
            Physics2D.simulationMode = SimulationMode2D.Script;
            float normalStep = Time.fixedDeltaTime;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            keyboard = InputSystem.AddDevice<Keyboard>();

            // 입력은 빈 게이지에서 D이며 집중 진입 거부를 기대한다.
            Frame(Key.D);
            Check("빈 게이지에서 집중", false, player.IsFocusingWeapon);
            Frame();

            // 입력은 피해를 준 실제 근접 공격과 후속 명중이고 세 번째에 한 칸을 기대한다.
            Vector3 origin = new Vector3(-10f, 3f, 0f);
            player.RestoreCheckpointState(EmptyState(), origin);
            var struck = new GameObject("Focus gauge target");
            struck.transform.position = origin + Vector3.right * 1.1f;
            struck.AddComponent<BoxCollider2D>().size = Vector2.one * 0.4f;
            var enemy = struck.AddComponent<TestEnemy>();
            Physics2D.SyncTransforms();
            Frame(Key.S);
            Check("S 실제 근접 피해", 90f, enemy.CurrentHealth);
            Check("S 첫 명중 누적", 1, player.HitsTowardFragment);
            enemy.ApplyPlayerDamage(0f, player);
            Check("피해 없는 공격 제외", 1, player.HitsTowardFragment);
            enemy.ApplyPlayerDamage(10f, player);
            Check("두 번째 명중 뒤 게이지", 0, player.WeaponGauge);
            enemy.ApplyPlayerDamage(10f, player);
            Check("세 번째 명중 뒤 게이지", 1, player.WeaponGauge);
            Check("세 번째 명중 뒤 누적 초기화", 0, player.HitsTowardFragment);
            Frame();

            // 입력은 비행 적의 실제 첫 발사와 그 적의 사망이며 파편이 한 번씩 나온다.
            var flyerObject = new GameObject("Focus drop flyer");
            flyerObject.transform.position = origin + new Vector3(2f, 0f, 0f);
            var flyer = flyerObject.AddComponent<FlyingEnemy>();
            int drops = 0;
            FragmentPickup lastDrop = null;
            flyer.FragmentDropped += (_, _, fragment) => {
                drops++;
                lastDrop = fragment.GetComponent<FragmentPickup>();
            };
            typeof(FlyingEnemy).GetMethod("FixedUpdate", Private).Invoke(flyer, null);
            Check("비행 적 공격 파편 한 개", 1, drops);
            Check("공격 파편 한 개분 수집", true, lastDrop.TryCollect(player));
            Check("공격 파편 재수집 방지", false, lastDrop.TryCollect(player));
            flyer.ApplyDamage(100f);
            flyer.ApplyDamage(100f);
            Check("사망 파편도 한 개", 2, drops);
            Check("사망 파편 수집", true, lastDrop.TryCollect(player));
            Check("충전 상한", 3, player.WeaponGauge);
            Check("가득 찬 게이지에서 수집 불가", false, player.TryCollectFragment(CollectibleType.A));
            Check("충전만으로 무기를 획득하지 않음", false, player.IsSpecialAvailable);

            // 입력은 D와 왼쪽 방향이며 이동 입력 차단과 게임·물리 시간 15%를 기대한다.
            Frame(Key.D, Key.LeftArrow);
            Check("D 집중 시작", true, player.IsFocusingWeapon);
            CheckNear("집중 게임 속도", 0.15f, Time.timeScale);
            CheckNear("집중 고정 갱신 간격", normalStep * 0.15f, Time.fixedDeltaTime);
            CheckNear("집중 왼쪽 입력은 이동에 쓰이지 않음", 0f,
                (float)typeof(PlayerBase).GetField("movementInput", Private).GetValue(player));
            Check("왼쪽 방향 첫 입력", CollectibleType.A, player.CombinationInputs[0]);
            Frame(Key.D, Key.LeftArrow);
            Check("누른 채 유지하면 방향 한 번", 1, player.CombinationInputs.Count);
            Frame();
            Check("미완성 해제 후 입력 폐기", 0, player.CombinationInputs.Count);
            Check("미완성 해제 후 게이지 보존", 3, player.WeaponGauge);
            CheckNear("미완성 해제 후 속도 복원", 1f, Time.timeScale);

            // 입력은 ← → ↓ →이며 최근 세 입력 → ↓ →로 BCC를 확정한다.
            Frame(Key.D, Key.LeftArrow);
            Check("미완성 해제 후 즉시 재진입", true, player.IsFocusingWeapon);
            Frame(Key.D, Key.RightArrow);
            Frame(Key.D, Key.DownArrow);
            Frame(Key.D, Key.RightArrow);
            Check("네 번째 입력은 맨 앞을 교체", CollectibleType.C, player.CombinationInputs[0]);
            Check("교체 후 가운데 방향", CollectibleType.B, player.CombinationInputs[1]);
            Check("교체 후 마지막 방향", CollectibleType.C, player.CombinationInputs[2]);
            Check("버튼을 누르는 동안 게이지 보존", 3, player.WeaponGauge);
            Frame();
            Check("BCC 확정 무기 이름", "Buckshot", player.GetActiveSpecialName());
            Check("완성 해제 후 2회 부여", 2, player.SpecialUsesRemaining);
            Check("완성 해제 시 3칸 차감", 0, player.WeaponGauge);
            CheckNear("확정 후 고정 갱신 간격 복원", normalStep, Time.fixedDeltaTime);
            Check("무기 보유 중 파편 충전", true, player.TryCollectFragment(CollectibleType.B));
            Frame(Key.D);
            Check("다음 D 입력으로 첫 시전", 1, player.SpecialUsesRemaining);
            Frame();
            Frame(Key.D);
            Check("두 번째 시전 후 무기 소진", 0, player.SpecialUsesRemaining);
            Frame();
            player.TryCollectFragment(CollectibleType.A);
            player.TryCollectFragment(CollectibleType.C);
            Frame(Key.D);
            Check("확정 쿨타임 3초 중 재진입 금지", false, player.IsFocusingWeapon);
            Frame();

            // 입력은 최근 완성 후 만료 시각을 경과한 상태이며 무기 미지급, 시간 복원 및 쿨타임을 기대한다.
            SetFocusTime("readyAt", Time.unscaledTimeAsDouble - 1d);
            Frame(Key.D, Key.LeftArrow);
            Frame(Key.D, Key.DownArrow);
            Frame(Key.D, Key.RightArrow);
            CheckNear("집중 제한은 실제 5초", 5f, (float)(GetFocusTime("endsAt") - Time.unscaledTimeAsDouble));
            SetFocusTime("endsAt", Time.unscaledTimeAsDouble - 1d);
            Frame(Key.D);
            Check("만료 시 무기 미지급", false, player.IsSpecialAvailable);
            Check("만료 시 게이지 유지", 3, player.WeaponGauge);
            Check("만료 시 방향 폐기", 0, player.CombinationInputs.Count);
            CheckNear("만료 후 속도 복원", 1f, Time.timeScale);
            CheckNear("만료 후 실제 3초 쿨타임", 3f, (float)(GetFocusTime("readyAt") - Time.unscaledTimeAsDouble));
            Frame();
            Frame(Key.D);
            Check("만료 직후 재진입 거부", false, player.IsFocusingWeapon);
            Frame();

            // 입력은 새 형식의 JSON 왕복과 이미 존재하는 이전 조각 저장이다.
            player.RestoreCheckpointState(new PlayerCheckpointState {
                WeaponSystemVersion = PlayerCheckpointState.CurrentWeaponSystemVersion,
                ActiveA = 1, ActiveB = 1, ActiveC = 1, WeaponGauge = 2, HitsTowardFragment = 1, SpecialUsesRemaining = 1
            }, origin);
            var saved = JsonUtility.FromJson<PlayerCheckpointState>(JsonUtility.ToJson(player.CaptureCheckpointState()));
            Check("새 저장 유효", true, PlayerBase.IsValidCheckpointState(saved));
            player.RestoreCheckpointState(saved, origin);
            Check("새 저장 게이지 복원", 2, player.WeaponGauge);
            Check("새 저장 누적 명중 복원", 1, player.HitsTowardFragment);
            Check("새 저장 무기 복원", "Piercing Spear", player.GetActiveSpecialName());
            Check("새 저장 사용 횟수 복원", 1, player.SpecialUsesRemaining);
            saved.WeaponGauge = 4;
            Check("초과 게이지 저장 거부", false, PlayerBase.IsValidCheckpointState(saved));
            var old = JsonUtility.FromJson<PlayerCheckpointState>("{\"CollectedA\":1,\"CollectedB\":1}");
            Check("기존 미완성 조각 저장 유효", true, PlayerBase.IsValidCheckpointState(old));
            player.RestoreCheckpointState(old, origin);
            Check("기존 미완성 조각을 게이지로 전환", 2, player.WeaponGauge);
            old = JsonUtility.FromJson<PlayerCheckpointState>("{\"ActiveA\":1,\"ActiveB\":1,\"ActiveC\":1,\"SpecialUsesRemaining\":3}");
            Check("기존 활성 조합 저장 유효", true, PlayerBase.IsValidCheckpointState(old));
            player.RestoreCheckpointState(old, origin);
            Check("기존 조합을 현행 관통 투창으로 복원", "Piercing Spear", player.GetActiveSpecialName());
            Check("기존 횟수는 최대 2회 복원", 2, player.SpecialUsesRemaining);

            // 입력은 게이지가 찬 플레이어의 집중 도중 비활성화이며 원래 속도를 기대한다.
            var full = EmptyState();
            full.WeaponGauge = 3;
            player.RestoreCheckpointState(full, origin);
            Frame(Key.D);
            Check("정리 검증용 집중 진입", true, player.IsFocusingWeapon);
            player.enabled = false;
            CheckNear("비활성화 후 게임 속도 복구", 1f, Time.timeScale);
            CheckNear("비활성화 후 물리 간격 복구", normalStep, Time.fixedDeltaTime);
            InputSystem.RemoveDevice(keyboard);
            Debug.Log($"[WeaponFocus] PASS {passed}/{passed}");
            SessionState.EraseBool(Running);
            EditorApplication.Exit(0);
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    // 빈 무기 진행 상태를 현재 세이브 버전으로 만든다.
    private static PlayerCheckpointState EmptyState()
    {
        return new PlayerCheckpointState { WeaponSystemVersion = PlayerCheckpointState.CurrentWeaponSystemVersion };
    }

    // 가상 키보드 이벤트와 실제 플레이어 입력 갱신을 한 프레임 실행한다.
    private static void Frame(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
        keyboard.MakeCurrent();
        typeof(PlayerBase).GetMethod("Update", Private).Invoke(player, null);
    }

    // 검증에 필요한 실제 시간 예약값의 리플렉션 필드를 반환한다.
    private static FieldInfo FocusTimeField(string name, out object focus)
    {
        focus = typeof(PlayerBase).GetField("weaponFocus", Private).GetValue(player);
        return focus.GetType().GetField(name, Private);
    }

    // 해당 집중 또는 쿨타임 예약 시각을 읽는다.
    private static double GetFocusTime(string name)
    {
        return (double)FocusTimeField(name, out object focus).GetValue(focus);
    }

    // 실제 시간을 기다리는 대신 만료 경계를 넘기도록 예약 시각을 조정한다.
    private static void SetFocusTime(string name, double value)
    {
        FocusTimeField(name, out object focus).SetValue(focus, value);
    }

    // 입력과 기대 실수 및 엔진 관측값을 출력하고 허용 오차 내인지 검사한다.
    private static void CheckNear(string input, float expected, float actual)
    {
        Check($"{input} 기대 {expected:F5} 실제 {actual:F5}", true, Mathf.Abs(expected - actual) < 0.001f);
    }

    // 입력과 기대값 및 실제값을 출력하고 다르면 실행을 중단한다.
    private static void Check<T>(string input, T expected, T actual)
    {
        string line = $"[WeaponFocus] 입력 {input} | 기대 [{expected}] | 실제 [{actual}]";
        Debug.Log(line);

        if (!Equals(expected, actual)) {
            throw new InvalidOperationException(line);
        }

        passed++;
    }
}
