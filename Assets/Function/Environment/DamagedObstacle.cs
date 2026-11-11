using UnityEngine;

namespace Mech.Function
{
    [DisallowMultipleComponent]
    public sealed class DamagedObstacle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 10f;

        public float Damage => damage;
    }
}
