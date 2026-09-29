class_name AttackStep
extends Resource
## One swing / shot / skill hit. Same fields as the Unity AttackStep.

enum Kind { MELEE, PROJECTILE }

@export var name := "Slash"
@export var kind := Kind.MELEE
@export var hit_weight := 0 ## 0 Auto, 1 Light, 2 Medium, 3 Heavy, 4 Finisher
@export_group("Damage")
@export var damage := 10
@export_range(0.0, 0.5) var variance := 0.1
@export_group("Timing (seconds)")
@export var windup := 0.06
@export var active := 0.08
@export var recovery := 0.2
@export_group("Melee")
@export var attack_range := 2.2
@export_range(10.0, 360.0) var arc := 120.0
@export var lunge := 0.6
@export_group("Projectile")
@export var proj_count := 1
@export var spread := 0.0
@export var proj_speed := 24.0
@export var proj_range := 16.0
@export var pierce := 0
@export var proj_size := 0.2
@export var proj_color := Color(1, 0.95, 0.7)
@export_group("Impact")
@export var knockback := 4.0
@export var stagger := 10.0
@export var hit_stop := 0.05
@export var shake := 0.12
@export_group("While attacking")
@export_range(0.0, 1.0) var move_mult := 0.15
@export var super_armor := false
@export_group("Slash FX")
@export var slash_color := Color.WHITE
@export var slash_width := 0.6
@export var slash_duration := 0.14
@export var reverse := false
