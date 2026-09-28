using System.Collections;
using UnityEngine;
using Vela.CameraRig;
using Vela.Combat;
using Vela.Config;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Visual;

namespace Vela.Enemies
{
    /// Phase changes for a boss. One HP bar across all phases; when HP crosses a phase's
    /// threshold, the boss roars (invulnerable, telegraphed shockwave), changes look, may
    /// summon adds, then fights with that phase's behaviour.
    [RequireComponent(typeof(EnemyBrain))]
    public class BossController : MonoBehaviour
    {
        public static BossController Current { get; private set; }

        private BossConfig config;
        private EnemyBrain brain;
        private Health health;
        private SpriteBillboard billboard;
        private int phaseIndex;

        public BossConfig Config => config;
        public Health Health => health;
        public int PhaseIndex => phaseIndex;
        public int PhaseCount => config != null ? config.phases.Length : 0;
        public string PhaseName => config != null && phaseIndex < config.phases.Length ? config.phases[phaseIndex].name : "";
        public bool IsEngaged => brain != null && (brain.IsEngaged || phaseIndex > 0);
        public bool IsDead => health == null || !health.IsAlive;
        public string Announcement { get; private set; } = "";
        public float AnnouncementTime { get; private set; } = -10f;

        public void Initialize(BossConfig newConfig, EnemyBrain newBrain)
        {
            config = newConfig;
            brain = newBrain;
            health = GetComponent<Health>();
            billboard = GetComponent<SpriteBillboard>();
            phaseIndex = 0;

            health.Changed += OnHealthChanged;
            Current = this;
            ApplyLook(0);
        }

        private void OnDestroy()
        {
            if (health != null) health.Changed -= OnHealthChanged;
            if (Current == this) Current = null;
        }

        public float PhaseThreshold(int index) =>
            config != null && index < config.phases.Length ? config.phases[index].startsAtHealthFraction : 0f;

        private void OnHealthChanged(Health h)
        {
            if (!h.IsAlive || config == null) return;

            var next = phaseIndex + 1;
            if (next < config.phases.Length && h.Normalized <= config.phases[next].startsAtHealthFraction)
            {
                StartCoroutine(EnterPhase(next));
            }
        }

        private IEnumerator EnterPhase(int index)
        {
            phaseIndex = index;
            var phase = config.phases[index];

            brain.BeginTransition(phase.transitionDuration);
            health.GrantInvulnerability(phase.transitionDuration);
            brain.SetBehaviour(phase.behaviour);

            Announcement = string.IsNullOrEmpty(phase.announcement) ? phase.name : phase.announcement;
            AnnouncementTime = Time.time;

            Telegraph warning = null;
            if (phase.shockwave)
            {
                warning = Telegraph.Create(TelegraphShape.Circle, transform.position, Vector3.forward,
                    phase.shockwaveRadius, 0f, new Color(1f, 0.2f, 0.2f, 0.55f));
            }

            // Roar: shake, flash, grow into the new look.
            for (var t = 0f; t < phase.transitionDuration; t += Time.deltaTime)
            {
                var progress = t / Mathf.Max(0.01f, phase.transitionDuration);
                CameraShake.Add(1.2f * Time.deltaTime);
                if (warning != null) warning.SetProgress(progress);
                if (billboard != null)
                {
                    billboard.SetBaseTint(Color.Lerp(PreviousTint(index), phase.tint, progress));
                    billboard.SetScaleMultiplier(Mathf.Lerp(PreviousScale(index), phase.spriteScale, progress));
                    if (Mathf.Repeat(t, 0.25f) < Time.deltaTime) billboard.Flash(phase.tint, 0.06f);
                }
                yield return null;
            }

            if (warning != null) warning.Release();
            ApplyLook(index);

            if (phase.shockwave)
            {
                var hits = new System.Collections.Generic.HashSet<Health>();
                CombatUtility.Circle(transform.position, phase.shockwaveRadius, Team.Enemy, hits, victim =>
                    CombatUtility.MakeHit(gameObject, Team.Enemy, victim,
                        Mathf.RoundToInt(phase.shockwaveDamage * VelaSettings.Feel.enemyDamageScale), false,
                        phase.shockwaveKnockback, 0f, 0f, 0f));
                FxManager.Ring(transform.position, 0.5f, phase.shockwaveRadius * 1.1f, 0.45f, phase.tint);
                FxManager.Ring(transform.position, 0.2f, phase.shockwaveRadius * 0.7f, 0.35f, Color.white);
                FxManager.Dust(transform.position, 24, new Color(0.7f, 0.65f, 0.6f, 0.9f));
                CameraShake.Add(0.6f);
                HitStop.Request(0.08f);
            }

            foreach (var summon in phase.summonOnEnter)
            {
                brain.Summon(summon, 1);
            }
        }

        private Color PreviousTint(int index) => index > 0 ? config.phases[index - 1].tint : Color.white;

        private float PreviousScale(int index) => index > 0 ? config.phases[index - 1].spriteScale : 1f;

        private void ApplyLook(int index)
        {
            if (billboard == null || config.phases.Length == 0) return;
            var phase = config.phases[Mathf.Clamp(index, 0, config.phases.Length - 1)];
            billboard.SetBaseTint(phase.tint);
            billboard.SetScaleMultiplier(phase.spriteScale);
        }
    }
}
