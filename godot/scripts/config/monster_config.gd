class_name MonsterConfig
extends Resource
## A monster (or, with `phases`, a boss). Same fields as the Unity MonsterConfig/BossConfig.

enum Movement { CHASE, KEEP_DISTANCE, CIRCLE, ERRATIC, STATIONARY }

@export var display_name := "Monster"
@export var texture: Texture2D
@export var world_height := 1.8
@export var shadow_size := 1.0
@export var hover := 0.0
@export_group("Body")
@export var max_hp := 40
@export var radius := 0.45
@export var height := 1.6
@export_group("Hit reactions")
@export var poise := 15.0
@export var stagger_duration := 0.35
@export_range(0.0, 1.0) var knockback_resist := 0.0
@export var alert_radius := 7.0
@export var immortal := false
@export var death_color := Color(1, 0.5, 0.4)
@export_group("Behaviour")
@export var movement := Movement.CHASE
@export var move_speed := 3.5
@export var detection := 11.0
@export var leash := 24.0
@export var preferred := 1.6
@export var erratic_interval := 0.5
@export var decision_delay := 0.35
@export var attacks: Array[EnemyAttack] = []
@export_group("Loot")
@export var loot: LootTable
@export_group("Boss")
@export var phases: Array[BossPhase] = []

func is_boss() -> bool:
	return not phases.is_empty()
