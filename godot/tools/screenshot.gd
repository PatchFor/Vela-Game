extends Node
## Captures a few gameplay screenshots with a bot driving the player (needs a real renderer):
##   xvfb-run -s "-screen 0 1600x900x24" godot --path . --rendering-driver opengl3 res://tools/screenshot.tscn
## Writes to user_shots/ (or the folder in the SHOTS_DIR env var).

var _main: Node
var _queued: Array = []
var _fight := false


func _ready() -> void:
	_main = load("res://scenes/main.tscn").instantiate()
	add_child(_main)
	_run.call_deferred()


func _bot() -> PlayerCommands:
	if not _queued.is_empty():
		return _queued.pop_front()
	var c := PlayerCommands.new()
	if not _fight:
		return c
	var p: Player = Game.player
	var best: Node3D = null
	var best_d := INF
	for e in Game.enemies:
		if is_instance_valid(e) and e.is_alive() and not e.cfg.immortal:
			var d := p.global_position.distance_to(e.global_position)
			if d < best_d:
				best_d = d
				best = e
	if best:
		var to := best.global_position - p.global_position
		if best_d > 2.0 + best.get_meta("radius", 0.4):
			c.move = Vector2(to.x, to.z).normalized()
		else:
			c.attack = true
	return c


func wait(s: float) -> void:
	var until := Game.real_time() + s
	while Game.real_time() < until:
		await get_tree().process_frame


func shot(name: String) -> void:
	await RenderingServer.frame_post_draw
	var dir := OS.get_environment("SHOTS_DIR")
	if dir == "":
		dir = ProjectSettings.globalize_path("res://user_shots")
	DirAccess.make_dir_recursive_absolute(dir)
	get_viewport().get_texture().get_image().save_png(dir.path_join(name + ".png"))
	print("saved " + name)


func _run() -> void:
	await wait(0.5)
	var p: Player = Game.player
	p.bot = _bot
	Game.god_mode = true
	Game.show_help = true

	# 1. Meadow fight: slimes, numbers, slashes.
	p.global_position = Vector3(5, 0, 23)
	_fight = true
	await wait(2.6)
	await shot("01_meadow_combat")

	# 2. Loot on the ground + labels + open inventory.
	Game.show_help = false
	Loot.drop(Game.level, Game.content.debug_loot, p.global_position + Vector3(0, 0, -2))
	_fight = false
	await wait(1.6)
	_main.inventory_ui._window.visible = true
	await wait(0.3)
	await shot("02_loot_inventory")
	_main.inventory_ui._window.visible = false

	# 3. Behind a tree: occlusion fade.
	p.global_position = Vector3(-5, 0, 12.2)
	await wait(1.0)
	await shot("03_occlusion_fade")

	# 4. Boss phase II.
	var boss: Enemy = null
	for e in Game.enemies:
		if e.is_boss():
			boss = e
	p.global_position = Vector3(0, 0, -29)
	boss._alert()
	await wait(0.3)
	boss.health.current = int(boss.health.max_hp * 0.52)
	boss.health.apply_damage(Combat.make_hit(p, DamageInfo.Team.PLAYER, boss.health, 60, true, 0.0, 0.0, 0.0, 0.0, 4))
	await wait(2.2)
	_fight = true
	await wait(1.2)
	await shot("04_boss_phase2")
	get_tree().quit()
