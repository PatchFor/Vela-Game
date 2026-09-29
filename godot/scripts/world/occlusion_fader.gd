class_name OcclusionFader
extends Node
## Makes scenery see-through when it hides the player or a nearby monster (Unity: OcclusionFader +
## FadeableObject). Casts camera → character rays against the fade volumes only (layer 2 areas),
## then eases each hit object's alpha down. Objects are opaque until they start fading, then
## alpha-blended (drawn after characters, so the hidden character shows through).

var faded_alpha := 0.3 ## how see-through a hiding object gets
var fade_speed := 6.0 ## alpha per second
var enemy_range := 14.0 ## monsters closer than this to the player also count

var _alpha := {} ## Area3D -> current alpha


func _process(delta: float) -> void:
	var cam := get_viewport().get_camera_3d()
	if cam == null or Game.player == null:
		return
	var space := cam.get_world_3d().direct_space_state
	var targets: Array = [Game.player]
	for e in Game.enemies:
		if is_instance_valid(e) and e.global_position.distance_to(Game.player.global_position) < enemy_range:
			targets.append(e)
	var hiding := {}
	for t in targets:
		var from := cam.global_position
		var to: Vector3 = t.global_position + Vector3.UP * 0.9
		var exclude: Array[RID] = []
		for i in 6:
			var q := PhysicsRayQueryParameters3D.create(from, to, 1 << (Level.FADE_LAYER - 1), exclude)
			q.collide_with_areas = true
			q.collide_with_bodies = false
			var hit := space.intersect_ray(q)
			if hit.is_empty():
				break
			hiding[hit.collider] = true
			exclude.append(hit.rid)

	for area in hiding.keys():
		if not _alpha.has(area):
			_alpha[area] = 1.0
	for area in _alpha.keys():
		if not is_instance_valid(area):
			_alpha.erase(area)
			continue
		var goal := faded_alpha if hiding.has(area) else 1.0
		var a: float = move_toward(_alpha[area], goal, fade_speed * delta)
		_alpha[area] = a
		for m in area.get_meta("fade_meshes"):
			var mat := m.material_override as StandardMaterial3D
			mat.albedo_color.a = a
			mat.transparency = BaseMaterial3D.TRANSPARENCY_DISABLED if a >= 1.0 else BaseMaterial3D.TRANSPARENCY_ALPHA
		if a >= 1.0 and not hiding.has(area):
			_alpha.erase(area)
