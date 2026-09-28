using UnityEngine;
using Vela.Gameplay;

namespace Vela.Combat
{
    /// Counts consecutive hits the player lands (for the HUD combo counter) and remembers
    /// recent crits (for the crit screen flash). Getting hit or waiting too long resets it.
    public static class ComboTracker
    {
        public static int Count { get; private set; }
        public static int Best { get; private set; }
        public static float LastHitTime { get; private set; } = -10f;

        /// Real time of the last counted hit (drives the HUD pop animation).
        public static float PopTime { get; private set; } = -10f;

        /// Real time of the last crit (drives the crit screen flash).
        public static float LastCritTime { get; private set; } = -10f;

        public static bool IsActive => Count > 0 && Time.time - LastHitTime <= VelaSettings.Feel.comboTimeout;

        /// 1 right after a hit, 0 when the combo is about to time out.
        public static float TimeLeftNormalized =>
            IsActive ? 1f - (Time.time - LastHitTime) / Mathf.Max(0.01f, VelaSettings.Feel.comboTimeout) : 0f;

        public static void RegisterHit(bool crit)
        {
            if (!IsActive) Count = 0;
            Count++;
            Best = Mathf.Max(Best, Count);
            LastHitTime = Time.time;
            PopTime = Time.unscaledTime;
            if (crit) LastCritTime = Time.unscaledTime;
        }

        public static void Break() => Count = 0;

        public static void Reset()
        {
            Count = 0;
            Best = 0;
            LastHitTime = -10f;
            PopTime = -10f;
            LastCritTime = -10f;
        }
    }
}
