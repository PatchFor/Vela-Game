extends Node
## Bot smoke test: loads the real game, drives the player through PlayerCommands (no keyboard)
## and checks the systems work together — perfect dodge, jump links, water, combat, kills, loot,
## pickups, a full inventory, the bow, and both boss phases. Run via tests/run.sh.

var _passed := 0
var _failed := 0
var _main: Node
var _queued: Array = [] ## PlayerCommands to play one per physics frame
var _fight := false


func _ready() -> void:
	_main = load("res://scenes/main.tscn").instantiate()
	add_child(_main)
	_run.call_deferred()


func check(cond: bool, what: String) -> void:
	if cond:
		_passed += 1
		print("  ok  " + what)
	else:
		_failed += 1
		printerr("FAIL: " + what)


func frames(n: int) -> void:
	for i in n:
		await get_tree().physics_frame


func seconds(s: float) -> void:
	var until := Game.time + s
	var guard := 0
	while Game.time < until and guard < 20000:
		await get_tree().physics_frame
		guard += 1


func cmd(move := Vector2.ZERO) -> PlayerCommands:
	var c := PlayerCommands.new()
	c.move = move
	return c


func _bot() -> PlayerCommands:
	if not _queued.is_empty():
		return _queued.pop_front()
	return _fight_cmd() if _fight else PlayerCommands.new()


## Simple fighter: walk to the nearest meadow monster and swing; collect items when it's quiet.
func _fight_cmd() -> PlayerCommands:
	var p: Player = Game.player
	var c := PlayerCommands.new()
	var best: Node3D = null
	var best_d := INF
	for e in Game.enemies:
		if is_instance_valid(e) and e.is_alive() and not e.cfg.immortal and e.global_position.z > 5.0:
			var d := p.global_position.distance_to(e.global_position)
			if d < best_d:
				best_d = d
				best = e
	if best:
		var to := best.global_position - p.global_position
		if best_d > 1.8 + best.get_meta("radius", 0.4):
			c.move = Vector2(to.x, to.z).normalized()
		else:
			c.attack = true
		return c
	for w in Game.world_items:
		if is_instance_valid(w) and w.can_pick_up() and not w.is_gold():
			var to: Vector3 = w.global_position - p.global_position
			if to.length() < 1.2:
				c.pick_up = true
			else:
				c.move = Vector2(to.x, to.z).normalized()
			return c
	return c


func _teleport(pos: Vector3) -> void:
	Game.player.global_position = pos
	await frames(2)


func _run() -> void:
	await frames(5)
	var p: Player = Game.player
	p.bot = _bot
	check(p != null and Game.enemies.size() >= 14, "scene built: player + %d monsters" % Game.enemies.size())
	check(Game.content.monsters.slime.loot.items.has(Game.content.items.potion), "monster loot and the bag share item resources (potions stack)")

	# --- Perfect dodge: a hit landing right after the dash starts is evaded and opens a counter.
	var slime: Enemy = null
	for e in Game.enemies:
		if e.cfg.display_name == "Green Slime":
			slime = e
			break
	var hp := p.health.current
	_queued = [_dash_cmd(Vector2(1, 0))]
	await frames(2)
	p.health.apply_damage(Combat.make_hit(slime, DamageInfo.Team.ENEMY, p.health, 10, false, 5.0, 0.0, 0.0, 0.0, 2))
	check(Game.stats.perfect_dodges == 1, "perfect dodge triggers on a hit during the dash window")
	check(p.health.current == hp, "perfect dodge takes no damage")
	check(p.counter_active(), "perfect dodge opens the counter window")
	await seconds(0.8)

	# --- A late hit after the dash is just damage.
	p.health.apply_damage(Combat.make_hit(slime, DamageInfo.Team.ENEMY, p.health, 10, false, 5.0, 0.0, 0.0, 0.0, 2))
	check(p.health.current < hp and Game.stats.perfect_dodges == 1, "hit outside the window hurts, no dodge")
	await seconds(0.8)

	# --- Jump link: dash toward the far pad crosses the river.
	await _teleport(Vector3(-12, 0, 6.6))
	p.dash_ready_at = 0.0
	_queued = [_dash_cmd(Vector2(0, -1))]
	await seconds(p.cfg.jump_time + 0.3)
	check(p.global_position.z < 0.0, "jump link carries the player over the water (z=%.1f)" % p.global_position.z)

	# --- Water blocks walking.
	await _teleport(Vector3(-22, 0, 7))
	for i in 90:
		_queued.append(cmd(Vector2(0, -1)))
	await frames(95)
	check(p.global_position.z > 3.5, "water blocks walking (z=%.1f)" % p.global_position.z)

	# --- Fight the meadow with the sword.
	await _teleport(Vector3(4, 0, 24))
	Game.god_mode = true
	_fight = true
	var guard := 0
	while guard < 60 * 60 and Game.stats.kills < 5:
		await get_tree().physics_frame
		guard += 1
	await seconds(4.0) # let loot pop, gold fly in, bot pick up
	_fight = false
	check(Game.stats.hits > 5, "sword hits land (%d hits)" % Game.stats.hits)
	check(Game.stats.kills >= 4, "bot killed the meadow monsters (%d kills)" % Game.stats.kills)
	check(Game.stats.drops > 0, "monsters drop loot (%d drops)" % Game.stats.drops)
	check(Game.stats.pickups > 0, "gold/items were picked up (%d pickups, %d gold)" % [Game.stats.pickups, p.inventory.gold])

	# --- Full bag: picking up fails and the item stays.
	var bag := p.inventory
	var rock := ItemDef.new()
	rock.max_stack = 1
	while bag.add(rock, 1) > 0:
		pass
	var w := WorldItem.spawn(Game.level, Game.content.items.helm, 1, 0, p.global_position, p.global_position + Vector3(1, 0, 0), 0.0)
	await seconds(1.0)
	check(not p.try_pick(w) and is_instance_valid(w), "full inventory refuses the pickup, item stays on the ground")
	w.queue_free()

	# --- Bow fires projectiles.
	p.cycle_weapon()
	var before := _count_projectiles()
	var shot := cmd()
	shot.attack = true
	_queued = [shot]
	await seconds(0.2)
	check(_count_projectiles() > before, "bow fires an arrow")

	# --- Boss: phase II at 50%, then defeat.
	await _teleport(Vector3(0, 0, -26))
	var boss: Enemy = null
	for e in Game.enemies:
		if e.is_boss():
			boss = e
	boss.health.current = int(boss.health.max_hp * 0.52)
	boss.health.apply_damage(Combat.make_hit(p, DamageInfo.Team.PLAYER, boss.health, int(boss.health.max_hp * 0.05), false, 0.0, 0.0, 0.0, 0.0, 1))
	check(boss.phase_index == 1 and boss.state == Enemy.State.TRANSITION, "boss enters phase II below 50%")
	var enemies_before := Game.enemies.size()
	await seconds(boss.cfg.phases[1].transition + 0.2)
	check(Game.enemies.size() > enemies_before, "phase II summons adds")
	boss.health.current = 1
	await seconds(0.1)
	var drops: int = Game.stats.drops
	boss.health.apply_damage(Combat.make_hit(p, DamageInfo.Team.PLAYER, boss.health, 50, false, 0.0, 0.0, 0.0, 0.0, 4))
	await frames(2)
	check(Game.boss_defeated, "boss can be defeated")
	check(Game.stats.drops > drops, "boss drops loot")

	print("\n%d passed, %d failed" % [_passed, _failed])
	get_tree().quit(1 if _failed > 0 else 0)


func _dash_cmd(move: Vector2) -> PlayerCommands:
	var c := cmd(move)
	c.dash = true
	return c


func _count_projectiles() -> int:
	var n := 0
	for c in Game.level.get_children():
		if c is Projectile:
			n += 1
	return n
