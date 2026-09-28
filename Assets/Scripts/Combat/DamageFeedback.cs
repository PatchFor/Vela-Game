using UnityEngine;
using Vela.CameraRig;
using Vela.Config;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Visual;

namespace Vela.Combat
{
    /// Turns a Health hit into feel. Same component on the player and on every monster.
    /// The strength of every cue comes from the hit's weight (its ImpactProfile in
    /// CombatFeelConfig), with crits layered on top:
    ///   victim   white flash → red tint fade, squash, tremble during the freeze
    ///   world    sparks, impact ring and dust (heavy+), crit burst
    ///   camera   hit-stop, shake, zoom punch (heavy+ / crit), crit slow-mo
    ///   UI       damage number sized by weight, combo counter
    [RequireComponent(typeof(Health))]
    public class DamageFeedback : MonoBehaviour
    {
        private Health health;
        private SpriteBillboard billboard;

        public static event System.Action<Health, DamageInfo> PlayerHurt;

        private void Awake()
        {
            health = GetComponent<Health>();
            billboard = GetComponent<SpriteBillboard>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Healed += OnHealed;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Healed -= OnHealed;
        }

        private float TopOfHead =>
            billboard != null ? billboard.Visual.worldHeight + billboard.Visual.hoverHeight : 1.8f;

        private void OnDamaged(Health victim, DamageInfo info)
        {
            if (victim.Team == Team.Player) PlayerFeedback(info);
            else EnemyFeedback(info);
        }

        private void EnemyFeedback(DamageInfo info)
        {
            var feel = VelaSettings.Feel;
            var profile = feel.Profile(info.Weight);
            var crit = info.IsCrit;
            var critBoost = crit ? 1.3f : 1f;

            // Victim reaction
            if (billboard != null)
            {
                billboard.Flash(feel.hitFlashColor, feel.hitFlashDuration);
                billboard.HurtTint(feel.hurtTint, feel.hurtTintDuration);
                var s = profile.squash * critBoost;
                billboard.Punch(new Vector2(1f + s, 1f - s));
                billboard.Tremble(profile.trembleAmount * critBoost, profile.trembleDuration * critBoost);
            }

            // Number
            var numberColor = crit ? feel.critColor : profile.numberColor;
            var numberScale = profile.numberScale * (crit ? feel.critNumberScale : 1f);
            DamageNumbers.SpawnDamage(transform.position + Vector3.up * TopOfHead, info.Amount, numberColor,
                numberScale, crit, GetInstanceID());

            // Particles
            var sparkColor = crit ? feel.critColor : feel.hitSparkColor;
            var sparks = Mathf.RoundToInt(feel.hitSparkCount * profile.sparkMultiplier) + (crit ? feel.critSparkBonus : 0);
            FxManager.HitSpark(info.HitPoint, info.Direction, sparkColor, sparks);

            var ground = new Vector3(info.HitPoint.x, transform.position.y, info.HitPoint.z);
            if (profile.impactRing) FxManager.Ring(ground, 0.2f, profile.impactRingRadius, 0.22f, sparkColor);
            if (profile.dustCount > 0) FxManager.Dust(transform.position, profile.dustCount, new Color(0.85f, 0.82f, 0.75f, 0.85f));

            // Time & camera
            var hitStop = info.HitStop * profile.hitStopMultiplier + profile.hitStopBonus;
            var shake = info.CameraShake + profile.extraShake;
            var zoom = profile.zoomPunch;

            if (crit)
            {
                hitStop += feel.critHitStopBonus;
                shake += feel.critExtraShake;
                zoom = Mathf.Max(zoom, feel.critZoomPunch);
                FxManager.CritBurst(info.HitPoint, feel.critColor);
            }

            // Callouts for the two "reward" hits.
            var labelY = TopOfHead + 0.6f;
            if (info.IsCounter)
            {
                DamageNumbers.Spawn(transform.position + Vector3.up * labelY, feel.counterLabel, feel.counterColor, 1.1f, GetInstanceID());
            }
            else if (info.IsPunish)
            {
                DamageNumbers.Spawn(transform.position + Vector3.up * labelY, feel.punishLabel, feel.punishColor, 1.05f, GetInstanceID());
                Audio.Sfx.Play(Audio.SfxEvent.Punish, transform.position);
            }

            Audio.Sfx.Play(HitSound(info.Weight), transform.position);
            if (crit) Audio.Sfx.Play(Audio.SfxEvent.Crit, transform.position);

            // Freeze first, then (for crits) the slow-mo tail starts when the freeze ends.
            // In LocalVisual (online-safe) mode only the attacker and victim hold their pose.
            HitStop.Request(hitStop * feel.hitStopScale, billboard, AttackerBillboard(info));
            if (crit) HitStop.SlowMotion(feel.critSlowMoDuration, feel.critSlowMoScale);
            CameraShake.Add(shake);
            CameraShake.Punch(zoom);

            if (info.SourceTeam == Team.Player) ComboTracker.RegisterHit(crit);
        }

        private void PlayerFeedback(DamageInfo info)
        {
            var feel = VelaSettings.Feel;
            var profile = feel.Profile(info.Weight);

            if (billboard != null)
            {
                billboard.Flash(feel.hitFlashColor, feel.hitFlashDuration * 1.3f);
                billboard.HurtTint(new Color(1f, 0.3f, 0.3f), feel.hurtTintDuration * 1.5f);
                var s = Mathf.Max(0.25f, profile.squash);
                billboard.Punch(new Vector2(1f + s, 1f - s));
                billboard.Tremble(Mathf.Max(0.08f, profile.trembleAmount), Mathf.Max(0.15f, profile.trembleDuration));
            }

            DamageNumbers.SpawnDamage(transform.position + Vector3.up * TopOfHead, info.Amount, feel.takenColor,
                Mathf.Max(1.1f, profile.numberScale), false, GetInstanceID());

            FxManager.HitSpark(info.HitPoint, info.Direction, feel.takenColor,
                Mathf.RoundToInt(feel.hitSparkCount * Mathf.Max(1f, profile.sparkMultiplier)));
            if (profile.impactRing) FxManager.Ring(transform.position, 0.2f, profile.impactRingRadius, 0.22f, feel.takenColor);

            Audio.Sfx.Play(Audio.SfxEvent.PlayerHurt, transform.position);
            HitStop.Request(feel.playerHurtHitStop * profile.hitStopMultiplier * feel.hitStopScale, billboard,
                AttackerBillboard(info));
            CameraShake.Add(feel.playerHurtShake + profile.extraShake);
            CameraShake.Punch(Mathf.Max(feel.playerHurtZoomPunch, profile.zoomPunch));

            ComboTracker.Break();
            PlayerHurt?.Invoke(health, info);
        }

        private static SpriteBillboard AttackerBillboard(DamageInfo info) =>
            info.Source != null ? info.Source.GetComponent<SpriteBillboard>() : null;

        private static Audio.SfxEvent HitSound(HitWeight weight) => weight switch
        {
            HitWeight.Medium => Audio.SfxEvent.HitMedium,
            HitWeight.Heavy => Audio.SfxEvent.HitHeavy,
            HitWeight.Finisher => Audio.SfxEvent.HitFinisher,
            _ => Audio.SfxEvent.HitLight
        };

        private void OnHealed(Health target, int amount)
        {
            var feel = VelaSettings.Feel;
            DamageNumbers.Spawn(transform.position + Vector3.up * TopOfHead, "+" + amount, feel.healColor, 0.9f);
        }
    }
}
