class_name LootTable
extends Resource
## Same rules as the Unity LootTable: gold chance/range, guaranteed items, weighted rolls.

@export_range(0.0, 1.0) var gold_chance := 0.8
@export var gold_min := 1
@export var gold_max := 5
@export var guaranteed: Array[ItemDef] = []
@export var rolls := 1
@export var nothing_weight := 1.0
@export var items: Array[ItemDef] = []
@export var weights: PackedFloat32Array = []

## Returns {"gold": int, "items": Array[ItemDef]}. `rand` returns a float in [0,1).
func roll(rand: Callable) -> Dictionary:
	var result := {"gold": 0, "items": []}
	if gold_max > 0 and rand.call() < gold_chance:
		var lo := mini(gold_min, gold_max)
		var hi := maxi(gold_min, gold_max)
		result.gold = lo + mini(hi - lo, int(rand.call() * (hi - lo + 1)))
	for item in guaranteed:
		if item:
			result.items.append(item)
	var total := nothing_weight
	for i in items.size():
		total += _weight(i)
	for r in rolls:
		var pick: float = rand.call() * total - nothing_weight
		if pick < 0.0:
			continue
		for i in items.size():
			pick -= _weight(i)
			if pick < 0.0:
				result.items.append(items[i])
				break
	return result

func _weight(i: int) -> float:
	return weights[i] if i < weights.size() else 1.0
