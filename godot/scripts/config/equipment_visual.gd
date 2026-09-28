class_name EquipmentVisual
extends Resource
## One wearable layer: 3 facings + a tint (palette swap). Slot names: head, chest, hands, feet, weapon.

@export_enum("head", "chest", "hands", "feet", "weapon") var slot := "head"
@export var down: Texture2D
@export var up: Texture2D
@export var side: Texture2D
@export var tint := Color.WHITE
@export var hides_hair := false

func get_texture(facing: int) -> Texture2D:
	match facing:
		1: return up if up else down
		2: return side if side else down
	return down
