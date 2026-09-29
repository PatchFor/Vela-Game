using System;
using UnityEngine;

namespace Vela.Audio
{
    /// Every sound the game plays is one of these events. Code never references clips directly.
    public enum SfxEvent
    {
        SwingLight,
        SwingHeavy,
        HitLight,
        HitMedium,
        HitHeavy,
        HitFinisher,
        Crit,
        Break,
        Punish,
        PlayerHurt,
        Dash,
        Jump,
        Land,
        PerfectDodge,
        EnemyWindup,
        EnemyWindupHeavy,
        Kill,
        BossPhase,
        SkillCast,
        ChargeReady,
        Pickup,
        PickupRare,
        Gold,
        InventoryFull,
        Shoot
    }

    /// Sound slots. Drop real clips into an entry to replace the generated placeholder for that
    /// event. Several clips per event = random pick (avoids the "machine gun" repetition).
    [CreateAssetMenu(menuName = "Vela/Sfx Library", fileName = "SfxLibrary")]
    public class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SfxEvent sfx;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1.5f)] public float volume = 1f;
            [Tooltip("Random pitch ± this fraction each play (0.08 = ±8%).")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.08f;
            [Tooltip("Ignore repeats of this sound inside this many seconds (multi-hit sweeps).")]
            public float minInterval = 0.03f;
        }

        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Tooltip("Play generated placeholder sounds for events that have no clips.")]
        public bool placeholderWhenEmpty = true;
        [Tooltip("Pan sounds left/right by where they happen on screen.")]
        [Range(0f, 1f)] public float stereoPan = 0.5f;

        public Entry[] entries = new Entry[0];

        public Entry Find(SfxEvent sfx)
        {
            if (entries == null) return null;
            foreach (var e in entries)
            {
                if (e != null && e.sfx == sfx) return e;
            }
            return null;
        }
    }
}
