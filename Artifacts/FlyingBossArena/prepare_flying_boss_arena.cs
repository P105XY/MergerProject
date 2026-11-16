using System;
using System.Linq;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrepareFlyingBossArena
{
    private const string Folder = "Assets/Prefabs/PlayTest/";
    private const string Original = "Assets/Scenes/TestScene/PlayTestScene.unity";
    private const string ScenePath = "Assets/Scenes/TestScene/FlyingBossTestScene.unity";
    private static Sprite square;
    private static Material material;

    // 기존 테스트 조작을 복사해 비행 보스 전용 씬과 프리팹을 저장한다.
    public static void Run()
    {
        if (!AssetDatabase.CopyAsset(Original, ScenePath)) {
            throw new InvalidOperationException("비행 보스 테스트 씬을 새로 만들 수 없습니다.");
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
        var display = UnityEngine.Object.FindAnyObjectByType<PlayTestDisplay>();
        var camera = Camera.main;
        var ground = GameObject.Find("Ground");
        var checkpoint = GameObject.Find("Checkpoint Start");
        var keep = new[] { player.gameObject, display.gameObject, camera.gameObject, ground, checkpoint };

        foreach (GameObject root in scene.GetRootGameObjects()) {
            if (!keep.Contains(root)) {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        square = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "PlayTestSquare.png");
        material = ground.GetComponent<SpriteRenderer>().sharedMaterial;
        player.transform.position = new Vector3(-11f, 0.41f, 0f);
        checkpoint.name = "Flying Arena Checkpoint";
        checkpoint.transform.position = player.transform.position;
        SetString(checkpoint.GetComponent<Checkpoint>(), "checkpointId", "playtest-flying-boss");
        ground.transform.localScale = new Vector3(29f, 1f, 1f);
        var hud = new SerializedObject(display);
        hud.FindProperty("enemies").arraySize = 0;
        var shapes = hud.FindProperty("shapes");
        shapes.arraySize = 3;
        shapes.GetArrayElementAtIndex(0).objectReferenceValue = ground.GetComponent<SpriteRenderer>();
        shapes.GetArrayElementAtIndex(1).objectReferenceValue = player.GetComponent<SpriteRenderer>();
        shapes.GetArrayElementAtIndex(2).objectReferenceValue = hud.FindProperty("attackIndicator").objectReferenceValue;
        hud.ApplyModifiedPropertiesWithoutUndo();

        var arena = new GameObject("Flying Boss Arena").transform;
        Color stone = new Color(0.20f, 0.31f, 0.43f);
        Solid("Left Wall", arena, new Vector2(-14.5f, 3.5f), new Vector2(1f, 9f), stone);
        Solid("Right Wall", arena, new Vector2(14.5f, 3.5f), new Vector2(1f, 9f), stone);
        Solid("Ceiling", arena, new Vector2(0f, 8f), new Vector2(29f, 1f), stone);
        var flightBox = new GameObject("Flight Bounds");
        flightBox.transform.SetParent(arena, false);
        flightBox.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        var bounds = flightBox.AddComponent<BoxCollider2D>();
        bounds.isTrigger = true;
        bounds.size = new Vector2(28f, 8f);
        Shape("Flight Centerline", arena, new Vector2(0f, 3.5f), new Vector2(27.9f, 0.025f),
            new Color(0.29f, 0.53f, 0.72f, 0.5f), -1);
        Shape("Checkpoint Marker", checkpoint.transform, new Vector2(0f, -0.7f), new Vector2(0.35f, 0.42f),
            new Color(0.3f, 0.84f, 0.78f), 1);
        AddSign(arena);

        var projectile = CreateProjectile();
        var charger = CreateCharger();
        var fragment = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "BossFragmentEnemy.prefab").GetComponent<FragmentEnemy>();
        var bossAsset = CreateBoss(fragment, charger, projectile);
        var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossAsset, arena);
        boss.transform.position = new Vector3(0f, 3.5f, 0f);
        var bossSettings = new SerializedObject(boss.GetComponent<FlyingBossEnemy>());
        bossSettings.FindProperty("targetPlayer").objectReferenceValue = player;
        bossSettings.FindProperty("arenaBounds").objectReferenceValue = bounds;
        bossSettings.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(boss.GetComponent<FlyingBossEnemy>());

        if (!EditorSceneManager.SaveScene(scene)) {
            throw new InvalidOperationException("비행 보스 아레나 저장 실패");
        }

        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        Debug.Log("[FlyingBossArena] Saved scene and prefabs.");
    }

    // 비행 보스의 체력과 소환·발사 프리팹을 지정한다.
    private static GameObject CreateBoss(FragmentEnemy fragment, BossChargeMinion charger, EnemyProjectile projectile)
    {
        var root = new GameObject("Flying Test Boss");
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        root.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1.45f);
        Shape("Body", root.transform, Vector2.zero, new Vector2(2f, 1.45f), new Color(0.27f, 0.49f, 0.88f), 2);
        Shape("Core", root.transform, Vector2.zero, new Vector2(0.68f, 0.45f), new Color(0.46f, 1f, 0.95f), 3);
        var settings = new SerializedObject(root.AddComponent<FlyingBossEnemy>());
        settings.FindProperty("maxHealth").floatValue = 3400f;
        settings.FindProperty("fragmentEnemyPrefab").objectReferenceValue = fragment;
        settings.FindProperty("chargeMinionPrefab").objectReferenceValue = charger;
        settings.FindProperty("projectilePrefab").objectReferenceValue = projectile;
        settings.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "FlyingTestBoss");
    }

    // 돌진 소환 적의 충돌체와 테스트 외형을 저장한다.
    private static BossChargeMinion CreateCharger()
    {
        var root = new GameObject("Flying Boss Charger");
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        root.AddComponent<BoxCollider2D>().size = Vector2.one * 0.65f;
        Shape("Body", root.transform, Vector2.zero, Vector2.one * 0.65f, new Color(0.96f, 0.36f, 0.33f), 2);
        root.AddComponent<BossChargeMinion>();
        return Save(root, "FlyingBossCharger").GetComponent<BossChargeMinion>();
    }

    // 포물선과 방사형 발사가 공통으로 쓰는 투사체 외형을 저장한다.
    private static EnemyProjectile CreateProjectile()
    {
        var root = new GameObject("Flying Boss Projectile");
        Shape("Bolt", root.transform, Vector2.zero, new Vector2(0.32f, 0.20f), new Color(0.98f, 0.77f, 0.3f), 4);
        root.AddComponent<EnemyProjectile>();
        return Save(root, "FlyingBossProjectile").GetComponent<EnemyProjectile>();
    }

    // 프리팹을 저장한 뒤 작성용 객체를 삭제한다.
    private static GameObject Save(GameObject root, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);

        if (prefab == null) {
            throw new InvalidOperationException("프리팹 저장 실패 " + name);
        }

        return prefab;
    }

    // 바닥과 벽의 외형에 고체 콜라이더를 추가한다.
    private static void Solid(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        Shape(name, parent, position, size, color, 0).gameObject.AddComponent<BoxCollider2D>();
    }

    // 크기와 색상을 지정한 테스트용 스프라이트를 배치한다.
    private static SpriteRenderer Shape(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.sharedMaterial = material;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    // 비행 보스의 패턴 순서를 월드 공간에 표시한다.
    private static void AddSign(Transform parent)
    {
        var child = new GameObject("Flying Arena Sign");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, 6.5f, 0f);
        var text = child.AddComponent<TextMesh>();
        text.text = "FLYING BOSS  /  3400 HP\nFragments  >  Chargers  >  Dive  >  Arc  >  X / +";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        child.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.08f;
        text.fontSize = 40;
    }

    // 씬 객체의 직렬화 문자열 필드를 갱신한다.
    private static void SetString(Component component, string property, string value)
    {
        var settings = new SerializedObject(component);
        settings.FindProperty(property).stringValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
