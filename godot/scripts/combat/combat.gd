class_name Combat
extends RefCounted
## Hit tests shared by player and monsters (Unity: CombatUtility).


## Everything of the other team inside `radius` and `arc_deg` of `forward`. Skips `already`.
## `make_hit` builds a DamageInfo per victim. Returns the number of hits that dealt damage.
static func melee_arc(world: World3D, origin: Vector3, forward: Vector3, radius: float, arc_deg: float, team: int, already: Dictionary, make_hit: Callable) -> int:
	forward.y = 0.0
	forward = forward.normalized() if forward.length() > 0.001 else Vector3.FORWARD
	var landed := 0
	for body in _bodies_in_sphere(world, origin + Vector3.UP * 0.8, radius + 1.0):
		var health: Health = body.get_node_or_null("Health")
		if health == null or not health.targetable_by(team) or already.has(health):
			continue
		var offset: Vector3 = body.global_position - origin
		offset.y = 0.0
		var body_radius: float = body.get_meta("radius", 0.4)
		if offset.length() - body_radius > radius:
			continue
		var within := arc_deg >= 359.0 or offset.length() < body_radius + 0.3
		if not within:
			var slack := rad_to_deg(atan2(body_radius, maxf(0.1, offset.length())))
			within = rad_to_deg(forward.angle_to(offset)) <= arc_deg * 0.5 + slack
		if not within:
			continue
		already[health] = true
		if health.apply_damage(make_hit.call(health)):
			landed += 1
	return landed


static func _bodies_in_sphere(world: World3D, center: Vector3, radius: float) -> Array:
	var shape := SphereShape3D.new()
	shape.radius = radius
	var q := PhysicsShapeQueryParameters3D.new()
	q.shape = shape
	q.transform = Transform3D(Basis(), center)
	q.collide_with_areas = false
	var found := []
	for hit in world.direct_space_state.intersect_shape(q, 64):
		var c = hit.collider
		if c is CharacterBody3D and not found.has(c):
			found.append(c)
	return found


static func make_hit(source: Node3D, team: int, victim: Health, amount: int, crit: bool, knockback: float, stagger: float, hit_stop: float, shake: float, weight: int) -> DamageInfo:
	var info := DamageInfo.new()
	var victim_pos: Vector3 = victim.get_parent().global_position
	var origin := source.global_position if is_instance_valid(source) else victim_pos
	var d := victim_pos - origin
	d.y = 0.0
	info.amount = maxi(1, amount)
	info.crit = crit
	info.team = team
	info.source = source
	info.weight = DamageInfo.resolve_weight(weight, stagger, amount)
	info.hit_point = victim_pos + Vector3.UP
	info.direction = d.normalized() if d.length() > 0.001 else Vector3.FORWARD
	info.knockback = knockback
	info.stagger = stagger
	info.hit_stop = hit_stop
	info.shake = shake
	return info


static func roll_damage(base: int, variance: float) -> int:
	return maxi(1, roundi(base * (1.0 + (Game.roll() * 2.0 - 1.0) * variance)))
