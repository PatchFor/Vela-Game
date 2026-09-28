class_name WorldItem
extends Node3D
## A dropped item: pops out in an arc, bounces, settles and bobs. Rarity shows as a glow ring and a
## light pillar that grows with rarity. Items are clicked (or F) to pick up; gold flies to the
## player when they walk near.

enum State { WAITING, POPPING, BOUNCING, SETTLED, COLLECTING }

var item: ItemDef
var count := 1
var gold := 0
var owner_node: Node = null ## personal loot later (online)
var state := State.WAITING
var hovered := false

var _origin: Vector3
var _landing: Vector3
var _delay := 0.0
var _t := 0.0
var _age := 0.0
var _nudge := -10.0
var _sprite: MeshInstance3D
var _glow: MeshInstance3D
var _beam: MeshInstance3D


static func spawn(parent: Node, it: ItemDef, n: int, gold_amount: int, origin: Vector3, landing: Vector3, delay: float) -> WorldItem:
	var w := WorldItem.new()
	w.item = it
	w.count = n
	w.gold = gold_amount
	w._origin = origin
	w._landing = landing
	w._delay = delay
	parent.add_child(w)
	w.global_position = origin
	w._build()
	Game.world_items.append(w)
	Game.stats.drops += 1
	return w


func is_gold() -> bool:
	return gold > 0


func rarity() -> int:
	return item.rarity if item else 0


func can_pick_up() -> bool:
	return state == State.BOUNCING or state == State.SETTLED


func label() -> String:
	if is_gold():
		return "%d Gold" % gold
	return item.display_name + (" x%d" % count if count > 1 else "")


func label_color() -> Color:
	return Color(1, 0.85, 0.3) if is_gold() else Game.loot_config.rarity_color(rarity())


func anchor() -> Vector3:
	return _sprite.global_position + Vector3.UP * 0.2 if _sprite else global_position


func nudge() -> void:
	_nudge = Game.real_time()


func collect() -> void:
	state = State.COLLECTING
	if _glow:
		_glow.visible = false
	if _beam:
		_beam.visible = false


func _build() -> void:
	var tex: Texture2D = Game.loot_config.gold_icon if is_gold() else item.icon
	_sprite = MeshInstance3D.new()
	var quad := QuadMesh.new()
	var aspect := float(tex.get_width()) / tex.get_height() if tex else 1.0
	quad.size = Vector2(0.7 * minf(aspect, 1.0), 0.7 / maxf(aspect, 1.0))
	_sprite.mesh = quad
	var mat := ShaderMaterial.new()
	mat.shader = preload("res://shaders/sprite.gdshader")
	mat.set_shader_parameter("tex", tex)
	mat.set_shader_parameter("tint", item.equipment.tint if item and item.equipment else Color.WHITE)
	_sprite.material_override = mat
	_sprite.visible = false
	add_child(_sprite)

	if is_gold():
		return
	var color := label_color()
	if rarity() >= ItemDef.Rarity.UNCOMMON:
		_glow = MeshInstance3D.new()
		_glow.mesh = Fx.sector_mesh(360.0, 0.88, 1.0, 32)
		_glow.material_override = Fx.material(color, true)
		_glow.position.y = 0.04
		_glow.visible = false
		add_child(_glow)
		var h: float = Game.loot_config.beam_heights[rarity()]
		if h > 0.0:
			_beam = MeshInstance3D.new()
			var bq := QuadMesh.new()
			bq.size = Vector2(0.22 + 0.04 * rarity(), h)
			_beam.mesh = bq
			_beam.material_override = Fx.material(Color(color.r, color.g, color.b, 0.35), true)
			_beam.position.y = h * 0.5
			_beam.visible = false
			add_child(_beam)


func _process(delta: float) -> void:
	var cfg := Game.loot_config
	var cam := get_viewport().get_camera_3d()
	if cam and _sprite:
		_sprite.global_rotation = cam.global_rotation
	match state:
		State.WAITING:
			_delay -= delta
			if _delay <= 0.0:
				state = State.POPPING
				_sprite.visible = true
		State.POPPING:
			_t += delta
			var t := clampf(_t / cfg.pop_time, 0.0, 1.0)
			global_position = _origin.lerp(_landing, t)
			_sprite.position = Vector3(0, 0.35 + 4.0 * cfg.pop_height * t * (1.0 - t), 0)
			_sprite.rotate_object_local(Vector3.FORWARD, t * 12.0)
			if t >= 1.0:
				state = State.BOUNCING
				_t = 0.0
				_landed()
		State.BOUNCING:
			_t += delta
			var t := clampf(_t / 0.22, 0.0, 1.0)
			_sprite.position = Vector3(0, 0.35 + 4.0 * 0.3 * t * (1.0 - t), 0)
			if t >= 1.0:
				state = State.SETTLED
				if _glow:
					_glow.visible = true
				if _beam:
					_beam.visible = true
		State.SETTLED:
			_age += delta
			var wiggle := sin((Game.real_time() - _nudge) * 60.0) * 0.2 if Game.real_time() - _nudge < 0.3 else 0.0
			_sprite.position = Vector3(0, 0.43 + sin(_age * 3.0) * 0.06, 0)
			_sprite.rotate_object_local(Vector3.FORWARD, wiggle)
			_sprite.scale = Vector3.ONE * (1.25 if hovered else 1.0)
			if _glow:
				var r := 0.5 + 0.07 * sin(_age * 4.0)
				_glow.scale = Vector3(r, 1, r)
			if _beam and cam:
				_beam.global_rotation = Vector3(0, cam.global_rotation.y, 0)
			if is_gold() and Game.player and Game.player.is_alive():
				var d: Vector3 = Game.player.global_position - global_position
				d.y = 0.0
				if d.length() <= cfg.gold_magnet:
					collect()
		State.COLLECTING:
			var p = Game.player
			if p == null:
				queue_free()
				return
			var target: Vector3 = p.global_position + Vector3.UP
			_sprite.global_position = _sprite.global_position.move_toward(target, 14.0 * delta)
			_sprite.scale *= 1.0 - 3.0 * delta
			if _sprite.global_position.distance_to(target) < 0.25:
				if is_gold():
					p.inventory.add(Game.content.items.gold, gold)
					Game.stats.pickups += 1
					Fx.number(p.global_position + Vector3.UP * 2.0, "+%d G" % gold, label_color(), 0.85)
					Sfx.play("gold")
				queue_free()


func _landed() -> void:
	Fx.dust(global_position, 4)
	if not is_gold() and rarity() >= ItemDef.Rarity.RARE:
		var c := label_color()
		Fx.ring(global_position, 0.2, 1.2 + 0.3 * rarity(), 0.35, c)
		Fx.sparks(global_position + Vector3.UP * 0.4, Vector3.UP, c, 6 + 4 * rarity())
		Sfx.play("pickup_rare", null, 0.6)


func _exit_tree() -> void:
	Game.world_items.erase(self)
