using UnityEngine;

namespace Vela.Core
{
    public enum Team
    {
        Player = 0,
        Monster = 1
    }

    public enum DamageResult
    {
        None = 0,
        Dodged = 1,
        Hit = 2,
        Killed = 3
    }

    /// Everything a hit needs to carry: how hard, from whom, which way it pushes,
    /// and how much feedback (hitstop / shake) it is worth.
    public struct DamageInfo
    {
        public int amount;
        public Team source;
        public Vector3 direction;
        public float knockback;
        public float hitstop;
        public float shake;
        public string label;
        public bool heavy;

        public static DamageInfo Create(int amount, Team source, Vector3 direction, string label)
        {
            DamageInfo info = new DamageInfo();
            info.amount = amount;
            info.source = source;
            info.label = label;

            direction.y = 0f;
            info.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
            return info;
        }

        public DamageInfo WithFeedback(float knockbackForce, float hitstopSeconds, float shakeAmount, bool isHeavy)
        {
            knockback = knockbackForce;
            hitstop = hitstopSeconds;
            shake = shakeAmount;
            heavy = isHeavy;
            return this;
        }
    }
}
