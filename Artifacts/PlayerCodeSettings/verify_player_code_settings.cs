using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyPlayerCodeSettings
{
    private const string Running = "VerifyPlayerCodeSettings.Running";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly string[] Fields = {
        "maxHealth", "contactDamageCooldown", "attackPower", "attacksPerSecond", "maximumMovementSpeed",
        "movementAcceleration", "movementDeceleration", "jumpForce", "gravityScale", "movementFriction",
        "maximumJumpHeight", "coyoteTime", "jumpReleaseMultiplier", "jumpDescentSpeedBoost", "attackReach",
        "attackSize", "attackLayers", "groundLayers", "specialRecipes", "equippedSpecialEquipment"
    };

    // 도메인 재로드 뒤에도 플레이 모드 검증을 연결한다.
    static VerifyPlayerCodeSettings()
    {
        if (SessionState.GetBool(Running, false)) {
            EditorApplication.playModeStateChanged += Verify;
        }
    }

    // 두 저장 씬에서 직렬화 속성 제거와 코드 초기값 적용을 확인한다.
    public static void Run()
    {
        foreach (string scene in new[] { "FlyingBossTestScene", "PlayTestScene" }) {
            EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + scene + ".unity");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var serialized = new SerializedObject(player);
            int exposed = Fields.Count(field => serialized.FindProperty(field) != null);
            Check(scene + " 직렬화 속성 수", 0, exposed);
            Check(scene + " 점프력", 35, (float)typeof(PlayerBase).GetField("jumpForce", Private).GetValue(player));
            Check(scene + " 초당 공격 횟수", 1, player.AttacksPerSecond);
        }

        SessionState.SetBool(Running, true);
        EditorApplication.playModeStateChanged -= Verify;
        EditorApplication.playModeStateChanged += Verify;
        EditorApplication.EnterPlaymode();
    }

    // 저장된 지상 테스트 씬의 실제 시작 체력과 점프 초기 속도를 검사한다.
    private static void Verify(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) {
            return;
        }

        int exit = 0;

        try {
            var store = typeof(CheckpointProgress).GetField("saveStore", Private);
            string directory = Path.Combine(Path.GetTempPath(), "merger-player-code-" + Guid.NewGuid().ToString("N"));
            store.SetValue(null, Activator.CreateInstance(store.FieldType, Private | BindingFlags.Public, null,
                new object[] { directory }, null));
            Physics2D.simulationMode = SimulationMode2D.Script;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            var body = player.GetComponent<Rigidbody2D>();
            Check("시작 체력", 100, player.CurrentHealth);
            typeof(PlayerBase).GetField("jumpRequested", Private).SetValue(player, true);
            typeof(PlayerBase).GetField("jumpHeld", Private).SetValue(player, true);
            Physics2D.SyncTransforms();
            typeof(PlayerBase).GetMethod("FixedUpdate", Private).Invoke(player, null);
            Check("실제 점프 시작 수직 속도", 35, body.linearVelocity.y);
            Check("실제 바디 중력 배율", 9.8f, body.gravityScale);
            Debug.Log("[PlayerCodeSettings] PASS 9/9");
        } catch (Exception error) {
            Debug.LogError("[PlayerCodeSettings] " + error);
            exit = 1;
        }

        SessionState.EraseBool(Running);
        EditorApplication.Exit(exit);
    }

    // 입력과 기대값·실제값을 기록하고 값이 다르면 검증을 중단한다.
    private static void Check(string input, float expected, float actual)
    {
        string line = "[PlayerCodeSettings] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!Mathf.Approximately(expected, actual)) {
            throw new InvalidOperationException(line);
        }
    }
}
