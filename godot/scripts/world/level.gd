class_name Level
extends Node3D
## Greybox level built from code (Unity: PrototypeSceneBuilder). Same layout idea:
## start meadow with dummy + slimes (south), river with a bridge and two jump links,
## ruins with archers / brute / cultist / bats, and the boss arena up north.
## Camera looks toward -Z, so "north" (up the screen) is -Z.

const FADE_LAYER := 2 ## Area3D volumes used only by the occlusion fader

var player_start := Vector3(0, 0, 30)
var boss_gate := Vector3(0, 0, -22)
var spawns: Array = [] ## [monster key, position]

var _mats := {}


func build() -> void:
	_environment()
	_ground(Vector3(0, 0, 0), Vector2(64, 84), "Grass", 0.0)
	_ground(Vector3(0, 0, 4), Vector2(4, 68), "Path", 0.005) # main path south → north
	_ground(Vector3(-10, 0, 20), Vector2(16, 3), "Path", 0.006) # side path to the dummy
	_ground(Vector3(0, 0, -33), Vector2(24, 20), "ArenaFloor", 0.007)

	# Border walls
	_wall(Vector3(0, 0, 42), Vector3(64, 3, 1))
	_wall(Vector3(0, 0, -43), Vector3(64, 3, 1))
	_wall(Vector3(-32, 0, 0), Vector3(1, 3, 86))
	_wall(Vector3(32, 0, 0), Vector3(1, 3, 86))

	# River (z 0..4) with bridge in the middle; blockers let projectiles fly over.
	_water(Vector3(-17, 0, 2), Vector2(30, 4))
	_water(Vector3(17, 0, 2), Vector2(30, 4))
	_box(Vector3(0, -0.05, 2), Vector3(4.4, 0.12, 5), "Bark", false, false) # bridge deck (visual only)
	_wall(Vector3(-2.4, 0, 2), Vector3(0.3, 0.8, 5), "Bark", false)
	_wall(Vector3(2.4, 0, 2), Vector3(0.3, 0.8, 5), "Bark", false)
	JumpLink.create(self, Vector3(-12, 0, 6.2), Vector3(-12, 0, -2.2))
	JumpLink.create(self, Vector3(14, 0, 6.2), Vector3(14, 0, -2.2))

	# Meadow props
	for p in [Vector3(-14, 0, 32), Vector3(12, 0, 34), Vector3(18, 0, 24), Vector3(-20, 0, 12), Vector3(9, 0, 10), Vector3(-5, 0, 13)]:
		_tree(p)
	for p in [Vector3(6, 0, 28), Vector3(-9, 0, 26), Vector3(20, 0, 14)]:
		_rock(p)

	# Ruins: corridor walls + pillars that hide characters walking behind them.
	_wall(Vector3(-7, 0, -10), Vector3(6, 3.2, 1))
	_wall(Vector3(7, 0, -10), Vector3(6, 3.2, 1))
	_wall(Vector3(-18, 0, -16), Vector3(1, 3.2, 10))
	_wall(Vector3(18, 0, -14), Vector3(1, 3.2, 8))
	for p in [Vector3(-4, 0, -5), Vector3(4, 0, -5), Vector3(-12, 0, -20), Vector3(12, 0, -20), Vector3(-3, 0, -16), Vector3(3, 0, -16)]:
		_pillar(p)
	for p in [Vector3(-24, 0, -6), Vector3(24, 0, -4), Vector3(-26, 0, -28), Vector3(26, 0, -30)]:
		_tree(p)

	# Boss arena ring of pillars
	for i in 8:
		var a := TAU * i / 8.0 + 0.2
		if i == 4:
			continue # gap to walk in from the south
		_pillar(Vector3(0, 0, -33) + Vector3(sin(a), 0, cos(a)) * 11.0)

	spawns = [
		["dummy", Vector3(-12, 0, 20)],
		["slime", Vector3(6, 0, 20)], ["slime", Vector3(9, 0, 22)], ["slime", Vector3(4, 0, 16)],
		["bat", Vector3(-10, 0, 10)], ["bat", Vector3(-7, 0, 9)],
		["archer", Vector3(-9, 0, -14)], ["archer", Vector3(10, 0, -16)],
		["brute", Vector3(0, 0, -13)],
		["cultist", Vector3(-22, 0, -10)], ["cultist", Vector3(22, 0, -18)],
		["bat", Vector3(14, 0, -6)], ["bat", Vector3(16, 0, -8)],
		["boss", Vector3(0, 0, -35)],
	]


# ------------------------------------------------------------------ pieces

func _environment() -> void:
	var env := WorldEnvironment.new()
	var e := Environment.new()
	e.background_mode = Environment.BG_COLOR
	e.background_color = Color(0.12, 0.14, 0.18)
	e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	e.ambient_light_color = Color(0.75, 0.78, 0.9)
	e.ambient_light_energy = 0.6
	env.environment = e
	add_child(env)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-55, -35, 0)
	sun.light_energy = 1.0
	sun.shadow_enabled = true
	add_child(sun)


func _mat(tex_name: String, fadeable := false) -> StandardMaterial3D:
	var key := tex_name + ("_f" if fadeable else "")
	if not fadeable and _mats.has(key):
		return _mats[key]
	var m := StandardMaterial3D.new()
	m.albedo_texture = Defaults.tex(tex_name)
	m.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	m.uv1_triplanar = true
	m.uv1_world_triplanar = true
	m.uv1_scale = Vector3(0.5, 0.5, 0.5)
	if not fadeable:
		_mats[key] = m # fadeable objects need their own material (their alpha changes)
	return m


func _ground(center: Vector3, size: Vector2, tex_name: String, lift: float) -> void:
	var m := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = size
	m.mesh = plane
	m.material_override = _mat(tex_name)
	m.position = center + Vector3.UP * lift
	add_child(m)
	if lift == 0.0:
		var body := StaticBody3D.new()
		var shape := CollisionShape3D.new()
		var box := BoxShape3D.new()
		box.size = Vector3(size.x, 1, size.y)
		shape.shape = box
		shape.position = Vector3(0, -0.5, 0)
		body.add_child(shape)
		body.position = center
		body.set_meta("ground", true)
		add_child(body)


## Solid box. `fade` adds an occlusion volume so it turns see-through over characters.
func _box(pos: Vector3, size: Vector3, tex_name: String, solid := true, fade := true) -> Node3D:
	var root := StaticBody3D.new() if solid else Node3D.new()
	root.position = pos
	add_child(root)
	var mesh := MeshInstance3D.new()
	var bm := BoxMesh.new()
	bm.size = size
	mesh.mesh = bm
	mesh.position.y = size.y * 0.5
	mesh.material_override = _mat(tex_name, fade)
	root.add_child(mesh)
	if solid:
		var shape := CollisionShape3D.new()
		var box := BoxShape3D.new()
		box.size = size
		shape.shape = box
		shape.position.y = size.y * 0.5
		root.add_child(shape)
	if fade:
		_fade_volume(root, [mesh], Vector3(size.x, size.y, size.z), size.y * 0.5)
	return root


func _wall(pos: Vector3, size: Vector3, tex_name := "Stone", fade := true) -> Node3D:
	return _box(pos, size, tex_name, true, fade)


func _pillar(pos: Vector3) -> void:
	_box(pos, Vector3(1.2, 4.0, 1.2), "Stone")


func _rock(pos: Vector3) -> void:
	_box(pos, Vector3(1.6, 0.9, 1.3), "Stone", true, false)


func _tree(pos: Vector3) -> void:
	var body := StaticBody3D.new()
	body.position = pos
	add_child(body)
	var trunk := MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = 0.3
	cyl.bottom_radius = 0.4
	cyl.height = 2.4
	trunk.mesh = cyl
	trunk.position.y = 1.2
	trunk.material_override = _mat("Bark", true)
	body.add_child(trunk)
	var leaves := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 1.8
	sphere.height = 3.0
	leaves.mesh = sphere
	leaves.position.y = 3.4
	leaves.material_override = _mat("Leaves", true)
	body.add_child(leaves)
	var shape := CollisionShape3D.new()
	var c := CylinderShape3D.new()
	c.radius = 0.45
	c.height = 2.4
	shape.shape = c
	shape.position.y = 1.2
	body.add_child(shape)
	_fade_volume(body, [trunk, leaves], Vector3(3.6, 5.0, 3.6), 2.5)


func _water(center: Vector3, size: Vector2) -> void:
	var m := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = size
	m.mesh = plane
	var mat := _mat("Water")
	m.material_override = mat
	m.position = center + Vector3.UP * 0.02
	add_child(m)
	# Invisible blocker: stops walking (and dashing — only jump links cross), not projectiles.
	var body := StaticBody3D.new()
	body.set_meta("projectile_pass", true)
	body.position = center
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = Vector3(size.x, 2, size.y - 0.6)
	shape.shape = box
	shape.position.y = 1
	body.add_child(shape)
	add_child(body)


func _fade_volume(parent: Node3D, meshes: Array, size: Vector3, center_y: float) -> void:
	var area := Area3D.new()
	area.collision_layer = 1 << (FADE_LAYER - 1)
	area.collision_mask = 0
	area.monitoring = false
	area.set_meta("fade_meshes", meshes)
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = size
	shape.shape = box
	shape.position.y = center_y
	area.add_child(shape)
	parent.add_child(area)
