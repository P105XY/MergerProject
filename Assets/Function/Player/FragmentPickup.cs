using UnityEngine;

namespace Mech.Function
{
    public class FragmentPickup : MonoBehaviour
    {
        private CollectibleType fragmentType;
        private bool isInitialized;
        private bool isCollected;

        // 드롭한 적이 지정한 조각 종류를 설정한다.
        internal void Initialize(CollectibleType type)
        {
            fragmentType = type;
            isInitialized = true;
        }

        // 플레이어가 조각을 받을 수 있으면 한 번만 수집한다.
        public bool TryCollect(PlayerBase player)
        {
            if (!isInitialized || isCollected || player == null || !player.isActiveAndEnabled) {
                return false;
            }

            isCollected = true;

            if (!player.TryCollectFragment(fragmentType)) {
                isCollected = false;
                return false;
            }

            Destroy(gameObject);
            return true;
        }
    }
}
