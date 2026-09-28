class_name PlayerConfig
extends Resource

@export var max_hp := 100
@export var iframes_after_hit := 0.6
@export var hurt_stun := 0.18
@export_group("Movement")
@export var move_speed := 6.5
@export var acceleration := 60.0
@export var deceleration := 70.0
@export_group("Dash")
@export var dash_speed := 20.0
@export var dash_time := 0.16
@export var dash_cooldown := 0.45
@export var dash_iframe_bonus := 0.08
@export_group("Perfect dodge")
@export var perfect_window := 0.15
@export var perfect_latency_allowance := 0.0
@export var perfect_slow_time := 0.5
@export var perfect_slow_scale := 0.3
@export var perfect_resets_dash := true
@export var counter_window := 1.5
@export var counter_mult := 1.5
@export var counter_always_crits := true
@export_group("Jump links")
@export var jump_height := 1.8
@export var jump_time := 0.55
@export var jump_max_angle := 65.0
@export_group("Lock-on / aim")
@export var lock_range := 14.0
@export var lock_break_range := 20.0
@export var aim_assist_angle := 40.0
@export var aim_assist_range := 1.2
@export_group("Mouse")
## What each mouse button does when it isn't clicking an item or an enemy.
@export_enum("attack", "charge", "skill_1", "skill_2", "skill_3", "skill_4", "none") var mouse_left := "attack"
@export_enum("attack", "charge", "skill_1", "skill_2", "skill_3", "skill_4", "none") var mouse_right := "skill_1"
@export var hover_radius := 0.9 ## how close the pointer must be to an item/enemy to hover it
@export_group("Loadout")
@export var weapons: Array[WeaponConfig] = []
@export var skills: Array[SkillConfig] = []
@export var starting_equipment: Array[ItemDef] = []
@export var starting_items: Array[ItemDef] = []
