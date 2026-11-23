using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float orthographicSize = 10f;
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 followOffset = new Vector2(2f, 0f);
        [SerializeField, Min(0f)] private float smoothTime = 0.3f;
        [SerializeField] private bool followVertical;
        [SerializeField] private bool limitToBounds;
        [SerializeField] private Rect worldBounds = new Rect(-25f, -10f, 50f, 20f);

        private Camera view;
        private float originalOrthographicSize;
        private Rect originalViewport;
        private float fixedHeight;
        private Vector2 followVelocity;
        private Vector2 viewExtents;
        private float uncroppedAspect;
        private float aspectMultiplier = 1f;
        private bool hasCroppedViewport;
        private bool HasBounds => limitToBounds && worldBounds.width > 0f && worldBounds.height > 0f;
        public float VisibleHeight => 2f * viewExtents.y;

        // 씬을 불러올 때 메인 직교 카메라에 공통 설정을 연결한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneSetup()
        {
            SceneManager.sceneLoaded -= ConfigureSceneCamera;
            SceneManager.sceneLoaded += ConfigureSceneCamera;
        }

        // 별도 설정이 없는 메인 카메라를 초기화하고 같은 씬의 플레이어를 연결한다.
        private static void ConfigureSceneCamera(Scene scene, LoadSceneMode mode)
        {
            foreach (Camera camera in Camera.allCameras) {
                if (camera.gameObject.scene != scene || !camera.orthographic || !camera.CompareTag("MainCamera")) {
                    continue;
                }

                if (!camera.TryGetComponent(out PlayerCameraFollow follow)) {
                    follow = camera.gameObject.AddComponent<PlayerCameraFollow>();
                }

                if (follow.target == null && follow.isActiveAndEnabled) {
                    follow.AssignScenePlayer(scene);
                }
            }
        }

        // 씬에 배치된 활성 플레이어가 있으면 추적 대상으로 삼는다.
        private void AssignScenePlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                PlayerBase player = root.GetComponentInChildren<PlayerBase>();

                if (player != null && player.isActiveAndEnabled) {
                    target = player.transform;
                    UpdateView(0f, snap: true);
                    return;
                }
            }
        }

        // 카메라에 설정된 직교 크기와 세로 추적을 끌 때 사용할 높이를 보관한다.
        private void Awake()
        {
            view = GetComponent<Camera>();
            originalOrthographicSize = view.orthographicSize;
            originalViewport = view.rect;
            uncroppedAspect = view.aspect;
            fixedHeight = transform.position.y;
        }

        // 시작 위치는 플레이어와 경계에 맞춰 즉시 배치한다.
        private void OnEnable()
        {
            followVelocity = Vector2.zero;
            UpdateView(0f, snap: true);
        }

        // 플레이어 이동을 처리한 뒤 카메라의 추적 위치를 갱신한다.
        private void LateUpdate()
        {
            UpdateView(Time.deltaTime);
        }

        // 카메라를 목표 지점으로 보간하고 화면이 경계를 벗어나지 않도록 제한한다.
        private void UpdateView(float deltaTime, bool snap = false)
        {
            if (!view.orthographic) {
                return;
            }

            Vector3 current = transform.position;
            Vector2 desired = target == null ? (Vector2)current : (Vector2)target.position + followOffset;

            if (!followVertical) {
                desired.y = fixedHeight;
            }

            UpdateFraming();
            Vector2 goal = ConstrainPosition(desired);
            Vector2 next = current;

            if (snap || smoothTime <= 0f) {
                next = goal;
                followVelocity = Vector2.zero;
            } else if (deltaTime > 0f) {
                next = Vector2.SmoothDamp(current, goal, ref followVelocity, smoothTime, Mathf.Infinity, deltaTime);
            }

            Vector2 constrained = ConstrainPosition(next);

            if (constrained.x != next.x) {
                followVelocity.x = 0f;
            }

            if (constrained.y != next.y) {
                followVelocity.y = 0f;
            }

            transform.position = new Vector3(constrained.x, constrained.y, current.z);
        }

        // 화면보다 좁은 경계에서는 직교 크기를 유지한 채 표시 영역만 잘라낸다.
        private void UpdateFraming()
        {
            float halfHeight = Mathf.Max(0.01f, orthographicSize);

            if (view.orthographicSize != halfHeight) {
                view.orthographicSize = halfHeight;
            }

            UpdateUncroppedAspect();
            var fullExtents = new Vector2(halfHeight * uncroppedAspect, halfHeight);
            viewExtents = HasBounds ? Vector2.Min(fullExtents, worldBounds.size * 0.5f) : fullExtents;

            if (viewExtents.Equals(fullExtents)) {
                RestoreViewport();
                return;
            }

            var visibleFraction = new Vector2(viewExtents.x / fullExtents.x, viewExtents.y / fullExtents.y);
            Vector2 viewportSize = Vector2.Scale(originalViewport.size, visibleFraction);
            view.rect = new Rect(originalViewport.center - viewportSize * 0.5f, viewportSize);
            view.projectionMatrix = Matrix4x4.Ortho(-viewExtents.x, viewExtents.x, -viewExtents.y, viewExtents.y,
                view.nearClipPlane, view.farClipPlane);
            hasCroppedViewport = true;
        }

        // 잘라낸 화면의 실제 픽셀 크기에서 원래 표시 영역의 종횡비를 복원한다.
        private void UpdateUncroppedAspect()
        {
            Rect pixels = view.pixelRect;

            if (pixels.width <= 0f || pixels.height <= 0f) {
                return;
            }

            float pixelAspect = pixels.width / pixels.height;

            if (!hasCroppedViewport) {
                uncroppedAspect = view.aspect;
                aspectMultiplier = uncroppedAspect / pixelAspect;
                return;
            }

            Rect viewport = view.rect;
            float widthScale = originalViewport.width / viewport.width;
            float heightScale = originalViewport.height / viewport.height;
            uncroppedAspect = pixelAspect * widthScale / heightScale * aspectMultiplier;
        }

        // 화면의 반폭과 반높이를 고려해 추적 위치와 렌더링 흔들림을 같은 경계로 제한한다.
        public Vector3 ConstrainPosition(Vector3 position)
        {
            if (!HasBounds) {
                return position;
            }

            Vector2 travel = Vector2.Max(Vector2.zero, worldBounds.size * 0.5f - viewExtents);
            Vector2 center = worldBounds.center;
            position.x = Mathf.Clamp(position.x, center.x - travel.x, center.x + travel.x);
            position.y = Mathf.Clamp(position.y, center.y - travel.y, center.y + travel.y);
            return position;
        }

        // 잘라낸 표시 영역과 투영을 카메라의 원래 상태로 되돌린다.
        private void RestoreViewport()
        {
            if (hasCroppedViewport) {
                view.rect = originalViewport;
                view.ResetProjectionMatrix();
                hasCroppedViewport = false;
            }
        }

        // 추적 컴포넌트를 끄면 원래 카메라 설정을 되돌린다.
        private void OnDisable()
        {
            RestoreViewport();
            view.orthographicSize = originalOrthographicSize;
        }

        // 선택한 카메라가 사용할 월드 경계를 씬 뷰에 표시한다.
        private void OnDrawGizmosSelected()
        {
            if (!HasBounds) {
                return;
            }

            Color previousColor = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldBounds.center, new Vector3(worldBounds.width, worldBounds.height, 0f));
            Gizmos.color = previousColor;
        }
    }
}
