using System;
using UnityEngine;

namespace Vela.Core
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private float invulnerabilityAfterHit = 0.7f;
        [SerializeField] private Team team = Team.Monster;
        [SerializeField] private string displayName = "";

        private int current;
        private float invulnerableUntil;

        /// Raised for any change to the number (damage, heal, revive).
        public event Action<Health> Changed;
        /// Raised when damage actually lands. The info carries the post-armor amount.
        public event Action<Health, DamageInfo> Damaged;
        /// Raised when a hit was refused because of i-frames — this is a dodge.
        public event Action<Health, DamageInfo> Dodged;
        public event Action<Health, int> Healed;
        public event Action<Health, DamageInfo> Died;

        /// Lets an owner reshape incoming damage (the Brute's front armor uses this).
        public Func<DamageInfo, int> IncomingDamageFilter;

        public int Max { get { return maxHealth; } }
        public int Current { get { return current; } }
        public Team Side { get { return team; } }
        public bool IsAlive { get { return current > 0; } }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }
        public float Normalized { get { return maxHealth > 0 ? Mathf.Clamp01((float)current / maxHealth) : 0f; } }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(displayName) ? name : displayName; }
            set { displayName = value; }
        }

        private void Awake()
        {
            if (current <= 0) current = maxHealth;
        }

        public void Configure(int newMax, float iframesAfterHit, Team side, string label)
        {
            maxHealth = Mathf.Max(1, newMax);
            invulnerabilityAfterHit = Mathf.Max(0f, iframesAfterHit);
            team = side;
            displayName = label;
            current = maxHealth;
        }

        public DamageResult Apply(DamageInfo info)
        {
            if (info.amount <= 0 || !IsAlive) return DamageResult.None;
            if (info.source == team) return DamageResult.None;

            if (IsInvulnerable)
            {
                if (Dodged != null) Dodged(this, info);
                return DamageResult.Dodged;
            }

            int applied = info.amount;
            if (IncomingDamageFilter != null) applied = Mathf.Max(0, IncomingDamageFilter(info));
            if (applied <= 0)
            {
                if (Dodged != null) Dodged(this, info);
                return DamageResult.Dodged;
            }

            info.amount = applied;
            current = Mathf.Max(0, current - applied);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;

            if (Damaged != null) Damaged(this, info);
            if (Changed != null) Changed(this);

            if (current == 0)
            {
                if (Died != null) Died(this, info);
                return DamageResult.Killed;
            }

            return DamageResult.Hit;
        }

        public bool Heal(int amount)
        {
            if (amount <= 0 || !IsAlive || current >= maxHealth) return false;

            int before = current;
            current = Mathf.Min(maxHealth, current + amount);
            if (Healed != null) Healed(this, current - before);
            if (Changed != null) Changed(this);
            return true;
        }

        public void Revive()
        {
            current = maxHealth;
            invulnerableUntil = 0f;
            if (Changed != null) Changed(this);
        }

        public void GrantInvulnerability(float duration)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
        }
    }
}
