using UnityEngine;
using Vela.CameraRig;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Visual;

namespace Vela.Combat
{
    /// Turns a Health hit into feel: white flash, squash, sparks, floating number, hit-stop
    /// and camera shake. Same component on the player and on every monster.
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

        private void OnDamaged(Health victim, DamageInfo info)
        {
            var feel = VelaSettings.Feel;
            var isPlayer = victim.Team == Team.Player;

            if (billboard != null)
            {
                billboard.Flash(feel.hitFlashColor, feel.hitFlashDuration);
                var s = feel.hitSquash * (info.IsCrit ? 1.4f : 1f);
                billboard.Punch(new Vector2(1f + s, 1f - s));
            }

            var height = billboard != null ? billboard.Visual.worldHeight + billboard.Visual.hoverHeight : 1.8f;
            var numberPos = transform.position + Vector3.up * height;
            DamageNumbers.Spawn(numberPos, info.Amount, info.IsCrit, isPlayer);

            var sparkColor = isPlayer ? feel.takenColor : info.IsCrit ? feel.critColor : feel.hitSparkColor;
            FxManager.HitSpark(info.HitPoint, info.Direction, sparkColor,
                Mathf.RoundToInt(feel.hitSparkCount * (info.IsCrit ? 1.6f : 1f)));

            if (isPlayer)
            {
                HitStop.Request(feel.playerHurtHitStop * feel.hitStopScale);
                CameraShake.Add(feel.playerHurtShake);
                PlayerHurt?.Invoke(victim, info);
            }
            else
            {
                HitStop.Request(info.HitStop * feel.hitStopScale * (info.IsCrit ? 1.3f : 1f));
                CameraShake.Add(info.CameraShake * (info.IsCrit ? 1.3f : 1f));
            }
        }

        private void OnHealed(Health target, int amount)
        {
            var feel = VelaSettings.Feel;
            DamageNumbers.Spawn(transform.position + Vector3.up * 2f, "+" + amount, feel.healColor, feel.fontSize);
        }
    }
}
