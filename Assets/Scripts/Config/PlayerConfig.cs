using UnityEngine;
using Vela.Items;
using Vela.Visual;

namespace Vela.Config
{
    [CreateAssetMenu(menuName = "Vela/Player Config", fileName = "Player")]
    public class PlayerConfig : ScriptableObject
    {
        public CharacterVisual visual = new CharacterVisual();

        [Tooltip("Layered 4-direction body (paper doll). Leave empty to use `visual.sprite` only.")]
        public CharacterRig rig;

        [Header("Health")]
        public int maxHealth = 100;
        [Tooltip("Invulnerable for this long after taking a hit.")]
        public float invulnerabilityAfterHit = 0.6f;
        [Tooltip("Can't act for this long after being hit (unless the attack has super armor).")]
        public float hurtStun = 0.18f;
        [Tooltip("Multiplier on knockback you take.")]
        public float knockbackTaken = 1f;

        [Header("Movement")]
        public float moveSpeed = 6.5f;
        public float acceleration = 60f;
        public float deceleration = 70f;

        [Header("Dash")]
        public float dashSpeed = 20f;
        public float dashDuration = 0.16f;
        public float dashCooldown = 0.45f;
        [Tooltip("Extra i-frames after the dash ends.")]
        public float dashInvulnerabilityBonus = 0.08f;

        [Header("Dash FX")]
        [Tooltip("Seconds between afterimages while dashing.")]
        public float afterImageInterval = 0.025f;
        public float afterImageLifetime = 0.25f;
        public Color afterImageColor = new Color(0.4f, 0.9f, 1f, 0.6f);

        [Header("Perfect dodge (dash through an attack at the last moment)")]
        [Tooltip("A hit that lands within this many seconds after the dash starts counts as perfect.")]
        public float perfectDodgeWindow = 0.15f;
        [Tooltip("Online later: extra seconds added to the window to forgive network delay. Keep 0 offline.")]
        public float perfectDodgeLatencyAllowance;
        [Tooltip("World slow-motion after a perfect dodge (single-player hit-stop mode only).")]
        public float perfectDodgeSlowMo = 0.5f;
        [Range(0.05f, 1f)] public float perfectDodgeSlowMoScale = 0.3f;
        [Tooltip("Dash is ready again immediately after a perfect dodge.")]
        public bool perfectDodgeResetsDash = true;
        [Tooltip("After a perfect dodge, your hits deal bonus damage for this long.")]
        public float counterWindow = 1.5f;
        public float counterDamageMultiplier = 1.5f;
        [Tooltip("Hits inside the counter window always crit.")]
        public bool counterAlwaysCrits = true;
        public Color perfectDodgeColor = new Color(0.5f, 0.85f, 1f);

        [Header("Jump links (dash near a marked edge to leap across)")]
        public float jumpArcHeight = 1.8f;
        public float jumpDuration = 0.55f;
        [Tooltip("Dash direction must be within this many degrees of the link to trigger the jump.")]
        [Range(10f, 90f)] public float jumpMaxAngle = 65f;

        [Header("Melee aim assist")]
        [Tooltip("Melee swings snap toward the nearest enemy within this many degrees of the aim. 0 = off.")]
        [Range(0f, 90f)] public float meleeAimAssistAngle = 40f;
        [Tooltip("Enemies up to this far beyond the weapon's range still pull the swing toward them.")]
        public float meleeAimAssistRange = 1.2f;

        [Header("Lock-on (Q toggles, E switches, or click a monster)")]
        public float lockOnRange = 14f;
        [Tooltip("The lock breaks if the target gets further than this.")]
        public float lockOnBreakRange = 20f;
        [Tooltip("Screen-space radius (px at 1080p) for hovering a monster with the mouse.")]
        public float hoverRadiusPixels = 60f;
        [Tooltip("Rim drawn around the monster under the mouse, so you know what a click will target.")]
        public Color hoverOutlineColor = new Color(1f, 0.15f, 0.1f, 1f);
        [Tooltip("Thickness of that rim, in art pixels (1 = one pixel of the monster's sprite).")]
        [Range(0.5f, 3f)] public float hoverOutlineWidth = 1f;

        [Header("Weapons (Tab cycles)")]
        public WeaponConfig[] weapons = new WeaponConfig[0];
        public int startingWeapon;

        [Header("Skills (keys 1–4)")]
        public SkillConfig[] skills = new SkillConfig[0];

        [Header("Mouse buttons")]
        public MouseAction leftMouse = MouseAction.BasicAttack;
        public MouseAction rightMouse = MouseAction.ChargedAttack;

        [Header("Starting gear")]
        public ItemDefinition[] startingEquipment = new ItemDefinition[0];
        public ItemDefinition[] startingItems = new ItemDefinition[0];
    }
}
