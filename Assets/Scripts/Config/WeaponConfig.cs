using System;
using UnityEngine;
using Vela.Core;

namespace Vela.Config
{
    public enum AttackKind
    {
        /// Instant sweep: everything inside `range` and `arcDegrees` in front is hit.
        MeleeArc,

        /// Fires `projectileCount` projectiles in a fan of `spreadDegrees`.
        Projectile
    }

    /// One swing / shot. A weapon's combo is a list of these; the charged attack is one too.
    [Serializable]
    public class AttackStep
    {
        public string name = "Slash";
        public AttackKind kind = AttackKind.MeleeArc;

        [Header("Damage")]
        public int damage = 10;
        [Tooltip("Random ± fraction, so numbers vary a little (0.1 = ±10%).")]
        [Range(0f, 0.5f)] public float damageVariance = 0.1f;

        [Header("Timing (seconds)")]
        [Tooltip("Before the hit comes out. You can't cancel this.")]
        public float windup = 0.06f;
        [Tooltip("How long the hitbox stays live. Melee keeps sweeping for new targets during it.")]
        public float active = 0.08f;
        [Tooltip("After the hit. The next combo step (or a dash, if allowed) can cancel this.")]
        public float recovery = 0.2f;

        [Header("Melee")]
        public float range = 2.2f;
        [Range(10f, 360f)] public float arcDegrees = 120f;
        [Tooltip("Step forward at the start of the swing.")]
        public float lungeDistance = 0.6f;

        [Header("Projectile")]
        public int projectileCount = 1;
        public float spreadDegrees;
        public float projectileSpeed = 24f;
        public float projectileRange = 16f;
        [Tooltip("How many extra enemies an arrow passes through.")]
        public int pierce;
        public float projectileSize = 0.2f;
        public Color projectileColor = new Color(1f, 0.95f, 0.7f);

        [Header("Impact feel")]
        [Tooltip("Light / Medium / Heavy / Finisher picks the impact profile in CombatFeel (hit-stop, shake, sparks, " +
                 "number size, zoom punch). Auto guesses from stagger and damage.")]
        public HitWeight hitWeight = HitWeight.Auto;
        public float knockback = 4f;
        [Tooltip("Poise damage dealt to enemies.")]
        public float stagger = 10f;
        [Tooltip("Freeze-frame length on hit (seconds).")]
        public float hitStop = 0.05f;
        [Range(0f, 1f)] public float cameraShake = 0.12f;

        [Header("While attacking")]
        [Tooltip("Movement speed multiplier during this attack.")]
        [Range(0f, 1f)] public float moveSpeedMultiplier = 0.15f;
        [Tooltip("Hits taken during this attack don't knock you back.")]
        public bool superArmor;

        [Header("Slash FX")]
        public Color slashColor = Color.white;
        [Tooltip("Thickness of the slash crescent.")]
        public float slashWidth = 0.6f;
        public float slashDuration = 0.14f;
        [Tooltip("Swing right-to-left instead of left-to-right.")]
        public bool reverseSwing;
    }

    [CreateAssetMenu(menuName = "Vela/Weapon Config", fileName = "Weapon")]
    public class WeaponConfig : ScriptableObject
    {
        public string displayName = "Sword";
        public Color uiColor = Color.white;
        [Tooltip("Optional HUD icon.")]
        public Sprite icon;
        [Tooltip("What the character holds (paper-doll weapon layer).")]
        public Vela.Visual.EquipmentVisual visual;

        [Header("Crit")]
        [Range(0f, 1f)] public float critChance = 0.15f;
        public float critMultiplier = 1.75f;

        [Header("Combo (left click)")]
        public AttackStep[] combo = { new AttackStep() };
        [Tooltip("Pause after a combo step before the chain resets to step 1.")]
        public float comboResetTime = 0.45f;
        [Tooltip("Presses this long before you're able to attack still count.")]
        public float inputBufferTime = 0.25f;
        [Tooltip("Keep attacking while the button is held (good for bows).")]
        public bool repeatWhileHeld;
        [Tooltip("Dash can cancel an attack's recovery.")]
        public bool canDashCancel = true;

        [Header("Charged attack (hold right click, release when full)")]
        public bool hasChargedAttack = true;
        public float chargeTime = 0.6f;
        [Range(0f, 1f)] public float chargeMoveSpeedMultiplier = 0.45f;
        public AttackStep chargedAttack = new AttackStep { name = "Charged" };
    }
}
