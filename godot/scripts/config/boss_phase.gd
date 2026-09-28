class_name BossPhase
extends Resource

@export var name := "Phase"
@export_range(0.0, 1.0) var starts_at := 1.0
@export var tint := Color.WHITE
@export var sprite_scale := 1.0
@export var transition := 1.4
@export var announcement := ""
@export var shockwave_radius := 0.0
@export var shockwave_damage := 10
@export var shockwave_knockback := 14.0
@export var summons: Array[Resource] = []
@export var move_speed := 3.0
@export var decision_delay := 0.5
@export var attacks: Array[EnemyAttack] = []
