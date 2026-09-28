class_name Projectile
extends Node3D
## Straight-flying projectile (shape cast each frame). Hurts the other team, stops on scenery,
## flies through dodging targets. Flat arrow/orb sprite with a small trail.

var team := DamageInfo.Team.PLAYER
var direction := Vector3.FORWARD
var speed := 20.0
var max_range := 16.0
var radius := 0.2
var pierce := 0
var make_hit: Callable
var on_landed: Callable

var _travelled := 0.0
var _hit := {}


static func fire(parent: Node, pos: Vector3, dir: Vector3, spec: Dictionary) -> Projectile:
	var p := Projectile.new()
	p.team = spec.team
	p.direction = Vector3(dir.x, 0, dir.z).normalized()
	p.speed = spec.speed
	p.max_range = spec.range
	p.radius = spec.radius
	p.pierce = spec.get("pierce", 0)
	p.make_hit = spec.make_hit
	p.on_landed = spec.get("on_landed", Callable())
	parent.add_child(p)
	p.global_position = pos
	p._build(spec.color, spec.get("arrow", false))
	return p


func _build(color: Color, arrow: bool) -> void:
	var sprite := MeshInstance3D.new()
	var quad := QuadMesh.new()
	var tex := Defaults.tex("Arrow" if arrow else "Orb")
	quad.size = Vector2(0.9 + radius, 0.3) if arrow else Vector2(radius * 2.6, radius * 2.6)
	sprite.mesh = quad
	sprite.material_override = Fx.material(color, not arrow, tex)
	sprite.rotation_degrees = Vector3(-90, 0, 0)
	sprite.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(sprite)
	look_at(global_position + direction, Vector3.UP)
	# Arrow art points along +X; turn it to face -Z (forward).
	sprite.rotate_object_local(Vector3.FORWARD, deg_to_rad(90))


func _physics_process(delta: float) -> void:
	var step := speed * delta
	var space := get_world_3d().direct_space_state
	# Anything overlapping along this frame's path?
	var hits := space.intersect_shape(_query_at(global_position + direction * step * 0.5, step * 0.5 + radius), 16)
	for h in hits:
		var body = h.collider
		var health: Health = body.get_node_or_null("Health") if body is Node else null
		if health:
			if health.team == team or _hit.has(health) or not health.is_alive():
				continue
			_hit[health] = true
			var landed := health.apply_damage(make_hit.call(health))
			if not landed and health.is_invulnerable():
				continue
			if landed and on_landed.is_valid():
				on_landed.call(health)
			if pierce <= 0:
				queue_free()
				return
			pierce -= 1
			continue
		if body is Node and body.has_meta("projectile_pass"):
			continue
		if body is StaticBody3D:
			Fx.sparks(global_position, -direction, Color(1, 0.95, 0.8), 5)
			queue_free()
			return
	global_position += direction * step
	_travelled += step
	if _travelled >= max_range:
		queue_free()


func _query_at(center: Vector3, r: float) -> PhysicsShapeQueryParameters3D:
	var s := SphereShape3D.new()
	s.radius = r
	var q := PhysicsShapeQueryParameters3D.new()
	q.shape = s
	q.transform = Transform3D(Basis(), center)
	return q
