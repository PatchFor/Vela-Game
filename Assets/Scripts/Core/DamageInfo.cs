using UnityEngine;

namespace Vela.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    /// Everything a hit carries: the number shown, and the feel data (knockback, hit-stop,
    /// shake) the attacker's config asked for.
    public struct DamageInfo
    {
        public int Amount;
        public bool IsCrit;
        public Team SourceTeam;
        public GameObject Source;

        /// World point the hit landed (used for sparks and damage numbers).
        public Vector3 HitPoint;

        /// Flat direction from attacker to victim; knockback pushes along it.
        public Vector3 Direction;

        public float Knockback;

        /// Poise damage. Enough of it inside the poise window staggers an enemy.
        public float Stagger;

        public float HitStop;
        public float CameraShake;
    }
}
