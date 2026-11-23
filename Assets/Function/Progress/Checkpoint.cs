using UnityEngine;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointId = "";
        [SerializeField] private Transform spawnPoint = null;

        private PlayerBase pendingPlayer;

        public string CheckpointId => checkpointId;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        // 새 체크포인트의 충돌체를 통과 가능한 트리거로 설정한다.
        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        // 살아 있는 플레이어가 진입하면 이 프레임의 자동 저장을 예약한다.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActiveAndEnabled) {
                return;
            }

            PlayerBase player = other.GetComponentInParent<PlayerBase>();

            if (player != null && player.isActiveAndEnabled && !player.IsDead) {
                pendingPlayer = player;
            }
        }

        // 같은 프레임의 충돌과 아이템 획득 처리가 끝난 뒤 진행을 저장한다.
        private void LateUpdate()
        {
            PlayerBase player = pendingPlayer;
            pendingPlayer = null;

            if (player != null && player.isActiveAndEnabled && !player.IsDead) {
                CheckpointProgress.SaveAt(this, player);
            }
        }

        // 체크포인트가 비활성화되면 아직 처리하지 않은 저장 예약을 지운다.
        private void OnDisable()
        {
            pendingPlayer = null;
        }
    }
}
