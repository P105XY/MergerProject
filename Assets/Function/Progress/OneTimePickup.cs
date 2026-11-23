using UnityEngine;
using UnityEngine.Events;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class OneTimePickup : MonoBehaviour
    {
        [SerializeField] private string itemId = "";
        [SerializeField] private bool autoSaveOnCollect;
        [SerializeField] private UnityEvent<PlayerBase> onCollected = new UnityEvent<PlayerBase>();

        // 새 아이템의 충돌체를 플레이어가 통과할 수 있는 트리거로 설정한다.
        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        // 나중에 생성되거나 다시 활성화된 아이템에도 획득 상태를 적용한다.
        private void OnEnable()
        {
            if (!CheckpointProgress.IsLoading) {
                HideIfCollected();
            }
        }

        // 트리거에 진입한 플레이어에게 아이템 획득을 시도한다.
        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other.GetComponentInParent<PlayerBase>());
        }

        // 살아 있는 플레이어의 최초 획득을 기록하고 보상 이벤트를 한 번 호출한다.
        public bool TryCollect(PlayerBase player)
        {
            if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled || player.IsDead
                || player.gameObject.scene != gameObject.scene) {
                return false;
            }

            if (!CheckpointProgress.TryRecordItemPickup(itemId)) {
                return false;
            }

            gameObject.SetActive(false);
            onCollected.Invoke(player);

            if (autoSaveOnCollect) {
                CheckpointProgress.SaveCurrentProgress(player);
            }

            return true;
        }

        // 현재 진행에 획득 기록이 있는 아이템을 보상 호출 없이 숨긴다.
        public void HideIfCollected()
        {
            if (CheckpointProgress.HasCollectedItem(itemId)) {
                gameObject.SetActive(false);
            }
        }
    }
}
