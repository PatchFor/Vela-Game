using UnityEngine;
using Vela.Core;

namespace Vela.Gameplay
{
    /// Ragnarok-style manual pickup: stand near the drop, press F.
    /// Manual on purpose — it makes the pickup a visible beat in the loop.
    public class PlayerPickup : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 2.2f;
        [Tooltip("Pick items up by walking over them instead of pressing F.")]
        [SerializeField] private bool autoPickup;

        private Health health;

        /// The drop the prompt is currently pointing at, or null.
        public LootItem Nearest { get; private set; }
        public float PickupRadius { get { return pickupRadius; } }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Update()
        {
            Nearest = FindNearest();

            if (health != null && !health.IsAlive) return;
            if (Nearest == null) return;

            if (autoPickup || VelaInput.PickupPressed) Nearest.TryPickup(gameObject);
        }

        private LootItem FindNearest()
        {
            LootItem best = null;
            float bestDistance = pickupRadius;

            for (int i = 0; i < LootItem.All.Count; i++)
            {
                LootItem item = LootItem.All[i];
                if (item == null) continue;

                Vector3 offset = item.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;

                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
