class_name DamageInfo
extends RefCounted
## Everything one hit carries. Same fields as the Unity DamageInfo.

enum Team { PLAYER, ENEMY }

var amount := 0
var crit := false
var punish := false
var counter := false
var team := Team.PLAYER
var source: Node3D
var weight := 1 ## 1 Light .. 4 Finisher (resolved)
var hit_point := Vector3.ZERO
var direction := Vector3.FORWARD
var knockback := 0.0
var stagger := 0.0
var hit_stop := 0.0
var shake := 0.0


## Auto (0) → a concrete weight from stagger / damage, same thresholds as Unity.
static func resolve_weight(weight: int, stagger_amount: float, damage: int) -> int:
	if weight != 0:
		return weight
	if stagger_amount >= 60.0 or damage >= 45:
		return 4
	if stagger_amount >= 30.0 or damage >= 25:
		return 3
	if stagger_amount >= 15.0 or damage >= 15:
		return 2
	return 1
