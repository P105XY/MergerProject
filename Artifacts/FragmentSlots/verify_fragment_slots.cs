using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyFragmentSlots
{
    private const string Running = "VerifyFragmentSlots.Running";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int passed;

    // 도메인 재로드 뒤에도 검증을 플레이 모드에 연결한다.
    static VerifyFragmentSlots()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 복사본의 저장 위치를 분리하고 PlayTestScene을 연다.
    public static void Run()
    {
        PlayerSettings.productName = "Merger Fragment Slot Verification";
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene/PlayTestScene.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 입력 설명과 기대값 및 실제값을 기록하고 불일치 시 종료한다.
    private static void Check<T>(string input, T expected, T actual)
    {
        string line = $"[FragmentSlots] 입력 {input} | 기대 {expected} | 실제 {actual}";
        Debug.Log(line);

        if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
            throw new InvalidOperationException(line);
        }

        passed++;
    }

    // 실제 게임의 큐와 JSON 저장 호환 및 위젯 위치를 확인한다.
    private static void Verify(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        try {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var hud = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
            var bounds = hud.GetType().GetMethod("GetSlotBounds", BindingFlags.Static | BindingFlags.NonPublic);

            // 큐가 HUD에 노출하는 획득 순서를 문자열로 조회한다.
            string Order() => string.Concat(player.QueuedFragments);

            // 새 플레이어 상태에 JSON을 반영하고 체크포인트를 복원한다.
            void Restore(string json)
            {
                var state = JsonUtility.FromJson<PlayerCheckpointState>(json);
                Check("체크포인트 저장 상태 유효", true, PlayerBase.IsValidCheckpointState(state));
                player.RestoreCheckpointState(state, player.transform.position);
            }

            // 입력: 새 게임에서 C, A 수집 및 JSON 왕복 → 기대: 두 칸이 CA 순서로 복원.
            Check("초기 조각 칸", "", Order());
            Check("초기 활성 여부", false, player.IsSpecialAvailable);
            player.TryCollectFragment(CollectibleType.C);
            player.TryCollectFragment(CollectibleType.A);
            Check("C, A 획득 순서", "CA", Order());
            string partial = JsonUtility.ToJson(player.CaptureCheckpointState());
            Restore(partial);
            Check("미조합 저장 복원 순서", "CA", Order());

            // 입력: C를 한 번 더 획득하고 B를 시도 → 기대: CAC 유지, 회전 베기 3회.
            player.TryCollectFragment(CollectibleType.C);
            Check("가득 찬 큐의 획득 순서", "CAC", Order());
            Check("네 번째 조각 거부", false, player.TryCollectFragment(CollectibleType.B));
            Check("활성 조합의 기존 미조합 A 수", 0, player.TypeACount);
            Check("활성 특수명", "Spinning Slash", player.GetActiveSpecialName());
            Check("활성 사용 횟수", 3, player.SpecialUsesRemaining);
            Check("실제 특수 1회 실행", true, player.TryUseSpecial());
            Check("한 번 시전 후 조각 순서", "CAC", Order());
            Check("한 번 시전 후 횟수", 2, player.SpecialUsesRemaining);

            // 입력: 활성 상태 JSON 왕복 후 남은 두 번 시전 → 기대: 저장 순서와 횟수 복원 후 네 칸 비움.
            string active = JsonUtility.ToJson(player.CaptureCheckpointState());
            Restore(active);
            Check("활성 저장 복원 순서", "CAC", Order());
            Check("활성 저장 복원 횟수", 2, player.SpecialUsesRemaining);
            Check("두 번째 시전", true, player.TryUseSpecial());
            Check("세 번째 시전", true, player.TryUseSpecial());
            Check("소진 후 큐 비움", "", Order());
            Check("소진 후 활성 해제", false, player.IsSpecialAvailable);
            Check("소진 후 새 조각 획득", true, player.TryCollectFragment(CollectibleType.B));

            // 입력: 순서 필드가 없는 기존 저장 → 기대: 원래 수량으로 A, B, C 순서 구성.
            Restore("{\"CollectedB\":1,\"CollectedC\":1}");
            Check("이전 미조합 저장 수량 복원", "BC", Order());
            Restore("{\"ActiveA\":1,\"ActiveC\":2,\"SpecialUsesRemaining\":2}");
            Check("이전 활성 저장 수량 복원", "ACC", Order());
            Check("이전 활성 공격 이름", "Spinning Slash", player.GetActiveSpecialName());
            var malformed = JsonUtility.FromJson<PlayerCheckpointState>("{\"CollectedA\":1,\"FragmentOrder\":[1]}");
            Check("수량과 맞지 않는 새 순서 거부", false, PlayerBase.IsValidCheckpointState(malformed));

            // 입력: 활성 조합에 집중탄 교체 장비 장착 → 기대: 실제 교체할 공격명으로 표시.
            var replacement = ScriptableObject.CreateInstance<FragmentSpecialDefinition>();
            typeof(FragmentSpecialDefinition).GetField("effectType", Private).SetValue(replacement, FragmentSpecialType.FocusedShot);
            var equipment = ScriptableObject.CreateInstance<SpecialEquipment>();
            typeof(SpecialEquipment).GetField("replacementEffect", Private).SetValue(equipment, replacement);
            player.SetSpecialEquipment(equipment);
            Check("교체된 활성 공격명", "Focused Shot", player.GetActiveSpecialName());

            // 입력: 넓은 화면과 좁은 화면 → 기대: 동일한 크기의 네 정사각형이 왼쪽 아래에 놓임.
            foreach ((int width, int height) in new[] { (1280, 720), (280, 160) }) {
                var slots = Enumerable.Range(0, 4).Select(index => (Rect)bounds.Invoke(null, new object[] { index, width, height })).ToArray();
                bool squares = slots.All(slot => slot.width == slot.height && slot.width == slots[0].width
                    && slot.xMin >= 0f && slot.xMax <= width && slot.yMax == height - 16f);
                bool inOrder = slots.Zip(slots.Skip(1), (left, right) => left.xMax < right.xMin).All(value => value);
                Check($"{width}×{height} 왼쪽 아래의 네 정사각형", true, squares && inOrder && slots[0].xMin == 16f);
            }

            Debug.Log($"[FragmentSlots] PASS {passed}/{passed}");
            SessionState.EraseBool(Running);
            EditorApplication.Exit(0);
        } catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
