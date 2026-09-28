using UnityEngine;
using Vela.Config;
using Vela.Enemies;

namespace Vela.Gameplay
{
    /// Place in the scene and drop a MonsterConfig or BossConfig in. The game manager spawns
    /// from these at start and again when you press T.
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private EnemyConfigBase config;
        [SerializeField] private bool spawnOnStart = true;

        private EnemyBrain spawned;

        public EnemyConfigBase Config
        {
            get => config;
            set => config = value;
        }

        public EnemyBrain Spawned => spawned;

        public EnemyBrain Spawn()
        {
            Despawn();
            if (config == null) return null;
            spawned = EnemyFactory.Spawn(config, transform.position);
            return spawned;
        }

        public void Despawn()
        {
            if (spawned != null) Destroy(spawned.gameObject);
            spawned = null;
        }

        public bool SpawnOnStart => spawnOnStart;

        private void OnDrawGizmos()
        {
            var isBoss = config is BossConfig;
            Gizmos.color = isBoss ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, isBoss ? 1.2f : 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
    }
}
