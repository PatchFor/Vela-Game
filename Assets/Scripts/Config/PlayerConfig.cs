using UnityEngine;

namespace Vela.Config
{
    [CreateAssetMenu(menuName = "Vela/Player Config", fileName = "Player")]
    public class PlayerConfig : ScriptableObject
    {
        public CharacterVisual visual = new CharacterVisual();

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

        [Header("Melee aim assist")]
        [Tooltip("Melee swings snap toward the nearest enemy within this many degrees of the cursor. 0 = off.")]
        [Range(0f, 90f)] public float meleeAimAssistAngle = 40f;
        [Tooltip("Enemies up to this far beyond the weapon's range still pull the swing toward them.")]
        public float meleeAimAssistRange = 1.2f;

        [Header("Weapons (keys 1 / 2 / 3, Tab cycles)")]
        public WeaponConfig[] weapons = new WeaponConfig[0];
        public int startingWeapon;
    }
}
