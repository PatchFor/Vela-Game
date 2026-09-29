class_name LootConfig
extends Resource

@export var gold_icon: Texture2D
@export var rarity_colors: Array[Color] = [Color(0.85, 0.85, 0.85), Color(0.4, 0.9, 0.4), Color(0.35, 0.6, 1), Color(0.75, 0.4, 1), Color(1, 0.65, 0.15)]
@export var beam_heights: PackedFloat32Array = [0.0, 0.8, 1.6, 2.6, 4.0]
@export var scatter := Vector2(0.8, 2.2)
@export var pop_height := 1.6
@export var pop_time := 0.45
@export var rarity_delay := 0.08
@export var pickup_range := 2.0
@export var gold_magnet := 1.4
@export var inventory_size := 24

func rarity_color(r: int) -> Color:
	return rarity_colors[r] if r < rarity_colors.size() else Color.WHITE
