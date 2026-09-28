using UnityEngine;
using Vela.Combat;
using Vela.Config;
using Vela.Core;
using Vela.Visual;

namespace Vela.Enemies
{
    /// Builds a monster (or boss) GameObject from its config at runtime. Spawn points and
    /// boss summons go through here, so a config change affects every copy.
    public static class EnemyFactory
    {
        public static EnemyBrain Spawn(EnemyConfigBase config, Vector3 position, Transform parent = null)
        {
            var go = new GameObject(config.displayName);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = position;

            var controller = go.AddComponent<CharacterController>();
            controller.radius = config.colliderRadius;
            controller.height = Mathf.Max(config.colliderHeight, config.colliderRadius * 2f + 0.01f);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
            controller.stepOffset = Mathf.Min(0.3f, controller.height * 0.3f);
            controller.slopeLimit = 45f;

            go.AddComponent<Health>();
            go.AddComponent<SpriteBillboard>();
            go.AddComponent<DamageFeedback>();
            var brain = go.AddComponent<EnemyBrain>();
            brain.Initialize(config);

            if (config is BossConfig boss) go.AddComponent<BossController>().Initialize(boss, brain);

            return brain;
        }
    }
}
