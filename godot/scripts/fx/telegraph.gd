class_name Telegraph
extends Node3D
## Ground warning for an incoming attack: faint area + a fill that grows with the windup.
## Shapes: circle (size = radius), sector (size = radius, extra = arc degrees), line (size = length, extra = width).

enum Shape { CIRCLE, SECTOR, LINE }

var shape := Shape.CIRCLE
var size := 1.0
var extra := 0.0
var color := Color.RED

var _area: MeshInstance3D
var _fill: MeshInstance3D
var _edge: MeshInstance3D


static func create(parent: Node, s: int, pos: Vector3, dir: Vector3, sz: float, ex: float, c: Color) -> Telegraph:
	var t := Telegraph.new()
	t.shape = s
	t.size = maxf(0.05, sz)
	t.extra = ex
	t.color = c
	parent.add_child(t)
	t._build()
	t.set_pose(pos, dir)
	t.set_progress(0.0)
	return t


func _build() -> void:
	var mesh: Mesh
	match shape:
		Shape.CIRCLE:
			mesh = Fx.sector_mesh(360.0, 0.0, 1.0, 40)
		Shape.SECTOR:
			mesh = Fx.sector_mesh(extra, 0.0, 1.0, maxi(8, int(extra / 6.0)))
		_:
			mesh = Fx.rect_mesh()
	_area = _part(mesh, 0.05)
	_fill = _part(mesh, 0.06)
	var base := Vector3(extra, 1, size) if shape == Shape.LINE else Vector3(size, 1, size)
	_area.scale = base
	if shape == Shape.CIRCLE:
		_edge = _part(Fx.sector_mesh(360.0, 0.88, 1.0, 40), 0.07)
		_edge.scale = base


func _part(mesh: Mesh, height: float) -> MeshInstance3D:
	var m := MeshInstance3D.new()
	m.mesh = mesh
	m.material_override = Fx.material(color)
	m.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	m.position.y = height
	add_child(m)
	return m


func set_pose(pos: Vector3, dir: Vector3) -> void:
	global_position = Vector3(pos.x, pos.y, pos.z)
	dir.y = 0.0
	if dir.length() > 0.001:
		look_at(global_position + dir, Vector3.UP)


func set_progress(t: float) -> void:
	t = clampf(t, 0.0, 1.0)
	_fill.scale = Vector3(extra, 1, size * t) if shape == Shape.LINE else Vector3(size * t, 1, size * t)
	var a := color
	a.a *= 0.3
	(_area.material_override as ShaderMaterial).set_shader_parameter("color", a)
	var f := color
	f.a *= lerpf(0.35, 0.75, t)
	(_fill.material_override as ShaderMaterial).set_shader_parameter("color", f)
	if _edge:
		var e := color
		e.a = minf(1.0, color.a * 1.6)
		(_edge.material_override as ShaderMaterial).set_shader_parameter("color", e)
