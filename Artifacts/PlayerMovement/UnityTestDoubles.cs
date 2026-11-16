// Unity 에디터 실행을 대신하지 않는 입력·시간·접지 제어용 테스트 대역이다.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class DisallowMultipleComponent : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class MinAttribute : Attribute
    {
        // 테스트 컴파일용 최솟값 특성을 만든다.
        public MinAttribute(float value) { }
    }
    public class RangeAttribute : Attribute
    {
        // 테스트 컴파일용 범위 특성을 만든다.
        public RangeAttribute(float minimum, float maximum) { }
    }
    public class RequireComponent : Attribute
    {
        // 테스트 컴파일용 필수 컴포넌트 특성을 만든다.
        public RequireComponent(Type first, Type second) { }
    }
    public struct Vector2
    {
        public float x;
        public float y;
        public static Vector2 zero => default;
        public static Vector2 down => new Vector2(0f, -1f);
        public static Vector2 right => new Vector2(1f, 0f);
        // 테스트 벡터를 초기화한다.
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        // 두 테스트 벡터를 더한다.
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        // 테스트 벡터에 배율을 적용한다.
        public static Vector2 operator *(Vector2 a, float factor) => new Vector2(a.x * factor, a.y * factor);
        // 위치를 2D 벡터로 변환한다.
        public static explicit operator Vector2(Vector3 value) => new Vector2(value.x, value.y);
    }
    public struct Vector3 { public float x; public float y; }
    public struct Quaternion { public static Quaternion identity => default; }
    public struct LayerMask
    {
        // 테스트 레이어 마스크를 만든다.
        public static implicit operator LayerMask(int value) => default;
    }
    public struct RaycastHit2D { public Vector2 normal; }
    public struct ContactFilter2D
    {
        public bool useTriggers;
        // 테스트에서는 레이어 필터를 저장하지 않는다.
        public void SetLayerMask(LayerMask mask) { }
    }
    public enum HideFlags { DontSave }
    public class Object
    {
        public HideFlags hideFlags;
        // 테스트용 객체를 복제한다.
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => (T)Activator.CreateInstance(original.GetType());
        // 테스트용 제거 호출을 받아들인다.
        public static void Destroy(Object target) { }
    }
    public class GameObject : Object
    {
        public UnityEngine.SceneManagement.Scene scene;
        // 테스트에서는 추가 컴포넌트를 검색하지 않는다.
        public T GetComponent<T>() where T : class => null;
        // 테스트용 컴포넌트를 생성한다.
        public T AddComponent<T>() where T : new() => new T();
    }
    public class ScriptableObject : Object { }
    public class Transform : Object { public Vector3 position; public Quaternion rotation; }
    public class MonoBehaviour : Object
    {
        public readonly GameObject gameObject = new GameObject();
        public readonly Transform transform = new Transform();
        public bool isActiveAndEnabled = true;
        public readonly Rigidbody2D TestBody = new Rigidbody2D();
        // 플레이어가 테스트용 물리 바디를 조회하도록 한다.
        public T GetComponent<T>() where T : class => TestBody as T;
    }
    public class Collider2D : Object
    {
        public bool isActiveAndEnabled = true;
        // 이동 테스트에서는 부모 컴포넌트를 조회하지 않는다.
        public T GetComponentInParent<T>() where T : class => null;
    }
    public class Collision2D { public Collider2D collider; }
    public enum ForceMode2D { Impulse }
    public class Rigidbody2D : Object
    {
        public float gravityScale;
        public float mass = 1f;
        public PhysicsMaterial2D sharedMaterial;
        public Vector2 linearVelocity;
        public float angularVelocity;
        public Vector2 position;
        public bool TestGrounded = true;
        public float TestGroundNormalY = 1f;

        // 테스트 바디에 충격량으로 인한 속도 변화를 적용한다.
        public void AddForce(Vector2 impulse, ForceMode2D mode)
        {
            linearVelocity += impulse * (1f / mass);
        }

        // 테스트에서 지정한 지면 법선으로 접지 쿼리를 응답한다.
        public int Cast(Vector2 direction, ContactFilter2D filter, RaycastHit2D[] hits, float distance)
        {
            if (!TestGrounded) {
                return 0;
            }

            hits[0] = new RaycastHit2D { normal = new Vector2(0f, TestGroundNormalY) };
            return 1;
        }
    }
    public class PhysicsMaterial2D : Object
    {
        public float friction;
        public float bounciness;
        // 테스트용 물리 재질을 만든다.
        public PhysicsMaterial2D(string name) { }
    }
    public struct PhysicsScene2D
    {
        // 이동 테스트에서는 공격 쿼리에 빈 결과를 제공한다.
        public int OverlapBox(Vector2 center, Vector2 size, float angle, ContactFilter2D filter, List<Collider2D> hits) => 0;
    }
    public static class Mathf
    {
        // 두 값 중 큰 값을 반환한다.
        public static float Max(float a, float b) => Math.Max(a, b);
        // 두 값 중 작은 값을 반환한다.
        public static float Min(float a, float b) => Math.Min(a, b);
        // 값을 지정한 최솟값과 최댓값 사이로 제한한다.
        public static float Clamp(float value, float minimum, float maximum) => Math.Clamp(value, minimum, maximum);
        // 목표를 초과하지 않도록 테스트 수치를 이동시킨다.
        public static float MoveTowards(float current, float target, float delta)
        {
            if (Math.Abs(target - current) <= delta) {
                return target;
            }

            return current + Math.Sign(target - current) * delta;
        }
    }
    public static class Time { public static float fixedDeltaTime = 0.02f; public static float deltaTime; public static float time; }
    public static class Debug
    {
        // 이동 테스트에서 사용하지 않는 경고를 받아들인다.
        public static void LogWarning(string message, Object context) { }
    }
}
namespace UnityEngine.Serialization
{
    public class FormerlySerializedAsAttribute : Attribute
    {
        // 테스트 컴파일용 직렬화 이전 특성을 만든다.
        public FormerlySerializedAsAttribute(string oldName) { }
    }
}
namespace Mech.Function
{
    internal static class GameHitStop
    {
        // 이동 검사에서 애니메이션 정지 호출을 받아들인다.
        internal static void Play(UnityEngine.MonoBehaviour owner) { }
    }

    internal static class AttackCameraShake
    {
        // 이동 검사에서 카메라 흔들림 호출을 받아들인다.
        internal static void Play() { }
    }

    internal sealed class EnemyWhiteFlash : UnityEngine.MonoBehaviour
    {
        internal bool IsFlashing => false;

        // 이동 테스트에서는 렌더링 없이 피격 플래시 호출만 받는다.
        internal void Play(EnemyBase owner) { }
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { }
    public static class SceneExtensions
    {
        // 테스트용 물리 씬을 제공한다.
        public static UnityEngine.PhysicsScene2D GetPhysicsScene2D(this Scene scene) => default;
    }
}
namespace UnityEngine.InputSystem
{
    public class TestButton { public bool isPressed; public bool wasPressedThisFrame; }
    public class Keyboard
    {
        public static Keyboard current;
        public readonly TestButton leftArrowKey = new TestButton();
        public readonly TestButton rightArrowKey = new TestButton();
        public readonly TestButton aKey = new TestButton();
        public readonly TestButton sKey = new TestButton();
        public readonly TestButton dKey = new TestButton();
    }
}
