using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using Mech.Function;
using UnityEngine;

public sealed class ObserveBossArena : MonoBehaviour
{
    private const string EntranceId = "playtest-boss-entrance";
    private static readonly string[] Patterns = { "Charge", "Slam", "Summon", "Sweep", "Charge", "Summon" };
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
    private CheckpointSaveStore store;
    private BossEnemy boss;
    private PlayerBase player;
    private FragmentEnemy contactTarget;
    private Checkpoint entrance;
    private int patternIndex;
    private int checks;
    private float startingHealth;
    private int startingFragments;
    private double previousEnd;
    private double previousEndReal;
    private double nextContact;
    private double timeout;
    private bool sawDeath;
    private bool finished;

    // 검증 빌드의 실제 씬과 게임 루프를 관찰할 오브젝트를 만든다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        Application.runInBackground = true;
        var host = new GameObject("Boss Live Probe");
        DontDestroyOnLoad(host);
        host.AddComponent<ObserveBossArena>();
    }

    // 독립적인 임시 슬롯에서 입구에 닿아 저장한 뒤 전투 위치로 이동한다.
    private IEnumerator Start()
    {
        timeout = Time.realtimeSinceStartupAsDouble + 70;
        store = new CheckpointSaveStore(Path.Combine(Path.GetTempPath(), "merger-boss-live-" + Guid.NewGuid().ToString("N")));
        typeof(CheckpointProgress).GetField("saveStore", Private).SetValue(null, store);
        player = FindAnyObjectByType<PlayerBase>();
        boss = FindAnyObjectByType<BossEnemy>();
        entrance = GameObject.Find("Boss Arena Checkpoint").GetComponent<Checkpoint>();
        PlayerBase.Died += OnDeath;
        CheckpointProgress.LoadFinished += OnLoadFinished;
        Place(entrance.SpawnPosition);
        yield return new WaitUntil(() => store.Read()?.CheckpointId == EntranceId);
        Check("입구 접촉 저장", EntranceId, store.Read().CheckpointId, true);
        boss.StateChanged += OnPatternStateChanged;
        Place(new Vector2(17f, 0.41f));
    }

    // 게임 물리를 직접 진행하지 않고 제한 시간과 실제 접촉 유도만 처리한다.
    private void FixedUpdate()
    {
        if (!finished && timeout > 0 && Time.realtimeSinceStartupAsDouble > timeout) {
            Fail("자연 진행 검증 시간 초과. 완료 패턴 " + patternIndex + ", 사망 " + sawDeath);
        }

        if (!finished && contactTarget != null && !sawDeath && !player.IsDead && Time.fixedTimeAsDouble >= nextContact) {
            nextContact = Time.fixedTimeAsDouble + 0.82;
            Place((Vector2)contactTarget.transform.position + new Vector2(0.3f, 0f));
        }
    }

    // 실제 상태 이벤트에서 패턴 순서와 대기 시간 및 물리 결과를 기록한다.
    private void OnPatternStateChanged(EnemyState oldState, EnemyState newState)
    {
        if (finished || patternIndex >= Patterns.Length) {
            return;
        }

        double now = Time.fixedTimeAsDouble;
        double real = Time.realtimeSinceStartupAsDouble;
        string expectedPattern = Patterns[patternIndex];

        if (newState == EnemyState.Attack) {
            int attacks = (int)typeof(BossEnemy).GetField("attacksSinceSummon", Private).GetValue(boss);
            string pattern = attacks >= 2 ? "Summon" : typeof(BossEnemy).GetField("nextAttack", Private).GetValue(boss).ToString();
            Check("패턴 " + (patternIndex + 1), expectedPattern, pattern, pattern == expectedPattern);

            if (patternIndex > 0) {
                double expectedGap = Patterns[patternIndex - 1] == "Summon" ? 5 : 3;
                double gameGap = now - previousEnd;
                double realGap = real - previousEndReal;
                string actual = Format(gameGap) + "초 게임 / " + Format(realGap) + "초 실시간";
                Check(Patterns[patternIndex - 1] + " → " + pattern, expectedGap + "초", actual, Math.Abs(gameGap - expectedGap) <= 0.025);
                Debug.Log("[BossLiveGap] " + Patterns[patternIndex - 1] + "|" + pattern + "|" + Format(expectedGap) + "|" + Format(gameGap) + "|" + Format(realGap));
            }

            startingHealth = player.CurrentHealth;
            startingFragments = FindObjectsByType<FragmentEnemy>().Length;
            return;
        }

        if (oldState != EnemyState.Attack || newState != EnemyState.Idle) {
            return;
        }

        previousEnd = now;
        previousEndReal = real;
        patternIndex++;
        float bossPhysicsX = boss.GetComponent<Rigidbody2D>().position.x;

        switch (patternIndex) {
            case 1:
                Check("왼쪽 돌진 완료", "X 14.02 근처, 피해 10", bossPhysicsX + " / " + (startingHealth - player.CurrentHealth),
                    Mathf.Abs(bossPhysicsX - 14.02f) < 0.07f && startingHealth - player.CurrentHealth == 10f);
                Place(new Vector2(16.5f, 0.41f));
                break;
            case 2:
                Check("내려찍기 명중", "피해 10", startingHealth - player.CurrentHealth, startingHealth - player.CurrentHealth == 10f);
                Place(new Vector2(26f, 0.41f));
                break;
            case 3:
                CheckSummon();
                Place(new Vector2(20f, 0.41f));
                break;
            case 4:
                Check("광역 공격 명중", "피해 10", startingHealth - player.CurrentHealth, startingHealth - player.CurrentHealth == 10f);
                break;
            case 5:
                Check("오른쪽 돌진 완료", "X 36.48 근처", bossPhysicsX, Mathf.Abs(bossPhysicsX - 36.48f) < 0.07f);
                Place(new Vector2(26f, 0.41f));
                break;
            case 6:
                CheckSummon();
                contactTarget = FindAnyObjectByType<FragmentEnemy>();
                break;
        }
    }

    // 소환 전후 수를 대조해 매번 실제로 조각 적 세 마리가 추가됐는지 확인한다.
    private void CheckSummon()
    {
        int added = FindObjectsByType<FragmentEnemy>().Length - startingFragments;
        Check("강제 소환 " + patternIndex, "3마리", added, added == 3);
    }

    // 조각 적과의 반복 접촉으로 발생한 실제 사망 이벤트를 확인한다.
    private void OnDeath(PlayerBase dead)
    {
        sawDeath = ReferenceEquals(dead, player) && dead.CurrentHealth == 0 && patternIndex == Patterns.Length;
        Check("실제 접촉에 의한 플레이어 사망", "기존 플레이어 HP 0", dead.CurrentHealth, sawDeath);
    }

    // 재로드 직후 새 플레이어의 좌표·체력과 보스 전투 초기화를 검증한다.
    private void OnLoadFinished(bool success)
    {
        var restored = FindAnyObjectByType<PlayerBase>();
        var restoredBoss = FindAnyObjectByType<BossEnemy>();
        var checkpoint = GameObject.Find("Boss Arena Checkpoint").GetComponent<Checkpoint>();
        Check("사망 후 씬 재로드", "성공, 새 플레이어", success,
            sawDeath && success && restored != null && !ReferenceEquals(restored, player));
        Check("입구 체크포인트 복귀", checkpoint.SpawnPosition.ToString("F3"), restored.transform.position.ToString("F3"),
            Vector3.Distance(restored.transform.position, checkpoint.SpawnPosition) < 0.01f && store.Read().CheckpointId == EntranceId);
        Check("체력 복원", restored.MaxHealth.ToString(), restored.CurrentHealth, restored.CurrentHealth == restored.MaxHealth);
        Check("보스전 초기화", "보스 HP 2250, 소환 적 0", restoredBoss.CurrentHealth + " / " + FindObjectsByType<FragmentEnemy>().Length,
            restoredBoss.CurrentHealth == 2250 && FindObjectsByType<FragmentEnemy>().Length == 0);

        if (!finished) {
            finished = true;
            Debug.Log("[BossLive] PASS " + checks + "/" + checks);
            Application.Quit(0);
        }
    }

    // 테스트 위치로만 이동시키고 물리·피해·패턴 시간은 원래 로직에 맡긴다.
    private void Place(Vector2 position)
    {
        var body = player.GetComponent<Rigidbody2D>();
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    // 측정 초를 플랫폼과 관계없이 같은 형식으로 출력한다.
    private static string Format(double seconds)
    {
        return seconds.ToString("F3", CultureInfo.InvariantCulture);
    }

    // 검증 입력과 기대값·실제값을 기록하고 불일치하면 실패를 반환한다.
    private void Check(string input, string expected, object actual, bool valid)
    {
        Debug.Log("[BossLive] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual);

        if (!valid) {
            Fail(input + " 불일치");
        } else {
            checks++;
        }
    }

    // 실패 기록을 남기고 테스트 실행을 중단한다.
    private void Fail(string reason)
    {
        if (!finished) {
            finished = true;
            Debug.LogError("[BossLive] FAIL " + reason);
            Application.Quit(1);
        }
    }
}
