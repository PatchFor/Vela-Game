using System;
using UnityEngine;

namespace Vela.Core
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float invulnerabilityAfterHit;
        [SerializeField] private Team team = Team.Enemy;

        private int current;
        private float invulnerableUntil;

        public event Action<Health> Changed;
        public event Action<Health, DamageInfo> Damaged;
        public event Action<Health, int> Healed;
        public event Action<Health> Died;

        /// A hit reached this target but i-frames stopped it (dash / jump). Perfect dodge hooks in here.
        public event Action<Health, DamageInfo> Evaded;

        public int Max => maxHealth;
        public int Current => current;
        public Team Team => team;
        public bool IsAlive => current > 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public float Normalized => maxHealth > 0 ? Mathf.Clamp01((float)current / maxHealth) : 0f;

        /// Takes hits (numbers, knockback) but never drops below 1 HP. Used by god mode
        /// and training dummies.
        public bool Immortal { get; set; }

        private void Awake()
        {
            current = maxHealth;
        }

        public void Configure(int max, Team newTeam, float iframesAfterHit)
        {
            maxHealth = Mathf.Max(1, max);
            team = newTeam;
            invulnerabilityAfterHit = iframesAfterHit;
            current = maxHealth;
            Changed?.Invoke(this);
        }

        public bool CanBeHurtBy(Team attacker) => attacker != team && IsAlive && !IsInvulnerable;

        /// Hostile and alive — a valid thing to swing at, even if i-frames will stop the hit.
        public bool IsTargetableBy(Team attacker) => attacker != team && IsAlive;

        /// Returns true if damage was dealt. Hits blocked by i-frames raise `Evaded` and return false.
        public bool ApplyDamage(DamageInfo info)
        {
            if (info.Amount <= 0 || !IsTargetableBy(info.SourceTeam)) return false;

            if (IsInvulnerable)
            {
                Evaded?.Invoke(this, info);
                return false;
            }

            var floor = Immortal ? 1 : 0;
            current = Mathf.Max(floor, current - info.Amount);
            if (invulnerabilityAfterHit > 0f) invulnerableUntil = Time.time + invulnerabilityAfterHit;

            Damaged?.Invoke(this, info);
            Changed?.Invoke(this);

            if (current == 0) Died?.Invoke(this);
            return true;
        }

        public bool Heal(int amount)
        {
            if (amount <= 0 || !IsAlive || current >= maxHealth) return false;

            var before = current;
            current = Mathf.Min(maxHealth, current + amount);
            Healed?.Invoke(this, current - before);
            Changed?.Invoke(this);
            return true;
        }

        public void GrantInvulnerability(float duration)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
        }
    }
}
