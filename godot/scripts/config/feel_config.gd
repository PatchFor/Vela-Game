class_name FeelConfig
extends Resource
## Global combat feel. Same knobs as the Unity CombatFeelConfig.

enum HitStopMode { GLOBAL_TIME_SCALE, LOCAL_VISUAL }

@export var hit_stop_mode := HitStopMode.GLOBAL_TIME_SCALE
@export_group("Multipliers")
@export var hit_stop_scale := 1.0
@export var shake_scale := 1.0
@export var zoom_punch_scale := 1.0
@export var knockback_scale := 1.0
@export var enemy_damage_scale := 1.0
@export_group("Profiles")
@export var light: ImpactProfile
@export var medium: ImpactProfile
@export var heavy: ImpactProfile
@export var finisher: ImpactProfile
@export_group("Crit")
@export var crit_hit_stop_bonus := 0.05
@export var crit_extra_shake := 0.15
@export var crit_zoom := 0.06
@export var crit_slow_time := 0.12
@export var crit_slow_scale := 0.35
@export var crit_color := Color(1, 0.85, 0.2)
@export var crit_number_scale := 1.5
@export_group("Break / punish / counter")
@export var break_color := Color(0.4, 0.85, 1)
@export var break_hit_stop := 0.08
@export var punish_mult := 1.5
@export var punish_color := Color(1, 0.55, 0.85)
@export var counter_color := Color(0.5, 0.85, 1)
@export_group("Player hurt")
@export var hurt_hit_stop := 0.1
@export var hurt_shake := 0.4
@export var hurt_zoom := 0.04
@export var blink_rate := 16.0
@export_group("Flash")
@export var flash_time := 0.07
@export var hurt_tint := Color(1, 0.45, 0.45)
@export var hurt_tint_time := 0.18
@export_group("Numbers")
@export var font_size := 48
@export var rise := 1.6
@export var number_life := 0.8
@export var taken_color := Color(1, 0.3, 0.3)
@export var heal_color := Color(0.4, 1, 0.5)
@export_group("Camera shake")
@export var shake_max_offset := 0.6
@export var shake_decay := 2.2
@export_group("Kills")
@export var kill_hit_stop := 0.06
@export var combo_timeout := 1.6

func profile(weight: int) -> ImpactProfile:
	match weight:
		2: return medium
		3: return heavy
		4: return finisher
	return light
