class_name JumpLink
extends Node3D
## A marked jump spot (Unity: JumpLink). Dashing from near one end toward the other end jumps
## across (water, gaps). Works both ways. Glowing pads mark both ends.

var a := Vector3.ZERO
var b := Vector3.ZERO
var trigger_radius := 1.6 ## how close to an end the dash must start


static func create(parent: Node, from: Vector3, to: Vector3, radius := 1.6) -> JumpLink:
	var j := JumpLink.new()
	j.a = from
	j.b = to
	j.trigger_radius = radius
	j.add_to_group("jump_link")
	parent.add_child(j)
	for p in [from, to]:
		var pad := MeshInstance3D.new()
		pad.mesh = Fx.sector_mesh(360.0, 0.0, radius * 0.55, 24)
		pad.material_override = Fx.material(Color(0.5, 0.85, 1, 0.35))
		j.add_child(pad)
		pad.global_position = p + Vector3.UP * 0.03
		var edge := MeshInstance3D.new()
		edge.mesh = Fx.sector_mesh(360.0, radius * 0.5, radius * 0.55, 24)
		edge.material_override = Fx.material(Color(0.6, 0.95, 1, 0.9), true)
		j.add_child(edge)
		edge.global_position = p + Vector3.UP * 0.035
	return j


## [start, end] if a dash from `pos` in `dir` should use this link, else [].
func match_dash(pos: Vector3, dir: Vector3, max_angle: float) -> Array:
	for pair in [[a, b], [b, a]]:
		var start: Vector3 = pair[0]
		var end: Vector3 = pair[1]
		var off := pos - start
		off.y = 0.0
		if off.length() > trigger_radius:
			continue
		var across := end - pos
		across.y = 0.0
		var d := Vector3(dir.x, 0, dir.z)
		if d.length() < 0.01 or across.length() < 0.01:
			continue
		if rad_to_deg(d.angle_to(across)) <= max_angle:
			return [start, end]
	return []
