extends Node3D
## Builds the whole prototype at startup (Unity: PrototypeSceneBuilder + CombatGameManager):
## level, player, monsters, camera, occlusion fader, HUD, inventory — then handles the debug keys.

var level: Level
var player: Player
var camera: CameraRig
var hud: Hud
var inventory_ui: InventoryUI

var _outfit_index := 0


func _ready() -> void:
	Game.reset_run()
	Fx.clear()
	level = Level.new()
	level.name = "Level"
	add_child(level)
	level.build()
	Game.level = level
	Game.spawner = spawn_monster

	player = Player.new()
	level.add_child(player)
	player.setup(Game.content.player)
	player.global_position = level.player_start
	Game.player = player

	for s in level.spawns:
		spawn_monster(Game.content.monsters[s[0]], s[1], false)

	camera = CameraRig.new()
	add_child(camera)
	camera.snap_to(player.global_position)
	add_child(OcclusionFader.new())

	var ui := CanvasLayer.new()
	add_child(ui)
	hud = Hud.new()
	ui.add_child(hud)
	inventory_ui = InventoryUI.new()
	inventory_ui.player = player
	ui.add_child(inventory_ui)

	Game.message("Vela — Godot prototype", Color(0.8, 0.9, 1), 3.0)


func spawn_monster(cfg: MonsterConfig, pos: Vector3, alert := true) -> Enemy:
	var e := Enemy.create(level, cfg, pos)
	if alert:
		e._alert()
		Fx.ring(pos + Vector3.UP * 0.05, 0.2, 1.5, 0.3, Color(0.8, 0.5, 1))
	return e


func _unhandled_key_input(event: InputEvent) -> void:
	if not event.is_pressed() or event.is_echo():
		return
	if event.is_action("restart"):
		get_tree().reload_current_scene()
	elif event.is_action("respawn"):
		player.respawn(level.player_start)
	elif event.is_action("god_mode"):
		Game.god_mode = not Game.god_mode
		Game.message("God mode " + ("ON" if Game.god_mode else "OFF"), Color(1, 0.9, 0.5), 1.2)
	elif event.is_action("to_boss"):
		if player.is_alive():
			_teleport(level.boss_gate)
		else:
			player.respawn(level.boss_gate)
	elif event.is_action("help"):
		Game.show_help = not Game.show_help
	elif event.is_action("debug_loot"):
		Loot.drop(level, Game.content.debug_loot, player.global_position + player.facing_dir() * 2.0)
		Game.message("Loot shower!", Color(1, 0.8, 0.3), 1.2)
	elif event.is_action("outfit"):
		_cycle_outfit()


func _teleport(pos: Vector3) -> void:
	player.global_position = pos
	camera.snap_to(pos)
	Fx.ring(pos + Vector3.UP * 0.05, 0.3, 2.0, 0.3, Color(0.6, 0.9, 1))


func _cycle_outfit() -> void:
	var it: Dictionary = Game.content.items
	var sets := [
		[it.hood, it.vest, it.gloves, it.boots],
		[it.helm, it.plate, it.gloves, it.greaves],
		[it.crown, it.warden_plate, it.ember_gloves, it.greaves],
		[],
	]
	_outfit_index = (_outfit_index + 1) % sets.size()
	for slot in ["head", "chest", "hands", "feet"]:
		player.unequip(slot)
	for piece in sets[_outfit_index]:
		player.equip(piece)
	Game.message("Outfit %d" % (_outfit_index + 1), Color(0.8, 0.9, 1), 1.0)
