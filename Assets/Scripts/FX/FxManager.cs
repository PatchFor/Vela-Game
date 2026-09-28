using System.Collections.Generic;
using UnityEngine;
using Vela.Gameplay;

namespace Vela.FX
{
    /// Every mock effect in one place: hit sparks, dust, death bursts, slashes, rings and dash
    /// afterimages. Particles are square (untextured), which reads as pixel debris.
    /// Replace any method body with your own VFX later; callers only use these entry points.
    public class FxManager : MonoBehaviour
    {
        private static FxManager instance;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

        private ParticleSystem sparks;
        private ParticleSystem dust;
        private readonly List<SlashArc> slashes = new List<SlashArc>();
        private readonly List<RingPulse> rings = new List<RingPulse>();
        private readonly List<AfterImage> afterImages = new List<AfterImage>();

        public static Material SlashMaterial => VelaSettings.AdditiveMaterial;

        private static FxManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("[FX]").AddComponent<FxManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            sparks = CreateSystem("Sparks", VelaSettings.AdditiveMaterial, 1.4f);
            dust = CreateSystem("Dust", VelaSettings.UnlitMaterial, -0.15f);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void SetColor(Renderer renderer, MaterialPropertyBlock block, Color color)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            // Legacy additive doubles _TintColor, so half of it is "neutral".
            block.SetColor(TintColorId, color * 0.5f);
            renderer.SetPropertyBlock(block);
        }

        // ------------------------------------------------------------------ particles

        public static void HitSpark(Vector3 position, Vector3 direction, Color color, int count)
        {
            var fx = Instance;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = Random.onUnitSphere;
            direction.Normalize();

            for (var i = 0; i < count; i++)
            {
                var spread = Random.insideUnitSphere * 0.9f;
                var velocity = (direction + spread + Vector3.up * Random.Range(0.2f, 0.9f)).normalized
                               * Random.Range(5f, 11f);
                fx.Emit(fx.sparks, position, velocity, color, Random.Range(0.07f, 0.16f), Random.Range(0.12f, 0.3f));
            }
        }

        public static void Dust(Vector3 position, int count, Color color)
        {
            var fx = Instance;
            for (var i = 0; i < count; i++)
            {
                var flat = Random.insideUnitCircle;
                var velocity = new Vector3(flat.x, Random.Range(0.3f, 1f), flat.y) * Random.Range(0.8f, 2.2f);
                var jitter = new Vector3(flat.x, 0f, flat.y) * 0.25f;
                fx.Emit(fx.dust, position + jitter + Vector3.up * 0.1f, velocity, color,
                    Random.Range(0.12f, 0.26f), Random.Range(0.25f, 0.5f));
            }
        }

        public static void DeathBurst(Vector3 position, Color color, int count)
        {
            var fx = Instance;
            for (var i = 0; i < count; i++)
            {
                var velocity = Random.onUnitSphere * Random.Range(3f, 8f);
                velocity.y = Mathf.Abs(velocity.y) + 1.5f;
                fx.Emit(fx.sparks, position, velocity, color, Random.Range(0.1f, 0.25f), Random.Range(0.3f, 0.7f));
            }

            Dust(position, count / 2, new Color(0.8f, 0.8f, 0.8f, 0.8f));
            Ring(position, 0.2f, 2.2f, 0.35f, color);
        }

        private void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, Color color, float size, float lifetime)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startColor = color,
                startSize = size,
                startLifetime = lifetime,
                applyShapeToPosition = false
            };
            system.Emit(emit, 1);
        }

        private ParticleSystem CreateSystem(string systemName, Material material, float gravity)
        {
            var go = new GameObject(systemName);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.maxParticles = 2000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.startSpeed = 0f;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.enabled = false;

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f;
            limit.dampen = 0f;
            limit.drag = 3f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Play();
            return system;
        }

        // ------------------------------------------------------------------ meshes

        /// Crescent sweep. `origin` is the attacker's feet; the arc is drawn around it.
        public static void Slash(Vector3 origin, Vector3 direction, float radius, float width, float arcDegrees,
            Color color, float duration, bool reverse)
        {
            if (!VelaSettings.Feel.showSlashes) return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;

            var slash = Instance.Take(Instance.slashes, "Slash");
            slash.transform.SetPositionAndRotation(origin + Vector3.up * 0.7f, Quaternion.LookRotation(direction));
            slash.Play(radius, width, arcDegrees, color, duration, reverse);
        }

        public static void Ring(Vector3 position, float fromRadius, float toRadius, float duration, Color color)
        {
            var ring = Instance.Take(Instance.rings, "Ring");
            ring.transform.SetPositionAndRotation(new Vector3(position.x, position.y + 0.06f, position.z), Quaternion.identity);
            ring.Play(fromRadius, toRadius, duration, color);
        }

        /// Solid-color ghost of a sprite (dash trail).
        public static void AfterImageOf(SpriteRenderer source, Color color, float lifetime)
        {
            if (source == null || source.sprite == null) return;

            var ghost = Instance.Take(Instance.afterImages, "AfterImage");
            ghost.Play(source, color, lifetime);
        }

        private T Take<T>(List<T> pool, string itemName) where T : Component
        {
            foreach (var item in pool)
            {
                if (!item.gameObject.activeSelf) return item;
            }

            var go = new GameObject(itemName);
            go.transform.SetParent(transform, false);
            var created = go.AddComponent<T>();
            pool.Add(created);
            return created;
        }
    }
}
