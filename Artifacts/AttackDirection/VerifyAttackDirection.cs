using System;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class VerifyAttackDirection
{
    private const string Running = "VerifyAttackDirection.Running";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Vector3 GroundStart = new Vector3(-24f, 0.4f, 0f);
    private static readonly Vector3 AirStart = new Vector3(-24f, 8f, 0f);
    private static PlayerBase player;
    private static Keyboard keyboard;
    private static SpriteRenderer indicator;
    private static int passed;

    // 도메인 재로드 후에도 검증이 플레이 모드 진입에 연결되도록 한다.
    static VerifyAttackDirection()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 기존 플레이 테스트 씬에서 별도 저장 위치를 사용해 검증을 시작한다.
    public static void Run()
    {
        PlayerSettings.productName = "Merger Attack Direction Verification";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/PlayTestScene.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 실제 점프와 가상 키 입력, 물리 판정으로 기본 공격과 무기 방향을 확인한다.
    private static void Verify(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        try {
            player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var hud = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
            indicator = (SpriteRenderer)typeof(PlayTestDisplay).GetField("attackIndicator", Private).GetValue(hud);
            Physics2D.simulationMode = SimulationMode2D.Script;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            keyboard = InputSystem.AddDevice<Keyboard>();

            // 입력은 실제 바닥에서 가상 A를 누른 후 ↓와 S이며 아래쪽의 회전된 범위에만 명중한다.
            PositionPlayer(GroundStart);
            var floorObject = new GameObject("Direction verification floor");
            floorObject.transform.position = GroundStart + new Vector3(0f, -1.15f, 0f);
            floorObject.AddComponent<BoxCollider2D>().size = new Vector2(12f, 0.5f);
            Physics2D.SyncTransforms();
            Frame(Key.A);
            var body = player.GetComponent<Rigidbody2D>();
            float groundY = body.position.y;

            for (int step = 0; step < 5; step++) {
                typeof(PlayerBase).GetMethod("FixedUpdate", Private).Invoke(player, null);
                Physics2D.Simulate(Time.fixedDeltaTime);
                Physics2D.SyncTransforms();
            }

            Check("입력 A로 실제 점프한 상태", true, player.IsJumping && body.position.y > groundY + 2f);
            Vector3 jumpPosition = player.transform.position;
            EnemyBase below = PlaceEnemy(jumpPosition + new Vector3(0.1f, -1.66f, 0f));
            EnemyBase beside = PlaceEnemy(jumpPosition + new Vector3(1.1f, 0f, 0f));
            Strike(Key.A, Key.DownArrow, Key.RightArrow);
            Check("점프 ↓ 아래쪽 사각 판정 피해", 90f, below.CurrentHealth);
            Check("점프 ↓ 옆 표적은 피해 없음", 100f, beside.CurrentHealth);
            CheckNear("점프 ↓ 사각 판정 중심 X", jumpPosition.x, player.LastAttackCenter.x);
            CheckNear("점프 ↓ 사각 판정 중심 Y", jumpPosition.y - 1.1f, player.LastAttackCenter.y);
            CheckNear("점프 ↓ 그림의 회전 방향", -1f, indicator.transform.right.y);
            CheckNear("점프 ↓ 그림의 세계 좌표 중심 Y", player.LastAttackCenter.y, indicator.transform.position.y);
            CheckNear("회전해도 그림 폭 실제 판정과 일치", player.AttackSize.x, indicator.transform.lossyScale.x);
            CheckNear("회전해도 그림 높이 실제 판정과 일치", player.AttackSize.y, indicator.transform.lossyScale.y);

            // 입력은 ↑, ←, →와 무입력으로, 상하 우선 뒤 마지막 좌우 방향을 기대한다.
            EnemyBase above = PlaceEnemy(jumpPosition + Vector3.up * 1.1f);
            Strike(Key.UpArrow);
            Check("↑ 위쪽 근접 피해", 90f, above.CurrentHealth);
            CheckNear("↑ 공격 표시 회전", 1f, indicator.transform.right.y);
            EnemyBase left = PlaceEnemy(jumpPosition + Vector3.left * 1.1f);
            Strike(Key.LeftArrow);
            Check("← 왼쪽 근접 피해", 90f, left.CurrentHealth);
            Strike();
            Check("방향을 놓으면 마지막 좌우 공격", 80f, left.CurrentHealth);
            Strike(Key.RightArrow, Key.UpArrow, Key.DownArrow);
            Check("상하 동시 입력 시 → 공격", 90f, beside.CurrentHealth);

            // 입력은 ↓ 조준 관통 투창과 이후 ↑ 입력이며 처음 발사 방향을 계속 유지한다.
            PositionPlayer(AirStart, a: 1, b: 1, c: 1);
            below = PlaceEnemy(AirStart + Vector3.down * 3f);
            EnemyBase behindBelow = PlaceEnemy(AirStart + Vector3.down * 5f);
            beside = PlaceEnemy(AirStart + Vector3.right * 3f);
            Frame(Key.DownArrow, Key.D);
            Check("↓ 관통 투창 시전 한 번 차감", 1, player.SpecialUsesRemaining);
            MonoBehaviour[] projectiles = Projectiles();
            Check("↓ 관통 투창 한 발", 1, projectiles.Length);
            MonoBehaviour spear = projectiles[0];
            CheckNear("↓ 관통 투창 발사 벡터", -1f, ProjectileDirection(spear).y);
            Frame(Key.UpArrow);
            CheckNear("입력이 바뀐 뒤에도 기존 창은 ↓", -1f, ProjectileDirection(spear).y);

            for (int step = 0; step < 10; step++) {
                spear.GetType().GetMethod("FixedUpdate", Private).Invoke(spear, null);
            }

            Check("↓ 투창 첫 표적 피해", 75f, below.CurrentHealth);
            Check("↓ 투창 뒤의 표적 관통 피해", 75f, behindBelow.CurrentHealth);
            Check("↓ 투창 옆의 표적 피해 없음", 100f, beside.CurrentHealth);

            // 입력은 ↑ 산탄이며 위쪽 중심으로 5도씩 벌어진 세 발을 기대한다.
            UnityEngine.Object.DestroyImmediate(spear.gameObject);
            PositionPlayer(AirStart, b: 1, c: 2);
            Frame(Key.UpArrow, Key.D);
            projectiles = Projectiles();
            Check("↑ 산탄 세 발 생성", 3, projectiles.Length);
            float[] angles = projectiles.Select(ProjectileDirection)
                .Select(direction => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg).OrderBy(angle => angle).ToArray();
            CheckNear("↑ 산탄 첫 방향", 85f, angles[0]);
            CheckNear("↑ 산탄 조준 중심", 90f, angles[1]);
            CheckNear("↑ 산탄 세 번째 방향", 95f, angles[2]);
            Frame();

            // 입력은 ↓ 칼날과 ↑ 부채꼴이며 실제 피해와 메시 회전이 일치해야 한다.
            Vector3 bladeStart = AirStart + Vector3.left * 8f;
            PositionPlayer(bladeStart, a: 2, b: 1);
            below = PlaceEnemy(bladeStart + Vector3.down * 3.65f);
            Frame(Key.DownArrow, Key.D);
            Check("↓ 늘어나는 칼날 끝부분 피해", 55f, below.CurrentHealth);
            CheckNear("↓ 칼날 메시 회전", -1f, GameObject.Find("Special extending blade").transform.right.y);
            Frame();

            Vector3 arcStart = AirStart + Vector3.right * 8f;
            PositionPlayer(arcStart, a: 2, c: 1);
            above = PlaceEnemy(arcStart + Vector3.up * 2f);
            EnemyBase sectorSide = PlaceEnemy(arcStart + new Vector3(-2f, 0f, 0f));
            Frame(Key.UpArrow, Key.D);
            Check("↑ 넓은 베기 위쪽 피해", 70f, above.CurrentHealth);
            Check("↑ 넓은 베기 부채꼴 밖 표적", 100f, sectorSide.CurrentHealth);
            CheckNear("↑ 넓은 베기 메시 회전", 1f, GameObject.Find("Special attack area").transform.right.y);
            Frame();

            // 입력은 D 집중과 ↓ 및 S이며 ↓는 조합에만 쓰이고 근접 공격은 마지막 수평 방향이다.
            player.RestoreCheckpointState(new PlayerCheckpointState {
                WeaponSystemVersion = PlayerCheckpointState.CurrentWeaponSystemVersion, WeaponGauge = 3
            }, AirStart);
            Strike(Key.D, Key.DownArrow);
            Check("집중 중 ↓가 조합 입력으로 사용됨", CollectibleType.B, player.CombinationInputs[0]);
            CheckNear("집중 중 ↓ 입력은 상하 조준으로 사용하지 않음", 0f, player.LastAttackAngle);
            Frame();

            InputSystem.RemoveDevice(keyboard);
            Debug.Log($"[AttackDirection] PASS {passed}/{passed}");
            SessionState.EraseBool(Running);
            EditorApplication.Exit(0);
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    // 공격 대상의 지정 위치에 실제 2D 충돌체를 둔다.
    private static EnemyBase PlaceEnemy(Vector3 position)
    {
        var target = new GameObject("Direction verification target");
        target.transform.position = position;
        target.AddComponent<BoxCollider2D>().size = Vector2.one * 0.06f;
        return target.AddComponent<TestEnemy>();
    }

    // 검증 위치와 이번에 사용할 무기 조합을 설정한다.
    private static void PositionPlayer(Vector3 position, int a = 0, int b = 0, int c = 0)
    {
        player.RestoreCheckpointState(new PlayerCheckpointState {
            WeaponSystemVersion = PlayerCheckpointState.CurrentWeaponSystemVersion,
            ActiveA = a, ActiveB = b, ActiveC = c, SpecialUsesRemaining = a + b + c == 0 ? 0 : 2
        }, position);
        Physics2D.SyncTransforms();
    }

    // 공격 대기시간을 건너뛰고 지정 방향으로 가상 S 입력을 실행한다.
    private static void Strike(params Key[] held)
    {
        typeof(PlayerBase).GetField("nextAttackTime", Private).SetValue(player, float.NegativeInfinity);
        Frame(held.Append(Key.S).ToArray());
    }

    // 가상 키 상태를 보내고 실제 입력을 처리하는 플레이어 프레임을 실행한다.
    private static void Frame(params Key[] held)
    {
        Physics2D.SyncTransforms();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
        InputSystem.Update();
        keyboard.MakeCurrent();
        typeof(PlayerBase).GetMethod("Update", Private).Invoke(player, null);
    }

    // 씬에서 실제로 발사된 특수 투사체를 조회한다.
    private static MonoBehaviour[] Projectiles()
    {
        return UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .Where(item => item.GetType().Name == "SpecialProjectile").ToArray();
    }

    // 발사된 투사체의 이동 벡터를 읽는다.
    private static Vector2 ProjectileDirection(MonoBehaviour shot)
    {
        return (Vector2)shot.GetType().GetField("direction", Private).GetValue(shot);
    }

    // 실수 입력과 기대값 및 실제값을 기록하고 허용 오차를 검사한다.
    private static void CheckNear(string input, float expected, float actual)
    {
        Check($"{input} 기대 {expected:F4} 실제 {actual:F4}", true, Mathf.Abs(expected - actual) < 0.001f);
    }

    // 입력이나 조작과 기대값 및 실제값을 기록하고 다르면 중단한다.
    private static void Check<T>(string input, T expected, T actual)
    {
        string line = $"[AttackDirection] 입력 {input} | 기대 [{expected}] | 실제 [{actual}]";
        Debug.Log(line);

        if (!Equals(expected, actual)) {
            throw new InvalidOperationException(line);
        }

        passed++;
    }
}
