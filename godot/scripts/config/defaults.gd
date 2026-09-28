class_name Defaults
extends RefCounted
## Default content (same numbers as the Unity ConfigDefaults). tools/make_data.gd saves these as
## .tres files under res://data so they can be tuned in the Godot inspector.

const ART := "res://art/"


static func tex(name: String) -> Texture2D:
	var path := ART + name + ".png"
	if ResourceLoader.exists(path):
		return load(path)
	# Not imported yet (fresh clone, headless run): load the PNG directly.
	var img := Image.load_from_file(ProjectSettings.globalize_path(path))
	return ImageTexture.create_from_image(img) if img else null


static func step(props: Dictionary) -> AttackStep:
	var s := AttackStep.new()
	for key in props:
		s.set(key, props[key])
	return s


static func attack(props: Dictionary) -> EnemyAttack:
	var a := EnemyAttack.new()
	for key in props:
		a.set(key, props[key])
	return a


# ------------------------------------------------------------------ feel

static func profile(label: String, props: Dictionary) -> ImpactProfile:
	var p := ImpactProfile.new()
	p.label = label
	for key in props:
		p.set(key, props[key])
	return p


static func feel() -> FeelConfig:
	var f := FeelConfig.new()
	f.light = profile("Light", {hit_stop_mult = 1.0, squash = 0.15, tremble_time = 0.06, tremble_amount = 0.04, spark_mult = 0.8, number_scale = 0.9})
	f.medium = profile("Medium", {hit_stop_mult = 1.1, hit_stop_bonus = 0.01, extra_shake = 0.04, squash = 0.22, tremble_time = 0.09, tremble_amount = 0.06, spark_mult = 1.2, number_scale = 1.05, number_color = Color(1, 0.97, 0.88)})
	f.heavy = profile("Heavy", {hit_stop_mult = 1.2, hit_stop_bonus = 0.03, extra_shake = 0.12, zoom_punch = 0.03, squash = 0.32, tremble_time = 0.14, tremble_amount = 0.1, knockback_mult = 1.15, spark_mult = 1.8, impact_ring = true, ring_radius = 1.4, dust = 6, number_scale = 1.3, number_color = Color(1, 0.8, 0.55)})
	f.finisher = profile("Finisher", {hit_stop_mult = 1.3, hit_stop_bonus = 0.05, extra_shake = 0.2, zoom_punch = 0.06, squash = 0.42, tremble_time = 0.2, tremble_amount = 0.14, knockback_mult = 1.3, spark_mult = 2.4, impact_ring = true, ring_radius = 2.0, dust = 12, number_scale = 1.55, number_color = Color(1, 0.65, 0.35)})
	return f


# ------------------------------------------------------------------ equipment & items

static func visual(slot: String, base: String, tint: Color, hides_hair := false) -> EquipmentVisual:
	var v := EquipmentVisual.new()
	v.slot = slot
	v.down = tex("Doll_%s_Down" % base)
	v.up = tex("Doll_%s_Up" % base)
	v.side = tex("Doll_%s_Side" % base)
	v.tint = tint
	v.hides_hair = hides_hair
	return v


static func item(display: String, icon: Texture2D, rarity: int, category: int, max_stack := 99, heal := 0, equipment: EquipmentVisual = null) -> ItemDef:
	var i := ItemDef.new()
	i.display_name = display
	i.icon = icon
	i.rarity = rarity
	i.category = category
	i.max_stack = max_stack
	i.heal = heal
	i.equipment = equipment
	return i


static func equip(display: String, v: EquipmentVisual, rarity: int) -> ItemDef:
	return item(display, v.down, rarity, ItemDef.Category.EQUIPMENT, 1, 0, v)


## All items, keyed by id.
static func items() -> Dictionary:
	var d := {}
	d.gold = item("Gold", tex("Icon_Gold"), 0, ItemDef.Category.CURRENCY, 9999)
	d.potion = item("Health Potion", tex("Icon_Potion"), 0, ItemDef.Category.CONSUMABLE, 10, 40)
	d.gel = item("Slime Gel", tex("Icon_Gel"), 0, ItemDef.Category.MATERIAL)
	d.bone = item("Old Bone", tex("Icon_Bone"), 0, ItemDef.Category.MATERIAL)
	d.ember = item("Ember Shard", tex("Icon_Ember"), 1, ItemDef.Category.MATERIAL, 50)
	d.hood = equip("Leather Hood", visual("head", "Hood", Color(0.62, 0.45, 0.3), true), 0)
	d.helm = equip("Iron Helm", visual("head", "Helm", Color(0.78, 0.82, 0.9), true), 2)
	d.crown = equip("Warden's Crown", visual("head", "Crown", Color(1, 0.8, 0.3)), 4)
	d.vest = equip("Leather Vest", visual("chest", "Vest", Color(0.65, 0.45, 0.3)), 0)
	d.plate = equip("Plate Armor", visual("chest", "Plate", Color(0.82, 0.86, 0.95)), 2)
	d.warden_plate = equip("Warden's Plate", visual("chest", "Plate", Color(0.45, 0.55, 0.85)), 3)
	d.gloves = equip("Leather Gloves", visual("hands", "Gloves", Color(0.6, 0.42, 0.28)), 0)
	d.ember_gloves = equip("Ember Gloves", visual("hands", "Gloves", Color(1, 0.5, 0.25)), 1)
	d.boots = equip("Leather Boots", visual("feet", "Boots", Color(0.5, 0.35, 0.25)), 0)
	d.greaves = equip("Iron Greaves", visual("feet", "Boots", Color(0.8, 0.84, 0.92)), 1)
	return d


static func loot(gold_chance: float, gmin: int, gmax: int, rolls: int, nothing: float, entries: Array, guaranteed: Array = []) -> LootTable:
	var t := LootTable.new()
	t.gold_chance = gold_chance
	t.gold_min = gmin
	t.gold_max = gmax
	t.rolls = rolls
	t.nothing_weight = nothing
	var list: Array[ItemDef] = []
	var weights := PackedFloat32Array()
	for e in entries:
		list.append(e[0])
		weights.append(e[1])
	t.items = list
	t.weights = weights
	var g: Array[ItemDef] = []
	for i in guaranteed:
		g.append(i)
	t.guaranteed = g
	return t


static func loot_config(it: Dictionary) -> LootConfig:
	var c := LootConfig.new()
	c.gold_icon = it.gold.icon
	return c


# ------------------------------------------------------------------ weapons & skills

static func sword() -> WeaponConfig:
	var w := WeaponConfig.new()
	w.display_name = "Sword"
	w.ui_color = Color(0.55, 0.85, 1)
	w.visual = visual("weapon", "Sword", Color.WHITE)
	var slash := Color(0.8, 0.95, 1)
	w.combo = [
		step({name = "Slash", hit_weight = 1, damage = 12, windup = 0.05, active = 0.08, recovery = 0.2, attack_range = 2.3, arc = 130.0, lunge = 0.5, knockback = 3.0, stagger = 8.0, hit_stop = 0.04, shake = 0.08, move_mult = 0.2, slash_color = slash, slash_width = 0.6, slash_duration = 0.13}),
		step({name = "Backslash", hit_weight = 1, damage = 12, windup = 0.05, active = 0.08, recovery = 0.2, attack_range = 2.3, arc = 130.0, lunge = 0.5, knockback = 3.0, stagger = 8.0, hit_stop = 0.04, shake = 0.08, move_mult = 0.2, slash_color = slash, slash_width = 0.6, slash_duration = 0.13, reverse = true}),
		step({name = "Thrust Finisher", hit_weight = 4, damage = 22, windup = 0.1, active = 0.1, recovery = 0.35, attack_range = 2.8, arc = 70.0, lunge = 1.4, knockback = 9.0, stagger = 25.0, hit_stop = 0.09, shake = 0.2, move_mult = 0.1, slash_width = 0.9, slash_duration = 0.16}),
	]
	w.charge_time = 0.55
	w.charge_move_mult = 0.5
	w.charged = step({name = "Dash Strike", hit_weight = 4, damage = 40, windup = 0.0, active = 0.18, recovery = 0.35, attack_range = 2.4, arc = 100.0, lunge = 5.5, knockback = 12.0, stagger = 40.0, hit_stop = 0.12, shake = 0.35, move_mult = 0.0, super_armor = true, slash_color = Color(0.6, 0.9, 1), slash_width = 1.0, slash_duration = 0.2})
	return w


static func bow() -> WeaponConfig:
	var w := WeaponConfig.new()
	w.display_name = "Bow"
	w.ui_color = Color(0.75, 1, 0.5)
	w.visual = visual("weapon", "Bow", Color.WHITE)
	w.crit_chance = 0.25
	w.crit_mult = 2.0
	w.combo_reset = 0.3
	w.repeat_held = true
	w.combo = [step({name = "Shot", kind = AttackStep.Kind.PROJECTILE, hit_weight = 1, damage = 9, windup = 0.08, active = 0.02, recovery = 0.28, proj_speed = 26.0, proj_range = 18.0, proj_size = 0.18, knockback = 2.0, stagger = 5.0, hit_stop = 0.03, shake = 0.04, move_mult = 0.6})]
	w.charge_time = 0.7
	w.charge_move_mult = 0.4
	w.charged = step({name = "Piercing Volley", kind = AttackStep.Kind.PROJECTILE, hit_weight = 3, damage = 22, windup = 0.02, active = 0.02, recovery = 0.4, proj_count = 3, spread = 16.0, proj_speed = 34.0, proj_range = 22.0, pierce = 3, proj_size = 0.26, proj_color = Color(0.7, 1, 0.6), knockback = 7.0, stagger = 25.0, hit_stop = 0.07, shake = 0.2, move_mult = 0.3})
	return w


static func greatsword() -> WeaponConfig:
	var w := WeaponConfig.new()
	w.display_name = "Greatsword"
	w.ui_color = Color(1, 0.65, 0.3)
	w.visual = visual("weapon", "Greatsword", Color.WHITE)
	w.crit_chance = 0.1
	w.crit_mult = 2.0
	w.combo_reset = 0.6
	w.input_buffer = 0.35
	w.dash_cancel = false
	w.combo = [
		step({name = "Cleave", hit_weight = 3, damage = 34, windup = 0.28, active = 0.12, recovery = 0.45, attack_range = 3.2, arc = 170.0, lunge = 0.8, knockback = 10.0, stagger = 35.0, hit_stop = 0.11, shake = 0.3, move_mult = 0.05, super_armor = true, slash_color = Color(1, 0.8, 0.5), slash_width = 1.3, slash_duration = 0.2}),
		step({name = "Overhead Smash", hit_weight = 4, damage = 52, windup = 0.36, active = 0.12, recovery = 0.6, attack_range = 3.4, arc = 110.0, lunge = 1.0, knockback = 14.0, stagger = 60.0, hit_stop = 0.16, shake = 0.45, move_mult = 0.0, super_armor = true, reverse = true, slash_color = Color(1, 0.7, 0.4), slash_width = 1.5, slash_duration = 0.22}),
	]
	w.charge_time = 0.9
	w.charge_move_mult = 0.25
	w.charged = step({name = "Whirlwind", hit_weight = 4, damage = 60, windup = 0.05, active = 0.2, recovery = 0.6, attack_range = 3.6, arc = 360.0, lunge = 0.0, knockback = 16.0, stagger = 80.0, hit_stop = 0.14, shake = 0.5, move_mult = 0.0, super_armor = true, slash_color = Color(1, 0.6, 0.3), slash_width = 1.6, slash_duration = 0.3})
	return w


static func skill(display: String, color: Color, cooldown: float, s: AttackStep, crit := 0.15) -> SkillConfig:
	var k := SkillConfig.new()
	k.display_name = display
	k.color = color
	k.cooldown = cooldown
	k.crit_chance = crit
	k.attack = s
	return k


static func skills() -> Array[SkillConfig]:
	return [
		skill("Spin Slash", Color(0.55, 0.85, 1), 4.0, step({name = "Spin Slash", hit_weight = 3, damage = 28, windup = 0.08, active = 0.15, recovery = 0.35, attack_range = 3.0, arc = 360.0, lunge = 0.0, knockback = 8.0, stagger = 30.0, hit_stop = 0.08, shake = 0.25, move_mult = 0.3, super_armor = true, slash_color = Color(0.7, 0.95, 1), slash_width = 1.2, slash_duration = 0.25})),
		skill("Piercing Shot", Color(1, 0.85, 0.4), 3.0, step({name = "Piercing Shot", kind = AttackStep.Kind.PROJECTILE, hit_weight = 3, damage = 30, windup = 0.15, active = 0.02, recovery = 0.3, proj_speed = 36.0, proj_range = 22.0, pierce = 5, proj_size = 0.3, proj_color = Color(1, 0.9, 0.45), knockback = 6.0, stagger = 20.0, hit_stop = 0.06, shake = 0.15, move_mult = 0.2}), 0.3),
		skill("Ground Slam", Color(1, 0.55, 0.3), 8.0, step({name = "Ground Slam", hit_weight = 4, damage = 55, windup = 0.35, active = 0.1, recovery = 0.6, attack_range = 4.0, arc = 360.0, lunge = 0.0, knockback = 16.0, stagger = 80.0, hit_stop = 0.14, shake = 0.5, move_mult = 0.0, super_armor = true, slash_color = Color(1, 0.6, 0.3), slash_width = 2.0, slash_duration = 0.3})),
		skill("Fan of Knives", Color(0.8, 0.8, 1), 5.0, step({name = "Fan of Knives", kind = AttackStep.Kind.PROJECTILE, hit_weight = 2, damage = 10, windup = 0.06, active = 0.02, recovery = 0.25, proj_count = 7, spread = 70.0, proj_speed = 24.0, proj_range = 12.0, proj_size = 0.18, proj_color = Color(0.85, 0.85, 1), knockback = 3.0, stagger = 8.0, hit_stop = 0.03, shake = 0.08, move_mult = 0.5})),
	]


static func player(it: Dictionary) -> PlayerConfig:
	var p := PlayerConfig.new()
	p.weapons = [sword(), bow(), greatsword()]
	p.skills = skills()
	p.starting_equipment = [it.vest, it.boots]
	p.starting_items = [it.potion, it.potion, it.hood]
	return p


# ------------------------------------------------------------------ monsters

static func monster(props: Dictionary, attacks: Array) -> MonsterConfig:
	var m := MonsterConfig.new()
	for key in props:
		m.set(key, props[key])
	var list: Array[EnemyAttack] = []
	for a in attacks:
		list.append(a)
	m.attacks = list
	return m


static func monsters(it: Dictionary) -> Dictionary:
	var d := {}
	var K := EnemyAttack.Kind
	d.slime = monster({display_name = "Green Slime", texture = tex("Slime"), world_height = 1.0, shadow_size = 1.1, max_hp = 45, height = 1.0, poise = 0.0, stagger_duration = 0.3, death_color = Color(0.5, 0.9, 0.3), move_speed = 3.2, detection = 10.0, preferred = 1.8, decision_delay = 0.4,
		loot = loot(0.8, 1, 5, 1, 1.2, [[it.gel, 3.0], [it.potion, 1.0], [it.gloves, 0.35], [it.boots, 0.35]])},
		[attack({name = "Hop Bite", kind = K.LUNGE, hit_weight = 2, max_range = 3.2, cooldown = 1.6, windup = 0.55, active = 0.28, recovery = 0.7, damage = 10, knockback = 6.0, radius = 0.9, dash_speed = 10.0, track_speed = 300.0, color = Color(0.5, 1, 0.3, 0.55)})])
	d.archer = monster({display_name = "Skeleton Archer", texture = tex("SkeletonArcher"), max_hp = 35, height = 1.7, poise = 10.0, stagger_duration = 0.4, death_color = Color(0.9, 0.88, 0.8), movement = MonsterConfig.Movement.KEEP_DISTANCE, move_speed = 3.0, detection = 13.0, preferred = 7.0, decision_delay = 0.5,
		loot = loot(0.9, 3, 8, 1, 1.0, [[it.bone, 3.0], [it.hood, 1.0], [it.boots, 1.0], [it.greaves, 0.5], [it.potion, 1.0]])},
		[attack({name = "Aimed Shot", kind = K.PROJECTILE, hit_weight = 1, min_range = 3.0, max_range = 14.0, cooldown = 1.8, windup = 0.7, active = 0.05, recovery = 0.6, damage = 12, knockback = 4.0, proj_speed = 13.0, proj_range = 16.0, proj_size = 0.25, track_speed = 200.0, color = Color(1, 0.4, 0.3, 0.5)}),
		 attack({name = "Scatter Volley", kind = K.PROJECTILE, hit_weight = 1, weight = 0.5, min_range = 3.0, max_range = 10.0, cooldown = 5.0, windup = 1.0, active = 0.05, recovery = 0.7, damage = 8, knockback = 3.0, proj_count = 5, spread = 50.0, volleys = 2, volley_interval = 0.35, proj_speed = 10.0, proj_range = 12.0, proj_size = 0.25, track_speed = 120.0, color = Color(1, 0.6, 0.2, 0.5)}),
		 attack({name = "Kick", kind = K.MELEE_ARC, hit_weight = 2, weight = 2.0, max_range = 2.0, cooldown = 2.0, windup = 0.35, active = 0.1, recovery = 0.4, damage = 6, knockback = 10.0, radius = 1.8, arc = 100.0, color = Color(1, 0.9, 0.6, 0.5)})])
	d.brute = monster({display_name = "Stone Brute", texture = tex("Brute"), world_height = 2.6, shadow_size = 1.9, max_hp = 220, radius = 0.75, height = 2.2, poise = 60.0, stagger_duration = 0.6, knockback_resist = 0.7, death_color = Color(0.65, 0.5, 0.75), move_speed = 2.3, detection = 10.0, preferred = 2.2, decision_delay = 0.6,
		loot = loot(1.0, 10, 20, 2, 0.8, [[it.helm, 1.2], [it.plate, 1.2], [it.greaves, 1.0], [it.potion, 1.5]])},
		[attack({name = "Club Swing", kind = K.MELEE_ARC, hit_weight = 3, max_range = 3.2, cooldown = 2.0, windup = 0.8, active = 0.12, recovery = 0.8, damage = 22, knockback = 14.0, radius = 3.2, arc = 140.0, track_speed = 120.0, color = Color(1, 0.3, 0.2, 0.5)}),
		 attack({name = "Ground Slam", kind = K.SELF_AOE, hit_weight = 4, weight = 0.6, max_range = 3.5, cooldown = 5.0, windup = 1.1, active = 0.1, recovery = 1.1, damage = 28, knockback = 16.0, radius = 4.0, color = Color(1, 0.5, 0.2, 0.5)}),
		 attack({name = "Charge", kind = K.CHARGE, hit_weight = 3, min_range = 5.0, max_range = 12.0, cooldown = 6.0, windup = 0.9, active = 0.9, recovery = 0.9, damage = 25, knockback = 16.0, radius = 1.2, dash_speed = 15.0, track_speed = 90.0, color = Color(1, 0.2, 0.2, 0.45)})])
	d.bat = monster({display_name = "Cave Bat", texture = tex("Bat"), world_height = 0.9, shadow_size = 0.8, hover = 0.7, max_hp = 18, radius = 0.35, height = 0.9, poise = 0.0, stagger_duration = 0.25, alert_radius = 10.0, death_color = Color(0.6, 0.4, 0.9), movement = MonsterConfig.Movement.ERRATIC, move_speed = 5.5, erratic_interval = 0.45, detection = 11.0, preferred = 2.5, decision_delay = 0.3,
		loot = loot(0.7, 1, 3, 1, 1.5, [[it.potion, 0.6], [it.hood, 0.3]])},
		[attack({name = "Dive", kind = K.LUNGE, hit_weight = 1, max_range = 4.0, cooldown = 1.2, windup = 0.35, active = 0.22, recovery = 0.45, damage = 6, knockback = 3.0, radius = 0.7, dash_speed = 14.0, track_speed = 400.0, color = Color(0.8, 0.4, 1, 0.5)})])
	d.cultist = monster({display_name = "Ember Cultist", texture = tex("Cultist"), world_height = 1.9, max_hp = 55, poise = 20.0, stagger_duration = 0.45, death_color = Color(1, 0.5, 0.2), movement = MonsterConfig.Movement.KEEP_DISTANCE, move_speed = 2.6, detection = 14.0, preferred = 8.0, decision_delay = 0.6,
		loot = loot(0.9, 5, 12, 1, 0.8, [[it.ember, 3.0], [it.ember_gloves, 1.2], [it.potion, 1.0]])},
		[attack({name = "Fire Circles", kind = K.GROUND_AOE, hit_weight = 2, max_range = 14.0, cooldown = 3.5, windup = 1.2, active = 0.1, recovery = 0.6, damage = 18, knockback = 6.0, radius = 1.8, aoe_count = 3, aoe_scatter = 3.0, track = false, color = Color(1, 0.45, 0.1, 0.5)}),
		 attack({name = "Ember Nova", kind = K.RADIAL, hit_weight = 1, weight = 1.5, max_range = 4.0, cooldown = 5.0, windup = 0.8, active = 0.1, recovery = 0.7, damage = 10, knockback = 5.0, proj_count = 10, volleys = 2, volley_interval = 0.3, proj_speed = 7.0, proj_range = 9.0, proj_size = 0.28, color = Color(1, 0.6, 0.2, 0.5)})])
	d.dummy = monster({display_name = "Training Dummy", texture = tex("TrainingDummy"), max_hp = 500, immortal = true, poise = 0.0, stagger_duration = 0.2, knockback_resist = 1.0, radius = 0.4, height = 1.7, movement = MonsterConfig.Movement.STATIONARY, detection = 0.0}, [])

	var boss := monster({display_name = "The Hollow Warden", texture = tex("HollowWarden"), world_height = 3.6, shadow_size = 2.6, max_hp = 1600, radius = 1.0, height = 3.0, poise = 150.0, stagger_duration = 0.8, knockback_resist = 0.9, alert_radius = 0.0, death_color = Color(0.5, 0.9, 1), move_speed = 2.8, detection = 14.0, leash = 40.0, preferred = 2.8, decision_delay = 0.6,
		loot = loot(1.0, 80, 120, 3, 0.5, [[it.crown, 1.0], [it.plate, 1.0], [it.helm, 1.0], [it.potion, 2.0]], [it.warden_plate])}, [])
	var p1 := BossPhase.new()
	p1.name = "Phase I — The Warden Wakes"
	p1.starts_at = 1.0
	p1.transition = 0.0
	p1.move_speed = 2.8
	p1.decision_delay = 0.6
	p1.attacks = [
		attack({name = "Great Sweep", kind = K.MELEE_ARC, hit_weight = 3, weight = 2.0, max_range = 4.0, cooldown = 2.2, windup = 0.75, active = 0.12, recovery = 0.7, damage = 20, knockback = 14.0, radius = 4.0, arc = 160.0, track_speed = 150.0}),
		attack({name = "Shoulder Charge", kind = K.CHARGE, hit_weight = 4, weight = 1.5, min_range = 6.0, max_range = 16.0, cooldown = 5.0, windup = 1.0, active = 1.0, recovery = 1.0, damage = 24, knockback = 18.0, radius = 1.5, dash_speed = 17.0, track_speed = 90.0}),
		attack({name = "Blade Fan", kind = K.PROJECTILE, hit_weight = 2, min_range = 4.0, max_range = 14.0, cooldown = 4.0, windup = 0.8, active = 0.05, recovery = 0.7, damage = 12, knockback = 5.0, proj_count = 5, spread = 60.0, proj_speed = 11.0, proj_range = 16.0, proj_size = 0.35, track_speed = 120.0, color = Color(0.5, 0.9, 1, 0.5)}),
	]
	var p2 := BossPhase.new()
	p2.name = "Phase II — Unbound"
	p2.starts_at = 0.5
	p2.tint = Color(1, 0.55, 0.45)
	p2.sprite_scale = 1.15
	p2.transition = 1.6
	p2.announcement = "The Warden breaks its chains!"
	p2.shockwave_radius = 6.0
	p2.shockwave_damage = 15
	p2.shockwave_knockback = 18.0
	p2.summons = [d.bat, d.bat]
	p2.move_speed = 3.6
	p2.decision_delay = 0.35
	p2.attacks = [
		attack({name = "Great Sweep", kind = K.MELEE_ARC, hit_weight = 3, weight = 2.0, max_range = 4.2, cooldown = 1.6, windup = 0.55, active = 0.12, recovery = 0.55, damage = 24, knockback = 15.0, radius = 4.2, arc = 180.0, track_speed = 200.0}),
		attack({name = "Rampage Charge", kind = K.CHARGE, hit_weight = 4, weight = 1.5, min_range = 5.0, max_range = 18.0, cooldown = 3.5, windup = 0.75, active = 1.0, recovery = 0.8, damage = 26, knockback = 20.0, radius = 1.6, dash_speed = 20.0, track_speed = 120.0}),
		attack({name = "Nova Burst", kind = K.RADIAL, hit_weight = 1, weight = 1.2, max_range = 8.0, cooldown = 5.0, windup = 0.9, active = 0.1, recovery = 0.8, damage = 12, knockback = 6.0, proj_count = 14, volleys = 3, volley_interval = 0.35, proj_speed = 8.0, proj_range = 14.0, proj_size = 0.35, color = Color(1, 0.4, 0.3, 0.5)}),
		attack({name = "Meteor Rain", kind = K.GROUND_AOE, hit_weight = 3, max_range = 16.0, cooldown = 6.0, windup = 1.1, active = 0.1, recovery = 0.7, damage = 20, knockback = 8.0, radius = 2.2, aoe_count = 5, aoe_scatter = 4.0, track = false, color = Color(1, 0.3, 0.1, 0.5)}),
		attack({name = "Call the Swarm", kind = K.SUMMON, weight = 0.5, max_range = 16.0, cooldown = 14.0, windup = 1.0, active = 0.1, recovery = 0.6, summon = d.bat, summon_count = 2, color = Color(0.8, 0.4, 1, 0.5)}),
	]
	boss.phases = [p1, p2]
	boss.attacks = p1.attacks
	d.boss = boss
	return d


static func debug_loot(it: Dictionary) -> LootTable:
	return loot(1.0, 20, 60, 8, 0.0, [[it.potion, 1.0], [it.gel, 1.0], [it.bone, 1.0], [it.ember, 1.0], [it.hood, 1.0], [it.helm, 1.0], [it.crown, 0.5], [it.vest, 1.0], [it.plate, 1.0], [it.warden_plate, 0.5], [it.gloves, 1.0], [it.ember_gloves, 1.0], [it.boots, 1.0], [it.greaves, 1.0]])
