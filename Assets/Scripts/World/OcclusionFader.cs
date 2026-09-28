using UnityEngine;
using Vela.Gameplay;

namespace Vela.World
{
    /// On the camera. Every frame, casts from the camera to the player and nearby monsters;
    /// any FadeableObject in the way turns see-through.
    public class OcclusionFader : MonoBehaviour
    {
        [Tooltip("Thickness of the line of sight. Bigger = fades objects that only partly cover someone.")]
        [SerializeField] private float castRadius = 0.35f;
        [Tooltip("Only monsters this close to the player are checked.")]
        [SerializeField] private float monsterCheckRange = 18f;
        [SerializeField] private bool includeMonsters = true;

        private readonly RaycastHit[] hits = new RaycastHit[32];

        private void LateUpdate()
        {
            var player = CombatRegistry.Player;
            if (player == null) return;

            CheckTarget(player.transform.position);

            if (!includeMonsters) return;

            var range = monsterCheckRange * monsterCheckRange;
            foreach (var enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if ((enemy.transform.position - player.transform.position).sqrMagnitude > range) continue;
                CheckTarget(enemy.transform.position);
            }
        }

        private void CheckTarget(Vector3 feet)
        {
            // Check the feet and the head so half-hidden characters count too.
            Cast(feet + Vector3.up * 0.3f);
            Cast(feet + Vector3.up * 1.4f);
        }

        private void Cast(Vector3 point)
        {
            var origin = transform.position;
            var toPoint = point - origin;
            var distance = toPoint.magnitude - castRadius;
            if (distance <= 0f) return;

            var count = Physics.SphereCastNonAlloc(origin, castRadius, toPoint / toPoint.magnitude, hits, distance,
                ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var fadeable = hits[i].collider.GetComponentInParent<FadeableObject>();
                if (fadeable != null) fadeable.RequestFade();
            }
        }
    }
}
