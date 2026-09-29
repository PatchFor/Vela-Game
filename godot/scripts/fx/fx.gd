extends Node
## Autoload "Fx": every mock effect in one place (Unity: FxManager, SlashArc, RingPulse, AfterImage,
## Telegraph, DamageNumbers). Swap bodies for real VFX later; callers only use these functions.

const FX_SHADER := preload("res://shaders/fx.gdshader")
const FX_ADD_SHADER := preload("res://shaders/fx_add.gdshader")
const MAX_PARTICLES := 1500

var _layer: Node3D
var _mm: MultiMesh
var _p_pos := PackedVector3Array()
var _p_vel := PackedVector3Array()
var _p_col := PackedColorArray()
var _p_size := PackedFloat32Array()
var _p_life := PackedFloat32Array()
var _p_age := PackedFloat32Array()
var _p_grav := PackedFloat32Array()

var _rings: Array = []   ## {node, from, to, t, dur, color}
var _slashes: Array = [] ## {node, mesh, radius, width, arc, color, t, dur, reverse}
var _ghosts: Array = []  ## {node, t, dur, color}
var _numbers: Array = [] ## {label, pos, drift, t, dur, scale, crit}
var _ring_mesh: ArrayMesh


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_layer = Node3D.new()
	_layer.name = "FxLayer"
	add_child(_layer)
	_ring_mesh = sector_mesh(360.0, 0.88, 1.0, 48)

	var mmi := MultiMeshInstance3D.new()
	_mm = MultiMesh.new()
	_mm.transform_format = MultiMesh.TRANSFORM_3D
	_mm.use_colors = true
	var quad := QuadMesh.new()
	quad.size = Vector2.ONE
	_mm.mesh = quad
	_mm.instance_count = MAX_PARTICLES
	_mm.visible_instance_count = 0
	mmi.multimesh = _mm
	mmi.material_override = material(Color.WHITE, true)
	mmi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_layer.add_child(mmi)


func clear() -> void:
	for list in [_rings, _slashes, _ghosts]:
		for e in list:
			if is_instance_valid(e.node):
				e.node.queue_free()
		list.clear()
	for n in _numbers:
		if is_instance_valid(n.label):
			n.label.queue_free()
	_numbers.clear()
	_p_pos.clear()
	_p_vel.clear()
	_p_col.clear()
	_p_size.clear()
	_p_life.clear()
	_p_age.clear()
	_p_grav.clear()


# ------------------------------------------------------------------ meshes & materials

static func material(color: Color, additive := false, texture: Texture2D = null) -> ShaderMaterial:
	var m := ShaderMaterial.new()
	m.shader = FX_ADD_SHADER if additive else FX_SHADER
	m.set_shader_parameter("color", color)
	if texture:
		m.set_shader_parameter("tex", texture)
	return m


## Flat pie slice / ring on the XZ plane, centered on -Z (Godot forward).
static func sector_mesh(arc_deg: float, inner: float, outer: float, segments: int) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var half := deg_to_rad(arc_deg) * 0.5
	for i in segments:
		var a0 := lerpf(-half, half, float(i) / segments)
		var a1 := lerpf(-half, half, float(i + 1) / segments)
		var d0 := Vector3(sin(a0), 0, -cos(a0))
		var d1 := Vector3(sin(a1), 0, -cos(a1))
		for v in [d0 * inner, d0 * outer, d1 * outer, d0 * inner, d1 * outer, d1 * inner]:
			st.set_color(Color.WHITE)
			st.add_vertex(v)
	return st.commit()


static func rect_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for v in [Vector3(-0.5, 0, 0), Vector3(0.5, 0, 0), Vector3(0.5, 0, -1), Vector3(-0.5, 0, 0), Vector3(0.5, 0, -1), Vector3(-0.5, 0, -1)]:
		st.set_color(Color.WHITE)
		st.add_vertex(v)
	return st.commit()


# ------------------------------------------------------------------ particles

func _emit(pos: Vector3, vel: Vector3, color: Color, size: float, life: float, gravity: float) -> void:
	if _p_pos.size() >= MAX_PARTICLES:
		return
	_p_pos.append(pos)
	_p_vel.append(vel)
	_p_col.append(color)
	_p_size.append(size)
	_p_life.append(life)
	_p_age.append(0.0)
	_p_grav.append(gravity)


func sparks(pos: Vector3, dir: Vector3, color: Color, count: int) -> void:
	dir.y = 0.0
	dir = dir.normalized() if dir.length() > 0.01 else Vector3.RIGHT.rotated(Vector3.UP, randf() * TAU)
	for i in count:
		var spread := Vector3(randf_range(-1, 1), randf_range(-1, 1), randf_range(-1, 1)) * 0.9
		var v := (dir + spread + Vector3.UP * randf_range(0.2, 0.9)).normalized() * randf_range(5.0, 11.0)
		_emit(pos, v, color, randf_range(0.07, 0.16), randf_range(0.12, 0.3), -14.0)


func dust(pos: Vector3, count: int, color := Color(0.85, 0.82, 0.75, 0.8)) -> void:
	for i in count:
		var f := Vector2(randf_range(-1, 1), randf_range(-1, 1))
		var v := Vector3(f.x, randf_range(0.3, 1.0), f.y) * randf_range(0.8, 2.2)
		_emit(pos + Vector3(f.x, 0.4, f.y) * 0.25, v, color, randf_range(0.12, 0.26), randf_range(0.25, 0.5), 1.5)


func death_burst(pos: Vector3, color: Color, count: int) -> void:
	for i in count:
		var v := Vector3(randf_range(-1, 1), randf_range(0.2, 1), randf_range(-1, 1)).normalized() * randf_range(3.0, 8.0)
		v.y = absf(v.y) + 1.5
		_emit(pos, v, color, randf_range(0.1, 0.25), randf_range(0.3, 0.7), -14.0)
	dust(pos, count / 2)
	ring(pos, 0.2, 2.2, 0.35, color)


func crit_burst(pos: Vector3, color: Color) -> void:
	var offset := randf() * TAU
	for i in 10:
		var d := Vector3.FORWARD.rotated(Vector3.UP, offset + TAU * i / 10.0)
		for k in 3:
			_emit(pos, (d + Vector3.UP * randf_range(0.1, 0.6)) * (9.0 + k * 3.0), Color.WHITE if k == 0 else color, 0.14 - k * 0.03, 0.18 + k * 0.04, -10.0)
	var ground := pos - Vector3(0, 0.9, 0)
	ring(ground, 0.3, 2.2, 0.28, color)
	ring(ground, 0.1, 1.2, 0.18, Color.WHITE)


# ------------------------------------------------------------------ rings, slashes, afterimages

func ring(pos: Vector3, from: float, to: float, duration: float, color: Color) -> void:
	var node := MeshInstance3D.new()
	node.mesh = _ring_mesh
	node.material_override = material(color, true)
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_layer.add_child(node)
	node.global_position = pos + Vector3(0, 0.06, 0)
	_rings.append({"node": node, "from": from, "to": to, "t": 0.0, "dur": maxf(0.02, duration), "color": color})


func slash(origin: Vector3, dir: Vector3, radius: float, width: float, arc_deg: float, color: Color, duration: float, reverse: bool) -> void:
	dir.y = 0.0
	if dir.length() < 0.01:
		return
	var node := MeshInstance3D.new()
	var mesh := ImmediateMesh.new()
	node.mesh = mesh
	node.material_override = material(Color.WHITE, true)
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_layer.add_child(node)
	node.global_position = origin + Vector3(0, 0.7, 0)
	node.look_at(node.global_position + dir, Vector3.UP)
	_slashes.append({"node": node, "mesh": mesh, "radius": radius, "width": minf(width, radius * 0.95), "arc": clampf(arc_deg, 10, 360), "color": color, "t": 0.0, "dur": maxf(0.02, duration), "reverse": reverse})


func afterimage(rig: SpriteRig, color: Color, life: float) -> void:
	for q in rig.visible_quads():
		var ghost := MeshInstance3D.new()
		ghost.mesh = q.mesh.duplicate()
		var mat := (q.material_override as ShaderMaterial).duplicate() as ShaderMaterial
		mat.set_shader_parameter("flash", 1.0)
		mat.set_shader_parameter("flash_color", color)
		ghost.material_override = mat
		ghost.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		_layer.add_child(ghost)
		ghost.global_transform = q.global_transform
		_ghosts.append({"node": ghost, "t": 0.0, "dur": life, "color": color})


# ------------------------------------------------------------------ damage numbers

func number(pos: Vector3, text: String, color: Color, scale := 1.0, crit := false, label := "") -> void:
	var l := Label3D.new()
	l.text = text if label == "" else label + "\n" + text
	l.modulate = color
	l.font_size = Game.feel.font_size
	l.outline_size = 14
	l.outline_modulate = Color(0, 0, 0, 0.9)
	l.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	l.no_depth_test = true
	l.fixed_size = true
	l.pixel_size = 0.0005 * scale
	l.render_priority = 10
	_layer.add_child(l)
	l.global_position = pos
	var drift := Vector3(randf_range(-0.4, 0.4), 0, randf_range(-0.1, 0.1))
	_numbers.append({"label": l, "pos": pos, "drift": drift, "t": 0.0, "dur": Game.feel.number_life * (1.35 if crit else 1.0), "scale": scale, "crit": crit})


# ------------------------------------------------------------------ update

func _process(delta: float) -> void:
	var real_dt := minf(get_process_delta_time() / maxf(Engine.time_scale, 0.001), 0.1)
	_update_particles(delta)
	_update_rings(delta)
	_update_slashes(delta)
	_update_ghosts(delta)
	_update_numbers(real_dt)


func _update_particles(dt: float) -> void:
	var i := _p_pos.size() - 1
	while i >= 0:
		_p_age[i] += dt
		if _p_age[i] >= _p_life[i]:
			_p_pos.remove_at(i)
			_p_vel.remove_at(i)
			_p_col.remove_at(i)
			_p_size.remove_at(i)
			_p_life.remove_at(i)
			_p_age.remove_at(i)
			_p_grav.remove_at(i)
		else:
			var v := _p_vel[i]
			v.y += _p_grav[i] * dt
			v *= exp(-3.0 * dt)
			_p_vel[i] = v
			_p_pos[i] += v * dt
		i -= 1

	var cam := get_viewport().get_camera_3d()
	var basis := cam.global_transform.basis if cam else Basis()
	_mm.visible_instance_count = _p_pos.size()
	for k in _p_pos.size():
		var life_t := _p_age[k] / _p_life[k]
		var s := _p_size[k] * (1.0 - life_t)
		_mm.set_instance_transform(k, Transform3D(basis.scaled(Vector3(s, s, s)), _p_pos[k]))
		var c := _p_col[k]
		c.a *= 1.0 if life_t < 0.6 else (1.0 - life_t) / 0.4
		_mm.set_instance_color(k, c)


func _update_rings(dt: float) -> void:
	for e in _rings.duplicate():
		e.t += dt
		var t: float = e.t / e.dur
		if t >= 1.0 or not is_instance_valid(e.node):
			if is_instance_valid(e.node):
				e.node.queue_free()
			_rings.erase(e)
			continue
		var r := lerpf(e.from, e.to, 1.0 - (1.0 - t) * (1.0 - t))
		e.node.scale = Vector3(r, 1, r)
		var c: Color = e.color
		c.a *= 1.0 - t
		(e.node.material_override as ShaderMaterial).set_shader_parameter("color", c)


func _update_slashes(dt: float) -> void:
	for e in _slashes.duplicate():
		e.t += dt
		var t: float = e.t / e.dur
		if t >= 1.0 or not is_instance_valid(e.node):
			if is_instance_valid(e.node):
				e.node.queue_free()
			_slashes.erase(e)
			continue
		var head := 1.0 - pow(1.0 - clampf(t * 1.8, 0, 1), 3.0)
		var tail := clampf((t - 0.2) / 0.8, 0, 1)
		tail *= tail
		var half := deg_to_rad(e.arc) * 0.5
		var dir_sign := -1.0 if e.reverse else 1.0
		var alpha := 1.0 - t * t
		var mesh: ImmediateMesh = e.mesh
		mesh.clear_surfaces()
		mesh.surface_begin(Mesh.PRIMITIVE_TRIANGLE_STRIP)
		for i in 21:
			var s := i / 20.0
			var along := lerpf(tail, head, s)
			var a := lerpf(-half, half, along) * dir_sign
			var d := Vector3(sin(a), 0, -cos(a))
			var thick: float = e.width * lerpf(0.15, 1.0, s)
			var c: Color = e.color
			c.a *= alpha * s
			mesh.surface_set_color(c * Color(1, 1, 1, 0.6))
			mesh.surface_add_vertex(d * (e.radius - thick))
			mesh.surface_set_color(c)
			mesh.surface_add_vertex(d * e.radius)
		mesh.surface_end()


func _update_ghosts(dt: float) -> void:
	for e in _ghosts.duplicate():
		e.t += dt
		var t: float = e.t / e.dur
		if t >= 1.0 or not is_instance_valid(e.node):
			if is_instance_valid(e.node):
				e.node.queue_free()
			_ghosts.erase(e)
			continue
		(e.node.material_override as ShaderMaterial).set_shader_parameter("fade", (1.0 - t) * e.color.a)


func _update_numbers(dt: float) -> void:
	for n in _numbers.duplicate():
		n.t += dt
		var t: float = n.t / n.dur
		if t >= 1.0 or not is_instance_valid(n.label):
			if is_instance_valid(n.label):
				n.label.queue_free()
			_numbers.erase(n)
			continue
		var hang := 0.18 if n.crit else 0.0
		var rt := clampf((t - hang) / (1.0 - hang), 0, 1)
		var rise: float = Game.feel.rise * n.dur * (1.0 - (1.0 - rt) * (1.0 - rt))
		n.label.global_position = n.pos + Vector3.UP * rise + n.drift * rt
		var peak := 1.8 if n.crit else 1.35
		var pop := lerpf(0.5, peak, n.t / 0.07) if n.t < 0.07 else lerpf(peak, 1.0, clampf((n.t - 0.07) / 0.14, 0, 1))
		n.label.pixel_size = 0.0005 * n.scale * pop
		var c: Color = n.label.modulate
		c.a = 1.0 if t < 0.7 else 1.0 - (t - 0.7) / 0.3
		n.label.modulate = c
		n.label.outline_modulate.a = c.a * 0.9
