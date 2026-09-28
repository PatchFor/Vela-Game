using UnityEngine;

namespace Vela.Config
{
    /// Global dials for combat feel: turn things off, scale them, restyle damage numbers.
    [CreateAssetMenu(menuName = "Vela/Combat Feel Config", fileName = "CombatFeel")]
    public class CombatFeelConfig : ScriptableObject
    {
        [Header("Global multipliers")]
        [Tooltip("0 disables hit-stop.")]
        public float hitStopScale = 1f;
        [Tooltip("0 disables camera shake.")]
        public float cameraShakeScale = 1f;
        public float knockbackScale = 1f;
        [Tooltip("Scales every enemy's damage (a difficulty dial).")]
        public float enemyDamageScale = 1f;

        [Header("Player getting hit")]
        public float playerHurtHitStop = 0.08f;
        [Range(0f, 1f)] public float playerHurtShake = 0.35f;
        public float playerHurtScreenFlash = 0.25f;

        [Header("Hit flash")]
        public float hitFlashDuration = 0.09f;
        public Color hitFlashColor = Color.white;
        [Tooltip("Squash/stretch punch on the sprite when hit.")]
        public float hitSquash = 0.25f;

        [Header("Damage numbers")]
        public bool showDamageNumbers = true;
        public int fontSize = 22;
        public int critFontSize = 32;
        public float riseSpeed = 1.6f;
        public float lifetime = 0.8f;
        [Tooltip("Random sideways scatter so stacked hits don't overlap.")]
        public float horizontalScatter = 0.4f;
        public Color dealtColor = Color.white;
        public Color critColor = new Color(1f, 0.85f, 0.2f);
        public Color takenColor = new Color(1f, 0.3f, 0.3f);
        public Color healColor = new Color(0.4f, 1f, 0.5f);
        public string critSuffix = "!";

        [Header("FX")]
        public bool showSlashes = true;
        public bool showTelegraphs = true;
        public int hitSparkCount = 10;
        public Color hitSparkColor = new Color(1f, 0.95f, 0.75f);
        public int deathBurstCount = 28;
    }
}
