extends Node
## Autoload "Game": shared state for the prototype.
## Registries, configs, game clock, hit-stop / slow-mo, camera trauma, RNG, HUD messages, combo.
## (In Unity these were VelaSettings, CombatRegistry, HitStop, CameraShake, VelaRandom, HudMessages, ComboTracker.)

signal enemy_defeated(enemy)

var feel: FeelConfig
var loot_config: LootConfig
var content: Dictionary = {} ## items, monsters, player, debug_loot

var player: Node3D
var camera: Camera3D
var level: Node3D ## parent for enemies, drops, projectiles
var spawner: Callable ## func(cfg: MonsterConfig, pos: Vector3) -> Enemy, set by main
var enemies: Array = []
var world_items: Array = []

## Scaled game clock (pauses with hit-stop). Use instead of Time for gameplay timers.
var time := 0.0

var trauma := 0.0
var zoom_punch := 0.0

var rng := RandomNumberGenerator.new()

var messages: Array = [] ## {text, color, t, duration}
var combo := 0
var combo_last := -10.0
var combo_pop := -10.0
var last_crit := -10.0
var kills := 0
var god_mode := false
var boss_defeated := false
var show_help := true

## Test/bot hooks
var stats := {"hits": 0, "kills": 0, "drops": 0, "pickups": 0, "perfect_dodges": 0, "player_hits": 0}

var _freeze_until := 0.0
var _slow_until := 0.0
var _slow_scale := 1.0
var _last_real := 0.0


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	rng.randomize()
	_setup_input()
	load_content()
	_last_real = real_time()


## Content comes from res://data/*.tres when present (tunable in the inspector; generate with
## tools/make_data.gd), otherwise from the built-in Defaults.
func load_content() -> void:
	var items := Defaults.items()
	for key in items.keys():
		items[key] = _load_or("res://data/items/%s.tres" % key, items[key])
	content.items = items
	var monsters := Defaults.monsters(items)
	for key in monsters.keys():
		monsters[key] = _load_or("res://data/monsters/%s.tres" % key, monsters[key])
	content.monsters = monsters
	content.player = _load_or("res://data/player.tres", Defaults.player(items))
	content.debug_loot = Defaults.debug_loot(items)
	feel = _load_or("res://data/feel.tres", Defaults.feel())
	loot_config = _load_or("res://data/loot_config.tres", Defaults.loot_config(items))


func _load_or(path: String, fallback):
	if ResourceLoader.exists(path):
		return load(path)
	return fallback


func real_time() -> float:
	return Time.get_ticks_usec() / 1_000_000.0


func _process(delta: float) -> void:
	time += delta
	var now := real_time()
	var real_delta := minf(now - _last_real, 0.1)
	_last_real = now

	if now < _freeze_until:
		Engine.time_scale = 0.02
	elif now < _slow_until:
		Engine.time_scale = _slow_scale
	else:
		Engine.time_scale = 1.0
		_slow_scale = 1.0

	trauma = maxf(0.0, trauma - feel.shake_decay * real_delta)
	zoom_punch = move_toward(zoom_punch * exp(-12.0 * real_delta), 0.0, 0.02 * real_delta)

	var i := messages.size() - 1
	while i >= 0:
		if now - messages[i].t > messages[i].duration:
			messages.remove_at(i)
		i -= 1


# ------------------------------------------------------------------ feel

## Freeze-frame. GLOBAL mode stops time; LOCAL mode only holds the participants' poses (online-safe).
func hit_stop(seconds: float, a: Node = null, b: Node = null) -> void:
	if seconds <= 0.0:
		return
	if feel.hit_stop_mode == FeelConfig.HitStopMode.LOCAL_VISUAL:
		for p in [a, b]:
			if p and is_instance_valid(p) and p.has_method("hit_pause"):
				p.hit_pause(seconds)
		return
	_freeze_until = maxf(_freeze_until, real_time() + seconds)


func slow_motion(seconds: float, scale: float) -> void:
	if seconds <= 0.0 or feel.hit_stop_mode == FeelConfig.HitStopMode.LOCAL_VISUAL:
		return
	var start := maxf(real_time(), _freeze_until)
	_slow_until = maxf(_slow_until, start + seconds)
	_slow_scale = minf(_slow_scale, clampf(scale, 0.05, 1.0))


func shake(amount: float) -> void:
	if amount > 0.0:
		trauma = clampf(trauma + amount * feel.shake_scale, 0.0, 1.0)


func punch(amount: float) -> void:
	if amount > 0.0:
		zoom_punch = clampf(maxf(zoom_punch, amount * feel.zoom_punch_scale), 0.0, 0.4)


func roll() -> float:
	return rng.randf()


func message(text: String, color := Color.WHITE, duration := 1.8) -> void:
	for m in messages:
		if m.text == text:
			m.t = real_time()
			m.color = color
			return
	messages.append({"text": text, "color": color, "t": real_time(), "duration": duration})
	if messages.size() > 5:
		messages.remove_at(0)


func register_hit(crit: bool) -> void:
	if time - combo_last > feel.combo_timeout:
		combo = 0
	combo += 1
	combo_last = time
	combo_pop = real_time()
	if crit:
		last_crit = real_time()
	stats.hits += 1


func combo_active() -> bool:
	return combo > 0 and time - combo_last <= feel.combo_timeout


func reset_run() -> void:
	enemies.clear()
	world_items.clear()
	messages.clear()
	combo = 0
	kills = 0
	boss_defeated = false
	trauma = 0.0
	zoom_punch = 0.0
	Engine.time_scale = 1.0
	_freeze_until = 0.0
	_slow_until = 0.0


# ------------------------------------------------------------------ input map

func _setup_input() -> void:
	var keys := {
		"move_left": [KEY_A, KEY_LEFT], "move_right": [KEY_D, KEY_RIGHT],
		"move_up": [KEY_W, KEY_UP], "move_down": [KEY_S, KEY_DOWN],
		"dash": [KEY_SPACE, KEY_SHIFT], "attack": [KEY_J], "charge": [KEY_K],
		"skill_1": [KEY_1], "skill_2": [KEY_2], "skill_3": [KEY_3], "skill_4": [KEY_4],
		"lock_on": [KEY_Q], "next_target": [KEY_E], "pick_up": [KEY_F], "inventory": [KEY_I],
		"cycle_weapon": [KEY_TAB], "restart": [KEY_R], "respawn": [KEY_T], "god_mode": [KEY_G],
		"to_boss": [KEY_B], "help": [KEY_H, KEY_F1], "debug_loot": [KEY_F5], "outfit": [KEY_O],
		"zoom_in": [KEY_EQUAL], "zoom_out": [KEY_MINUS],
	}
	for action in keys:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
		for code in keys[action]:
			var ev := InputEventKey.new()
			ev.physical_keycode = code
			InputMap.action_add_event(action, ev)
