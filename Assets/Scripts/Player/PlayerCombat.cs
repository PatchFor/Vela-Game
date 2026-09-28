using System.Collections.Generic;
using UnityEngine;
using Vela.CameraRig;
using Vela.Combat;
using Vela.Config;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Items;
using Vela.Visual;

namespace Vela.Player
{
    /// Attacks, charge and skills.
    ///  - Basic attack (J, or a mouse button bound to it) runs the weapon's combo.
    ///  - Charged attack (hold K, or a bound mouse button) charges the weapon's heavy attack.
    ///  - Skills on 1–4 (or bound mouse buttons), each with its own cooldown.
    ///  - Tab cycles weapons.
    /// Aim: locked target first; otherwise toward the mouse pointer when the attack came from
    /// the mouse, or the facing direction when it came from the keyboard. Melee then gets a
    /// soft aim assist toward the nearest monster in the cone.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        public const int SkillSlots = 4;

        private enum Phase
        {
            Idle,
            Charging,
            Windup,
            Active,
            Recovery
        }

        private PlayerController player;
        private PlayerTargeting targeting;
        private PaperDoll paperDoll;
        private WeaponConfig[] weapons = new WeaponConfig[0];
        private SkillConfig[] skills = new SkillConfig[SkillSlots];
        private readonly float[] skillReadyAt = new float[SkillSlots];
        private int weaponIndex;

        private Phase phase;
        private float phaseTimer;
        private AttackStep step;
        private bool stepIsFinal;
        private float stepCritChance;
        private float stepCritMultiplier;
        private Color stepColor = Color.white;
        private bool aimFromMouse;
        private int comboIndex;
        private float lastStepEndedAt = -10f;
        private float bufferedAt = -10f;
        private bool bufferedFromMouse;
        private Vector3 attackDirection;
        private bool swingFlip;
        private readonly HashSet<Health> hitThisStep = new HashSet<Health>();

        private float chargeTimer;
        private bool chargeReadyAnnounced;
        private bool chargeFromMouse;
        private Telegraph chargeRing;

        // Per-frame input, resolved from keys + mouse bindings.
        private bool basicPressed;
        private bool basicPressedByMouse;
        private bool basicHeld;
        private bool chargeHeld;
        private bool chargeHeldByMouse;
        private readonly bool[] skillPressed = new bool[SkillSlots];
        private readonly bool[] skillPressedByMouse = new bool[SkillSlots];

        public MouseAction LeftMouse { get; set; } = MouseAction.BasicAttack;
        public MouseAction RightMouse { get; set; } = MouseAction.ChargedAttack;

        public WeaponConfig CurrentWeapon => weapons.Length > 0 ? weapons[weaponIndex] : null;
        public int WeaponIndex => weaponIndex;
        public IReadOnlyList<WeaponConfig> Weapons => weapons;
        public IReadOnlyList<SkillConfig> Skills => skills;
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

        /// 0 = ready, 1 = just used.
        public float SkillCooldownNormalized(int index)
        {
            var skill = index >= 0 && index < skills.Length ? skills[index] : null;
            if (skill == null || skill.cooldown <= 0f) return 0f;
            return Mathf.Clamp01((skillReadyAt[index] - Time.time) / skill.cooldown);
        }

        public float SkillCooldownRemaining(int index) =>
            index >= 0 && index < SkillSlots ? Mathf.Max(0f, skillReadyAt[index] - Time.time) : 0f;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            targeting = GetComponent<PlayerTargeting>();
            paperDoll = GetComponent<PaperDoll>();
        }

        private void Start()
        {
            var config = player.Config;
            weapons = config != null && config.weapons != null ? config.weapons : new WeaponConfig[0];
            weapons = System.Array.FindAll(weapons, w => w != null);
            weaponIndex = config != null ? Mathf.Clamp(config.startingWeapon, 0, Mathf.Max(0, weapons.Length - 1)) : 0;

            if (config != null)
            {
                for (var i = 0; i < SkillSlots; i++)
                {
                    skills[i] = config.skills != null && i < config.skills.Length ? config.skills[i] : null;
                }
                LeftMouse = config.leftMouse;
                RightMouse = config.rightMouse;
            }

            player.Dashed += OnDashed;
            ApplyWeaponVisual();
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

        // ------------------------------------------------------------------ input

        private void ReadInput()
        {
            basicPressed = VelaInput.AttackKeyPressed;
            basicPressedByMouse = false;
            basicHeld = VelaInput.AttackKeyHeld;
            chargeHeld = VelaInput.ChargeKeyHeld;
            chargeHeldByMouse = false;
            for (var i = 0; i < SkillSlots; i++)
            {
                skillPressed[i] = VelaInput.SkillPressed(i);
                skillPressedByMouse[i] = false;
            }

            ReadMouse(VelaInput.MouseButton.Left, LeftMouse);
            ReadMouse(VelaInput.MouseButton.Right, RightMouse);
        }

        private void ReadMouse(VelaInput.MouseButton button, MouseAction action)
        {
            var down = VelaInput.MouseDown(button);
            var held = VelaInput.MouseHeld(button);

            switch (action)
            {
                case MouseAction.BasicAttack:
                    if (down)
                    {
                        basicPressed = true;
                        basicPressedByMouse = true;
                    }
                    basicHeld |= held;
                    break;
                case MouseAction.ChargedAttack:
                    if (held && !chargeHeld) chargeHeldByMouse = true;
                    chargeHeld |= held;
                    break;
                case MouseAction.Skill1:
                case MouseAction.Skill2:
                case MouseAction.Skill3:
                case MouseAction.Skill4:
                    var index = action - MouseAction.Skill1;
                    if (down)
                    {
                        skillPressed[index] = true;
                        skillPressedByMouse[index] = true;
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            player.MoveSpeedMultiplier = 1f;
            if (!player.IsAlive || CurrentWeapon == null)
            {
                if (phase != Phase.Idle) EndAttack(true);
                return;
            }

            ReadInput();
            HandleWeaponSwitch();

            var weapon = CurrentWeapon;
            if (basicPressed)
            {
                bufferedAt = Time.time;
                bufferedFromMouse = basicPressedByMouse;
            }
            var buffered = Time.time - bufferedAt <= weapon.inputBufferTime;

            if (player.IsDashing || player.IsStunned || player.IsJumping) return;

            // Skills can start from idle, while charging, or cancel an attack's recovery.
            if (phase == Phase.Idle || phase == Phase.Charging || phase == Phase.Recovery)
            {
                for (var i = 0; i < SkillSlots; i++)
                {
                    if (skillPressed[i] && TryStartSkill(i, skillPressedByMouse[i])) return;
                }
            }

            phaseTimer -= Time.deltaTime;

            switch (phase)
            {
                case Phase.Idle:
                    if (weapon.hasChargedAttack && chargeHeld)
                    {
                        BeginCharge(chargeHeldByMouse);
                    }
                    else if (weapon.combo != null && weapon.combo.Length > 0 &&
                             (buffered || (weapon.repeatWhileHeld && basicHeld)))
                    {
                        if (Time.time - lastStepEndedAt > weapon.comboResetTime) comboIndex = 0;
                        StartStep(weapon.combo[comboIndex % weapon.combo.Length], false, weapon.critChance,
                            weapon.critMultiplier, weapon.uiColor, buffered ? bufferedFromMouse : basicHeld && !VelaInput.AttackKeyHeld);
                    }
                    break;

                case Phase.Charging:
                    UpdateCharge(weapon);
                    break;

                case Phase.Windup:
                    player.MoveSpeedMultiplier = step.moveSpeedMultiplier;
                    attackDirection = AimDirection(aimFromMouse, step);
                    player.Face(attackDirection);

                    // Heavy swings glow while winding up: "something big is coming".
                    if (IsHeavy(step) && player.Billboard != null && step.windup > 0f)
                    {
                        var progress = 1f - Mathf.Clamp01(phaseTimer / step.windup);
                        player.Billboard.SetTint(Color.Lerp(Color.white, stepColor * 1.4f, progress * 0.85f));
                    }

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
                    var hasNext = !stepIsFinal && comboIndex + 1 < weapon.combo.Length;
                    var wantsNext = buffered || (weapon.repeatWhileHeld && basicHeld);
                    if (hasNext && wantsNext)
                    {
                        comboIndex++;
                        StartStep(weapon.combo[comboIndex], false, weapon.critChance, weapon.critMultiplier,
                            weapon.uiColor, buffered ? bufferedFromMouse : aimFromMouse);
                        break;
                    }

                    if (phaseTimer <= 0f)
                    {
                        var finishedCombo = stepIsFinal || comboIndex + 1 >= weapon.combo.Length;
                        EndAttack(resetCombo: false);
                        comboIndex = finishedCombo ? 0 : comboIndex + 1;
                    }
                    break;
            }
        }

        private void HandleWeaponSwitch()
        {
            if (!VelaInput.CycleWeaponPressed || weapons.Length < 2) return;

            weaponIndex = (weaponIndex + 1) % weapons.Length;
            comboIndex = 0;
            EndAttack(resetCombo: true);
            ApplyWeaponVisual();

            var weapon = CurrentWeapon;
            DamageNumbers.Spawn(transform.position + Vector3.up * 2.4f, weapon.displayName, weapon.uiColor, 0.9f);
            FxManager.Ring(transform.position, 0.3f, 1.4f, 0.25f, weapon.uiColor);
        }

        private void ApplyWeaponVisual()
        {
            if (paperDoll == null) return;
            var weapon = CurrentWeapon;
            paperDoll.SetEquipment(EquipmentSlot.Weapon, weapon != null ? weapon.visual : null);
        }

        // ------------------------------------------------------------------ aim

        private Vector3 AimDirection(bool fromMouse, AttackStep s)
        {
            if (targeting != null && targeting.DirectionToTarget.HasValue) return targeting.DirectionToTarget.Value;
            return fromMouse ? player.PointerDirection : player.Facing;
        }

        private static bool IsHeavy(AttackStep s)
        {
            var weight = DamageInfo.Resolve(s.hitWeight, s.stagger, s.damage);
            return weight == HitWeight.Heavy || weight == HitWeight.Finisher;
        }

        /// Soft aim assist: swing toward the nearest monster inside a cone around the aim, so near
        /// misses still connect. Skipped while locked on (the lock already aims).
        private Vector3 AssistedDirection(Vector3 aim, float reach)
        {
            var config = player.Config;
            if (config == null || config.meleeAimAssistAngle <= 0f) return aim;
            if (targeting != null && targeting.HasTarget) return aim;

            var best = aim;
            var bestScore = float.MaxValue;
            var maxDistance = reach + config.meleeAimAssistRange;
            foreach (var enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var offset = CombatUtility.Flat(enemy.transform.position - transform.position);
                var distance = offset.magnitude;
                if (distance < 0.01f || distance > maxDistance) continue;

                var angle = Vector3.Angle(aim, offset);
                if (angle > config.meleeAimAssistAngle) continue;

                var score = angle + distance * 4f;
                if (score >= bestScore) continue;
                bestScore = score;
                best = offset / distance;
            }
            return best;
        }

        // ------------------------------------------------------------------ skills

        private bool TryStartSkill(int index, bool fromMouse)
        {
            var skill = skills[index];
            if (skill == null || skill.attack == null) return false;

            if (Time.time < skillReadyAt[index])
            {
                // Tell the player why nothing happened.
                HudMessages.Show($"{skill.displayName}: {SkillCooldownRemaining(index):0.0}s", new Color(1f, 1f, 1f, 0.7f), 0.6f);
                return false;
            }

            ReleaseChargeRing();
            skillReadyAt[index] = Time.time + skill.cooldown;
            StartStep(skill.attack, true, skill.critChance, skill.critMultiplier, skill.color, fromMouse);
            FxManager.Ring(transform.position, 0.2f, 1.6f, 0.2f, skill.color);
            return true;
        }

        // ------------------------------------------------------------------ charge

        private void BeginCharge(bool fromMouse)
        {
            phase = Phase.Charging;
            chargeTimer = 0f;
            chargeReadyAnnounced = false;
            chargeFromMouse = fromMouse;
            ReleaseChargeRing();
            chargeRing = Telegraph.Create(TelegraphShape.Circle, transform.position, Vector3.forward, 1.1f, 0f,
                CurrentWeapon.uiColor * new Color(1f, 1f, 1f, 0.6f));
        }

        private void UpdateCharge(WeaponConfig weapon)
        {
            player.MoveSpeedMultiplier = weapon.chargeMoveSpeedMultiplier;
            chargeTimer += Time.deltaTime;
            player.Face(AimDirection(chargeFromMouse, weapon.chargedAttack));

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

            if (chargeHeld) return;

            ReleaseChargeRing();
            if (progress >= 1f)
            {
                StartStep(weapon.chargedAttack, true, weapon.critChance, weapon.critMultiplier, weapon.uiColor,
                    chargeFromMouse);
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

        private void StartStep(AttackStep next, bool final, float critChance, float critMultiplier, Color color,
            bool fromMouse)
        {
            step = next;
            stepIsFinal = final;
            stepCritChance = critChance;
            stepCritMultiplier = critMultiplier;
            stepColor = color;
            aimFromMouse = fromMouse;
            bufferedAt = -10f;
            hitThisStep.Clear();
            phase = Phase.Windup;
            phaseTimer = step.windup;
            attackDirection = AimDirection(aimFromMouse, step);
            player.Face(attackDirection);
            swingFlip = !swingFlip;

            if (player.Billboard != null) player.Billboard.Punch(new Vector2(0.85f, 1.15f));
            if (phaseTimer <= 0f) BeginActive();
        }

        private void BeginActive()
        {
            phase = Phase.Active;
            phaseTimer = step.active;
            attackDirection = AimDirection(aimFromMouse, step);

            if (player.Billboard != null)
            {
                player.Billboard.SetTint(Color.white);
                var heavy = IsHeavy(step);
                player.Billboard.Punch(heavy ? new Vector2(1.3f, 0.78f) : new Vector2(1.2f, 0.85f));
                // Heavy swings have weight even when they whiff.
                if (heavy) CameraShake.Add(0.06f);
            }

            if (step.kind == AttackKind.MeleeArc)
            {
                attackDirection = AssistedDirection(attackDirection, step.range);
                player.Face(attackDirection);

                if (step.lungeDistance > 0f)
                {
                    var duration = Mathf.Max(0.05f, step.active);
                    player.ApplyImpulse(attackDirection * (step.lungeDistance / duration), duration);
                }

                var reverse = step.reverseSwing ^ (swingFlip && !stepIsFinal);
                FxManager.Slash(transform.position, attackDirection, step.range, step.slashWidth, step.arcDegrees,
                    step.slashColor, step.slashDuration, reverse);
                SweepMelee();
            }
            else
            {
                player.Face(attackDirection);
                FireProjectiles();
            }
        }

        private void SweepMelee()
        {
            var s = step;
            CombatUtility.MeleeArc(transform.position, attackDirection, s.range, s.arcDegrees, Team.Player,
                hitThisStep, victim => BuildHit(victim, s));
        }

        private void FireProjectiles()
        {
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
                    MakeHit = victim => BuildHit(victim, s)
                });
            }

            FxManager.HitSpark(transform.position + Vector3.up + attackDirection * 0.6f, attackDirection,
                s.projectileColor, 4);
            player.ApplyImpulse(-attackDirection * 2f, 0.06f);
        }

        private DamageInfo BuildHit(Health victim, AttackStep s)
        {
            var crit = Random.value < stepCritChance;
            var amount = CombatUtility.RollDamage(s.damage, s.damageVariance);
            if (crit) amount = Mathf.RoundToInt(amount * stepCritMultiplier);

            return CombatUtility.MakeHit(gameObject, Team.Player, victim, amount, crit,
                s.knockback, s.stagger, s.hitStop, s.cameraShake, s.hitWeight);
        }

        private void EndAttack(bool resetCombo)
        {
            if (phase != Phase.Idle && phase != Phase.Charging) lastStepEndedAt = Time.time;
            phase = Phase.Idle;
            phaseTimer = 0f;
            ReleaseChargeRing();
            if (player != null && player.Billboard != null) player.Billboard.SetTint(Color.white);
            if (resetCombo) comboIndex = 0;
        }
    }
}
