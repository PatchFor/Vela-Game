class_name SkillConfig
extends Resource

@export var display_name := "Skill"
@export var color := Color.WHITE
@export var cooldown := 4.0
@export_range(0.0, 1.0) var crit_chance := 0.15
@export var crit_mult := 1.75
@export var attack: AttackStep
