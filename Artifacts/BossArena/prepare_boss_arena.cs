using System;
using System.IO;
using Mech.Function;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;

public static class PrepareBossArena
{
    private const string Folder = "Assets/Prefabs/PlayTest";
    private const string ScenePath = "Assets/Scenes/TestScene/PlayTestScene.unity";
    private static Sprite square;
    private static Material spriteMaterial;

    // 임시 프로젝트에서 프리팹을 만들고 기존 플레이 테스트 씬에 보스 구역을 저장한다.
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (scene.isDirty || GameObject.Find("Boss Arena") != null) {
            throw new InvalidOperationException("저장된 원본 PlayTestScene이 필요합니다.");
        }

        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CreateSquare();
        spriteMaterial = GameObject.Find("Ground").GetComponent<SpriteRenderer>().sharedMaterial;

        if (spriteMaterial == null) {
            throw new InvalidOperationException("기본 스프라이트 머티리얼을 찾지 못했습니다.");
        }

        GameObject drop = CreateDrop();
        GameObject fragment = CreateFragment(drop);
        GameObject boss = CreateBoss(fragment);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerBase>();
        var arena = new GameObject("Boss Arena").transform;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(boss, arena);
        instance.transform.position = new Vector3(26f, 0.7f, 0f);
        var bossSettings = new SerializedObject(instance.GetComponent<BossEnemy>());
        bossSettings.FindProperty("targetPlayer").objectReferenceValue = player;
        bossSettings.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<BossEnemy>());

        Color stone = new Color(0.24f, 0.31f, 0.41f);
        Solid("Arena Floor", arena, new Vector2(25.25f, -1f), new Vector2(26.54f, 1f), stone);
        Solid("Entrance Wall", arena, new Vector2(12.5f, 0.325f), new Vector2(1f, 1.65f), stone);
        Solid("Far Wall", arena, new Vector2(38f, 2.5f), new Vector2(1f, 6f), stone);

        Color sweep = new Color(0.98f, 0.55f, 0.26f, 0.85f);
        Color slam = new Color(0.95f, 0.26f, 0.32f, 0.95f);
        SpriteShape("Sweep Range at Entrance Wall", arena, new Vector2(18f, -0.46f), new Vector2(6f, 0.08f), sweep, 1);
        SpriteShape("Slam Range at Entrance Wall", arena, new Vector2(16f, -0.37f), new Vector2(2f, 0.08f), slam, 1);
        SpriteShape("Sweep Range at Far Wall", arena, new Vector2(32.5f, -0.46f), new Vector2(6f, 0.08f), sweep, 1);
        SpriteShape("Slam Range at Far Wall", arena, new Vector2(34.5f, -0.37f), new Vector2(2f, 0.08f), slam, 1);
        AddSign(arena, new Vector2(10.2f, 2.4f), "BOSS ARENA  >\nS  Jump over the wall", 0.08f);
        AddSign(arena, new Vector2(25.5f, 3.5f), "BOSS ARENA\nCharge / Slam / Sweep / Summon\nRed: slam    Orange: sweep  (boss at wall)", 0.085f);
        AddCheckpoint(arena);
        FollowPlayer(player);

        if (!EditorSceneManager.SaveScene(scene)) {
            throw new InvalidOperationException("보스 구역이 포함된 씬을 저장하지 못했습니다.");
        }

        Debug.Log("[BossArena] Saved 3 prefabs and PlayTestScene.");
    }

    // 프리팹과 경기장에 공통으로 사용할 1유닛 테스트 스프라이트를 저장한다.
    private static void CreateSquare()
    {
        string path = Folder + "/PlayTestSquare.png";
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var pixels = new Color32[16 * 16];
        Array.Fill(pixels, new Color32(255, 255, 255, 255));
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        square = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // B 조각으로 초기화될 드롭 프리팹을 만든다.
    private static GameObject CreateDrop()
    {
        var root = new GameObject("Boss Fragment B");
        root.AddComponent<CircleCollider2D>().radius = 0.24f;
        root.GetComponent<CircleCollider2D>().isTrigger = true;
        root.AddComponent<FragmentPickup>();
        var glyph = SpriteShape("Drop", root.transform, Vector2.zero, Vector2.one * 0.36f, new Color(1f, 0.76f, 0.29f), 3);
        glyph.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        return SavePrefab(root, "BossFragmentB");
    }

    // 기본 공격 두 번에 처치되며 접촉 피해를 주는 고정 조각 적 프리팹을 만든다.
    private static GameObject CreateFragment(GameObject drop)
    {
        var root = new GameObject("Boss Fragment Enemy");
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        root.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1f);
        SpriteShape("Body", root.transform, Vector2.zero, new Vector2(0.8f, 1f), new Color(0.83f, 0.53f, 0.15f), 2);
        SpriteShape("Core", root.transform, new Vector2(0f, 0.12f), new Vector2(0.25f, 0.25f), new Color(1f, 0.87f, 0.46f), 3);
        var settings = new SerializedObject(root.AddComponent<FragmentEnemy>());
        settings.FindProperty("maxHealth").floatValue = 20f;
        settings.FindProperty("heldObjectType").enumValueIndex = (int)CollectibleType.B;
        settings.FindProperty("heldObject").objectReferenceValue = drop;
        settings.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, "BossFragmentEnemy");
    }

    // 네 패턴과 조각 적 프리팹을 연결한 보스 프리팹을 만든다.
    private static GameObject CreateBoss(GameObject fragment)
    {
        var root = new GameObject("Test Boss");
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        root.AddComponent<BoxCollider2D>().size = new Vector2(2f, 2.4f);
        SpriteShape("Armor", root.transform, Vector2.zero, new Vector2(2f, 2.4f), new Color(0.52f, 0.2f, 0.43f), 2);
        SpriteShape("Crest", root.transform, new Vector2(0f, 0.78f), new Vector2(1.45f, 0.26f), new Color(0.94f, 0.39f, 0.61f), 3);
        var settings = new SerializedObject(root.AddComponent<BossEnemy>());
        settings.FindProperty("maxHealth").floatValue = 2250f;
        settings.FindProperty("fragmentEnemyPrefab").objectReferenceValue = fragment.GetComponent<FragmentEnemy>();
        settings.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, "TestBoss");
    }

    // 프리팹을 저장하고 작성용 씬 객체를 정리한다.
    private static GameObject SavePrefab(GameObject root, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);

        if (prefab == null) {
            throw new InvalidOperationException("프리팹을 저장하지 못했습니다. " + name);
        }

        return prefab;
    }

    // 단색 테스트 스프라이트를 지정 위치와 크기로 배치한다.
    private static SpriteRenderer SpriteShape(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
    {
        var instance = new GameObject(name);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.sharedMaterial = spriteMaterial;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    // 표시용 외형과 같은 크기의 고체 콜라이더를 배치한다.
    private static void Solid(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        SpriteShape(name, parent, position, size, color, 0).gameObject.AddComponent<BoxCollider2D>();
    }

    // 경기장 안내를 월드 공간에 표시한다.
    private static void AddSign(Transform parent, Vector2 position, string content, float size)
    {
        var instance = new GameObject("Arena Sign");
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        var text = instance.AddComponent<TextMesh>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instance.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = size;
        text.fontSize = 40;
        text.color = new Color(0.9f, 0.94f, 1f);
    }

    // 경기장 밖에 기존 저장 시스템을 사용하는 재도전 체크포인트를 배치한다.
    private static void AddCheckpoint(Transform parent)
    {
        var instance = new GameObject("Boss Arena Checkpoint");
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = new Vector3(6.5f, 0.41f, 0f);
        instance.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1.5f);
        var settings = new SerializedObject(instance.AddComponent<Checkpoint>());
        settings.FindProperty("checkpointId").stringValue = "playtest-boss-entrance";
        settings.ApplyModifiedPropertiesWithoutUndo();
        SpriteShape("Checkpoint Marker", instance.transform, new Vector2(0f, -0.7f), new Vector2(0.35f, 0.42f), new Color(0.3f, 0.84f, 0.78f), 1);
    }

    // 기존 메인 카메라가 수평으로만 플레이어를 따라가도록 설정한다.
    private static void FollowPlayer(PlayerBase player)
    {
        var camera = Camera.main;
        var constraint = camera.gameObject.AddComponent<PositionConstraint>();
        constraint.AddSource(new ConstraintSource { sourceTransform = player.transform, weight = 1f });
        constraint.translationAxis = Axis.X;
        constraint.translationAtRest = camera.transform.localPosition;
        constraint.translationOffset = Vector3.right * 2f;
        constraint.locked = true;
        constraint.constraintActive = true;
    }
}
