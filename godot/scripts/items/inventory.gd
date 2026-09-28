class_name Inventory
extends RefCounted
## Plain-logic bag (Unity: Inventory). Stacks fill first, then empty slots; the rest is returned
## to the caller (it stays on the ground). Gold never uses a slot.

signal changed

var slots: Array = [] ## each: {"item": ItemDef, "count": int} or null
var gold := 0


func _init(capacity := 24) -> void:
	slots.resize(maxi(1, capacity))


func capacity() -> int:
	return slots.size()


func is_full() -> bool:
	return not slots.has(null)


## Adds up to `count`; returns how many were added.
func add(item: ItemDef, count: int) -> int:
	if item == null or count <= 0:
		return 0
	if item.category == ItemDef.Category.CURRENCY:
		gold += count
		changed.emit()
		return count
	var remaining := count
	if item.max_stack > 1:
		for s in slots:
			if s != null and s.item == item and s.count < item.max_stack and remaining > 0:
				var moved := mini(item.max_stack - s.count, remaining)
				s.count += moved
				remaining -= moved
	for i in slots.size():
		if remaining <= 0:
			break
		if slots[i] == null:
			var moved := mini(item.max_stack, remaining)
			slots[i] = {"item": item, "count": moved}
			remaining -= moved
	var added := count - remaining
	if added > 0:
		changed.emit()
	return added


func remove_at(index: int, count := 999999) -> Dictionary:
	var s = slots[index]
	if s == null:
		return {}
	var taken := mini(count, s.count)
	s.count -= taken
	if s.count <= 0:
		slots[index] = null
	changed.emit()
	return {"item": s.item, "count": taken}


func place_at(index: int, item: ItemDef, count: int) -> bool:
	if slots[index] != null:
		return false
	slots[index] = {"item": item, "count": count}
	changed.emit()
	return true


## Drag & drop: merge same stackable item (overflow stays), otherwise swap.
func move(from: int, to: int) -> void:
	if from == to or slots[from] == null:
		return
	var a = slots[from]
	var b = slots[to]
	if b != null and a.item == b.item and a.item.max_stack > 1:
		var moved := mini(a.item.max_stack - b.count, a.count)
		b.count += moved
		a.count -= moved
		if a.count <= 0:
			slots[from] = null
	else:
		slots[from] = b
		slots[to] = a
	changed.emit()


func count_of(item: ItemDef) -> int:
	var total := 0
	for s in slots:
		if s != null and s.item == item:
			total += s.count
	return total
