using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    static class EnemyDash
    {
        // 돌진 경로의 플레이어를 타격하고 가장 가까운 지형 앞에서 이동을 멈춘다.
        public static bool Step(Rigidbody2D body, Vector2 direction, float distance, LayerMask terrainLayers,
            float damage, List<RaycastHit2D> hits)
        {
            const float clearance = 0.01f;
            body.Cast(direction, new ContactFilter2D { useTriggers = true }, hits, distance + clearance);
            float obstruction = float.PositiveInfinity;

            foreach (RaycastHit2D hit in hits) {
                if (IsTerrain(hit.collider, terrainLayers) && Vector2.Dot(hit.normal, direction) < -0.01f) {
                    obstruction = Mathf.Min(obstruction, hit.distance);
                }
            }

            float step = Mathf.Min(distance, Mathf.Max(0f, obstruction - clearance));

            foreach (RaycastHit2D hit in hits) {
                if (hit.distance <= step && hit.collider.GetComponentInParent<PlayerBase>() is PlayerBase player) {
                    player.TryReceiveContactDamage(damage);
                }
            }

            body.position += direction * step;
            return obstruction <= distance + clearance;
        }

        // 지정 레이어의 고체 지형만 돌진 장애물과 소환 바닥으로 취급한다.
        public static bool IsTerrain(Collider2D collider, LayerMask layers)
        {
            return !collider.isTrigger && (layers.value & (1 << collider.gameObject.layer)) != 0
                && collider.GetComponentInParent<EnemyBase>() == null && collider.GetComponentInParent<PlayerBase>() == null;
        }
    }
}
