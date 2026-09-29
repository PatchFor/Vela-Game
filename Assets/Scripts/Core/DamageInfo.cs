using UnityEngine;

namespace Vela.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    /// How "big" a hit feels. Each weight has an impact profile in CombatFeelConfig
    /// (hit-stop, shake, sparks, number size, zoom punch...). Auto picks one from the
    /// attack's stagger / damage, so older config assets still get sensible feedback.
    public enum HitWeight
    {
        Auto,
        Light,
        Medium,
        Heavy,
        Finisher
    }

    /// Everything a hit carries: the number shown, and the feel data (knockback, hit-stop,
    /// shake) the attacker's config asked for.
    public struct DamageInfo
    {
        public int Amount;
        public bool IsCrit;

        /// Hit on a staggered (poise-broken) enemy: bonus damage and a "PUNISH" callout.
        public bool IsPunish;

        /// Landed inside a perfect-dodge counter window.
        public bool IsCounter;
        public Team SourceTeam;
        public GameObject Source;
        public HitWeight Weight;

        /// World point the hit landed (used for sparks and damage numbers).
        public Vector3 HitPoint;

        /// Flat direction from attacker to victim; knockback pushes along it.
        public Vector3 Direction;

        public float Knockback;

        /// Poise damage. Enough of it inside the poise window staggers an enemy.
        public float Stagger;

        public float HitStop;
        public float CameraShake;

        /// Resolves Auto into a concrete weight.
        public static HitWeight Resolve(HitWeight weight, float stagger, int damage)
        {
            if (weight != HitWeight.Auto) return weight;
            if (stagger >= 60f || damage >= 45) return HitWeight.Finisher;
            if (stagger >= 30f || damage >= 25) return HitWeight.Heavy;
            if (stagger >= 15f || damage >= 15) return HitWeight.Medium;
            return HitWeight.Light;
        }
    }
}
