using System;
using System.Collections;
using System.Collections.Generic;
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
    /// One brain for every monster and the boss. Behaviour is pure data (EnemyBehaviour):
    /// a movement style plus a list of attacks. Loop: Idle → Engage (move, pick an attack in
    /// range) → Windup (telegraph) → Active → Recovery → Engage. Enough poise damage causes a
    /// Stagger that cancels the attack.
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class EnemyBrain : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Engage,
            Windup,
            Active,
            Recovery,
            Stagger,
            Transition,
            Dead
        }

        private const float PoiseResetDelay = 2f;
        private const float DummyRegenDelay = 2.5f;

        private EnemyConfigBase config;
        private EnemyBehaviour behaviour;
        private CharacterController controller;
        private Health health;
        private SpriteBillboard billboard;

        private State state;
        private float stateTimer;
        private float[] cooldowns = new float[0];
        private EnemyAttack attack;
        private Vector3 attackDirection = Vector3.back;
        private readonly List<Vector3> aoePoints = new List<Vector3>();
        private readonly List<Telegraph> telegraphs = new List<Telegraph>();
        private readonly HashSet<Health> hitThisAttack = new HashSet<Health>();
        private int volleysFired;
        private float volleyTimer;
        private bool bumpedWall;

        private Vector3 planarVelocity;
        private Vector3 knockbackVelocity;
        private float verticalVelocity;
        private float poiseDamage;
        private float lastHitTime = -10f;
        private float decisionTimer;
        private Vector3 erraticDirection;
        private float erraticTimer;
        private float circleSign = 1f;
        private float circleSwapTimer;

        public event Action<EnemyBrain> Defeated;

        /// Any enemy died (spawned or summoned).
        public static event Action<EnemyBrain> AnyDefeated;

        public EnemyConfigBase Config => config;
        public Health Health => health;
        public State CurrentState => state;
        public bool IsAlive => state != State.Dead && health != null && health.IsAlive;
        public bool IsEngaged => state != State.Idle && state != State.Dead;
        public string CurrentAttackName => attack != null && (state == State.Windup || state == State.Active) ? attack.name : "";

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            billboard = GetComponent<SpriteBillboard>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            if (!CombatRegistry.Enemies.Contains(this)) CombatRegistry.Enemies.Add(this);
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            CombatRegistry.Enemies.Remove(this);
            ClearTelegraphs();
        }

        public void Initialize(EnemyConfigBase newConfig)
        {
            config = newConfig;
            health.Configure(config.maxHealth, Team.Enemy, 0f);
            if (config is MonsterConfig monster && monster.immortal) health.Immortal = true;
            if (billboard != null) billboard.Setup(config.visual);
            SetBehaviour(config.InitialBehaviour);
            circleSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        }

        public void SetBehaviour(EnemyBehaviour next)
        {
            behaviour = next ?? new EnemyBehaviour();
            var count = behaviour.attacks != null ? behaviour.attacks.Length : 0;
            cooldowns = new float[count];

            // Stagger the first use a little so groups don't attack in lockstep.
            for (var i = 0; i < count; i++) cooldowns[i] = UnityEngine.Random.Range(0f, 0.6f);
        }

        /// Wake up (used when a nearby ally spots the player).
        public void Alert()
        {
            if (state != State.Idle) return;
            state = State.Engage;
            decisionTimer = UnityEngine.Random.Range(0.2f, 0.6f);
        }

        /// Boss phase change: stand still, can't be staggered, for `duration`.
        public void BeginTransition(float duration)
        {
            CancelAttack();
            state = State.Transition;
            stateTimer = duration;
            planarVelocity = Vector3.zero;
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            if (state == State.Dead || config == null) return;

            var dt = Time.deltaTime;
            stateTimer -= dt;
            for (var i = 0; i < cooldowns.Length; i++) cooldowns[i] -= dt;
            if (Time.time - lastHitTime > PoiseResetDelay) poiseDamage = 0f;
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 35f * dt);

            if (health.Immortal && Time.time - lastHitTime > DummyRegenDelay && health.Current < health.Max)
            {
                health.Heal(health.Max - health.Current);
            }

            var player = CombatRegistry.Player;
            if (player == null || !player.IsAlive)
            {
                if (state != State.Idle && state != State.Transition)
                {
                    CancelAttack();
                    state = State.Idle;
                }

                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 20f * dt);
                Move();
                UpdateVisual();
                return;
            }

            var toTarget = player.transform.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            var direction = distance > 0.001f ? toTarget / distance : transform.forward;

            switch (state)
            {
                case State.Idle:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 20f * dt);
                    var canAct = behaviour.movement != MovementStyle.Stationary ||
                                 (behaviour.attacks != null && behaviour.attacks.Length > 0);
                    if (canAct && distance <= behaviour.detectionRange)
                    {
                        Alert();
                        AlertGroup();
                    }
                    break;

                case State.Engage:
                    UpdateEngage(direction, distance, dt);
                    break;

                case State.Windup:
                    UpdateWindup(direction, player.transform.position, dt);
                    break;

                case State.Active:
                    UpdateActive(dt);
                    break;

                case State.Recovery:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * dt);
                    if (stateTimer <= 0f) ReturnToEngage();
                    break;

                case State.Stagger:
                    planarVelocity = Vector3.zero;
                    if (stateTimer <= 0f) ReturnToEngage();
                    break;

                case State.Transition:
                    planarVelocity = Vector3.zero;
                    if (stateTimer <= 0f) ReturnToEngage();
                    break;
            }

            Move();
            UpdateVisual();
        }

        private void UpdateEngage(Vector3 direction, float distance, float dt)
        {
            if (distance > behaviour.leashRange)
            {
                state = State.Idle;
                return;
            }

            var move = DesiredMove(direction, distance, dt) + Separation();
            var wanted = Vector3.ClampMagnitude(move, 1f) * behaviour.moveSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, wanted, 30f * dt);
            Face(direction);

            decisionTimer -= dt;
            if (decisionTimer > 0f) return;

            var index = PickAttack(distance);
            if (index >= 0) StartAttack(index, direction);
        }

        private Vector3 DesiredMove(Vector3 direction, float distance, float dt)
        {
            var preferred = behaviour.preferredDistance;
            var side = Vector3.Cross(Vector3.up, direction) * circleSign;

            circleSwapTimer -= dt;
            if (circleSwapTimer <= 0f)
            {
                circleSwapTimer = UnityEngine.Random.Range(1.5f, 3.5f);
                if (UnityEngine.Random.value < 0.4f) circleSign = -circleSign;
            }

            switch (behaviour.movement)
            {
                case MovementStyle.Chase:
                    return distance > preferred ? direction : Vector3.zero;

                case MovementStyle.KeepDistance:
                    if (distance < preferred - 1.2f) return (-direction + side * 0.4f).normalized;
                    if (distance > preferred + 1.2f) return direction;
                    return side * 0.45f;

                case MovementStyle.Circle:
                    var radial = Mathf.Clamp((distance - preferred) * 0.5f, -1f, 1f);
                    return (side + direction * radial).normalized;

                case MovementStyle.Erratic:
                    erraticTimer -= dt;
                    if (erraticTimer <= 0f || erraticDirection == Vector3.zero)
                    {
                        erraticTimer = behaviour.erraticInterval * UnityEngine.Random.Range(0.6f, 1.4f);
                        var toward = distance > preferred ? direction : -direction;
                        erraticDirection = CombatUtility.Rotate(toward, UnityEngine.Random.Range(-75f, 75f));
                    }
                    return erraticDirection;

                default:
                    return Vector3.zero;
            }
        }

        private Vector3 Separation()
        {
            var push = Vector3.zero;
            foreach (var other in CombatRegistry.Enemies)
            {
                if (other == this || other == null || !other.IsAlive) continue;
                var offset = transform.position - other.transform.position;
                offset.y = 0f;
                var minDistance = controller.radius + other.controller.radius + 0.3f;
                var d = offset.magnitude;
                if (d < 0.001f || d > minDistance) continue;
                push += offset / d * (1f - d / minDistance);
            }
            return push * 1.5f;
        }

        private int PickAttack(float distance)
        {
            var attacks = behaviour.attacks;
            if (attacks == null || attacks.Length == 0) return -1;

            var total = 0f;
            for (var i = 0; i < attacks.Length; i++)
            {
                if (IsUsable(i, distance)) total += Mathf.Max(0.01f, attacks[i].weight);
            }
            if (total <= 0f) return -1;

            var roll = UnityEngine.Random.value * total;
            for (var i = 0; i < attacks.Length; i++)
            {
                if (!IsUsable(i, distance)) continue;
                roll -= Mathf.Max(0.01f, attacks[i].weight);
                if (roll <= 0f) return i;
            }
            return -1;
        }

        private bool IsUsable(int index, float distance)
        {
            var a = behaviour.attacks[index];
            return a != null && cooldowns[index] <= 0f && distance >= a.minRange && distance <= a.maxRange;
        }

        // ------------------------------------------------------------------ attacks

        private void StartAttack(int index, Vector3 direction)
        {
            attack = behaviour.attacks[index];
            cooldowns[index] = attack.cooldown;
            state = State.Windup;
            stateTimer = attack.windup;
            attackDirection = direction;
            hitThisAttack.Clear();
            volleysFired = 0;
            bumpedWall = false;
            Face(direction);

            aoePoints.Clear();
            var player = CombatRegistry.Player;
            if (attack.kind == EnemyAttackKind.GroundAoE && player != null)
            {
                var center = player.transform.position;
                aoePoints.Add(center);
                for (var i = 1; i < Mathf.Max(1, attack.aoeCount); i++)
                {
                    var scatter = UnityEngine.Random.insideUnitCircle * Mathf.Max(0.5f, attack.aoeScatter);
                    aoePoints.Add(center + new Vector3(scatter.x, 0f, scatter.y));
                }
            }

            CreateTelegraphs();
            if (billboard != null) billboard.Punch(new Vector2(0.85f, 1.2f));
        }

        private void CreateTelegraphs()
        {
            ClearTelegraphs();
            if (!attack.showTelegraph) return;

            var c = attack.color;
            var pos = transform.position;

            switch (attack.kind)
            {
                case EnemyAttackKind.MeleeArc:
                    telegraphs.Add(Telegraph.Create(TelegraphShape.Sector, pos, attackDirection, attack.radius, attack.arcDegrees, c));
                    break;

                case EnemyAttackKind.Lunge:
                case EnemyAttackKind.Charge:
                    telegraphs.Add(Telegraph.Create(TelegraphShape.Line, pos, attackDirection,
                        attack.dashSpeed * attack.active + attack.radius, attack.radius * 2f, c));
                    break;

                case EnemyAttackKind.Projectile:
                    if (attack.projectileCount > 1 && attack.spreadDegrees > 0f)
                    {
                        telegraphs.Add(Telegraph.Create(TelegraphShape.Sector, pos, attackDirection,
                            Mathf.Min(attack.projectileRange, 6f), attack.spreadDegrees + 8f, c));
                    }
                    else
                    {
                        telegraphs.Add(Telegraph.Create(TelegraphShape.Line, pos, attackDirection,
                            Mathf.Min(attack.projectileRange, 7f), attack.projectileSize * 2.5f, c));
                    }
                    break;

                case EnemyAttackKind.GroundAoE:
                    foreach (var point in aoePoints)
                    {
                        telegraphs.Add(Telegraph.Create(TelegraphShape.Circle, point, Vector3.forward, attack.radius, 0f, c));
                    }
                    break;

                case EnemyAttackKind.SelfAoE:
                    telegraphs.Add(Telegraph.Create(TelegraphShape.Circle, pos, Vector3.forward, attack.radius, 0f, c));
                    break;

                case EnemyAttackKind.RadialBurst:
                case EnemyAttackKind.Summon:
                    telegraphs.Add(Telegraph.Create(TelegraphShape.Circle, pos, Vector3.forward, 1.4f, 0f, c));
                    break;
            }
        }

        private void UpdateWindup(Vector3 direction, Vector3 playerPosition, float dt)
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * dt);

            if (attack.trackDuringWindup && attack.kind != EnemyAttackKind.GroundAoE)
            {
                attackDirection = Vector3.RotateTowards(attackDirection, direction,
                    attack.trackTurnSpeed * Mathf.Deg2Rad * dt, 0f);
                Face(attackDirection);
            }

            var progress = attack.windup > 0f ? 1f - Mathf.Clamp01(stateTimer / attack.windup) : 1f;
            for (var i = 0; i < telegraphs.Count; i++)
            {
                var t = telegraphs[i];
                if (t == null) continue;
                if (attack.kind != EnemyAttackKind.GroundAoE) t.SetPose(transform.position, attackDirection);
                t.SetProgress(progress);
            }

            // Pulse toward the attack color as a body telegraph.
            if (billboard != null)
            {
                var pulse = Mathf.PingPong(Time.time * 8f, 1f) * 0.6f;
                billboard.SetTint(Color.Lerp(Color.white, new Color(attack.color.r, attack.color.g, attack.color.b), pulse));
            }

            if (stateTimer <= 0f) BeginActive();
        }

        private void BeginActive()
        {
            ClearTelegraphs();
            if (billboard != null)
            {
                billboard.SetTint(Color.white);
                billboard.Punch(new Vector2(1.25f, 0.8f));
            }

            state = State.Active;
            stateTimer = attack.active;
            var opaque = new Color(attack.color.r, attack.color.g, attack.color.b, 1f);

            switch (attack.kind)
            {
                case EnemyAttackKind.MeleeArc:
                    FxManager.Slash(transform.position, attackDirection, attack.radius, 0.7f, attack.arcDegrees,
                        opaque, 0.16f, false);
                    CombatUtility.MeleeArc(transform.position, attackDirection, attack.radius, attack.arcDegrees,
                        Team.Enemy, hitThisAttack, MakeHit);
                    break;

                case EnemyAttackKind.Lunge:
                case EnemyAttackKind.Charge:
                    planarVelocity = attackDirection * attack.dashSpeed;
                    FxManager.Dust(transform.position, 6, new Color(0.8f, 0.75f, 0.7f, 0.8f));
                    break;

                case EnemyAttackKind.Projectile:
                    FireVolley();
                    break;

                case EnemyAttackKind.RadialBurst:
                    FireRadial();
                    break;

                case EnemyAttackKind.GroundAoE:
                    foreach (var point in aoePoints) Blast(point, opaque);
                    break;

                case EnemyAttackKind.SelfAoE:
                    Blast(transform.position, opaque);
                    break;

                case EnemyAttackKind.Summon:
                    Summon(attack.summon, attack.summonCount);
                    FxManager.Ring(transform.position, 0.5f, 3f, 0.4f, opaque);
                    break;
            }
        }

        private void UpdateActive(float dt)
        {
            switch (attack.kind)
            {
                case EnemyAttackKind.Lunge:
                case EnemyAttackKind.Charge:
                    planarVelocity = attackDirection * attack.dashSpeed;
                    CombatUtility.MeleeArc(transform.position, attackDirection, attack.radius, 360f, Team.Enemy,
                        hitThisAttack, MakeHit);

                    if (attack.kind == EnemyAttackKind.Charge)
                    {
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            FxManager.Dust(transform.position, 1, new Color(0.8f, 0.75f, 0.7f, 0.7f));
                        }

                        if (bumpedWall)
                        {
                            // Slammed into a wall: dazed, punishable.
                            CameraShake.Add(0.25f);
                            FxManager.HitSpark(transform.position + Vector3.up + attackDirection * controller.radius,
                                -attackDirection, Color.white, 14);
                            planarVelocity = -attackDirection * 3f;
                            state = State.Recovery;
                            stateTimer = attack.recovery + 0.8f;
                            return;
                        }
                    }
                    break;

                case EnemyAttackKind.Projectile:
                case EnemyAttackKind.RadialBurst:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * dt);
                    volleyTimer -= dt;
                    if (volleysFired < attack.volleys && volleyTimer <= 0f)
                    {
                        if (attack.kind == EnemyAttackKind.Projectile)
                        {
                            var player = CombatRegistry.Player;
                            if (player != null && attack.trackDuringWindup)
                            {
                                attackDirection = CombatUtility.Flat(player.transform.position - transform.position).normalized;
                            }
                            FireVolley();
                        }
                        else
                        {
                            FireRadial();
                        }
                    }
                    // Hold the Active state until the last volley is out.
                    if (volleysFired < attack.volleys && stateTimer <= 0f) stateTimer = 0.01f;
                    break;

                default:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * dt);
                    break;
            }

            if (stateTimer <= 0f)
            {
                state = State.Recovery;
                stateTimer = attack.recovery;
            }
        }

        private void FireVolley()
        {
            var count = Mathf.Max(1, attack.projectileCount);
            for (var i = 0; i < count; i++)
            {
                var offset = count == 1 ? 0f : Mathf.Lerp(-attack.spreadDegrees * 0.5f, attack.spreadDegrees * 0.5f, i / (count - 1f));
                FireProjectile(CombatUtility.Rotate(attackDirection, offset));
            }
            volleysFired++;
            volleyTimer = attack.volleyInterval;
        }

        private void FireRadial()
        {
            var count = Mathf.Max(3, attack.projectileCount);
            var step = 360f / count;
            var offset = volleysFired % 2 == 1 ? step * 0.5f : 0f;
            for (var i = 0; i < count; i++)
            {
                FireProjectile(CombatUtility.Rotate(attackDirection, offset + step * i));
            }
            volleysFired++;
            volleyTimer = attack.volleyInterval;
        }

        private void FireProjectile(Vector3 direction)
        {
            var opaque = new Color(attack.color.r, attack.color.g, attack.color.b, 1f);
            Projectile.Fire(new Projectile.Spec
            {
                Team = Team.Enemy,
                Source = gameObject,
                Position = transform.position + Vector3.up * 1f + direction * (controller.radius + 0.2f),
                Direction = direction,
                Speed = attack.projectileSpeed,
                Range = attack.projectileRange,
                Radius = attack.projectileSize,
                Pierce = 0,
                Color = opaque,
                Sprite = VelaSettings.Fx.orbSprite,
                SpriteLength = attack.projectileSize * 2.6f,
                MakeHit = MakeHit
            });
        }

        private void Blast(Vector3 point, Color color)
        {
            CombatUtility.Circle(point, attack.radius, Team.Enemy, hitThisAttack, MakeHit);
            FxManager.Ring(point, attack.radius * 0.3f, attack.radius * 1.1f, 0.3f, color);
            FxManager.HitSpark(point + Vector3.up * 0.3f, Vector3.up, color, 12);
            FxManager.Dust(point, 10, new Color(0.7f, 0.65f, 0.6f, 0.8f));
            CameraShake.Add(0.12f);
        }

        public void Summon(MonsterConfig summon, int count)
        {
            if (summon == null) return;
            for (var i = 0; i < count; i++)
            {
                var angle = 360f / Mathf.Max(1, count) * i + UnityEngine.Random.Range(-20f, 20f);
                var offset = CombatUtility.Rotate(Vector3.forward, angle) * 2.2f;
                var spawned = EnemyFactory.Spawn(summon, transform.position + offset);
                spawned.Alert();
                FxManager.DeathBurst(spawned.transform.position + Vector3.up * 0.5f, summon.deathBurstColor, 10);
            }
        }

        private DamageInfo MakeHit(Health victim)
        {
            var amount = Mathf.RoundToInt(attack.damage * VelaSettings.Feel.enemyDamageScale);
            return CombatUtility.MakeHit(gameObject, Team.Enemy, victim, amount, false, attack.knockback, 0f, 0f, 0f,
                attack.hitWeight);
        }

        private void CancelAttack()
        {
            ClearTelegraphs();
            if (billboard != null) billboard.SetTint(Color.white);
            if (state == State.Windup || state == State.Active) planarVelocity = Vector3.zero;
        }

        private void ClearTelegraphs()
        {
            foreach (var t in telegraphs)
            {
                if (t != null) t.Release();
            }
            telegraphs.Clear();
        }

        private void ReturnToEngage()
        {
            state = State.Engage;
            decisionTimer = behaviour.attackDecisionDelay * UnityEngine.Random.Range(0.7f, 1.3f);
        }

        private void AlertGroup()
        {
            var radius = config.groupAlertRadius;
            foreach (var other in CombatRegistry.Enemies)
            {
                if (other == this || other == null) continue;
                if ((other.transform.position - transform.position).sqrMagnitude <= radius * radius) other.Alert();
            }
        }

        // ------------------------------------------------------------------ hits & death

        private void OnDamaged(Health self, DamageInfo info)
        {
            lastHitTime = Time.time;

            if (state == State.Idle)
            {
                Alert();
                AlertGroup();
            }

            var feel = VelaSettings.Feel;
            var resist = 1f - config.knockbackResistance;
            var weightPush = feel.Profile(info.Weight).knockbackMultiplier * (info.IsCrit ? 1.2f : 1f);
            var push = info.Direction * (info.Knockback * resist * weightPush * feel.knockbackScale);
            if (push.sqrMagnitude > knockbackVelocity.sqrMagnitude) knockbackVelocity = push;

            if (state == State.Transition || state == State.Dead || !health.IsAlive) return;

            poiseDamage += info.Stagger;
            var staggered = config.poise <= 0f || poiseDamage >= config.poise;
            if (!staggered) return;

            poiseDamage = 0f;
            CancelAttack();
            state = State.Stagger;
            stateTimer = config.staggerDuration;
            if (billboard != null) billboard.Punch(new Vector2(1.35f, 0.65f));

            // Armored enemies announce a poise break: this is the payoff for heavy attacks.
            if (config.poise > 0f) PoiseBreakFeedback();
        }

        private void PoiseBreakFeedback()
        {
            var feel = VelaSettings.Feel;
            var height = billboard != null ? billboard.Visual.worldHeight + billboard.Visual.hoverHeight : 1.8f;
            DamageNumbers.Spawn(transform.position + Vector3.up * (height + 0.7f), feel.breakLabel, feel.breakColor, 1.25f);
            FxManager.Ring(transform.position, 0.3f, config.colliderRadius * 2f + 1.5f, 0.3f, feel.breakColor);
            FxManager.HitSpark(transform.position + Vector3.up * height * 0.6f, Vector3.up, feel.breakColor, 14);
            if (billboard != null) billboard.HurtTint(feel.breakColor, config.staggerDuration);
            HitStop.Request(feel.breakHitStop * feel.hitStopScale);
            CameraShake.Add(feel.breakShake);
        }

        /// Time of the last hit taken (for the enemy health bar).
        public float LastHitTime => lastHitTime;

        /// How close the next hit is to breaking poise (0..1). 0 for enemies without poise.
        public float PoiseNormalized => config != null && config.poise > 0f ? Mathf.Clamp01(poiseDamage / config.poise) : 0f;

        public bool IsStaggered => state == State.Stagger;

        private void OnDied(Health self)
        {
            CancelAttack();
            state = State.Dead;
            controller.enabled = false;
            CombatRegistry.Enemies.Remove(this);

            var feel = VelaSettings.Feel;
            FxManager.DeathBurst(transform.position + Vector3.up * 0.8f, config.deathBurstColor, feel.deathBurstCount);
            HitStop.Request(feel.killHitStop * feel.hitStopScale);
            CameraShake.Add(feel.killShake);

            if (config is BossConfig)
            {
                HitStop.Request(0.25f * feel.hitStopScale);
                HitStop.SlowMotion(feel.bossKillSlowMo, feel.bossKillSlowMoScale);
                CameraShake.Add(0.6f);
                CameraShake.Punch(0.12f);
            }

            Defeated?.Invoke(this);
            AnyDefeated?.Invoke(this);
            StartCoroutine(FadeAndDestroy());
        }

        private IEnumerator FadeAndDestroy()
        {
            const float duration = 0.45f;
            if (billboard != null)
            {
                billboard.SetState(VisualState.Hurt);
                billboard.Punch(new Vector2(1.5f, 0.5f));
            }

            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                if (billboard != null) billboard.SetFade(1f - t / duration);
                yield return null;
            }

            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ movement & visuals

        private void Move()
        {
            if (!controller.enabled) return;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity -= 25f * Time.deltaTime;

            var motion = planarVelocity + knockbackVelocity;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (state != State.Active || attack == null || attack.kind != EnemyAttackKind.Charge) return;
            if (hit.normal.y > 0.5f) return;
            if (hit.collider.GetComponentInParent<Health>() != null) return;
            if (Vector3.Dot(hit.normal, attackDirection) < -0.4f) bumpedWall = true;
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction),
                behaviour.turnSpeed * Time.deltaTime);
            if (billboard != null) billboard.SetFacing(direction);
        }

        private void UpdateVisual()
        {
            if (billboard == null) return;

            switch (state)
            {
                case State.Stagger:
                    billboard.SetState(VisualState.Hurt);
                    break;
                case State.Windup:
                case State.Active:
                    billboard.SetState(VisualState.Attack);
                    break;
                default:
                    billboard.SetState(planarVelocity.sqrMagnitude > 0.3f ? VisualState.Move : VisualState.Idle);
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var b = behaviour ?? (config != null ? config.InitialBehaviour : null);
            if (b == null) return;

            Gizmos.color = new Color(1f, 1f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, b.detectionRange);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, b.preferredDistance);
        }
    }
}
