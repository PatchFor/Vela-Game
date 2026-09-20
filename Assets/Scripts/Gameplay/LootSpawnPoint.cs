using UnityEngine;

namespace Vela.Gameplay
{
    /// Keeps one item lying on the ground so the pickup loop can be tested
    /// without killing anything first.
    public class LootSpawnPoint : MonoBehaviour
    {
        [SerializeField] private LootKind kind = LootKind.Potion;
        [SerializeField] private float respawnSeconds = 12f;

        private LootItem current;
        private float nextSpawnAt;

        private void Start()
        {
            SpawnNow();
        }

        private void Update()
        {
            if (current != null) return;

            if (Time.time >= nextSpawnAt) SpawnNow();
        }

        private void SpawnNow()
        {
            current = LootItem.Spawn(kind, transform.position);
            nextSpawnAt = Time.time + respawnSeconds;
        }
    }
}
