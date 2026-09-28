class_name SpriteRig
extends Node3D
## 2.5D character look (Unity: SpriteBillboard + PaperDoll in one).
## Flat pixel quads facing the locked camera, 4-way facing (down / up / side, left = mirrored),
## paper-doll layers with per-facing draw order, blob shadow, and procedural motion
## (bob, breathe, lean, squash spring, flash, hurt tint, tremble, blink, hit-pause).

enum Facing { DOWN, UP, SIDE }
enum State { IDLE, MOVE, ATTACK, HURT }

const SHADER := preload("res://shaders/sprite.gdshader")
const ORDER := {
	Facing.DOWN: ["body", "feet", "chest", "hands", "hair", "head", "weapon"],
	Facing.UP: ["weapon", "body", "feet", "chest", "hands", "hair", "head"],
	Facing.SIDE: ["body", "feet", "chest", "hair", "head", "weapon", "hands"],
}
const LAYER_GAP := 0.004

var world_height := 1.8
var tilt := 0.35
var hover := 0.0
var move_bob := 0.08
var idle_breath := 0.03
var move_lean := 6.0

var facing := Facing.DOWN
var facing_left := false
var state := State.IDLE

var _pivot: Node3D
var _shadow: MeshInstance3D
var _quads := {} ## layer -> MeshInstance3D
var _sets := {} ## layer -> {Facing: Texture2D}
var _tints := {} ## layer -> Color
var _hides_hair := false
var _directional := false

var _state_time := 0.0
var _squash := Vector2.ONE
var _squash_vel := Vector2.ZERO
var _flash_until := 0.0
var _flash_color := Color.WHITE
var _hurt_until := 0.0
var _hurt_time := 0.1
var _hurt_color := Color.WHITE
var _tremble_until := 0.0
var _tremble_amount := 0.0
var _tremble_time := 0.1
var _paused_until := 0.0
var _blink := false
var _blink_rate := 16.0
var _state_tint := Color.WHITE
var _base_tint := Color.WHITE
var _scale_mult := 1.0
var _air := 0.0
var _fade := 1.0


func _ready() -> void:
	_pivot = Node3D.new()
	_pivot.name = "Pivot"
	add_child(_pivot)
	_shadow = MeshInstance3D.new()
	var quad := QuadMesh.new()
	quad.size = Vector2.ONE
	_shadow.mesh = quad
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.albedo_color = Color(0, 0, 0, 0.4)
	mat.albedo_texture = Defaults.tex("SoftCircle")
	_shadow.material_override = mat
	_shadow.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_shadow.rotation_degrees = Vector3(-90, 0, 0)
	_shadow.position = Vector3(0, 0.03, 0)
	add_child(_shadow)


## Single-sprite character (monsters).
func setup_single(texture: Texture2D, height: float, shadow_size: float, hover_height := 0.0) -> void:
	world_height = height
	hover = hover_height
	_directional = false
	_sets.body = {Facing.DOWN: texture}
	_tints.body = Color.WHITE
	_shadow.scale = Vector3(shadow_size, shadow_size * 0.6, 1)


## Layered 4-way character (player).
func setup_doll(body: Dictionary, hair: Dictionary, hair_tint: Color, height: float) -> void:
	world_height = height
	_directional = true
	_sets.body = body
	_tints.body = Color.WHITE
	_sets.hair = hair
	_tints.hair = hair_tint
	_shadow.scale = Vector3(0.9, 0.54, 1)


func set_equipment(slot: String, visual: EquipmentVisual) -> void:
	if visual == null:
		_sets.erase(slot)
	else:
		_sets[slot] = {Facing.DOWN: visual.down, Facing.UP: visual.up, Facing.SIDE: visual.side}
		_tints[slot] = visual.tint
	if slot == "head":
		_hides_hair = visual != null and visual.hides_hair


func set_facing(world_dir: Vector3) -> void:
	var cam := get_viewport().get_camera_3d()
	if cam == null:
		return
	var right := cam.global_transform.basis.x
	var fwd := -cam.global_transform.basis.z
	fwd.y = 0.0
	fwd = fwd.normalized()
	var side := world_dir.dot(right)
	var forward := world_dir.dot(fwd)
	if absf(side) > 0.15:
		facing_left = side < 0.0
	if absf(side) >= absf(forward) * 0.85:
		facing = Facing.SIDE
	else:
		facing = Facing.UP if forward > 0.0 else Facing.DOWN


func set_state(s: int) -> void:
	if s != state:
		state = s
		_state_time = 0.0


func flash(color: Color, seconds: float) -> void:
	_flash_color = color
	_flash_until = Game.time + seconds


func hurt_tint(color: Color, seconds: float) -> void:
	_hurt_color = color
	_hurt_time = maxf(0.01, seconds)
	_hurt_until = maxf(_flash_until, Game.time) + seconds


func tremble(amount: float, seconds: float) -> void:
	if amount <= 0.0 or seconds <= 0.0:
		return
	_tremble_amount = maxf(amount, _tremble_amount if Game.real_time() < _tremble_until else 0.0)
	_tremble_time = seconds
	_tremble_until = Game.real_time() + seconds


func punch(scale_xy: Vector2) -> void:
	_squash = scale_xy
	_squash_vel = Vector2.ZERO


## Local hit-stop (online-safe mode): hold the pose without stopping the game.
func hit_pause(seconds: float) -> void:
	_paused_until = maxf(_paused_until, Game.real_time() + seconds)


func set_blink(on: bool, rate: float) -> void:
	_blink = on
	_blink_rate = rate


func set_tint(c: Color) -> void:
	_state_tint = c


func set_base_tint(c: Color) -> void:
	_base_tint = c


func set_scale_mult(k: float) -> void:
	_scale_mult = maxf(0.05, k)


func set_air(h: float) -> void:
	_air = maxf(0.0, h)


func set_fade(a: float) -> void:
	_fade = clampf(a, 0.0, 1.0)


## Visible layer quads (for afterimages).
func visible_quads() -> Array:
	var list := []
	for q in _quads.values():
		if q.visible:
			list.append(q)
	return list


func _texture_for(layer: String) -> Texture2D:
	if not _sets.has(layer):
		return null
	if layer == "hair" and _hides_hair:
		return null
	var set: Dictionary = _sets[layer]
	if not _directional:
		return set.get(Facing.DOWN)
	var t: Texture2D = set.get(facing)
	return t if t else set.get(Facing.DOWN)


func _quad(layer: String) -> MeshInstance3D:
	if _quads.has(layer):
		return _quads[layer]
	var q := MeshInstance3D.new()
	q.name = "Layer_" + layer
	q.mesh = QuadMesh.new()
	var mat := ShaderMaterial.new()
	mat.shader = SHADER
	q.material_override = mat
	q.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_pivot.add_child(q)
	_quads[layer] = q
	return q


func _process(delta: float) -> void:
	var paused := Game.real_time() < _paused_until
	var dt := 0.0 if paused else delta
	_state_time += dt

	# Squash spring, sub-stepped: one big step at a low frame rate would blow the spring up.
	var left := dt
	while left > 0.0:
		var h := minf(left, 1.0 / 120.0)
		var accel := (Vector2.ONE - _squash) * 320.0 - _squash_vel * 18.0
		_squash_vel += accel * h
		_squash += _squash_vel * h
		left -= h
	_squash = _squash.clamp(Vector2(0.4, 0.4), Vector2(1.8, 1.8))

	var bob := 0.0
	var lean := 0.0
	var breathe := 1.0
	if state == State.MOVE:
		bob = absf(sin(_state_time * 14.0)) * move_bob
		lean = move_lean
	elif state == State.IDLE:
		breathe = 1.0 + sin(_state_time * 3.0) * idle_breath

	var cam := get_viewport().get_camera_3d()
	var cam_rot := cam.global_rotation if cam else Vector3.ZERO
	var hover_y := 0.0
	if hover > 0.0:
		hover_y = hover + sin(Game.time * 5.0 + get_instance_id()) * 0.12
	var tremble_off := Vector3.ZERO
	var now := Game.real_time()
	if now < _tremble_until and cam:
		var k := (_tremble_until - now) / maxf(0.001, _tremble_time)
		tremble_off = cam.global_transform.basis.x * signf(sin(now * 110.0)) * _tremble_amount * k

	_pivot.global_rotation = Vector3(cam_rot.x * tilt, cam_rot.y, 0.0)
	_pivot.rotate_object_local(Vector3.FORWARD, deg_to_rad(lean * (1.0 if facing_left else -1.0)))
	_pivot.position = Vector3(0, bob + hover_y + _air, 0) + tremble_off
	_pivot.scale = Vector3(_squash.x, _squash.y * breathe, 1.0)

	var flashing := Game.time < _flash_until
	var color := _base_tint * _state_tint
	if not flashing and Game.time < _hurt_until:
		var t := 1.0 - (_hurt_until - Game.time) / _hurt_time
		color = (color * _hurt_color).lerp(color, t * t)
	var fade := _fade
	if _blink and fmod(Game.time * _blink_rate, 1.0) < 0.5:
		fade *= 0.3
	var mirror := facing_left and (not _directional or facing == Facing.SIDE)

	var body_tex := _texture_for("body")
	var px := world_height * _scale_mult / (body_tex.get_height() if body_tex else 24)
	var order: Array = ORDER[facing] if _directional else ["body"]
	for layer in _quads.keys():
		if not order.has(layer):
			_quads[layer].visible = false
	for i in order.size():
		var layer: String = order[i]
		var tex := _texture_for(layer)
		if tex == null:
			if _quads.has(layer):
				_quads[layer].visible = false
			continue
		var q := _quad(layer)
		q.visible = true
		var size := Vector2(tex.get_width(), tex.get_height()) * px
		(q.mesh as QuadMesh).size = size
		q.position = Vector3(0, size.y * 0.5, i * LAYER_GAP)
		var mat := q.material_override as ShaderMaterial
		mat.set_shader_parameter("tex", tex)
		mat.set_shader_parameter("tint", color * _tints.get(layer, Color.WHITE))
		mat.set_shader_parameter("flash", 1.0 if flashing else 0.0)
		mat.set_shader_parameter("flash_color", _flash_color)
		mat.set_shader_parameter("fade", fade)
		mat.set_shader_parameter("flip", 1.0 if mirror else 0.0)

	(_shadow.material_override as StandardMaterial3D).albedo_color = Color(0, 0, 0, 0.4 * _fade)
