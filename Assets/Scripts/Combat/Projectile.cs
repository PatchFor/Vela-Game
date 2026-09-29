using System.Collections.Generic;
using UnityEngine;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;

namespace Vela.Combat
{
    /// Straight-flying projectile using sphere casts (no rigidbody). Hurts the other team,
    /// stops on scenery. Sprite lies flat on the ground plane pointing where it flies, with a
    /// trail behind it.
    public class Projectile : MonoBehaviour
    {
        public struct Spec
        {
            public Team Team;
            public GameObject Source;
            public Vector3 Position;
            public Vector3 Direction;
            public float Speed;
            public float Range;
            public float Radius;
            public int Pierce;
            public Color Color;
            public Sprite Sprite;
            public float SpriteLength;

            /// Called per victim to build the hit (damage, crit roll, knockback...).
            public System.Func<Health, DamageInfo> MakeHit;

            /// Called after a hit actually deals damage (hit-confirm, sounds...).
            public System.Action<Health> OnLanded;
        }

        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        private Spec spec;
        private float travelled;
        private int pierceLeft;
        private readonly HashSet<Health> hitAlready = new HashSet<Health>();
        private TrailRenderer trail;

        public static Projectile Fire(Spec spec)
        {
            var go = new GameObject(spec.Team == Team.Player ? "PlayerProjectile" : "EnemyProjectile");
            var projectile = go.AddComponent<Projectile>();
            projectile.Init(spec);
            return projectile;
        }

        private void Init(Spec newSpec)
        {
            spec = newSpec;
            spec.Direction.y = 0f;
            spec.Direction = spec.Direction.sqrMagnitude > 0.001f ? spec.Direction.normalized : Vector3.forward;
            pierceLeft = spec.Pierce;

            transform.SetPositionAndRotation(spec.Position, Quaternion.LookRotation(spec.Direction));

            // Sprite lying on the ground plane, pointing along +Z.
            var visual = new GameObject("Sprite");
            visual.transform.SetParent(transform, false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = spec.Sprite;
            renderer.color = spec.Color;
            renderer.sharedMaterial = VelaSettings.UnlitMaterial;
            if (spec.Sprite != null)
            {
                var length = Mathf.Max(0.1f, spec.SpriteLength);
                var scale = length / Mathf.Max(0.01f, spec.Sprite.bounds.size.x);
                visual.transform.localScale = Vector3.one * scale;
            }

            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = VelaSettings.AdditiveMaterial;
            trail.time = 0.12f;
            trail.minVertexDistance = 0.1f;
            trail.widthCurve = AnimationCurve.Linear(0f, spec.Radius * 1.4f, 1f, 0f);
            var faded = spec.Color;
            faded.a = 0f;
            trail.startColor = spec.Color;
            trail.endColor = faded;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.alignment = LineAlignment.View;
        }

        private void Update()
        {
            var step = spec.Speed * Time.deltaTime;
            var count = Physics.SphereCastNonAlloc(transform.position, spec.Radius, spec.Direction, Hits, step,
                ~0, QueryTriggerInteraction.Ignore);

            // Resolve hits nearest-first.
            System.Array.Sort(Hits, 0, count, HitDistanceComparer.Instance);

            for (var i = 0; i < count; i++)
            {
                var hit = Hits[i];
                var health = hit.collider.GetComponentInParent<Health>();

                if (health != null)
                {
                    if (health.Team == spec.Team || hitAlready.Contains(health)) continue;
                    if (!health.IsAlive) continue;

                    hitAlready.Add(health);
                    var landed = spec.MakeHit != null && health.ApplyDamage(spec.MakeHit(health));

                    // Dodged (i-frames): the projectile flies on through.
                    if (!landed && health.IsInvulnerable) continue;
                    if (landed) spec.OnLanded?.Invoke(health);
                    if (pierceLeft-- <= 0)
                    {
                        Stop(hit.point);
                        return;
                    }
                    continue;
                }

                if (hit.collider.GetComponentInParent<Projectile>() != null) continue;
                if (hit.collider.GetComponent<Vela.World.ProjectilePassThrough>() != null) continue;

                // Scenery.
                FxManager.HitSpark(hit.point, -spec.Direction, spec.Color, 5);
                Stop(hit.point);
                return;
            }

            transform.position += spec.Direction * step;
            travelled += step;
            if (travelled >= spec.Range) Stop(transform.position);
        }

        private void Stop(Vector3 at)
        {
            if (trail != null) trail.emitting = false;
            Destroy(gameObject);
        }

        private class HitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
