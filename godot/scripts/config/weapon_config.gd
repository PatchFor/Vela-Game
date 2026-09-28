class_name WeaponConfig
extends Resource

@export var display_name := "Sword"
@export var ui_color := Color.WHITE
@export var visual: EquipmentVisual
@export_group("Crit")
@export_range(0.0, 1.0) var crit_chance := 0.15
@export var crit_mult := 1.75
@export_group("Combo")
@export var combo: Array[AttackStep] = []
@export var combo_reset := 0.45
@export var input_buffer := 0.25
@export var repeat_held := false
@export var dash_cancel := true
@export_group("Hit-confirm")
@export var hit_confirm_cancel := true
@export var hit_confirm_delay := 0.06
@export_group("Charged")
@export var has_charged := true
@export var charge_time := 0.6
@export_range(0.0, 1.0) var charge_move_mult := 0.45
@export var charged: AttackStep
