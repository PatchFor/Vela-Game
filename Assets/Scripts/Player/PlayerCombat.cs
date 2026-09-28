using System.Collections.Generic;
using UnityEngine;
using Vela.Combat;
using Vela.Config;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;

namespace Vela.Player
{
    /// Weapon handling. Left click runs the weapon's combo; hold right click to charge its
    /// heavy attack and release when full. 1/2/3 (or Tab) switch weapons.
    /// Every number comes from the WeaponConfig asset for that weapon.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Charging,
            Windup,
            Active,
            Recovery
        }

        private PlayerController player;
        private WeaponConfig[] weapons = new WeaponConfig[0];
        private int weaponIndex;

        private Phase phase;
        private float phaseTimer;
        private AttackStep step;
        private bool stepIsCharged;
        private int comboIndex;
        private float lastStepEndedAt = -10f;
        private float bufferedAt = -10f;
        private Vector3 attackDirection;
        private bool swingFlip;
        private readonly HashSet<Health> hitThisStep = new HashSet<Health>();

        private float chargeTimer;
        private bool chargeReadyAnnounced;
        private Telegraph chargeRing;

        public WeaponConfig CurrentWeapon => weapons.Length > 0 ? weapons[weaponIndex] : null;
        public int WeaponIndex => weaponIndex;
        public IReadOnlyList<WeaponConfig> Weapons => weapons;
        public int ComboIndex => comboIndex;

        /// True while any part of an attack (or a charge) is running.
        public bool IsBusy => phase != Phase.Idle;

        public bool IsCharging => phase == Phase.Charging;

        public float ChargeNormalized =>
            phase == Phase.Charging && CurrentWeapon != null
                ? Mathf.Clamp01(chargeTimer / Mathf.Max(0.01f, CurrentWeapon.chargeTime))
                : 0f;

        public bool HasSuperArmor => step != null && step.superArmor && (phase == Phase.Windup || phase == Phase.Active);

        public bool CanDashNow
        {
            get
            {
                if (phase == Phase.Idle || phase == Phase.Charging) return true;
                return phase == Phase.Recovery && CurrentWeapon != null && CurrentWeapon.canDashCancel;
            }
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            var config = player.Config;
            weapons = config != null && config.weapons != null ? config.weapons : new WeaponConfig[0];
            weapons = System.Array.FindAll(weapons, w => w != null);
            weaponIndex = config != null ? Mathf.Clamp(config.startingWeapon, 0, Mathf.Max(0, weapons.Length - 1)) : 0;
            player.Dashed += OnDashed;
        }

        private void OnDestroy()
        {
            if (player != null) player.Dashed -= OnDashed;
            ReleaseChargeRing();
        }

        public void Interrupt()
        {
            if (HasSuperArmor) return;
            EndAttack(resetCombo: true);
        }

        private void OnDashed()
        {
            // Dashing cancels charging and recovery.
            EndAttack(resetCombo: false);
        }

        private void Update()
        {
            player.MoveSpeedMultiplier = 1f;
            if (!player.IsAlive || CurrentWeapon == null)
            {
                if (phase != Phase.Idle) EndAttack(true);
                return;
            }

            HandleWeaponSwitch();

            var weapon = CurrentWeapon;
            if (VelaInput.AttackPressed) bufferedAt = Time.time;
            var buffered = Time.time - bufferedAt <= weapon.inputBufferTime;

            if (player.IsDashing || player.IsStunned) return;

            phaseTimer -= Time.deltaTime;

            switch (phase)
            {
                case Phase.Idle:
                    if (weapon.hasChargedAttack && VelaInput.SpecialHeld)
                    {
                        BeginCharge();
                    }
                    else if (weapon.combo != null && weapon.combo.Length > 0 &&
                             (buffered || (weapon.repeatWhileHeld && VelaInput.AttackHeld)))
                    {
                        if (Time.time - lastStepEndedAt > weapon.comboResetTime) comboIndex = 0;
                        StartStep(weapon.combo[comboIndex % weapon.combo.Length], false);
                    }
                    break;

                case Phase.Charging:
                    UpdateCharge(weapon);
                    break;

                case Phase.Windup:
                    player.MoveSpeedMultiplier = step.moveSpeedMultiplier;
                    attackDirection = player.AimDirection;
                    player.Face(attackDirection);
                    if (phaseTimer <= 0f) BeginActive();
                    break;

                case Phase.Active:
                    player.MoveSpeedMultiplier = step.moveSpeedMultiplier;
                    if (step.kind == AttackKind.MeleeArc) SweepMelee();
                    if (phaseTimer <= 0f)
                    {
                        phase = Phase.Recovery;
                        phaseTimer = step.recovery;
                    }
                    break;

                case Phase.Recovery:
                    player.MoveSpeedMultiplier = Mathf.Lerp(1f, step.moveSpeedMultiplier, 0.5f);

                    // Chain into the next combo step as soon as the player asks.
                    var hasNext = !stepIsCharged && comboIndex + 1 < weapon.combo.Length;
                    var wantsNext = buffered || (weapon.repeatWhileHeld && VelaInput.AttackHeld);
                    if (hasNext && wantsNext)
                    {
                        comboIndex++;
                        StartStep(weapon.combo[comboIndex], false);
                        break;
                    }

                    if (phaseTimer <= 0f)
                    {
                        var finishedCombo = stepIsCharged || comboIndex + 1 >= weapon.combo.Length;
                        EndAttack(resetCombo: false);
                        comboIndex = finishedCombo ? 0 : comboIndex + 1;
                    }
                    break;
            }
        }

        private void HandleWeaponSwitch()
        {
            var slot = VelaInput.WeaponSlotPressed;
            if (VelaInput.CycleWeaponPressed && weapons.Length > 0) slot = (weaponIndex + 1) % weapons.Length;
            if (slot < 0 || slot >= weapons.Length || slot == weaponIndex) return;

            weaponIndex = slot;
            comboIndex = 0;
            EndAttack(resetCombo: true);

            var weapon = CurrentWeapon;
            DamageNumbers.Spawn(transform.position + Vector3.up * 2.4f, weapon.displayName, weapon.uiColor,
                VelaSettings.Feel.fontSize);
            FxManager.Ring(transform.position, 0.3f, 1.4f, 0.25f, weapon.uiColor);
        }

        // ------------------------------------------------------------------ charge

        private void BeginCharge()
        {
            phase = Phase.Charging;
            chargeTimer = 0f;
            chargeReadyAnnounced = false;
            ReleaseChargeRing();
            chargeRing = Telegraph.Create(TelegraphShape.Circle, transform.position, Vector3.forward, 1.1f, 0f,
                CurrentWeapon.uiColor * new Color(1f, 1f, 1f, 0.6f));
        }

        private void UpdateCharge(WeaponConfig weapon)
        {
            player.MoveSpeedMultiplier = weapon.chargeMoveSpeedMultiplier;
            chargeTimer += Time.deltaTime;
            player.Face(player.AimDirection);

            var progress = ChargeNormalized;
            if (chargeRing != null)
            {
                chargeRing.SetPose(transform.position, Vector3.forward);
                chargeRing.SetProgress(progress);
            }

            if (progress >= 1f && !chargeReadyAnnounced)
            {
                chargeReadyAnnounced = true;
                FxManager.Ring(transform.position, 0.4f, 1.8f, 0.2f, Color.white);
                if (player.Billboard != null) player.Billboard.Flash(weapon.uiColor, 0.08f);
            }

            if (VelaInput.SpecialHeld) return;

            ReleaseChargeRing();
            if (progress >= 1f)
            {
                StartStep(weapon.chargedAttack, true);
            }
            else
            {
                phase = Phase.Idle;
            }
        }

        private void ReleaseChargeRing()
        {
            if (chargeRing == null) return;
            chargeRing.Release();
            chargeRing = null;
        }

        // ------------------------------------------------------------------ attack steps

        private void StartStep(AttackStep next, bool charged)
        {
            step = next;
            stepIsCharged = charged;
            bufferedAt = -10f;
            hitThisStep.Clear();
            phase = Phase.Windup;
            phaseTimer = step.windup;
            attackDirection = player.AimDirection;
            player.Face(attackDirection);
            swingFlip = !swingFlip;

            if (player.Billboard != null) player.Billboard.Punch(new Vector2(0.85f, 1.15f));
            if (phaseTimer <= 0f) BeginActive();
        }

        private void BeginActive()
        {
            phase = Phase.Active;
            phaseTimer = step.active;
            attackDirection = player.AimDirection;
            player.Face(attackDirection);

            if (player.Billboard != null) player.Billboard.Punch(new Vector2(1.2f, 0.85f));

            if (step.kind == AttackKind.MeleeArc)
            {
                if (step.lungeDistance > 0f)
                {
                    var duration = Mathf.Max(0.05f, step.active);
                    player.ApplyImpulse(attackDirection * (step.lungeDistance / duration), duration);
                }

                var reverse = step.reverseSwing ^ (swingFlip && !stepIsCharged);
                FxManager.Slash(transform.position, attackDirection, step.range, step.slashWidth, step.arcDegrees,
                    step.slashColor, step.slashDuration, reverse);
                SweepMelee();
            }
            else
            {
                FireProjectiles();
            }
        }

        private void SweepMelee()
        {
            var weapon = CurrentWeapon;
            var s = step;
            CombatUtility.MeleeArc(transform.position, attackDirection, s.range, s.arcDegrees, Team.Player,
                hitThisStep, victim => BuildHit(victim, weapon, s));
        }

        private void FireProjectiles()
        {
            var weapon = CurrentWeapon;
            var s = step;
            var count = Mathf.Max(1, s.projectileCount);
            var fx = VelaSettings.Fx;

            for (var i = 0; i < count; i++)
            {
                var offset = count == 1 ? 0f : Mathf.Lerp(-s.spreadDegrees * 0.5f, s.spreadDegrees * 0.5f, i / (count - 1f));
                var direction = CombatUtility.Rotate(attackDirection, offset);

                Projectile.Fire(new Projectile.Spec
                {
                    Team = Team.Player,
                    Source = gameObject,
                    Position = transform.position + Vector3.up * 1f + direction * 0.5f,
                    Direction = direction,
                    Speed = s.projectileSpeed,
                    Range = s.projectileRange,
                    Radius = s.projectileSize,
                    Pierce = s.pierce,
                    Color = s.projectileColor,
                    Sprite = fx.arrowSprite,
                    SpriteLength = 0.9f + s.projectileSize,
                    MakeHit = victim => BuildHit(victim, weapon, s)
                });
            }

            FxManager.HitSpark(transform.position + Vector3.up + attackDirection * 0.6f, attackDirection,
                s.projectileColor, 4);
            player.ApplyImpulse(-attackDirection * 2f, 0.06f);
        }

        private DamageInfo BuildHit(Health victim, WeaponConfig weapon, AttackStep s)
        {
            var crit = Random.value < weapon.critChance;
            var amount = CombatUtility.RollDamage(s.damage, s.damageVariance);
            if (crit) amount = Mathf.RoundToInt(amount * weapon.critMultiplier);

            return CombatUtility.MakeHit(gameObject, Team.Player, victim, amount, crit,
                s.knockback, s.stagger, s.hitStop, s.cameraShake);
        }

        private void EndAttack(bool resetCombo)
        {
            if (phase != Phase.Idle && phase != Phase.Charging) lastStepEndedAt = Time.time;
            phase = Phase.Idle;
            phaseTimer = 0f;
            ReleaseChargeRing();
            if (resetCombo) comboIndex = 0;
        }
    }
}
