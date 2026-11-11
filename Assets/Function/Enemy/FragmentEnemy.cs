using System.Collections;
using UnityEngine;

namespace Mech.Function
{
    public class FragmentEnemy : EnemyBase
    {
        private Rigidbody2D hoverBody;
        private Vector2 hoverCenter;
        private float hoverTime;

        // 바닥에 떠 있는 위치를 맞추고 키네마틱 바디로 가볍게 부유하도록 설정한다.
        internal void HoverAboveFloor(float floorY, float clearance)
        {
            PlaceAsObstacle(floorY + clearance);
            Vector2 center = transform.position;
            hoverBody = GetComponent<Rigidbody2D>();

            if (hoverBody == null) {
                hoverBody = gameObject.AddComponent<Rigidbody2D>();
            }

            hoverBody.bodyType = RigidbodyType2D.Kinematic;
            hoverBody.position = center;
            hoverCenter = center;
        }

        // 비행 보스가 소환한 조각 적만 중심 높이 주변에서 위아래로 움직인다.
        private void FixedUpdate()
        {
            if (hoverBody != null && !IsDefeated) {
                hoverTime += Time.fixedDeltaTime;
                hoverBody.MovePosition(hoverCenter + Vector2.up * (Mathf.Sin(hoverTime * 3f) * 0.15f));
            }
        }

        // 소환된 조각 적의 발을 바닥에 맞추고 밀리지 않는 장애물로 만든다.
        internal void PlaceAsObstacle(float floorY)
        {
            if (TryGetComponent(out Rigidbody2D body)) {
                body.bodyType = RigidbodyType2D.Static;
            }

            Physics2D.SyncTransforms();
            float bottom = float.PositiveInfinity;

            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) {
                if (hitbox.enabled && !hitbox.isTrigger) {
                    bottom = Mathf.Min(bottom, hitbox.bounds.min.y);
                }
            }

            if (!float.IsPositiveInfinity(bottom)) {
                transform.position += Vector3.up * (floorY - bottom);
            }

            Defeated += RemoveObstacle;
        }

        // 처치된 소환 적의 충돌을 즉시 끄고 피격 연출을 마저 표시한다.
        private void RemoveObstacle(EnemyBase enemy)
        {
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) {
                hitbox.enabled = false;
            }

            StartCoroutine(DestroyAfterFlash());
        }

        // 피격 플래시가 끝난 뒤 처치된 조각 적을 제거한다.
        private IEnumerator DestroyAfterFlash()
        {
            if (IsWhiteFlashing) {
                yield return new WaitWhile(() => IsWhiteFlashing);
            }

            Destroy(gameObject);
        }
    }
}
