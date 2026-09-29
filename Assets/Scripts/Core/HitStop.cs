using UnityEngine;

namespace Vela.Core
{
    public enum HitStopMode
    {
        /// Freeze the whole game (Time.timeScale). Strongest feel; single-player only.
        GlobalTimeScale,

        /// Freeze only the attacker's and victim's visuals; the simulation keeps running.
        /// Online-safe: one player's hit can't pause everyone else's game.
        LocalVisual
    }

    /// Anything that can hold its pose for a hit-stop without stopping the simulation.
    public interface IHitPausable
    {
        void HitPause(float seconds);
    }

    /// Freeze-frame on impact, optionally followed by a short slow-motion tail (crits, boss kill).
    /// Works in real time so the effect lasts the same regardless of the time scale.
    public class HitStop : MonoBehaviour
    {
        private const float FreezeScale = 0.02f;

        private static HitStop instance;

        private float freezeUntil;
        private float slowUntil;
        private float slowScale = 1f;

        public static bool IsFrozen => instance != null && Time.unscaledTime < instance.freezeUntil;

        /// Set from CombatFeelConfig by the game manager.
        public static HitStopMode Mode { get; set; } = HitStopMode.GlobalTimeScale;

        private static HitStop Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("[HitStop]").AddComponent<HitStop>();
                return instance;
            }
        }

        /// Freeze for `seconds` (real time). Global mode stops time; LocalVisual mode only pauses
        /// the given participants' visuals. Overlapping requests keep the longest.
        public static void Request(float seconds, IHitPausable a = null, IHitPausable b = null)
        {
            if (seconds <= 0f) return;

            if (Mode == HitStopMode.LocalVisual)
            {
                a?.HitPause(seconds);
                b?.HitPause(seconds);
                return;
            }

            var hs = Instance;
            hs.freezeUntil = Mathf.Max(hs.freezeUntil, Time.unscaledTime + seconds);
            hs.Apply();
        }

        /// Slow motion for `seconds` (real time), starting after any active freeze.
        public static void SlowMotion(float seconds, float timeScale)
        {
            // Slowing the world is single-player only.
            if (seconds <= 0f || Mode == HitStopMode.LocalVisual) return;
            var hs = Instance;
            var start = Mathf.Max(Time.unscaledTime, hs.freezeUntil);
            hs.slowUntil = Mathf.Max(hs.slowUntil, start + seconds);
            hs.slowScale = Mathf.Min(hs.slowUntil > Time.unscaledTime ? hs.slowScale : 1f, Mathf.Clamp(timeScale, 0.05f, 1f));
            hs.Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            var now = Time.unscaledTime;
            if (now < freezeUntil) Time.timeScale = FreezeScale;
            else if (now < slowUntil) Time.timeScale = slowScale;
            else
            {
                Time.timeScale = 1f;
                slowScale = 1f;
            }
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            Time.timeScale = 1f;
        }
    }
}
