using System.Collections.Generic;
using UnityEngine;
using Vela.Core;

namespace Vela.Combat
{
    public static class CombatUtility
    {
        private static readonly Collider[] Overlaps = new Collider[64];

        /// Hits every enemy of `team` within `radius` and `arcDegrees` of `forward`. Targets in
        /// `alreadyHit` are skipped and added. Returns the number of new hits.
        public static int MeleeArc(Vector3 origin, Vector3 forward, float radius, float arcDegrees, Team team,
            HashSet<Health> alreadyHit, System.Func<Health, DamageInfo> makeHit)
        {
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            var count = Physics.OverlapSphereNonAlloc(origin + Vector3.up * 0.8f, radius + 0.5f, Overlaps,
                ~0, QueryTriggerInteraction.Ignore);
            var landed = 0;

            for (var i = 0; i < count; i++)
            {
                var health = Overlaps[i].GetComponentInParent<Health>();
                if (health == null || !health.CanBeHurtBy(team)) continue;
                if (alreadyHit != null && alreadyHit.Contains(health)) continue;

                var offset = health.transform.position - origin;
                offset.y = 0f;

                // Account for the target's body so big monsters are easier to hit.
                var bodyRadius = Overlaps[i] is CharacterController cc ? cc.radius : 0.4f;
                if (offset.magnitude - bodyRadius > radius) continue;

                var inArc = arcDegrees >= 359f || offset.magnitude < bodyRadius + 0.3f ||
                            Vector3.Angle(forward, offset) <= arcDegrees * 0.5f +
                            Mathf.Rad2Deg * Mathf.Atan2(bodyRadius, Mathf.Max(0.1f, offset.magnitude));
                if (!inArc) continue;

                alreadyHit?.Add(health);
                if (health.ApplyDamage(makeHit(health))) landed++;
            }

            return landed;
        }

        /// Hits every enemy of `team` inside a circle. Returns the number of hits.
        public static int Circle(Vector3 center, float radius, Team team, HashSet<Health> alreadyHit,
            System.Func<Health, DamageInfo> makeHit)
        {
            return MeleeArc(center, Vector3.forward, radius, 360f, team, alreadyHit, makeHit);
        }

        public static DamageInfo MakeHit(GameObject source, Team team, Health victim, int amount, bool crit,
            float knockback, float stagger, float hitStop, float shake)
        {
            var origin = source != null ? source.transform.position : victim.transform.position;
            var direction = victim.transform.position - origin;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

            return new DamageInfo
            {
                Amount = Mathf.Max(1, amount),
                IsCrit = crit,
                SourceTeam = team,
                Source = source,
                HitPoint = victim.transform.position + Vector3.up * 1f,
                Direction = direction,
                Knockback = knockback,
                Stagger = stagger,
                HitStop = hitStop,
                CameraShake = shake
            };
        }

        public static int RollDamage(int baseDamage, float variance)
        {
            var roll = baseDamage * (1f + Random.Range(-variance, variance));
            return Mathf.Max(1, Mathf.RoundToInt(roll));
        }

        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        public static Vector3 Rotate(Vector3 direction, float degrees) => Quaternion.Euler(0f, degrees, 0f) * direction;
    }
}
