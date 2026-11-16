using System;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyPlayerMovementBaseline
{
    private static readonly (string Field, float Value)[] Baseline = {
        ("maximumMovementSpeed", 5f),
        ("movementAcceleration", 120f),
        ("movementDeceleration", 180f),
        ("jumpForce", 27.82f),
        ("maximumJumpHeight", 30f),
        ("gravityScale", 9.8f),
        ("coyoteTime", 0.12f),
        ("jumpReleaseMultiplier", 0.704f),
        ("movementFriction", 0f)
    };

    // 새 컴포넌트와 두 저장된 씬이 합의한 이동·점프 수치를 쓰는지 확인한다.
    public static void Run()
    {
        Scene preview = EditorSceneManager.NewPreviewScene();

        try {
            var instance = new GameObject("PlayerBase baseline check");
            SceneManager.MoveGameObjectToScene(instance, preview);
            instance.AddComponent<Rigidbody2D>();
            instance.AddComponent<BoxCollider2D>();
            CheckComponent("새 PlayerBase", instance.AddComponent<PlayerBase>());
        } finally {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        foreach (string name in new[] { "PlayTestScene", "FlyingBossTestScene" }) {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestScene/" + name + ".unity", OpenSceneMode.Single);

            if (scene.isDirty) {
                throw new InvalidOperationException("저장된 씬을 읽을 수 없습니다. " + name);
            }

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
            CheckComponent(name, player);
            Check(name + " 바디 중력 배율", 9.8f, player.GetComponent<Rigidbody2D>().gravityScale);
        }

        Debug.Log("[PlayerBaseline] PASS 29/29");
    }

    // Unity가 직렬화한 이동·점프 필드를 기준값과 비교한다.
    private static void CheckComponent(string name, PlayerBase player)
    {
        var serialized = new SerializedObject(player);

        foreach (var entry in Baseline) {
            Check(name + " " + entry.Field, entry.Value, serialized.FindProperty(entry.Field).floatValue);
        }
    }

    // 입력·기대값·실제값을 기록하고 기준과 다르면 중단한다.
    private static void Check(string input, float expected, float actual)
    {
        string line = "[PlayerBaseline] 입력 " + input + " | 기대 " + expected + " | 실제 " + actual;
        Debug.Log(line);

        if (!Mathf.Approximately(expected, actual)) {
            throw new InvalidOperationException(line);
        }
    }
}
