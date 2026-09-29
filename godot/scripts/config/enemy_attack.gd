class_name EnemyAttack
extends Resource
## One monster attack. Same data model as the Unity EnemyAttack.

enum Kind { MELEE_ARC, LUNGE, CHARGE, PROJECTILE, GROUND_AOE, SELF_AOE, RADIAL, SUMMON }

@export var name := "Attack"
@export var kind := Kind.MELEE_ARC
@export_group("Selection")
@export var weight := 1.0
@export var min_range := 0.0
@export var max_range := 2.5
@export var cooldown := 1.5
@export_group("Timing")
@export var windup := 0.6
@export var active := 0.2
@export var recovery := 0.6
@export var track := true
@export var track_speed := 240.0
@export_group("Damage")
@export var damage := 10
@export var knockback := 6.0
@export var hit_weight := 0
@export_group("Shape")
@export var radius := 2.0
@export var arc := 100.0
@export var dash_speed := 14.0
@export_group("Projectiles")
@export var proj_count := 1
@export var spread := 0.0
@export var volleys := 1
@export var volley_interval := 0.15
@export var proj_speed := 10.0
@export var proj_range := 14.0
@export var proj_size := 0.3
@export_group("Ground AoE")
@export var aoe_count := 1
@export var aoe_scatter := 0.0
@export_group("Summon")
@export var summon: Resource ## MonsterConfig
@export var summon_count := 2
@export var color := Color(1, 0.3, 0.2, 0.55)
