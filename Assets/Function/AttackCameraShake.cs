using UnityEngine;
using UnityEngine.Rendering;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    sealed class AttackCameraShake : MonoBehaviour
    {
        private const double Duration = 0.03;
        private const float AmplitudeInPixels = 5f;
        private static readonly Vector2[] Directions = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
        private Camera target;
        private int directionIndex = -1;
        private int startFrame = -1;
        private double endsAt;
        private Vector3 renderOffset;

        // 명중한 프레임에 메인 직교 카메라의 흔들림을 예약한다.
        public static void Play()
        {
            Camera camera = Camera.main;

            if (camera == null || !camera.orthographic) {
                return;
            }

            if (!camera.TryGetComponent(out AttackCameraShake shake)) {
                shake = camera.gameObject.AddComponent<AttackCameraShake>();
            }

            shake.StartShake(camera);
        }

        // 한 프레임에 여러 적을 맞혀도 흔들림을 한 번 시작한다.
        private void StartShake(Camera camera)
        {
            if (startFrame == Time.frameCount) {
                return;
            }

            target = camera;
            directionIndex = (directionIndex + 1) % Directions.Length;
            startFrame = Time.frameCount;
            endsAt = Time.unscaledTimeAsDouble + Duration;
            enabled = true;
        }

        // 흔들림이 필요할 때 카메라 렌더링 시작과 종료를 구독한다.
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

        // 실제 시간이 지나면 흔들림을 종료한다.
        private void Update()
        {
            if (HasExpired) {
                enabled = false;
            }
        }

        // 카메라를 그리는 동안 화면 픽셀 기준으로 상하좌우 오프셋을 적용한다.
        private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (target != camera || HasExpired || renderOffset != Vector3.zero) {
                return;
            }

            Vector2 direction = Directions[(directionIndex + Time.frameCount - startFrame) % Directions.Length];
            bool followsPlayer = camera.TryGetComponent(out PlayerCameraFollow follow) && follow.isActiveAndEnabled;
            float visibleHeight = followsPlayer ? follow.VisibleHeight : 2f * camera.orthographicSize;
            float unitsPerPixel = visibleHeight / Mathf.Max(1, camera.pixelHeight);
            Vector3 position = transform.position;
            Vector3 shaken = position + (transform.right * direction.x + transform.up * direction.y) * (AmplitudeInPixels * unitsPerPixel);

            if (followsPlayer) {
                shaken = follow.ConstrainPosition(shaken);
            }

            renderOffset = shaken - position;
            transform.position = shaken;
        }

        private bool HasExpired => Time.frameCount != startFrame && Time.unscaledTimeAsDouble >= endsAt;

        // 카메라 렌더링이 끝나면 해당 프레임에 더한 오프셋을 되돌린다.
        private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (target == camera) {
                RestorePosition();
            }
        }

        // 렌더링 구독을 해제하고 남은 오프셋을 정리한다.
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
            RestorePosition();
        }

        // 기존 카메라 움직임을 유지하면서 흔들림 오프셋만 제거한다.
        private void RestorePosition()
        {
            if (renderOffset != Vector3.zero) {
                transform.position -= renderOffset;
                renderOffset = Vector3.zero;
            }
        }
    }
}
