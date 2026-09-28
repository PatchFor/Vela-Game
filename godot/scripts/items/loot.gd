class_name Loot
extends RefCounted
## Rolls a LootTable and scatters the drops (Unity: LootSpawner): evenly spread, never through walls
## or into water, rarer items land last.


static func drop(parent: Node3D, table: LootTable, origin: Vector3, owner_node: Node = null) -> void:
	if table == null:
		return
	var result := table.roll(Game.roll)
	var gold: int = result.gold
	var piles := 0 if gold <= 0 else (3 if gold >= 30 else (2 if gold >= 10 else 1))
	var items: Array = result.items
	var total := items.size() + piles
	if total == 0:
		return
	var start := randf() * TAU
	var index := 0
	var remaining := gold
	for p in piles:
		var amount := remaining if p == piles - 1 else maxi(1, gold / piles)
		remaining -= amount
		var w := WorldItem.spawn(parent, null, 0, amount, origin, _landing(parent, origin, start, index, total), 0.02 * index)
		w.owner_node = owner_node
		index += 1
	for it in items:
		var delay: float = 0.02 * index + Game.loot_config.rarity_delay * it.rarity
		var w := WorldItem.spawn(parent, it, 1, 0, origin, _landing(parent, origin, start, index, total), delay)
		w.owner_node = owner_node
		index += 1


static func drop_stack(parent: Node3D, item: ItemDef, count: int, origin: Vector3) -> void:
	WorldItem.spawn(parent, item, count, 0, origin, _landing(parent, origin, randf() * TAU, 0, 1), 0.0)


static func _landing(parent: Node3D, origin: Vector3, start: float, index: int, total: int) -> Vector3:
	var cfg := Game.loot_config
	var angle := start + TAU * index / maxi(1, total) + randf_range(-0.25, 0.25)
	var dir := Vector3.FORWARD.rotated(Vector3.UP, angle)
	var dist := randf_range(cfg.scatter.x, cfg.scatter.y)
	var from := origin + Vector3.UP * 0.5
	var q := PhysicsRayQueryParameters3D.create(from, from + dir * dist)
	var hit := parent.get_world_3d().direct_space_state.intersect_ray(q)
	if hit and hit.collider is StaticBody3D:
		dist = maxf(0.2, from.distance_to(hit.position) - 0.5)
	var landing := origin + dir * dist
	landing.y = origin.y
	return landing
