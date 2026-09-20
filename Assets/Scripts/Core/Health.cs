using System;
using UnityEngine;

namespace Vela.Core
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private float invulnerabilityAfterHit = 0.7f;

        private int current;
        private float invulnerableUntil;

        public event Action<Health> Changed;
        public event Action<Health> Died;

        public int Max => maxHealth;
        public int Current => current;
        public bool IsAlive => current > 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public float Normalized => maxHealth > 0 ? Mathf.Clamp01((float)current / maxHealth) : 0f;

        private void Awake()
        {
            current = maxHealth;
        }

        public bool TryDamage(int amount)
        {
            if (amount <= 0 || !IsAlive || IsInvulnerable) return false;

            current = Mathf.Max(0, current - amount);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;
            Changed?.Invoke(this);

            if (current == 0) Died?.Invoke(this);
            return true;
        }

        public bool Heal(int amount)
        {
            if (amount <= 0 || !IsAlive || current >= maxHealth) return false;

            current = Mathf.Min(maxHealth, current + amount);
            Changed?.Invoke(this);
            return true;
        }

        public void GrantInvulnerability(float duration)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
        }
    }
}
