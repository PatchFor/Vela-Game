using System;
using UnityEngine;
using Vela.Core;

namespace Vela.Config
{
    /// How strongly one class of hit (Light / Medium / Heavy / Finisher) is sold to the player.
    /// These multiply on top of each attack's own hitStop / cameraShake numbers.
    [Serializable]
    public class ImpactProfile
    {
        public string label = "Light";

        [Header("Time")]
        [Tooltip("Multiplier on the attack's hitStop.")]
        public float hitStopMultiplier = 1f;
        [Tooltip("Seconds added to the freeze-frame.")]
        public float hitStopBonus;

        [Header("Camera")]
        [Tooltip("Trauma added on top of the attack's cameraShake.")]
        public float extraShake;
        [Tooltip("Quick zoom-in kick (fraction of distance). 0 = none.")]
        [Range(0f, 0.3f)] public float zoomPunch;

        [Header("Victim")]
        [Tooltip("Squash on the victim sprite (0.25 = squashed 25%).")]
        public float squash = 0.2f;
        [Tooltip("Victim sprite trembles for this long (real time, so it shakes during the freeze).")]
        public float trembleDuration = 0.08f;
        [Tooltip("Tremble distance in world units.")]
        public float trembleAmount = 0.05f;
        [Tooltip("Multiplier on knockback.")]
        public float knockbackMultiplier = 1f;

        [Header("Particles")]
        public float sparkMultiplier = 1f;
        [Tooltip("Expanding ring at the impact point.")]
        public bool impactRing;
        public float impactRingRadius = 1.2f;
        public int dustCount;

        [Header("Damage number")]
        public float numberScale = 1f;
        public Color numberColor = Color.white;

        public ImpactProfile Clone() => (ImpactProfile)MemberwiseClone();
    }

    /// Global dials for combat feel: turn things off, scale them, restyle damage numbers.
    [CreateAssetMenu(menuName = "Vela/Combat Feel Config", fileName = "CombatFeel")]
    public class CombatFeelConfig : ScriptableObject
    {
        [Header("Online readiness")]
        [Tooltip("GlobalTimeScale: hit-stop / slow-mo freeze the whole game (single-player feel). " +
                 "LocalVisual: only the attacker and victim hold their pose, nothing slows down (online-safe). " +
                 "Switch to LocalVisual to preview how combat will feel in multiplayer.")]
        public HitStopMode hitStopMode = HitStopMode.GlobalTimeScale;

        [Header("Global multipliers")]
        [Tooltip("0 disables hit-stop.")]
        public float hitStopScale = 1f;
        [Tooltip("0 disables camera shake.")]
        public float cameraShakeScale = 1f;
        [Tooltip("0 disables zoom punches.")]
        public float zoomPunchScale = 1f;
        public float knockbackScale = 1f;
        [Tooltip("Scales every enemy's damage (a difficulty dial).")]
        public float enemyDamageScale = 1f;

        [Header("Impact profiles (by hit weight)")]
        public ImpactProfile light = DefaultLight();
        public ImpactProfile medium = DefaultMedium();
        public ImpactProfile heavy = DefaultHeavy();
        public ImpactProfile finisher = DefaultFinisher();

        [Header("Critical hits (added on top of the profile)")]
        public float critHitStopBonus = 0.05f;
        public float critExtraShake = 0.15f;
        [Range(0f, 0.3f)] public float critZoomPunch = 0.06f;
        [Tooltip("Slow motion right after the crit freeze. 0 = off.")]
        public float critSlowMoDuration = 0.12f;
        [Range(0.05f, 1f)] public float critSlowMoScale = 0.35f;
        [Tooltip("Quick white screen flash on crits.")]
        [Range(0f, 1f)] public float critScreenFlash = 0.12f;
        public Color critColor = new Color(1f, 0.85f, 0.2f);
        public string critLabel = "CRITICAL";
        public float critNumberScale = 1.5f;
        public int critSparkBonus = 10;

        [Header("Stagger / poise break")]
        public string breakLabel = "BREAK!";
        public Color breakColor = new Color(0.4f, 0.85f, 1f);
        public float breakHitStop = 0.08f;
        public float breakShake = 0.2f;

        [Header("Punish (hitting a staggered enemy)")]
        public float punishDamageMultiplier = 1.5f;
        public string punishLabel = "PUNISH";
        public Color punishColor = new Color(1f, 0.55f, 0.85f);

        [Header("Counter (hits after a perfect dodge)")]
        public string counterLabel = "COUNTER";
        public Color counterColor = new Color(0.5f, 0.85f, 1f);
        public string perfectDodgeLabel = "PERFECT";

        [Header("Kills")]
        public float killHitStop = 0.06f;
        public float killShake = 0.15f;
        public float bossKillSlowMo = 1.2f;
        [Range(0.05f, 1f)] public float bossKillSlowMoScale = 0.25f;

        [Header("Player getting hit")]
        public float playerHurtHitStop = 0.1f;
        [Range(0f, 1f)] public float playerHurtShake = 0.4f;
        [Range(0f, 0.3f)] public float playerHurtZoomPunch = 0.04f;
        public float playerHurtScreenFlash = 0.3f;
        [Tooltip("Sprite blinks while invulnerable after a hit (blinks per second).")]
        public float invulnerableBlinkRate = 16f;
        [Tooltip("Red pulsing screen edges below this HP fraction.")]
        [Range(0f, 1f)] public float lowHealthWarning = 0.3f;

        [Header("Hit flash")]
        public float hitFlashDuration = 0.07f;
        public Color hitFlashColor = Color.white;
        [Tooltip("After the white flash, the sprite fades back from this tint.")]
        public Color hurtTint = new Color(1f, 0.45f, 0.45f);
        public float hurtTintDuration = 0.18f;

        [Header("Damage numbers")]
        public bool showDamageNumbers = true;
        public int fontSize = 22;
        public float riseSpeed = 1.6f;
        public float lifetime = 0.8f;
        [Tooltip("Random sideways scatter so stacked hits don't overlap.")]
        public float horizontalScatter = 0.4f;
        [Tooltip("Hits on the same target within this window stack upward.")]
        public float stackWindow = 0.35f;
        public float stackOffset = 0.35f;
        public Color takenColor = new Color(1f, 0.3f, 0.3f);
        public Color healColor = new Color(0.4f, 1f, 0.5f);

        [Header("Combo counter")]
        public bool showComboCounter = true;
        [Tooltip("Combo resets if you don't land a hit for this long.")]
        public float comboTimeout = 1.6f;
        [Tooltip("Show the counter from this many hits.")]
        public int comboMinimum = 3;

        [Header("Enemy health bars")]
        public bool showEnemyHealthBars = true;
        [Tooltip("Bar stays visible this long after the enemy was hit.")]
        public float enemyBarLinger = 3f;

        [Header("FX")]
        public bool showSlashes = true;
        public bool showTelegraphs = true;
        public int hitSparkCount = 8;
        public Color hitSparkColor = new Color(1f, 0.95f, 0.75f);
        public int deathBurstCount = 28;

        public ImpactProfile Profile(HitWeight weight)
        {
            var profile = weight switch
            {
                HitWeight.Medium => medium,
                HitWeight.Heavy => heavy,
                HitWeight.Finisher => finisher,
                _ => light
            };
            return profile ?? DefaultLight();
        }

        public static ImpactProfile DefaultLight() => new ImpactProfile
        {
            label = "Light", hitStopMultiplier = 1f, extraShake = 0f, zoomPunch = 0f,
            squash = 0.15f, trembleDuration = 0.06f, trembleAmount = 0.04f,
            sparkMultiplier = 0.8f, numberScale = 0.9f, numberColor = Color.white
        };

        public static ImpactProfile DefaultMedium() => new ImpactProfile
        {
            label = "Medium", hitStopMultiplier = 1.1f, hitStopBonus = 0.01f, extraShake = 0.04f, zoomPunch = 0f,
            squash = 0.22f, trembleDuration = 0.09f, trembleAmount = 0.06f,
            sparkMultiplier = 1.2f, numberScale = 1.05f, numberColor = new Color(1f, 0.97f, 0.88f)
        };

        public static ImpactProfile DefaultHeavy() => new ImpactProfile
        {
            label = "Heavy", hitStopMultiplier = 1.2f, hitStopBonus = 0.03f, extraShake = 0.12f, zoomPunch = 0.03f,
            squash = 0.32f, trembleDuration = 0.14f, trembleAmount = 0.1f, knockbackMultiplier = 1.15f,
            sparkMultiplier = 1.8f, impactRing = true, impactRingRadius = 1.4f, dustCount = 6,
            numberScale = 1.3f, numberColor = new Color(1f, 0.8f, 0.55f)
        };

        public static ImpactProfile DefaultFinisher() => new ImpactProfile
        {
            label = "Finisher", hitStopMultiplier = 1.3f, hitStopBonus = 0.05f, extraShake = 0.2f, zoomPunch = 0.06f,
            squash = 0.42f, trembleDuration = 0.2f, trembleAmount = 0.14f, knockbackMultiplier = 1.3f,
            sparkMultiplier = 2.4f, impactRing = true, impactRingRadius = 2f, dustCount = 12,
            numberScale = 1.55f, numberColor = new Color(1f, 0.65f, 0.35f)
        };
    }
}
