class_name ItemDef
extends Resource

enum Rarity { COMMON, UNCOMMON, RARE, EPIC, LEGENDARY }
enum Category { CURRENCY, CONSUMABLE, MATERIAL, EQUIPMENT }

@export var display_name := "Item"
@export var icon: Texture2D
@export var rarity := Rarity.COMMON
@export var category := Category.MATERIAL
@export var max_stack := 99
@export var heal := 0
@export var equipment: EquipmentVisual

func is_equipment() -> bool:
	return category == Category.EQUIPMENT and equipment != null
