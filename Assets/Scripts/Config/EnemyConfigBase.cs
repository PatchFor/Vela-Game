using System;
using UnityEngine;
using Vela.Core;

namespace Vela.Config
{
    public enum MovementStyle
    {
        /// Walk straight at the player, stop at `preferredDistance`.
        Chase,

        /// Hold `preferredDistance`: back off when crowded, close in when far, strafe in between.
        KeepDistance,

        /// Orbit the player at `preferredDistance`, flipping direction now and then.
        Circle,

        /// Dart toward the player along random angles, re-picking every `erraticInterval`.
        Erratic,

        /// Never moves (turrets, dummies).
        Stationary
    }

    public enum EnemyAttackKind
    {
        /// Instant cone in front after the windup.
        MeleeArc,

        /// Short hop forward; hurts on contact.
        Lunge,

        /// Long straight rush; stops at walls (and is briefly dazed when it hits one).
        Charge,

        /// Aimed projectiles; `projectileCount` in a fan, `volleys` times.
        Projectile,

        /// Circles on the ground under/around the player that go off after the windup.
        GroundAoE,

        /// Blast centred on the enemy itself.
        SelfAoE,

        /// Ring of projectiles in every direction.
        RadialBurst,

        /// Calls in more monsters.
        Summon
    }

    [Serializable]
    public class EnemyAttack
    {
        public string name = "Attack";
        public EnemyAttackKind kind = EnemyAttackKind.MeleeArc;

        [Header("Selection")]
        [Tooltip("Relative chance vs. other attacks that are in range and off cooldown.")]
        public float weight = 1f;
        public float minRange;
        public float maxRange = 2.5f;
        public float cooldown = 1.5f;

        [Header("Timing (seconds)")]
        [Tooltip("The telegraph. The longer, the easier to dodge.")]
        public float windup = 0.6f;
        public float active = 0.2f;
        public float recovery = 0.6f;
        [Tooltip("Keep turning toward the player during the windup.")]
        public bool trackDuringWindup = true;
        [Tooltip("Degrees per second while tracking in the windup.")]
        public float trackTurnSpeed = 240f;

        [Header("Damage")]
        public int damage = 10;
        public float knockback = 6f;
        [Tooltip("How hard this hit feels when it lands on the player (shake, hit-stop, zoom). Auto guesses from damage.")]
        public HitWeight hitWeight = HitWeight.Auto;

        [Header("Shape")]
        [Tooltip("Melee reach, AoE radius, or contact radius for lunges/charges.")]
        public float radius = 2f;
        [Range(10f, 360f)] public float arcDegrees = 100f;
        [Tooltip("Lunge/charge speed.")]
        public float dashSpeed = 14f;

        [Header("Projectiles")]
        public int projectileCount = 1;
        public float spreadDegrees;
        public int volleys = 1;
        public float volleyInterval = 0.15f;
        public float projectileSpeed = 10f;
        public float projectileRange = 14f;
        public float projectileSize = 0.3f;

        [Header("Ground AoE")]
        public int aoeCount = 1;
        [Tooltip("Extra circles land randomly within this distance of the player.")]
        public float aoeScatter;

        [Header("Summon")]
        public MonsterConfig summon;
        public int summonCount = 2;

        [Header("Look")]
        public bool showTelegraph = true;
        public Color color = new Color(1f, 0.3f, 0.2f, 0.55f);
    }

    [Serializable]
    public class EnemyBehaviour
    {
        public MovementStyle movement = MovementStyle.Chase;
        public float moveSpeed = 3.5f;
        public float turnSpeed = 540f;

        [Tooltip("Notices the player inside this range.")]
        public float detectionRange = 11f;
        [Tooltip("Gives up if the player gets further than this.")]
        public float leashRange = 24f;
        [Tooltip("Chase: stop distance. KeepDistance/Circle: the distance to hold.")]
        public float preferredDistance = 1.6f;
        [Tooltip("Erratic: seconds between direction changes.")]
        public float erraticInterval = 0.5f;

        [Tooltip("Pause between finishing an attack and choosing the next (randomised ±30%).")]
        public float attackDecisionDelay = 0.35f;

        public EnemyAttack[] attacks = { new EnemyAttack() };
    }

    /// Fields shared by regular monsters and bosses.
    public abstract class EnemyConfigBase : ScriptableObject
    {
        public string displayName = "Monster";
        public CharacterVisual visual = new CharacterVisual();

        [Header("Body")]
        public int maxHealth = 40;
        public float colliderRadius = 0.45f;
        public float colliderHeight = 1.6f;

        [Header("Hit reactions")]
        [Tooltip("Poise: stagger damage needed to interrupt it. 0 = every hit interrupts. Very high = never.")]
        public float poise = 15f;
        public float staggerDuration = 0.35f;
        [Tooltip("0 = full knockback, 1 = immovable.")]
        [Range(0f, 1f)] public float knockbackResistance;

        [Tooltip("When it notices you, monsters within this radius wake up too.")]
        public float groupAlertRadius = 7f;

        [Header("Death")]
        public Color deathBurstColor = new Color(1f, 0.5f, 0.4f);
        [Tooltip("What it drops (gold + items).")]
        public Vela.Items.LootTable loot;

        public abstract EnemyBehaviour InitialBehaviour { get; }
    }
}
