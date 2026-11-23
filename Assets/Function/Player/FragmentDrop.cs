using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    static class FragmentDrop
    {
        private const float FallbackSize = 0.38f;
        private static Sprite fallbackSprite;

        // 지정된 파편 원본을 복제하고 원본이 없으면 기본 파편을 생성한다.
        public static GameObject Spawn(GameObject prefab, CollectibleType type, Vector3 position, Scene scene)
        {
            GameObject fragment = prefab != null
                ? Object.Instantiate(prefab, position, Quaternion.identity)
                : CreateFallback(position);

            if (fragment.scene != scene) {
                SceneManager.MoveGameObjectToScene(fragment, scene);
            }

            if (!fragment.TryGetComponent(out FragmentPickup pickup)) {
                pickup = fragment.AddComponent<FragmentPickup>();
            }

            pickup.Initialize(type);
            return fragment;
        }

        // 씬의 바닥과 가까운 위치를 계산해 공중에서 나온 파편에도 수집 기회를 준다.
        public static Vector3 FindFloor(Vector3 origin, Scene scene)
        {
            var hits = new System.Collections.Generic.List<RaycastHit2D>();
            var filter = new ContactFilter2D { useTriggers = false };
            scene.GetPhysicsScene2D().Raycast(origin, Vector2.down, 64f, filter, hits);
            float closestDistance = float.PositiveInfinity;

            foreach (RaycastHit2D hit in hits) {
                if (hit.distance < closestDistance && hit.normal.y > 0.5f && EnemyDash.IsTerrain(hit.collider, ~0)) {
                    closestDistance = hit.distance;
                }
            }

            if (float.IsFinite(closestDistance)) {
                origin.y += 0.22f - closestDistance;
            }

            return origin;
        }

        // 공격 판정에 맞힐 수 있는 표시와 트리거를 갖춘 기본 파편을 만든다.
        private static GameObject CreateFallback(Vector3 position)
        {
            if (fallbackSprite == null) {
                Texture2D white = Texture2D.whiteTexture;
                fallbackSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height),
                    new Vector2(0.5f, 0.5f), white.width);
            }

            var fragment = new GameObject("Weapon fragment");
            fragment.transform.position = position;
            fragment.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            fragment.transform.localScale = new Vector3(FallbackSize, FallbackSize, 1f);
            fragment.AddComponent<BoxCollider2D>().isTrigger = true;
            SpriteRenderer renderer = fragment.AddComponent<SpriteRenderer>();
            renderer.sprite = fallbackSprite;
            renderer.color = new Color32(115, 244, 213, 255);
            renderer.sortingOrder = 5;
            return fragment;
        }
    }
}
