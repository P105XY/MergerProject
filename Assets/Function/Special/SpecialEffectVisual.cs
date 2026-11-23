using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    sealed class SpecialEffectVisual : MonoBehaviour
    {
        private static readonly int tintId = Shader.PropertyToID("_Tint");
        private readonly List<(Mesh Mesh, MeshRenderer Renderer, Color Color)> layers = new();
        private Material material;
        private MaterialPropertyBlock properties;
        private Transform pulse;
        private float elapsed;
        private float duration;

        // 피해 판정과 같은 위치와 반경 및 각도로 베기나 폭발을 그린다.
        public static SpecialEffectVisual ShowArea(in SpecialContext context, FragmentSpecialType type,
            Vector2 center, float radius, float angle, bool burst, bool followsDash = false)
        {
            SpecialEffectVisual visual = CreateVisual(context, burst ? "Special burst" : "Special attack area",
                center, new Vector3(radius, radius, 1f));
            Color color = GetColor(type);
            visual.AddArc(angle, 0f, color, 0.22f);
            visual.AddArc(angle, 0.92f, color, 0.95f);
            Color highlight = Color.Lerp(color, Color.white, 0.55f);
            visual.pulse = (burst && type == FragmentSpecialType.ShardSlash
                ? visual.AddShards(angle, highlight)
                : visual.AddArc(angle, 0.78f, highlight, 0.78f)).transform;

            if (!followsDash) {
                visual.Fade(burst ? 0.35f : 0.26f);
            }

            return visual;
        }

        // 실제로 뻗은 길이만큼 앞쪽 칼날을 그리고 추가 피해가 적용되는 끝부분을 밝힌다.
        public static void ShowBlade(in SpecialContext context, float length, float width, float tipStart)
        {
            if (length <= 0f) {
                return;
            }

            SpecialEffectVisual visual = CreateVisual(context, "Special extending blade", context.Origin,
                new Vector3(length, width * 0.5f, 1f));
            Color color = GetColor(FragmentSpecialType.ExtendingBlade);
            ushort[] triangles = { 0, 1, 2, 0, 2, 3 };
            visual.AddLayer(new[] { new Vector2(0f, -1f), new Vector2(0f, 1f), Vector2.right, new Vector2(0.62f, -0.55f) },
                triangles, color, 0.88f);

            if (tipStart < length) {
                float tipFraction = tipStart / length;
                visual.AddLayer(new[] { new Vector2(tipFraction, -0.3f), new Vector2(tipFraction, 0.3f),
                    Vector2.right, new Vector2((1f + tipFraction) * 0.5f, -0.1f) },
                    triangles, Color.Lerp(color, Color.white, 0.75f), 1f);
            }

            visual.Fade(0.22f);
        }

        // 현재 시전의 씬과 방향으로 월드 공간 효과를 생성한다.
        private static SpecialEffectVisual CreateVisual(in SpecialContext context, string name, Vector2 center, Vector3 scale)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, context.Caster.gameObject.scene);
            root.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, context.AttackAngle));
            root.transform.localScale = scale;
            return root.AddComponent<SpecialEffectVisual>();
        }

        // 투사체에 빛나는 탄두와 꼬리를 붙여 실제 이동 방향을 표시한다.
        public static void AttachProjectile(Transform projectile, FragmentSpecialType type, Vector2 direction, float radius)
        {
            var root = new GameObject("Special projectile visual");
            root.transform.SetParent(projectile, false);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            float length = type switch {
                FragmentSpecialType.PiercingSpear => 8f,
                FragmentSpecialType.PiercingWave => 5.5f,
                _ => 4f
            };
            root.transform.localScale = new Vector3(radius * length, radius * 1.25f, 1f);
            var visual = root.AddComponent<SpecialEffectVisual>();
            Color color = GetColor(type);
            visual.AddLayer(new[] { new Vector2(-1f, 0f), new Vector2(0.04f, 0.7f), new Vector2(0.72f, 0f), new Vector2(0.04f, -0.7f) },
                new ushort[] { 0, 1, 2, 0, 2, 3 }, color, 0.38f);
            visual.AddLayer(new[] { new Vector2(-0.48f, 0f), new Vector2(0.02f, 1f), new Vector2(0.45f, 0f), new Vector2(0.02f, -1f) },
                new ushort[] { 0, 1, 2, 0, 2, 3 }, color, 1f);
            visual.AddLayer(new[] { new Vector2(-0.19f, 0f), new Vector2(0.04f, 0.48f), new Vector2(0.39f, 0f), new Vector2(0.04f, -0.48f) },
                new ushort[] { 0, 1, 2, 0, 2, 3 }, Color.Lerp(color, Color.white, 0.8f), 1f);
        }

        // 남은 시각 효과를 짧게 감쇠시키고 제거한다.
        public void Fade(float seconds)
        {
            elapsed = 0f;
            duration = seconds;

            if (pulse != null) {
                pulse.localScale = Vector3.one * 0.45f;
            }
        }

        // 원 또는 부채꼴의 바깥 경계와 안쪽 경계로 메시를 만든다.
        private MeshRenderer AddArc(float angle, float innerRadius, Color color, float alpha)
        {
            int segments = Mathf.Max(4, Mathf.CeilToInt(angle / 6f));
            var vertices = new Vector2[(segments + 1) * 2];
            var triangles = new ushort[segments * 6];

            for (int index = 0; index <= segments; index++) {
                float radians = (index / (float)segments - 0.5f) * angle * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                int vertex = index * 2;
                vertices[vertex] = direction;
                vertices[vertex + 1] = direction * innerRadius;

                if (index < segments) {
                    int triangle = index * 6;
                    triangles[triangle] = (ushort)vertex;
                    triangles[triangle + 1] = (ushort)(vertex + 1);
                    triangles[triangle + 2] = (ushort)(vertex + 2);
                    triangles[triangle + 3] = (ushort)(vertex + 1);
                    triangles[triangle + 4] = (ushort)(vertex + 3);
                    triangles[triangle + 5] = (ushort)(vertex + 2);
                }
            }

            return AddLayer(vertices, triangles, color, alpha);
        }

        // 부채꼴 안에 여러 파편을 배치해 명중점에서 퍼지는 방향을 보여준다.
        private MeshRenderer AddShards(float angle, Color color)
        {
            const int count = 5;
            var vertices = new Vector2[count * 3];
            var triangles = new ushort[count * 3];

            for (int index = 0; index < count; index++) {
                float radians = ((index + 0.5f) / count - 0.5f) * angle * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                Vector2 side = Vector2.Perpendicular(direction) * 0.05f;
                int vertex = index * 3;
                vertices[vertex] = direction * 0.48f - side;
                vertices[vertex + 1] = direction * 0.48f + side;
                vertices[vertex + 2] = direction * 0.98f;
                triangles[vertex] = (ushort)vertex;
                triangles[vertex + 1] = (ushort)(vertex + 1);
                triangles[vertex + 2] = (ushort)(vertex + 2);
            }

            return AddLayer(vertices, triangles, color, 0.95f);
        }

        // 월드 도형을 실제 삼각형으로 그리고 레이어별 색과 투명도를 설정한다.
        private MeshRenderer AddLayer(Vector2[] vertices, ushort[] triangles, Color color, float alpha)
        {
            if (material == null) {
                material = new Material(Resources.Load<Shader>("SpecialEffectVisual"));
                properties = new MaterialPropertyBlock();
            }

            var positions = new Vector3[vertices.Length];

            for (int index = 0; index < vertices.Length; index++) {
                positions[index] = vertices[index];
            }

            var mesh = new Mesh { name = "Special effect shape", vertices = positions };
            mesh.SetTriangles(triangles, 0);
            var layer = new GameObject("Shape");
            layer.transform.SetParent(transform, false);
            layer.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = layer.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            color.a = alpha;
            renderer.sortingOrder = 10 + layers.Count;
            properties.SetColor(tintId, color);
            renderer.SetPropertyBlock(properties);
            layers.Add((mesh, renderer, color));
            return renderer;
        }

        // 조합에 따라 서로 구별되는 색을 선택한다.
        private static Color GetColor(FragmentSpecialType type)
        {
            return type switch {
                FragmentSpecialType.HeavyStrike => new Color32(255, 202, 91, 255),
                FragmentSpecialType.DashThrust or FragmentSpecialType.ExtendingBlade => new Color32(255, 144, 91, 255),
                FragmentSpecialType.WideSlash => new Color32(84, 225, 247, 255),
                FragmentSpecialType.PiercingWave => new Color32(110, 184, 255, 255),
                FragmentSpecialType.ExplosiveThrust or FragmentSpecialType.ShardSlash => new Color32(255, 109, 122, 255),
                FragmentSpecialType.SpinningSlash => new Color32(92, 247, 181, 255),
                FragmentSpecialType.FocusedShot or FragmentSpecialType.FiveShot => new Color32(127, 154, 255, 255),
                FragmentSpecialType.ExplosiveShot => new Color32(255, 164, 70, 255),
                FragmentSpecialType.SpreadingShot or FragmentSpecialType.Buckshot => new Color32(250, 111, 221, 255),
                FragmentSpecialType.PiercingSpear => new Color32(245, 110, 138, 255),
                _ => new Color32(187, 125, 255, 255)
            };
        }

        // 판정 범위는 고정한 채 내부 파동을 넓히고 전체의 투명도를 낮춘다.
        private void Update()
        {
            if (duration <= 0f) {
                return;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (pulse != null) {
                pulse.localScale = Vector3.one * Mathf.Lerp(0.45f, 1f, progress);
            }

            foreach (var layer in layers) {
                Color color = layer.Color;
                color.a *= 1f - progress;
                properties.SetColor(tintId, color);
                layer.Renderer.SetPropertyBlock(properties);
            }

            if (progress >= 1f) {
                Destroy(gameObject);
            }
        }

        // 효과와 함께 실행 중 만든 메시와 재질을 정리한다.
        private void OnDestroy()
        {
            foreach (var layer in layers) {
                Destroy(layer.Mesh);
            }

            if (material != null) {
                Destroy(material);
            }
        }
    }
}
