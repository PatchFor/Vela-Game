class_name Enemy
extends CharacterBody3D
## Data-driven monster (Unity: EnemyBrain + BossController). Everything it does comes from its
## MonsterConfig: movement style, weighted attack list, telegraphs, poise/break, loot, and — when
## the config has phases — boss phase transitions with shockwave and summons.

enum State { IDLE, MOVE, WINDUP, ACTIVE, RECOVERY, STAGGER, RETURN, TRANSITION, DEAD }

signal phase_changed(index: int)

var cfg: MonsterConfig
var health: Health
var rig: SpriteRig
var state := State.IDLE
var phase_index := 0
var home := Vector3.ZERO

var _state_t := 0.0
var _move_vel := Vector3.ZERO
var _knock := Vector3.ZERO
var _attacks: Array = []
var _move_speed := 3.0
var _decision_delay := 0.5
var _ready_at := {} ## EnemyAttack -> time
var _next_decision := 0.0
var _atk: EnemyAttack
var _atk_dir := Vector3.FORWARD
var _atk_hit := {}
var _atk_end := 0.0
var _volleys_left := 0
var _next_volley := 0.0
var _telegraphs: Array = []
var _aoe_points: Array = []
var _poise_dmg := 0.0
var _stagger_time := 0.0
var _last_damaged := -10.0
var _erratic_dir := Vector3.ZERO
var _erratic_at := 0.0
var _circle_sign := 1.0
var _paused_until := 0.0


static func create(parent: Node, config: MonsterConfig, pos: Vector3) -> Enemy:
	var e := Enemy.new()
	e.cfg = config
	e.name = config.display_name.replace(" ", "")
	parent.add_child(e)
	e.global_position = pos
	e.home = pos
	e._setup()
	return e


func _setup() -> void:
	set_meta("radius", cfg.radius)
	collision_layer = 1
	collision_mask = 1
	var shape := CollisionShape3D.new()
	var cap := CapsuleShape3D.new()
	cap.radius = cfg.radius
	cap.height = maxf(cfg.height, cfg.radius * 2.0 + 0.01)
	shape.shape = cap
	shape.position.y = cap.height * 0.5
	add_child(shape)

	health = Health.new()
	health.name = "Health"
	health.setup(cfg.max_hp, DamageInfo.Team.ENEMY)
	health.immortal = cfg.immortal
	add_child(health)
	health.damaged.connect(_on_damaged)
	health.died.connect(_on_died)

	rig = SpriteRig.new()
	rig.name = "Rig"
	add_child(rig)
	rig.setup_single(cfg.texture, cfg.world_height, cfg.shadow_size, cfg.hover)

	_circle_sign = 1.0 if Game.roll() < 0.5 else -1.0
	_apply_phase(0)
	Game.enemies.append(self)


func is_alive() -> bool:
	return state != State.DEAD and health.is_alive()


func is_boss() -> bool:
	return cfg.is_boss()


## Hitting a monster while it recovers from its own attack is a punish (bonus damage).
func is_punishable() -> bool:
	return state == State.RECOVERY


func phase_name() -> String:
	return cfg.phases[phase_index].name if is_boss() else ""


func hit_pause(seconds: float) -> void:
	_paused_until = maxf(_paused_until, Game.real_time() + seconds)
	rig.hit_pause(seconds)


func _exit_tree() -> void:
	Game.enemies.erase(self)
	_clear_telegraphs()


# ------------------------------------------------------------------ frame

func _physics_process(delta: float) -> void:
	if state == State.DEAD:
		_state_t += delta
		rig.set_fade(1.0 - _state_t / 0.6)
		if _state_t >= 0.6:
			queue_free()
		return
	if Game.real_time() < _paused_until:
		return
	_state_t += delta
	_poise_dmg = maxf(0.0, _poise_dmg - cfg.poise * 0.25 * delta) if Game.time - _last_damaged > 2.0 else _poise_dmg

	var p = Game.player
	var player_ok: bool = p != null and is_instance_valid(p) and p.is_alive()
	var to: Vector3 = (p.global_position - global_position) if player_ok else Vector3.ZERO
	to.y = 0.0
	var dist: float = to.length()
	var want := Vector3.ZERO

	match state:
		State.IDLE:
			if cfg.immortal and Game.time - _last_damaged > 3.0 and health.current < health.max_hp:
				health.current = health.max_hp
			if player_ok and cfg.detection > 0.0 and dist <= cfg.detection:
				_alert()
		State.MOVE:
			if not player_ok or global_position.distance_to(home) > cfg.leash:
				_set_state(State.RETURN)
			else:
				want = _move_dir(to, dist) * _move_speed
				if dist > 0.1:
					rig.set_facing(to)
				if Game.time >= _next_decision:
					var a := _pick_attack(dist)
					if a:
						_start_windup(a, to)
		State.WINDUP:
			_windup_tick(to, delta)
		State.ACTIVE:
			want = _active_tick(delta)
		State.RECOVERY:
			if _state_t >= _atk.recovery:
				_end_attack()
		State.STAGGER:
			if _state_t >= _stagger_time:
				_set_state(State.MOVE)
				rig.set_state(SpriteRig.State.MOVE)
		State.RETURN:
			var back := home - global_position
			back.y = 0.0
			health.heal(maxi(1, int(cfg.max_hp * 0.5 * delta)))
			if back.length() < 0.5:
				_set_state(State.IDLE)
				health.current = health.max_hp
			else:
				want = back.normalized() * _move_speed
				rig.set_facing(back)
				if player_ok and dist < cfg.detection * 0.5 and global_position.distance_to(home) < cfg.leash * 0.7:
					_set_state(State.MOVE)
		State.TRANSITION:
			if _state_t >= cfg.phases[phase_index].transition:
				_finish_transition()

	if state != State.ACTIVE:
		_move_vel = _move_vel.move_toward(want, 30.0 * delta)
	velocity = _move_vel + _knock
	velocity.y = 0.0
	move_and_slide()
	global_position.y = 0.0
	_knock = _knock.move_toward(Vector3.ZERO, 30.0 * delta)
	if state == State.MOVE or state == State.IDLE or state == State.RETURN:
		rig.set_state(SpriteRig.State.MOVE if _move_vel.length() > 0.3 else SpriteRig.State.IDLE)


func _move_dir(to: Vector3, dist: float) -> Vector3:
	if dist < 0.01:
		return Vector3.ZERO
	var toward := to / dist
	var side := Vector3(-toward.z, 0, toward.x) * _circle_sign
	match cfg.movement:
		MonsterConfig.Movement.CHASE:
			return toward if dist > cfg.preferred else Vector3.ZERO
		MonsterConfig.Movement.KEEP_DISTANCE:
			if dist < cfg.preferred - 1.5:
				return -toward
			if dist > cfg.preferred + 1.5:
				return toward
			return side * 0.4
		MonsterConfig.Movement.CIRCLE:
			var pull := clampf((dist - cfg.preferred) * 0.5, -1.0, 1.0)
			return (side + toward * pull).normalized()
		MonsterConfig.Movement.ERRATIC:
			if Game.time >= _erratic_at:
				_erratic_at = Game.time + cfg.erratic_interval
				var jitter := Vector3(Game.roll() * 2.0 - 1.0, 0, Game.roll() * 2.0 - 1.0)
				var bias := toward if dist > cfg.preferred else -toward * 0.5
				_erratic_dir = (bias + jitter * 1.2).normalized()
			return _erratic_dir
	return Vector3.ZERO


func _alert() -> void:
	if state != State.IDLE and state != State.RETURN:
		return
	_set_state(State.MOVE)
	_next_decision = Game.time + _decision_delay
	rig.punch(Vector2(0.85, 1.2))


# ------------------------------------------------------------------ attacks

func _pick_attack(dist: float) -> EnemyAttack:
	var options := []
	var total := 0.0
	for a in _attacks:
		if Game.time < _ready_at.get(a, 0.0) or dist < a.min_range or dist > a.max_range:
			continue
		options.append(a)
		total += a.weight
	if options.is_empty():
		return null
	var pick := Game.roll() * total
	for a in options:
		pick -= a.weight
		if pick <= 0.0:
			return a
	return options.back()


func _start_windup(a: EnemyAttack, to: Vector3) -> void:
	_atk = a
	_atk_dir = to.normalized() if to.length() > 0.01 else Vector3.BACK
	_atk_hit = {}
	_ready_at[a] = Game.time + a.cooldown
	_set_state(State.WINDUP)
	_move_vel = Vector3.ZERO
	rig.set_state(SpriteRig.State.ATTACK)
	rig.set_facing(_atk_dir)
	rig.punch(Vector2(1.15, 0.88))
	rig.flash(Color(a.color.r, a.color.g, a.color.b), 0.06)
	var heavy := DamageInfo.resolve_weight(a.hit_weight, 0.0, a.damage) >= 3
	Sfx.play("enemy_windup_heavy" if heavy else "enemy_windup", global_position)

	var T := Telegraph.Shape
	var c := a.color
	match a.kind:
		EnemyAttack.Kind.MELEE_ARC:
			_telegraphs = [Telegraph.create(Game.level, T.SECTOR, global_position, _atk_dir, a.radius, a.arc, c)]
		EnemyAttack.Kind.LUNGE, EnemyAttack.Kind.CHARGE:
			_telegraphs = [Telegraph.create(Game.level, T.LINE, global_position, _atk_dir, a.dash_speed * a.active + a.radius, a.radius * 2.0, c)]
		EnemyAttack.Kind.PROJECTILE:
			_telegraphs = [Telegraph.create(Game.level, T.LINE, global_position, _atk_dir, minf(a.proj_range, 6.0), a.proj_size * 2.5, c)]
		EnemyAttack.Kind.GROUND_AOE:
			_aoe_points = []
			var center: Vector3 = Game.player.global_position if Game.player else global_position
			for i in maxi(1, a.aoe_count):
				var off := Vector3.ZERO if i == 0 else Vector3(Game.roll() * 2.0 - 1.0, 0, Game.roll() * 2.0 - 1.0).normalized() * a.aoe_scatter * sqrt(Game.roll())
				var pt := center + off
				pt.y = 0.0
				_aoe_points.append(pt)
				_telegraphs.append(Telegraph.create(Game.level, T.CIRCLE, pt, Vector3.FORWARD, a.radius, 0.0, c))
		EnemyAttack.Kind.SELF_AOE:
			_telegraphs = [Telegraph.create(Game.level, T.CIRCLE, global_position, Vector3.FORWARD, a.radius, 0.0, c)]
		EnemyAttack.Kind.RADIAL, EnemyAttack.Kind.SUMMON:
			_telegraphs = [Telegraph.create(Game.level, T.CIRCLE, global_position, Vector3.FORWARD, 1.6, 0.0, c)]


func _windup_tick(to: Vector3, delta: float) -> void:
	var a := _atk
	if a.track and to.length() > 0.1 and a.kind != EnemyAttack.Kind.GROUND_AOE:
		var want := to.normalized()
		var ang := _atk_dir.signed_angle_to(want, Vector3.UP)
		var step := deg_to_rad(a.track_speed) * delta
		_atk_dir = _atk_dir.rotated(Vector3.UP, clampf(ang, -step, step))
		rig.set_facing(_atk_dir)
	var t := _state_t / maxf(0.01, a.windup)
	for i in _telegraphs.size():
		var tg: Telegraph = _telegraphs[i]
		if a.kind != EnemyAttack.Kind.GROUND_AOE:
			tg.set_pose(global_position, _atk_dir)
		tg.set_progress(t)
	if _state_t >= a.windup:
		_enter_active()


func _enter_active() -> void:
	var a := _atk
	_clear_telegraphs()
	_set_state(State.ACTIVE)
	_atk_end = a.active
	match a.kind:
		EnemyAttack.Kind.MELEE_ARC:
			Fx.slash(global_position + Vector3.UP * 0.8, _atk_dir, a.radius, 0.9, a.arc, Color(1, 0.55, 0.45), 0.16, false)
			Sfx.play("swing_heavy", global_position, 0.8)
			Combat.melee_arc(get_world_3d(), global_position, _atk_dir, a.radius, a.arc, DamageInfo.Team.ENEMY, _atk_hit, _hit_maker(a))
		EnemyAttack.Kind.LUNGE, EnemyAttack.Kind.CHARGE:
			Sfx.play("dash", global_position, 0.8)
		EnemyAttack.Kind.PROJECTILE, EnemyAttack.Kind.RADIAL:
			_volleys_left = maxi(1, a.volleys)
			_next_volley = 0.0
			_atk_end = maxf(a.active, (a.volleys - 1) * a.volley_interval + 0.01)
		EnemyAttack.Kind.GROUND_AOE:
			for pt in _aoe_points:
				_blast(pt, a.radius, a)
		EnemyAttack.Kind.SELF_AOE:
			_blast(global_position, a.radius, a)
		EnemyAttack.Kind.SUMMON:
			if a.summon and Game.spawner.is_valid():
				for i in a.summon_count:
					var off := Vector3.FORWARD.rotated(Vector3.UP, TAU * i / a.summon_count) * 2.0
					Game.spawner.call(a.summon, global_position + off)
				Fx.ring(global_position + Vector3.UP * 0.05, 0.5, 2.5, 0.3, a.color)


func _active_tick(_delta: float) -> Vector3:
	var a := _atk
	var move := Vector3.ZERO
	match a.kind:
		EnemyAttack.Kind.LUNGE, EnemyAttack.Kind.CHARGE:
			move = _atk_dir * a.dash_speed
			_move_vel = move
			if Engine.get_physics_frames() % 3 == 0:
				Fx.afterimage(rig, Color(1, 0.5, 0.4, 0.4), 0.15)
			Combat.melee_arc(get_world_3d(), global_position, _atk_dir, a.radius + cfg.radius, 360.0, DamageInfo.Team.ENEMY, _atk_hit, _hit_maker(a))
			if a.kind == EnemyAttack.Kind.CHARGE and is_on_wall() and _state_t > 0.1:
				Game.shake(0.3)
				Fx.dust(global_position, 10)
				Sfx.play("hit_heavy", global_position, 0.6)
				_atk_end = _state_t
		EnemyAttack.Kind.PROJECTILE, EnemyAttack.Kind.RADIAL:
			if _volleys_left > 0 and _state_t >= _next_volley:
				_fire_volley(a, maxi(1, a.volleys) - _volleys_left)
				_volleys_left -= 1
				_next_volley = _state_t + a.volley_interval
	if _state_t >= _atk_end:
		_move_vel = Vector3.ZERO
		_set_state(State.RECOVERY)
	return move


func _fire_volley(a: EnemyAttack, index: int) -> void:
	var n := maxi(1, a.proj_count)
	var color := Color(a.color.r, a.color.g, a.color.b, 1.0)
	Sfx.play("shoot", global_position, 0.7)
	for i in n:
		var d: Vector3
		if a.kind == EnemyAttack.Kind.RADIAL:
			d = Vector3.FORWARD.rotated(Vector3.UP, TAU * (i + 0.5 * (index % 2)) / n)
		else:
			var off := 0.0 if n == 1 else lerpf(-a.spread * 0.5, a.spread * 0.5, float(i) / (n - 1))
			d = _atk_dir.rotated(Vector3.UP, deg_to_rad(off))
		Projectile.fire(Game.level, global_position + Vector3.UP * 1.0 + d * (cfg.radius + 0.2), d, {
			"team": DamageInfo.Team.ENEMY, "speed": a.proj_speed, "range": a.proj_range,
			"radius": a.proj_size, "make_hit": _hit_maker(a), "color": color,
		})


func _blast(pt: Vector3, radius: float, a: EnemyAttack) -> void:
	Fx.ring(pt + Vector3.UP * 0.05, 0.3, radius, 0.3, Color(a.color.r, a.color.g, a.color.b, 0.9))
	Fx.dust(pt, 8)
	Fx.sparks(pt + Vector3.UP * 0.3, Vector3.UP, Color(1, 0.6, 0.3), 10)
	Game.shake(0.12)
	Sfx.play("hit_heavy", pt, 0.6)
	Combat.melee_arc(get_world_3d(), pt, Vector3.FORWARD, radius, 360.0, DamageInfo.Team.ENEMY, _atk_hit, _hit_maker(a))


func _hit_maker(a: EnemyAttack) -> Callable:
	return func(victim: Health) -> DamageInfo:
		return Combat.make_hit(self, DamageInfo.Team.ENEMY, victim, roundi(a.damage * Game.feel.enemy_damage_scale), false,
			a.knockback * Game.feel.knockback_scale, 0.0, 0.0, 0.0, a.hit_weight)


func _end_attack() -> void:
	_set_state(State.MOVE)
	_next_decision = Game.time + _decision_delay
	rig.set_state(SpriteRig.State.MOVE)


func _clear_telegraphs() -> void:
	for t in _telegraphs:
		if is_instance_valid(t):
			t.queue_free()
	_telegraphs = []


# ------------------------------------------------------------------ damage

func _on_damaged(info: DamageInfo) -> void:
	Feedback.enemy_hit(self, rig, info)
	_last_damaged = Game.time
	_knock = info.direction * info.knockback * (1.0 - cfg.knockback_resist)
	if state == State.IDLE or state == State.RETURN:
		_alert()
	for e in Game.enemies:
		if e != self and is_instance_valid(e) and e.global_position.distance_to(global_position) <= cfg.alert_radius:
			e._alert()

	if is_boss() and phase_index + 1 < cfg.phases.size() and health.ratio() <= cfg.phases[phase_index + 1].starts_at:
		_begin_transition(phase_index + 1)
		return
	if state == State.TRANSITION or not health.is_alive():
		return

	if cfg.poise <= 0.0:
		_stagger(cfg.stagger_duration)
		return
	_poise_dmg += info.stagger
	if _poise_dmg >= cfg.poise:
		_poise_dmg = 0.0
		_stagger(cfg.stagger_duration * 1.5)
		Fx.number(global_position + Vector3.UP * (cfg.world_height + 0.3), "BREAK", Game.feel.break_color, 1.3, true)
		Fx.ring(global_position + Vector3.UP * 0.05, 0.4, 2.2, 0.3, Game.feel.break_color)
		rig.flash(Game.feel.break_color, 0.12)
		Game.hit_stop(Game.feel.break_hit_stop, self, info.source)
		Sfx.play("break", global_position)


func _stagger(seconds: float) -> void:
	_clear_telegraphs()
	_set_state(State.STAGGER)
	_stagger_time = seconds
	_move_vel = Vector3.ZERO
	rig.set_state(SpriteRig.State.HURT)


func _on_died() -> void:
	_clear_telegraphs()
	_set_state(State.DEAD)
	collision_layer = 0
	collision_mask = 0
	Game.enemies.erase(self)
	Fx.death_burst(global_position + Vector3.UP * cfg.world_height * 0.5, cfg.death_color, 40 if is_boss() else 22)
	Fx.ring(global_position + Vector3.UP * 0.05, 0.3, 2.0 + cfg.radius, 0.35, cfg.death_color)
	Loot.drop(Game.level, cfg.loot, global_position, Game.player)
	Sfx.play("kill", global_position)
	Game.hit_stop(Game.feel.kill_hit_stop, self)
	Game.kills += 1
	Game.stats.kills += 1
	Game.enemy_defeated.emit(self)
	if is_boss():
		Game.boss_defeated = true
		Game.slow_motion(1.2, 0.25)
		Game.shake(0.8)
		Game.message("VICTORY — the Warden falls", Color(1, 0.85, 0.3), 6.0)


# ------------------------------------------------------------------ boss phases

func _apply_phase(i: int) -> void:
	phase_index = i
	if is_boss():
		var ph: BossPhase = cfg.phases[i]
		_attacks = ph.attacks
		_move_speed = ph.move_speed
		_decision_delay = ph.decision_delay
		rig.set_base_tint(ph.tint)
		rig.set_scale_mult(ph.sprite_scale)
	else:
		_attacks = cfg.attacks
		_move_speed = cfg.move_speed
		_decision_delay = cfg.decision_delay


func _begin_transition(i: int) -> void:
	_clear_telegraphs()
	_apply_phase(i)
	var ph: BossPhase = cfg.phases[i]
	_set_state(State.TRANSITION)
	_move_vel = Vector3.ZERO
	health.grant_invulnerability(ph.transition)
	rig.flash(Color.WHITE, 0.2)
	rig.tremble(0.12, ph.transition)
	Game.shake(0.5)
	Game.slow_motion(0.5, 0.3)
	if ph.announcement != "":
		Game.message(ph.announcement, Color(1, 0.45, 0.35), 3.0)
	Sfx.play("boss_phase", global_position)
	phase_changed.emit(i)


func _finish_transition() -> void:
	var ph: BossPhase = cfg.phases[phase_index]
	if ph.shockwave_radius > 0.0:
		Fx.ring(global_position + Vector3.UP * 0.05, 0.5, ph.shockwave_radius, 0.4, Color(1, 0.5, 0.35))
		Fx.dust(global_position, 16)
		Game.shake(0.6)
		Sfx.play("hit_finisher", global_position)
		var hit := {}
		Combat.melee_arc(get_world_3d(), global_position, Vector3.FORWARD, ph.shockwave_radius, 360.0, DamageInfo.Team.ENEMY, hit,
			func(victim: Health) -> DamageInfo:
				return Combat.make_hit(self, DamageInfo.Team.ENEMY, victim, roundi(ph.shockwave_damage * Game.feel.enemy_damage_scale), false, ph.shockwave_knockback, 0.0, 0.0, 0.0, 3))
	if Game.spawner.is_valid():
		var n := ph.summons.size()
		for k in n:
			Game.spawner.call(ph.summons[k], global_position + Vector3.FORWARD.rotated(Vector3.UP, TAU * k / n) * 3.0)
	_set_state(State.MOVE)
	_next_decision = Game.time + _decision_delay


func _set_state(s: int) -> void:
	state = s
	_state_t = 0.0
