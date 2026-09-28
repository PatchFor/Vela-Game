using System;
using UnityEngine;

namespace Vela.Config
{
    [Serializable]
    public class BossPhase
    {
        public string name = "Phase";
        [Tooltip("This phase begins when HP drops to this fraction (1 = the first phase).")]
        [Range(0f, 1f)] public float startsAtHealthFraction = 1f;
        public EnemyBehaviour behaviour = new EnemyBehaviour();

        [Header("Look")]
        public Color tint = Color.white;
        [Tooltip("Multiplier on the boss's sprite size.")]
        public float spriteScale = 1f;

        [Header("Transition into this phase")]
        [Tooltip("Invulnerable, stationary roar before the new phase starts.")]
        public float transitionDuration = 1.4f;
        public string announcement = "";
        public bool shockwave = true;
        public float shockwaveRadius = 5f;
        public int shockwaveDamage = 10;
        public float shockwaveKnockback = 14f;
        public MonsterConfig[] summonOnEnter = new MonsterConfig[0];
    }

    [CreateAssetMenu(menuName = "Vela/Boss Config", fileName = "Boss")]
    public class BossConfig : EnemyConfigBase
    {
        [Tooltip("The boss bar appears once the boss notices you.")]
        public BossPhase[] phases = { new BossPhase() };

        public override EnemyBehaviour InitialBehaviour =>
            phases != null && phases.Length > 0 ? phases[0].behaviour : new EnemyBehaviour();
    }
}
