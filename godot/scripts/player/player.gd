class_name Player
extends CharacterBody3D
## The hero (Unity: PlayerController + PlayerCombat + PlayerTargeting + PlayerInventory +
## ItemPickupController). Reads only PlayerCommands, so `bot` can drive it in tests.
## States: normal (move / walk-to), attack (windup → active → recovery), dash, jump, hurt, dead.

enum State { NORMAL, ATTACK, CHARGING, DASH, JUMP, HURT, DEAD }

signal weapon_changed
signal equipment_changed

var cfg: PlayerConfig
var health: Health
var rig: SpriteRig
var inventory: Inventory
var input: PlayerInput
var bot: Callable ## optional: func() -> PlayerCommands (tests / replays / network)

var state := State.NORMAL
var weapon_index := 0
var equipment := {} ## slot -> ItemDef
var target: Node3D = null ## locked-on enemy
var hovered_item: Node = null
var hovered_enemy: Node = null
var skill_ready_at: Array[float] = [0.0, 0.0, 0.0, 0.0]
var dash_ready_at := 0.0
var counter_until := -10.0
var charge := 0.0 ## 0..1 while charging

var _facing_dir := Vector3.BACK
var _knock := Vector3.ZERO
var _move_vel := Vector3.ZERO
var _state_t := 0.0
var _hurt_until := 0.0
var _dash_dir := Vector3.ZERO
var _dash_start := -10.0
var _last_perfect := -10.0
var _jump_from := Vector3.ZERO
var _jump_to := Vector3.ZERO
var _walk_item: Node = null
var _reticle: MeshInstance3D

# attack in progress
var _atk: AttackStep
var _atk_phase := 0
var _atk_dir := Vector3.FORWARD
var _atk_hit := {}
var _atk_crit_chance := 0.0
var _atk_crit_mult := 1.0
var _atk_landed_at := -1.0
var _atk_is_combo := false
var _combo_index := -1
var _combo_last_end := -10.0
var _buffered := false


func _init() -> void:
	name = "Player"


func setup(config: PlayerConfig) -> void:
	cfg = config
	set_meta("radius", 0.4)
	collision_layer = 1
	collision_mask = 1
	var shape := CollisionShape3D.new()
	var cap := CapsuleShape3D.new()
	cap.radius = 0.4
	cap.height = 1.6
	shape.shape = cap
	shape.position.y = 0.8
	add_child(shape)

	health = Health.new()
	health.name = "Health"
	health.setup(cfg.max_hp, DamageInfo.Team.PLAYER, cfg.iframes_after_hit)
	add_child(health)
	health.damaged.connect(_on_damaged)
	health.evaded.connect(_on_evaded)
	health.died.connect(_on_died)

	rig = SpriteRig.new()
	rig.name = "Rig"
	add_child(rig)
	var body := {}
	var hair := {}
	for f in [[SpriteRig.Facing.DOWN, "Down"], [SpriteRig.Facing.UP, "Up"], [SpriteRig.Facing.SIDE, "Side"]]:
		body[f[0]] = Defaults.tex("Doll_Body_" + f[1])
		hair[f[0]] = Defaults.tex("Doll_Hair_" + f[1])
	rig.setup_doll(body, hair, Color(0.45, 0.3, 0.2), 1.8)

	input = PlayerInput.new()
	input.name = "Input"
	input.config = cfg
	add_child(input)

	inventory = Inventory.new(Game.loot_config.inventory_size)
	for it in cfg.starting_equipment:
		equip(it)
	for it in cfg.starting_items:
		inventory.add(it, 1)
	_apply_weapon_visual()

	_reticle = MeshInstance3D.new()
	_reticle.mesh = Fx.sector_mesh(360.0, 0.85, 1.0, 32)
	_reticle.material_override = Fx.material(Color(1, 0.35, 0.3, 0.9), true)
	_reticle.visible = false
	_reticle.top_level = true
	add_child(_reticle)


func weapon() -> WeaponConfig:
	return cfg.weapons[weapon_index]


func is_alive() -> bool:
	return state != State.DEAD and health.is_alive()


func dash_ratio() -> float:
	return clampf(1.0 - (dash_ready_at - Game.time) / cfg.dash_cooldown, 0.0, 1.0)


func skill_ratio(i: int) -> float:
	if i >= cfg.skills.size():
		return 0.0
	return clampf(1.0 - (skill_ready_at[i] - Game.time) / cfg.skills[i].cooldown, 0.0, 1.0)


func counter_active() -> bool:
	return Game.time < counter_until


# ------------------------------------------------------------------ frame

func _physics_process(delta: float) -> void:
	health.immortal = Game.god_mode
	var cmd: PlayerCommands = bot.call() if bot.is_valid() else input.read()
	_state_t += delta
	_update_hover(cmd)
	_update_target(cmd)
	if cmd.cycle_weapon and state != State.ATTACK and state != State.DEAD:
		cycle_weapon()

	var want := Vector3.ZERO
	match state:
		State.DEAD:
			_move_vel = Vector3.ZERO
			return
		State.NORMAL:
			want = _normal(cmd)
		State.CHARGING:
			want = _charging(cmd)
		State.ATTACK:
			want = _attacking(cmd, delta)
		State.DASH:
			_dashing(cmd)
		State.JUMP:
			_jumping()
			return
		State.HURT:
			if Game.time >= _hurt_until:
				_set_state(State.NORMAL)

	if state != State.DASH:
		var rate := cfg.acceleration if want.length() > 0.01 else cfg.deceleration
		_move_vel = _move_vel.move_toward(want, rate * delta)
	velocity = _move_vel + _knock
	velocity.y = 0.0
	move_and_slide()
	global_position.y = 0.0
	_knock = _knock.move_toward(Vector3.ZERO, 40.0 * delta)

	# Blink only for post-hit i-frames, not dash i-frames.
	rig.set_blink(health.is_invulnerable() and Game.time - _dash_start > cfg.dash_time + cfg.dash_iframe_bonus and state != State.JUMP, Game.feel.blink_rate)
	if state == State.NORMAL:
		rig.set_state(SpriteRig.State.MOVE if want.length() > 0.1 else SpriteRig.State.IDLE)


func _normal(cmd: PlayerCommands) -> Vector3:
	var move := Vector3(cmd.move.x, 0, cmd.move.y)
	if move.length() > 0.1:
		_walk_item = null
	if cmd.click_item and is_instance_valid(cmd.click_item):
		_walk_item = cmd.click_item
	if cmd.pick_up:
		_pick_nearest()
	if cmd.click_enemy and is_instance_valid(cmd.click_enemy):
		target = cmd.click_enemy
	if cmd.dash and _try_dash(cmd):
		return Vector3.ZERO
	if cmd.skill >= 0 and _try_skill(cmd.skill, cmd):
		return Vector3.ZERO
	if cmd.charge_held and weapon().has_charged:
		_set_state(State.CHARGING)
		charge = 0.0
		return Vector3.ZERO
	if cmd.attack or (cmd.attack_held and weapon().repeat_held):
		_start_combo(cmd)
		return Vector3.ZERO

	if _walk_item and is_instance_valid(_walk_item):
		var to: Vector3 = _walk_item.global_position - global_position
		to.y = 0.0
		if to.length() <= Game.loot_config.pickup_range * 0.8:
			try_pick(_walk_item)
			_walk_item = null
		else:
			move = to.normalized()
	elif _walk_item:
		_walk_item = null

	if move.length() > 1.0:
		move = move.normalized()
	if move.length() > 0.1:
		_face(move)
	elif cmd.has_aim:
		_face(cmd.aim_point - global_position)
	return move * cfg.move_speed


func _charging(cmd: PlayerCommands) -> Vector3:
	var w := weapon()
	charge = clampf(_state_t / w.charge_time, 0.0, 1.0)
	var move := Vector3(cmd.move.x, 0, cmd.move.y).limit_length(1.0)
	var aim := _aim_dir(cmd, w.charged)
	_face(aim)
	if charge >= 1.0 and not has_meta("charge_ready"):
		set_meta("charge_ready", true)
		Sfx.play("charge_ready", global_position)
		rig.flash(w.ui_color, 0.08)
		Fx.ring(global_position + Vector3.UP * 0.05, 0.3, 1.4, 0.25, w.ui_color)
	if cmd.dash and _try_dash(cmd):
		remove_meta("charge_ready")
		return Vector3.ZERO
	if not cmd.charge_held:
		var ready := charge >= 1.0
		charge = 0.0
		if has_meta("charge_ready"):
			remove_meta("charge_ready")
		_set_state(State.NORMAL)
		if ready:
			_start_attack(w.charged, aim, w.crit_chance, w.crit_mult, false)
		return Vector3.ZERO
	return move * cfg.move_speed * w.charge_move_mult


# ------------------------------------------------------------------ attacks

func _start_combo(cmd: PlayerCommands) -> void:
	var w := weapon()
	if w.combo.is_empty():
		return
	if Game.time - _combo_last_end > w.combo_reset:
		_combo_index = -1
	_combo_index = (_combo_index + 1) % w.combo.size()
	var s: AttackStep = w.combo[_combo_index]
	_start_attack(s, _aim_dir(cmd, s), w.crit_chance, w.crit_mult, true)


func _try_skill(i: int, cmd: PlayerCommands) -> bool:
	if i >= cfg.skills.size() or Game.time < skill_ready_at[i]:
		return false
	var k := cfg.skills[i]
	skill_ready_at[i] = Game.time + k.cooldown
	Sfx.play("skill", global_position)
	Fx.ring(global_position + Vector3.UP * 0.05, 0.4, 1.6, 0.3, k.color)
	_start_attack(k.attack, _aim_dir(cmd, k.attack), k.crit_chance, k.crit_mult, false)
	return true


func _start_attack(s: AttackStep, dir: Vector3, crit_chance: float, crit_mult: float, is_combo: bool) -> void:
	_atk = s
	_atk_phase = 0
	_atk_dir = dir
	_atk_hit = {}
	_atk_crit_chance = crit_chance
	_atk_crit_mult = crit_mult
	_atk_landed_at = -1.0
	_atk_is_combo = is_combo
	_buffered = false
	_walk_item = null
	_set_state(State.ATTACK)
	_face(dir)
	rig.set_state(SpriteRig.State.ATTACK)
	rig.punch(Vector2(0.88, 1.12))
	if s.windup <= 0.0:
		_enter_active()


func _attacking(cmd: PlayerCommands, delta: float) -> Vector3:
	var s := _atk
	var w := weapon()
	var active_end := s.windup + s.active
	var end := active_end + s.recovery

	if _atk_phase == 0 and _state_t >= s.windup:
		_enter_active()
	if _atk_phase == 1:
		if s.kind == AttackStep.Kind.MELEE:
			_melee_tick()
		if _state_t >= active_end:
			_atk_phase = 2

	# Buffer the next combo press near the end; hit-confirm lets it cancel recovery early.
	if cmd.attack and _atk_is_combo and end - _state_t <= w.input_buffer:
		_buffered = true
	var confirmed := _atk_landed_at >= 0.0 and Game.time - _atk_landed_at >= w.hit_confirm_delay
	if _atk_phase == 2:
		if cmd.dash and (w.dash_cancel or confirmed) and _try_dash(cmd):
			return Vector3.ZERO
		if _atk_is_combo and confirmed and w.hit_confirm_cancel and (cmd.attack or _buffered):
			_finish_attack()
			_start_combo(cmd)
			return Vector3.ZERO
	if _state_t >= end:
		_finish_attack()
		if _buffered or (cmd.attack_held and w.repeat_held and _atk_is_combo):
			_start_combo(cmd)
		return Vector3.ZERO

	# Lunge through windup + active; small steering otherwise.
	var lunge := Vector3.ZERO
	if s.lunge > 0.0 and _state_t < active_end:
		lunge = _atk_dir * (s.lunge / maxf(0.05, active_end))
	var move := Vector3(cmd.move.x, 0, cmd.move.y).limit_length(1.0) * cfg.move_speed * s.move_mult
	return lunge + move


func _finish_attack() -> void:
	_combo_last_end = Game.time
	_set_state(State.NORMAL)


func _enter_active() -> void:
	_atk_phase = 1
	var s := _atk
	var heavy := s.hit_weight >= 3 or DamageInfo.resolve_weight(s.hit_weight, s.stagger, s.damage) >= 3
	if s.kind == AttackStep.Kind.PROJECTILE:
		Sfx.play("shoot", global_position)
		var n := maxi(1, s.proj_count)
		for i in n:
			var off := 0.0 if n == 1 else lerpf(-s.spread * 0.5, s.spread * 0.5, float(i) / (n - 1))
			var d := _atk_dir.rotated(Vector3.UP, deg_to_rad(off))
			Projectile.fire(Game.level, global_position + Vector3.UP * 1.0 + d * 0.5, d, {
				"team": DamageInfo.Team.PLAYER, "speed": s.proj_speed, "range": s.proj_range,
				"radius": s.proj_size, "pierce": s.pierce, "make_hit": _hit_maker(s),
				"on_landed": func(_h): _on_landed(), "color": s.proj_color, "arrow": true,
			})
		return
	Sfx.play("swing_heavy" if heavy else "swing_light", global_position)
	var color := s.slash_color
	if counter_active():
		color = Game.feel.counter_color
	Fx.slash(global_position + Vector3.UP * 0.9, _atk_dir, s.attack_range, s.slash_width, s.arc, color, s.slash_duration, s.reverse)
	if s.arc >= 359.0:
		Fx.ring(global_position + Vector3.UP * 0.05, 0.5, s.attack_range, 0.25, color)
		Fx.dust(global_position, 8)
	_melee_tick()


func _melee_tick() -> void:
	var landed := Combat.melee_arc(get_world_3d(), global_position, _atk_dir, _atk.attack_range, _atk.arc, DamageInfo.Team.PLAYER, _atk_hit, _hit_maker(_atk))
	if landed > 0:
		_on_landed()


func _on_landed() -> void:
	if _atk_landed_at < 0.0:
		_atk_landed_at = Game.time
	counter_until = -10.0


func _hit_maker(s: AttackStep) -> Callable:
	var crit_chance := _atk_crit_chance
	var crit_mult := _atk_crit_mult
	var countering := counter_active()
	return func(victim: Health) -> DamageInfo:
		var amount := float(Combat.roll_damage(s.damage, s.variance))
		var crit := Game.roll() < crit_chance
		var counter := countering and victim.get_parent() != null
		if counter:
			amount *= cfg.counter_mult
			crit = crit or cfg.counter_always_crits
		var punish := false
		var body = victim.get_parent()
		if body and body.has_method("is_punishable") and body.is_punishable():
			punish = true
			amount *= Game.feel.punish_mult
		if crit:
			amount *= crit_mult
		var info := Combat.make_hit(self, DamageInfo.Team.PLAYER, victim, roundi(amount), crit,
			s.knockback * Game.feel.knockback_scale, s.stagger, s.hit_stop, s.shake, s.hit_weight)
		info.punish = punish
		info.counter = counter
		return info


## Aim: locked target > pointer > movement > facing, then aim assist toward a nearby enemy.
func _aim_dir(cmd: PlayerCommands, s: AttackStep) -> Vector3:
	var dir := _facing_dir
	if target and is_instance_valid(target):
		dir = target.global_position - global_position
	elif cmd.has_aim and (cmd.aim_point - global_position).length() > 0.3:
		dir = cmd.aim_point - global_position
	elif cmd.move.length() > 0.1:
		dir = Vector3(cmd.move.x, 0, cmd.move.y)
	dir.y = 0.0
	dir = dir.normalized() if dir.length() > 0.001 else Vector3.FORWARD
	if target == null or not is_instance_valid(target):
		var reach := (s.attack_range if s and s.kind == AttackStep.Kind.MELEE else 10.0) + cfg.aim_assist_range
		var best: Node3D = null
		var best_angle := cfg.aim_assist_angle
		for e in Game.enemies:
			if not is_instance_valid(e) or not e.is_alive():
				continue
			var to: Vector3 = e.global_position - global_position
			to.y = 0.0
			if to.length() > reach + e.get_meta("radius", 0.4):
				continue
			var ang := rad_to_deg(dir.angle_to(to))
			if ang < best_angle:
				best_angle = ang
				best = e
		if best:
			dir = (best.global_position - global_position) * Vector3(1, 0, 1)
			dir = dir.normalized()
	return dir


# ------------------------------------------------------------------ dash / perfect dodge / jump

func _try_dash(cmd: PlayerCommands) -> bool:
	if Game.time < dash_ready_at:
		return false
	var dir := Vector3(cmd.move.x, 0, cmd.move.y)
	if dir.length() < 0.1:
		dir = _facing_dir
	dir = dir.normalized()
	_walk_item = null
	for link in get_tree().get_nodes_in_group("jump_link"):
		var ends: Array = link.match_dash(global_position, dir, cfg.jump_max_angle)
		if not ends.is_empty():
			_start_jump(ends[1])
			return true
	_dash_dir = dir
	_dash_start = Game.time
	dash_ready_at = Game.time + cfg.dash_cooldown
	health.grant_invulnerability(cfg.dash_time + cfg.dash_iframe_bonus)
	_set_state(State.DASH)
	_face(dir)
	rig.punch(Vector2(1.25, 0.8))
	Fx.dust(global_position, 5)
	Sfx.play("dash", global_position)
	return true


func _dashing(_cmd: PlayerCommands) -> void:
	_move_vel = _dash_dir * cfg.dash_speed
	if Engine.get_physics_frames() % 2 == 0:
		Fx.afterimage(rig, Color(0.5, 0.8, 1, 0.5), 0.2)
	if _state_t >= cfg.dash_time:
		_move_vel = _dash_dir * cfg.move_speed
		_set_state(State.NORMAL)


func _on_evaded(_info: DamageInfo) -> void:
	# Perfect dodge: an attack met the i-frames right after the dash started.
	var since := Game.time - _dash_start
	if since > cfg.perfect_window + cfg.perfect_latency_allowance or _last_perfect >= _dash_start:
		return
	_last_perfect = Game.time
	counter_until = Game.time + cfg.counter_window
	if cfg.perfect_resets_dash:
		dash_ready_at = Game.time
	Game.slow_motion(cfg.perfect_slow_time, cfg.perfect_slow_scale)
	Game.message("PERFECT DODGE — counter!", Game.feel.counter_color, 1.2)
	Fx.ring(global_position + Vector3.UP * 0.05, 0.3, 2.5, 0.35, Game.feel.counter_color)
	Fx.afterimage(rig, Game.feel.counter_color, 0.45)
	Sfx.play("perfect_dodge", global_position)
	Game.stats.perfect_dodges += 1


func _start_jump(to: Vector3) -> void:
	_jump_from = global_position
	_jump_to = Vector3(to.x, 0, to.z)
	dash_ready_at = Game.time + cfg.dash_cooldown
	health.grant_invulnerability(cfg.jump_time)
	collision_mask = 0
	_set_state(State.JUMP)
	_face(_jump_to - _jump_from)
	rig.punch(Vector2(0.8, 1.25))
	Fx.dust(global_position, 6)
	Sfx.play("jump", global_position)


func _jumping() -> void:
	var t := clampf(_state_t / cfg.jump_time, 0.0, 1.0)
	global_position = _jump_from.lerp(_jump_to, t)
	rig.set_air(4.0 * cfg.jump_height * t * (1.0 - t))
	if t >= 1.0:
		rig.set_air(0.0)
		collision_mask = 1
		_move_vel = Vector3.ZERO
		_set_state(State.NORMAL)
		rig.punch(Vector2(1.3, 0.75))
		Fx.dust(global_position, 8)
		Sfx.play("land", global_position)


# ------------------------------------------------------------------ damage

func _on_damaged(info: DamageInfo) -> void:
	Feedback.player_hurt(self, rig, info)
	var armored := state == State.ATTACK and _atk and _atk.super_armor
	_knock = info.direction * info.knockback * Game.feel.knockback_scale * (0.3 if armored else 1.0)
	if armored or state == State.DEAD or state == State.JUMP:
		return
	_set_state(State.HURT)
	_hurt_until = Game.time + cfg.hurt_stun
	rig.set_state(SpriteRig.State.HURT)


func _on_died() -> void:
	_set_state(State.DEAD)
	target = null
	rig.set_base_tint(Color(0.45, 0.45, 0.5))
	Fx.death_burst(global_position + Vector3.UP, Color(1, 0.4, 0.4), 30)
	Game.message("You fell — T respawn · R restart", Color(1, 0.5, 0.5), 999.0)


func respawn(at: Vector3) -> void:
	global_position = at
	health.setup(cfg.max_hp, DamageInfo.Team.PLAYER, cfg.iframes_after_hit)
	health.grant_invulnerability(1.0)
	rig.set_base_tint(Color.WHITE)
	_knock = Vector3.ZERO
	_move_vel = Vector3.ZERO
	collision_mask = 1
	_set_state(State.NORMAL)
	Game.messages = Game.messages.filter(func(m): return m.duration < 100.0)


# ------------------------------------------------------------------ targeting / hover

func _update_hover(cmd: PlayerCommands) -> void:
	var new_item: Node = null
	var new_enemy: Node = null
	if cmd.has_aim:
		var best := cfg.hover_radius
		for w in Game.world_items:
			if is_instance_valid(w) and w.can_pick_up() and not w.is_gold():
				var d: float = (w.global_position - cmd.aim_point).length()
				if d < best:
					best = d
					new_item = w
		best = cfg.hover_radius
		for e in Game.enemies:
			if is_instance_valid(e) and e.is_alive():
				var d: float = (e.global_position * Vector3(1, 0, 1) - cmd.aim_point * Vector3(1, 0, 1)).length() - e.get_meta("radius", 0.4)
				if d < best:
					best = d
					new_enemy = e
	if hovered_item != new_item:
		if hovered_item and is_instance_valid(hovered_item):
			hovered_item.hovered = false
		if new_item:
			new_item.hovered = true
	hovered_item = new_item
	hovered_enemy = new_enemy
	input.hovered_item = new_item
	input.hovered_enemy = new_enemy


func _update_target(cmd: PlayerCommands) -> void:
	if target and (not is_instance_valid(target) or not target.is_alive() or global_position.distance_to(target.global_position) > cfg.lock_break_range):
		target = null
	if cmd.lock_on:
		target = null if target else _best_target(cmd)
		if target:
			Sfx.play("pickup", null, 0.4)
	if cmd.next_target:
		target = _next_target()
	_reticle.visible = target != null
	if target:
		var r: float = target.get_meta("radius", 0.4) + 0.35
		_reticle.global_position = target.global_position + Vector3.UP * 0.06
		_reticle.scale = Vector3(r, 1, r) * (1.0 + 0.08 * sin(Game.time * 8.0))
		_reticle.rotation.y += 0.03


func _candidates() -> Array:
	var list := []
	for e in Game.enemies:
		if is_instance_valid(e) and e.is_alive() and global_position.distance_to(e.global_position) <= cfg.lock_range:
			list.append(e)
	return list


func _best_target(cmd: PlayerCommands) -> Node3D:
	var ref := cmd.aim_point if cmd.has_aim else global_position + _facing_dir * 4.0
	var best: Node3D = null
	var best_d := INF
	for e in _candidates():
		var d: float = e.global_position.distance_to(ref)
		if d < best_d:
			best_d = d
			best = e
	return best


func _next_target() -> Node3D:
	var list := _candidates()
	if list.is_empty():
		return null
	list.sort_custom(func(x, y): return global_position.distance_to(x.global_position) < global_position.distance_to(y.global_position))
	var i := list.find(target)
	return list[(i + 1) % list.size()]


# ------------------------------------------------------------------ items & equipment

func _pick_nearest() -> void:
	var best: Node = hovered_item
	var best_d := Game.loot_config.pickup_range
	if best == null or not is_instance_valid(best) or best.global_position.distance_to(global_position) > best_d:
		best = null
		for w in Game.world_items:
			if is_instance_valid(w) and w.can_pick_up() and not w.is_gold():
				var d: float = w.global_position.distance_to(global_position)
				if d <= best_d:
					best_d = d
					best = w
	if best:
		try_pick(best)


## True if at least part of the stack went into the bag.
func try_pick(w: Node) -> bool:
	if not is_instance_valid(w) or not w.can_pick_up():
		return false
	var added := inventory.add(w.item, w.count)
	if added <= 0:
		w.nudge()
		Game.message("Inventory full", Color(1, 0.5, 0.4))
		Sfx.play("inventory_full")
		return false
	Game.stats.pickups += 1
	var rare: bool = w.rarity() >= ItemDef.Rarity.RARE
	Sfx.play("pickup_rare" if rare else "pickup", global_position)
	Game.message("+ " + w.item.display_name + (" x%d" % added if added > 1 else ""), w.label_color(), 1.4)
	if added < w.count:
		w.count -= added
		w.nudge()
		Game.message("Inventory full", Color(1, 0.5, 0.4))
	else:
		w.collect()
	return true


## Puts `it` on; returns what was in that slot (or null).
func equip(it: ItemDef) -> ItemDef:
	if it == null or not it.is_equipment():
		return null
	var slot := it.equipment.slot
	var old: ItemDef = equipment.get(slot)
	equipment[slot] = it
	rig.set_equipment(slot, it.equipment)
	equipment_changed.emit()
	return old


func unequip(slot: String) -> ItemDef:
	var old: ItemDef = equipment.get(slot)
	equipment.erase(slot)
	rig.set_equipment(slot, null)
	equipment_changed.emit()
	return old


## Right-click / double-click in the bag: drink potions, wear equipment.
func use_slot(index: int) -> void:
	var s = inventory.slots[index]
	if s == null:
		return
	var it: ItemDef = s.item
	if it.heal > 0:
		if health.heal(it.heal):
			inventory.remove_at(index, 1)
			Fx.number(global_position + Vector3.UP * 2.0, "+%d" % it.heal, Game.feel.heal_color, 1.0)
			Sfx.play("pickup", global_position)
		else:
			Game.message("Already at full health", Color(0.8, 0.8, 0.8), 1.0)
	elif it.is_equipment():
		inventory.remove_at(index, 1)
		var old := equip(it)
		if old:
			inventory.place_at(index, old, 1)
		Sfx.play("pickup", global_position, 0.7)


func cycle_weapon() -> void:
	weapon_index = (weapon_index + 1) % cfg.weapons.size()
	_combo_index = -1
	_apply_weapon_visual()
	Game.message(weapon().display_name, weapon().ui_color, 1.0)
	weapon_changed.emit()


func _apply_weapon_visual() -> void:
	rig.set_equipment("weapon", weapon().visual)


# ------------------------------------------------------------------ helpers

func _set_state(s: int) -> void:
	state = s
	_state_t = 0.0


func _face(dir: Vector3) -> void:
	dir.y = 0.0
	if dir.length() < 0.01:
		return
	_facing_dir = dir.normalized()
	rig.set_facing(_facing_dir)


func facing_dir() -> Vector3:
	return _facing_dir


func hit_pause(seconds: float) -> void:
	rig.hit_pause(seconds)
