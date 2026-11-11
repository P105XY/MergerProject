using UnityEngine;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RegionTransition : MonoBehaviour
    {
        [SerializeField] private string destinationScenePath = "";
        [SerializeField] private string destinationCheckpointId = "";
        private PlayerBase pendingPlayer;

        // 지역 이동용 충돌체를 통과할 수 있는 트리거로 설정한다.
        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        // 플레이어가 지역 출구에 닿으면 프레임 끝에 이동하도록 예약한다.
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerBase player = other.GetComponentInParent<PlayerBase>();

            if (isActiveAndEnabled && player != null && !player.IsDead) {
                pendingPlayer = player;
            }
        }

        // 같은 프레임의 획득 처리가 끝나면 지역 이동을 시작한다.
        private void LateUpdate()
        {
            PlayerBase player = pendingPlayer;
            pendingPlayer = null;

            if (player != null) {
                CheckpointProgress.TryTravelToRegion(player, destinationScenePath, destinationCheckpointId);
            }
        }

        // 통로가 꺼지면 이동 예약을 취소한다.
        private void OnDisable()
        {
            pendingPlayer = null;
        }
    }
}
