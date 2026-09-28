extends Node
## Pure-logic tests (same rules as the Unity EditMode tests). Run headless via tests/run.sh, or:
##   godot --headless --path godot res://tests/run_tests.tscn
## Exit code 0 = all passed.

var _passed := 0
var _failed := 0


func _ready() -> void:
	_run.call_deferred()


func _run() -> void:
	for m in get_method_list():
		if m.name.begins_with("test_"):
			call(m.name)
	print("\n%d passed, %d failed" % [_passed, _failed])
	get_tree().quit(1 if _failed > 0 else 0)


func check(cond: bool, what: String) -> void:
	if cond:
		_passed += 1
	else:
		_failed += 1
		printerr("FAIL: " + what)


func _item(stack: int, category := ItemDef.Category.MATERIAL) -> ItemDef:
	var it := ItemDef.new()
	it.max_stack = stack
	it.category = category
	return it


# ------------------------------------------------------------------ inventory

func test_inventory_stacks_before_new_slots() -> void:
	var inv := Inventory.new(4)
	var gel := _item(10)
	check(inv.add(gel, 15) == 15, "adds 15")
	check(inv.slots[0].count == 10 and inv.slots[1].count == 5, "fills a stack to max then opens a new slot")
	inv.add(gel, 3)
	check(inv.slots[1].count == 8, "tops up the partial stack first")


func test_inventory_full_returns_partial() -> void:
	var inv := Inventory.new(2)
	var sword := _item(1)
	check(inv.add(sword, 3) == 2, "only 2 fit in 2 slots")
	check(inv.is_full(), "bag is full")
	check(inv.add(sword, 1) == 0, "full bag adds nothing (item stays on the ground)")


func test_gold_never_uses_a_slot() -> void:
	var inv := Inventory.new(1)
	inv.add(_item(1), 1)
	var gold := _item(9999, ItemDef.Category.CURRENCY)
	check(inv.add(gold, 25) == 25 and inv.gold == 25, "gold goes to the purse even when the bag is full")


func test_inventory_move_merges_and_swaps() -> void:
	var inv := Inventory.new(3)
	var gel := _item(10)
	var bone := _item(10)
	inv.place_at(0, gel, 6)
	inv.place_at(1, gel, 7)
	inv.place_at(2, bone, 1)
	inv.move(0, 1)
	check(inv.slots[1].count == 10 and inv.slots[0].count == 3, "merge fills the target, overflow stays")
	inv.move(0, 2)
	check(inv.slots[0].item == bone and inv.slots[2].item == gel, "different items swap")
	var taken := inv.remove_at(2)
	check(taken.count == 3 and inv.slots[2] == null, "remove_at empties the slot")


# ------------------------------------------------------------------ loot

func test_loot_rates_match_weights() -> void:
	var a := _item(99)
	var b := _item(99)
	var t := LootTable.new()
	t.gold_chance = 0.5
	t.gold_min = 2
	t.gold_max = 4
	t.nothing_weight = 2.0
	t.items = [a, b]
	t.weights = PackedFloat32Array([1.0, 1.0])
	var rng := RandomNumberGenerator.new()
	rng.seed = 1234
	var n := 20000
	var gold_hits := 0
	var counts := {a: 0, b: 0}
	var gold_ok := true
	for i in n:
		var r := t.roll(rng.randf)
		if r.gold > 0:
			gold_hits += 1
			gold_ok = gold_ok and r.gold >= 2 and r.gold <= 4
		for it in r.items:
			counts[it] += 1
	check(absf(gold_hits / float(n) - 0.5) < 0.02, "gold chance ~50%% (got %.3f)" % (gold_hits / float(n)))
	check(gold_ok, "gold amount stays in [min, max]")
	check(absf(counts[a] / float(n) - 0.25) < 0.02, "item A ~25%% (nothing weight 2 of 4)")
	check(absf(counts[b] / float(n) - 0.25) < 0.02, "item B ~25%%")


func test_loot_guaranteed_always_drops() -> void:
	var boss_item := _item(1)
	var t := LootTable.new()
	t.gold_chance = 0.0
	t.guaranteed = [boss_item]
	t.rolls = 0
	var always := true
	for i in 50:
		always = always and t.roll(randf).items.has(boss_item)
	check(always, "guaranteed item drops every time")


# ------------------------------------------------------------------ combat rules

func test_hit_weight_auto_thresholds() -> void:
	check(DamageInfo.resolve_weight(0, 5.0, 10) == 1, "small hit = light")
	check(DamageInfo.resolve_weight(0, 15.0, 10) == 2, "stagger 15 = medium")
	check(DamageInfo.resolve_weight(0, 0.0, 25) == 3, "25 damage = heavy")
	check(DamageInfo.resolve_weight(0, 60.0, 1) == 4, "stagger 60 = finisher")
	check(DamageInfo.resolve_weight(2, 99.0, 99) == 2, "explicit weight wins over auto")


func test_feel_profiles_by_weight() -> void:
	var f := Defaults.feel()
	check(f.profile(1) == f.light and f.profile(4) == f.finisher, "weight → profile mapping")
	check(f.finisher.hit_stop_mult > f.light.hit_stop_mult, "finisher freezes longer than light")


func test_health_iframes_emit_evaded() -> void:
	var h := Health.new()
	h.setup(50, DamageInfo.Team.PLAYER)
	var evaded := [0]
	h.evaded.connect(func(_i): evaded[0] += 1)
	h.grant_invulnerability(1.0)
	var info := DamageInfo.new()
	info.amount = 10
	info.team = DamageInfo.Team.ENEMY
	check(not h.apply_damage(info) and h.current == 50, "i-frames block damage")
	check(evaded[0] == 1, "blocked hit emits evaded (perfect dodge hook)")
	h.free()


func test_same_team_cannot_hurt() -> void:
	var h := Health.new()
	h.setup(50, DamageInfo.Team.ENEMY)
	var info := DamageInfo.new()
	info.amount = 10
	info.team = DamageInfo.Team.ENEMY
	check(not h.apply_damage(info), "no friendly fire")
	h.free()


func test_immortal_stays_at_one() -> void:
	var h := Health.new()
	h.setup(10, DamageInfo.Team.ENEMY)
	h.immortal = true
	var info := DamageInfo.new()
	info.amount = 999
	info.team = DamageInfo.Team.PLAYER
	h.apply_damage(info)
	check(h.current == 1 and h.is_alive(), "training dummy never dies")
	h.free()


# ------------------------------------------------------------------ world

func test_jump_link_needs_position_and_direction() -> void:
	var j := JumpLink.new()
	j.a = Vector3(0, 0, 6)
	j.b = Vector3(0, 0, -2)
	j.trigger_radius = 1.6
	check(not j.match_dash(Vector3(0, 0, 6.5), Vector3.FORWARD, 65.0).is_empty(), "dash toward the far pad jumps")
	check(j.match_dash(Vector3(0, 0, 6.5), Vector3.RIGHT, 65.0).is_empty(), "dash sideways does not jump")
	check(j.match_dash(Vector3(0, 0, 10), Vector3.FORWARD, 65.0).is_empty(), "too far from the pad does not jump")
	check(not j.match_dash(Vector3(0, 0, -2.5), Vector3.BACK, 65.0).is_empty(), "works both ways")
	j.free()


# ------------------------------------------------------------------ content

func test_default_content() -> void:
	var it := Defaults.items()
	var m := Defaults.monsters(it)
	for key in ["slime", "archer", "brute", "bat", "cultist"]:
		check(m.has(key) and not m[key].attacks.is_empty(), "monster %s has attacks" % key)
	check(m.boss.is_boss() and m.boss.phases.size() == 2, "boss has 2 phases")
	check(m.boss.phases[1].starts_at == 0.5, "phase II at 50% HP")
	var p := Defaults.player(it)
	check(p.weapons.size() == 3 and p.skills.size() == 4, "3 weapons, 4 skills")
