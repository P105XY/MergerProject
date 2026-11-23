using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class FragmentSpecialModifierProbe : SpecialModifierDefinition
{
    public int ProjectileCount = 6;

    // 검증용 장비로 이번 시전의 투사체 개수를 바꾼다.
    public override void Apply(ISpecialEffect effect)
    {
        if (effect is FragmentSpecialEffect fragment) {
            fragment.ProjectileCount = ProjectileCount;
        }
    }
}

[InitializeOnLoad]
public static class VerifyFragmentSpecials
{
    private const string Running = "VerifyFragmentSpecials.Running";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly List<GameObject> objects = new();
    private static PlayerBase player;
    private static int checks;

    // 도메인 재로드 후에도 검증을 플레이 모드에 연결한다.
    static VerifyFragmentSpecials()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 복사본의 빈 장면에서 실제 Unity 물리 검증을 시작한다.
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 입력과 기대값 및 실제값을 출력하고 결과를 확인한다.
    private static void Check(string input, double expected, double actual, double tolerance = 0.001d)
    {
        string message = $"[FragmentSpecials] 입력 {input} | 기대 {expected:F3} ± {tolerance:F3} | 실제 {actual:F3}";
        Debug.Log(message);

        if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance) {
            throw new InvalidOperationException(message);
        }

        checks++;
    }

    // 이전 시전과 표적을 정리하고 저장된 무기를 장착한 플레이어를 준비한다.
    private static void Prepare(string combination)
    {
        foreach (MonoBehaviour projectile in Projectiles()) {
            UnityEngine.Object.DestroyImmediate(projectile.gameObject);
        }

        foreach (MonoBehaviour item in UnityEngine.Object.FindObjectsByType<MonoBehaviour>()) {
            if (item.GetType().Name == "SpecialEffectVisual") {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }

        foreach (GameObject instance in objects) {
            if (instance != null) {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        foreach (FragmentPickup drop in UnityEngine.Object.FindObjectsByType<FragmentPickup>()) {
            UnityEngine.Object.DestroyImmediate(drop.gameObject);
        }

        objects.Clear();
        var hero = new GameObject("Special verification player");
        objects.Add(hero);
        var body = hero.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        hero.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 1f);
        player = hero.AddComponent<PlayerBase>();
        body.gravityScale = 0f;

        player.RestoreCheckpointState(new PlayerCheckpointState {
            WeaponSystemVersion = PlayerCheckpointState.CurrentWeaponSystemVersion,
            SpecialUsesRemaining = 2,
            ActiveA = combination.Count(direction => direction == 'A'),
            ActiveB = combination.Count(direction => direction == 'B'),
            ActiveC = combination.Count(direction => direction == 'C')
        }, Vector3.zero);
        Check(combination + " 무기 복원", 2, player.SpecialUsesRemaining);
    }

    // 지정한 위치에 기본 체력 100의 적을 배치한다.
    private static EnemyBase Enemy(float x, float y = 0f)
    {
        var instance = new GameObject("Special verification enemy");
        objects.Add(instance);
        instance.transform.position = new Vector3(x, y, 0f);
        instance.AddComponent<CircleCollider2D>().radius = 0.25f;
        return instance.AddComponent<TestEnemy>();
    }

    // 전방 칼날과 범위 공격의 사선 차단을 검사할 벽 충돌체를 배치한다.
    private static BoxCollider2D Wall(float x)
    {
        var instance = new GameObject("Special verification wall");
        objects.Add(instance);
        instance.transform.position = new Vector3(x, 0f, 0f);
        var collider = instance.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.2f, 10f);
        return collider;
    }

    // 실제 특수 사용 진입점을 호출하고 성공 시 한 번만 차감되는지 확인한다.
    private static void Cast(string label)
    {
        Physics2D.SyncTransforms();
        Check(label + " 시전", 1, player.TryUseSpecial() ? 1 : 0);
        Check(label + " 사용 횟수", 1, player.SpecialUsesRemaining);
    }

    // 현재 만들어진 특수 투사체를 조회한다.
    private static MonoBehaviour[] Projectiles()
    {
        return UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .Where(item => item.GetType().Name == "SpecialProjectile").ToArray();
    }

    // 실제 플레이어와 투사체의 고정 갱신을 반복해 이동 경로의 충돌을 처리한다.
    private static void Tick(int count = 100)
    {
        for (int index = 0; index < count; index++) {
            Physics2D.SyncTransforms();
            typeof(PlayerBase).GetMethod("FixedUpdate", Private).Invoke(player, null);

            foreach (MonoBehaviour projectile in Projectiles()) {
                projectile.GetType().GetMethod("FixedUpdate", Private).Invoke(projectile, null);
            }

            Physics2D.Simulate(Time.fixedDeltaTime);
        }
    }

    // 열 가지 조합과 충돌 차단, 장비 보정 및 사용 횟수를 검증한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        try {
            Physics2D.simulationMode = SimulationMode2D.Script;
            // 입력: AAA, 앞의 두 적 → 기대: 가장 가까운 적 한 개에 60 피해.
            Prepare("AAA");
            EnemyBase near = Enemy(1f);
            EnemyBase next = Enemy(1.6f);
            near.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.4f;
            Cast("AAA 강타");
            Check("AAA 가까운 단일 적", 40, near.CurrentHealth);
            Check("AAA 두 번째 적", 100, next.CurrentHealth);

            // 입력: AAB, 손잡이에 가까운 적과 칼끝 위치의 적 → 기대: 첫 적에만 30 피해.
            Prepare("AAB");
            EnemyBase tipTarget = Enemy(3.9f);
            EnemyBase normalTarget = Enemy(3.1f);
            EnemyBase offBlade = Enemy(1.5f, 0.8f);
            Cast("AAB 늘어나는 칼날");
            Check("AAB 충돌 표면 기준 첫 적의 기본 피해", 70, normalTarget.CurrentHealth);
            Check("AAB 첫 적 뒤에는 관통하지 않음", 100, tipTarget.CurrentHealth);
            Check("AAB 칼날 폭 밖의 적", 100, offBlade.CurrentHealth);
            Check("AAB 시전 후 플레이어 위치", 0, player.transform.position.x);

            // 입력: AAB, 왼쪽 칼끝 범위의 적 → 기대: 해당 적에게 45 피해와 칼끝 강조.
            Prepare("AAB");
            typeof(PlayerBase).GetField("facingDirection", Private).SetValue(player, -1);
            tipTarget = Enemy(-3.75f);
            EnemyBase rightOfCaster = Enemy(2f);
            EnemyBase beyondBlade = Enemy(-4.5f);
            Cast("AAB 왼쪽 칼끝 명중");
            Check("AAB 칼끝 추가 피해", 55, tipTarget.CurrentHealth);
            Check("AAB 반대 방향의 적", 100, rightOfCaster.CurrentHealth);
            Check("AAB 최대 길이 밖의 적", 100, beyondBlade.CurrentHealth);
            Check("AAB HUD 이름", 1, player.GetActiveSpecialName() == "Extending Blade" ? 1 : 0);
            GameObject bladeVisual = GameObject.Find("Special extending blade");
            Check("AAB 칼끝 강조 메시 수", 2, bladeVisual.GetComponentsInChildren<MeshFilter>().Length);
            Check("AAB 효과의 실제 명중 길이", 3.5, bladeVisual.transform.localScale.x, tolerance: 0.01d);

            // 입력: AAC, 전방 부채꼴의 두 적과 뒤쪽 적 → 기대: 전방 두 적만 각각 30 피해.
            Prepare("AAC");
            EnemyBase front = Enemy(2f);
            EnemyBase diagonal = Enemy(2f, 1f);
            EnemyBase back = Enemy(-1f);
            EnemyBase above = Enemy(0f, 2f);
            Cast("AAC 넓은 베기");
            Check("AAC 전방 피해", 70, front.CurrentHealth);
            Check("AAC 대각선 피해", 70, diagonal.CurrentHealth);
            Check("AAC 후방 제외", 100, back.CurrentHealth);
            Check("AAC 부채꼴 바깥 제외", 100, above.CurrentHealth);

            // 입력: ABB, 일렬 적 두 개와 뒤의 벽 → 기대: 두 적에 각각 30 피해, 벽 뒤 적 보호.
            Prepare("ABB");
            EnemyBase first = Enemy(3f);
            EnemyBase second = Enemy(6f);
            EnemyBase behindWall = Enemy(10f);
            Wall(8f);
            Cast("ABB 관통 검기");
            Tick();
            Check("ABB 첫 적 피해", 70, first.CurrentHealth);
            Check("ABB 관통한 두 번째 적 피해", 70, second.CurrentHealth);
            Check("ABB 벽 뒤 적", 100, behindWall.CurrentHealth);

            // 입력: ABC, 일렬의 여러 적과 벽 → 기대: 창이 벽 앞의 각 적을 관통하며 파편 한 개씩 생성.
            Prepare("ABC");
            EnemyBase spearFirst = Enemy(3f);
            spearFirst.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.45f;
            EnemyBase spearSecond = Enemy(5f);
            EnemyBase spearBlocked = Enemy(9f);
            Wall(8f);
            Cast("ABC 관통 투창");
            Check("ABC HUD 이름", 1, player.GetActiveSpecialName() == "Piercing Spear" ? 1 : 0);
            Check("ABC 투사체 수", 1, Projectiles().Length);
            Tick();
            Check("ABC 여러 콜라이더의 첫 적 피해", 75, spearFirst.CurrentHealth);
            Check("ABC 후속 적 피해", 75, spearSecond.CurrentHealth);
            Check("ABC 벽 뒤 적 보호", 100, spearBlocked.CurrentHealth);
            Check("ABC 적 한 명마다 파편 한 개", 2, UnityEngine.Object.FindObjectsByType<FragmentPickup>().Length);
            Check("ABC 실제 명중 두 번 누적", 2, player.HitsTowardFragment);

            // 입력: ACC, 가까운 앞뒤 적과 먼 적 → 기대: 가까운 적들만 30 피해.
            Prepare("ACC");
            front = Enemy(1.5f);
            back = Enemy(-1.5f);
            next = Enemy(4f);
            Cast("ACC 회전 베기");
            Check("ACC 앞의 적", 70, front.CurrentHealth);
            Check("ACC 뒤의 적", 70, back.CurrentHealth);
            Check("ACC 범위 바깥 적", 100, next.CurrentHealth);

            // 입력: BBB, 직선상의 먼 적 둘 → 기대: 다섯 발 발사, 중앙 탄의 첫 적만 10 피해.
            Prepare("BBB");
            first = Enemy(8f);
            second = Enemy(12f);
            Cast("BBB 5발 사격");
            Check("BBB 동시 투사체 수", 5, Projectiles().Length);
            Tick();
            Check("BBB 중앙 탄의 첫 적", 90, first.CurrentHealth);
            Check("BBB 중앙 탄의 두 번째 적 제외", 100, second.CurrentHealth);

            // 입력: BBC, 인접한 적과 더 먼 적 → 기대: 작은 폭발 안의 적들만 30 피해.
            Prepare("BBC");
            first = Enemy(3f);
            second = Enemy(4.1f, 0.5f);
            next = Enemy(5.5f);
            Cast("BBC 폭발탄");
            Tick();
            Check("BBC 직접 표적 폭발 피해", 70, first.CurrentHealth);
            Check("BBC 인접한 적 폭발 피해", 70, second.CurrentHealth);
            Check("BBC 작은 폭발 바깥 적", 100, next.CurrentHealth);

            // 입력: BCC, 전방과 양쪽 5도에 적 → 기대: 총 10도에 산탄 세 발과 각각 10 피해.
            Prepare("BCC");
            first = Enemy(9f);
            second = Enemy(9f, 0.8f);
            next = Enemy(9f, -0.8f);
            Cast("BCC 산탄");
            MonoBehaviour[] pellets = Projectiles();
            Check("BCC 동시 산탄 수", 3, pellets.Length);
            var directionField = pellets[0].GetType().GetField("direction", Private);
            float[] angles = pellets.Select(pellet => (Vector2)directionField.GetValue(pellet))
                .Select(direction => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg).OrderBy(angle => angle).ToArray();
            Check("BCC 좌측 탄 각도", -5, angles[0]);
            Check("BCC 중앙 탄 각도", 0, angles[1]);
            Check("BCC 우측 탄 각도", 5, angles[2]);
            Tick();
            Check("BCC 중앙 산탄 피해", 90, first.CurrentHealth);
            Check("BCC 위쪽 산탄 피해", 90, second.CurrentHealth);
            Check("BCC 아래쪽 산탄 피해", 90, next.CurrentHealth);

            // 입력: CCC, 전후와 위쪽의 먼 적 및 범위 밖 적 → 기대: 반경 6 안의 적 모두 25 피해.
            Prepare("CCC");
            front = Enemy(5.5f);
            back = Enemy(-5.5f);
            above = Enemy(0f, 5.5f);
            next = Enemy(7.5f);
            Cast("CCC 광역 파동");
            Check("CCC 전방 피해", 75, front.CurrentHealth);
            Check("CCC 후방 피해", 75, back.CurrentHealth);
            Check("CCC 위쪽 피해", 75, above.CurrentHealth);
            Check("CCC 범위 밖 제외", 100, next.CurrentHealth);

            // 입력: AAB 전방의 벽 → 기대: 칼날만 벽 앞에서 멈추고 뒤의 적은 보호.
            Prepare("AAB");
            Wall(1.2f);
            behindWall = Enemy(3f);
            Cast("벽 앞 늘어나는 칼날");
            Check("AAB 칼날이 벽 앞에서 멈춘 길이", 1.1,
                GameObject.Find("Special extending blade").transform.localScale.x, tolerance: 0.01d);
            Check("AAB 벽 뒤 적 보호", 100, behindWall.CurrentHealth);

            // 입력: AAA와 가까운 벽 뒤 적 → 기대: 근접 타격도 벽을 통과하지 않음.
            Prepare("AAA");
            behindWall = Enemy(1.2f);
            Wall(0.7f);
            Cast("벽에 가린 강타");
            Check("근접 사선 차단", 100, behindWall.CurrentHealth);

            // 입력: BBC 폭발 반경 안이지만 벽으로 가려진 적 → 기대: 폭발 피해 차단.
            Prepare("BBC");
            first = Enemy(3f);
            behindWall = Enemy(4.5f);
            Wall(3.8f);
            Cast("폭발의 벽 차단");
            Tick();
            Check("벽 앞 폭발 표적", 70, first.CurrentHealth);
            Check("벽 뒤 폭발 표적 보호", 100, behindWall.CurrentHealth);

            // 입력: 왼쪽을 바라보고 BBB → 기대: 가운데 탄으로 왼쪽 적만 피해.
            Prepare("BBB");
            typeof(PlayerBase).GetField("facingDirection", Private).SetValue(player, -1);
            back = Enemy(-8f);
            front = Enemy(8f);
            Cast("왼쪽 5발 사격");
            Tick();
            Check("왼쪽 투사체 피해", 90, back.CurrentHealth);
            Check("반대쪽 투사체 표적 제외", 100, front.CurrentHealth);

            // 입력: BBB에 투사체 +1 장비 → 기대: 6발, 장비를 빼면 다음 시전은 다시 5발.
            Prepare("BBB");
            var modifier = ScriptableObject.CreateInstance<FragmentSpecialModifierProbe>();
            var equipment = ScriptableObject.CreateInstance<SpecialEquipment>();
            typeof(SpecialEquipment).GetField("modifiers", Private).SetValue(equipment, new SpecialModifierDefinition[] { modifier });
            player.SetSpecialEquipment(equipment);
            Cast("6발 장비");
            Check("장비 적용 투사체 수", 6, Projectiles().Length);
            player.SetSpecialEquipment();
            Check("장비 해제 후 재시전", 1, player.TryUseSpecial() ? 1 : 0);
            Check("장비 해제 후 추가된 투사체 수", 11, Projectiles().Length);

            // 입력: 투사체 수를 0으로 만드는 장비 → 기대: 시전 실패, 사용 횟수 2 보존.
            Prepare("BBB");
            modifier.ProjectileCount = 0;
            player.SetSpecialEquipment(equipment);
            Check("잘못된 보정의 시전 실패", 0, player.TryUseSpecial() ? 1 : 0);
            Check("실패 시 사용 횟수 보존", 2, player.SpecialUsesRemaining);

            // 입력: BBB 전용 레시피를 지정한 +1발 장비 → 기대: 기본 조합에도 6발 적용.
            Prepare("BBB");
            modifier.ProjectileCount = 6;
            var targetRecipe = ScriptableObject.CreateInstance<SpecialRecipe>();
            typeof(SpecialRecipe).GetField("bCount", Private).SetValue(targetRecipe, 3);
            typeof(SpecialEquipment).GetField("targetRecipe", Private).SetValue(equipment, targetRecipe);
            player.SetSpecialEquipment(equipment);
            Cast("기본 조합의 전용 장비");
            Check("기본 BBB 조합 전용 장비", 6, Projectiles().Length);

            // 입력: AAA 전용 장비로 BBB 시전 → 기대: 기본 5발.
            Prepare("BBB");
            typeof(SpecialRecipe).GetField("aCount", Private).SetValue(targetRecipe, 3);
            typeof(SpecialRecipe).GetField("bCount", Private).SetValue(targetRecipe, 0);
            player.SetSpecialEquipment(equipment);
            Cast("다른 조합 전용 장비");
            Check("다른 조합 전용 장비 제외", 5, Projectiles().Length);

            // 입력: BBB를 강타로 교체하는 장비 → 기대: 즉시 근접 피해 60, 투사체 생성 0.
            Prepare("BBB");
            var replacement = ScriptableObject.CreateInstance<FragmentSpecialDefinition>();
            var replacementItem = ScriptableObject.CreateInstance<SpecialEquipment>();
            typeof(SpecialEquipment).GetField("replacementEffect", Private).SetValue(replacementItem, replacement);
            near = Enemy(1f);
            player.SetSpecialEquipment(replacementItem);
            Cast("장비의 기능 교체");
            Check("교체된 강타 피해", 40, near.CurrentHealth);
            Check("기능 교체 후 투사체 수", 0, Projectiles().Length);

            // 입력: AAA에 회전 베기 레시피를 직접 등록 → 기대: 등록된 레시피가 기본 강타보다 우선.
            Prepare("AAA");
            var customEffect = ScriptableObject.CreateInstance<FragmentSpecialDefinition>();
            typeof(FragmentSpecialDefinition).GetField("effectType", Private).SetValue(customEffect, FragmentSpecialType.SpinningSlash);
            typeof(SpecialRecipe).GetField("baseEffect", Private).SetValue(targetRecipe, customEffect);
            typeof(PlayerBase).GetField("specialRecipes", Private).SetValue(player, new[] { targetRecipe });
            front = Enemy(1f);
            back = Enemy(-1f);
            Cast("등록 레시피의 기본 효과 우선");
            Check("등록 효과의 전방 피해", 70, front.CurrentHealth);
            Check("등록 효과의 후방 피해", 70, back.CurrentHealth);

            // 입력: AAA를 두 번 사용 → 기대: 0회, 조합 초기화, 다음 수집 허용.
            Prepare("AAA");
            Check("특수 첫 시전", 1, player.TryUseSpecial() ? 1 : 0);
            Check("특수 두 번째 시전", 1, player.TryUseSpecial() ? 1 : 0);
            Check("두 번 사용 뒤 남은 횟수", 0, player.SpecialUsesRemaining);
            Check("두 번 사용 뒤 재시전", 0, player.TryUseSpecial() ? 1 : 0);
            Check("다음 조각 수집 허용", 1, player.TryCollectFragment(CollectibleType.C) ? 1 : 0);

            // 입력: 가상 D 키로 CCC 시전 → 기대: 일반 입력 진입점에서 사용 횟수 1.
            Prepare("CCC");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            keyboard.MakeCurrent();
            typeof(PlayerBase).GetMethod("Update", Private).Invoke(player, null);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            keyboard.MakeCurrent();
            typeof(PlayerBase).GetMethod("Update", Private).Invoke(player, null);
            Check("D 특수 공격 입력 연결", 1, player.SpecialUsesRemaining);
            InputSystem.RemoveDevice(keyboard);

            Debug.Log($"[FragmentSpecials] PASS {checks}/{checks}");
            SessionState.EraseBool(Running);
            EditorApplication.Exit(0);
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
