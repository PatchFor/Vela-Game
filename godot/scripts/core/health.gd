class_name Health
extends Node
## HP, team, i-frames. Hits stopped by i-frames emit `evaded` (perfect dodge listens to it).

signal damaged(info: DamageInfo)
signal evaded(info: DamageInfo)
signal healed(amount: int)
signal died

var max_hp := 100
var current := 100
var team := DamageInfo.Team.ENEMY
var iframes_after_hit := 0.0
var immortal := false
var _invulnerable_until := 0.0


func setup(hp: int, new_team: int, iframes := 0.0) -> void:
	max_hp = maxi(1, hp)
	current = max_hp
	team = new_team
	iframes_after_hit = iframes


func is_alive() -> bool:
	return current > 0


func is_invulnerable() -> bool:
	return Game.time < _invulnerable_until


func ratio() -> float:
	return clampf(float(current) / max_hp, 0.0, 1.0)


func targetable_by(attacker_team: int) -> bool:
	return attacker_team != team and is_alive()


func grant_invulnerability(seconds: float) -> void:
	_invulnerable_until = maxf(_invulnerable_until, Game.time + seconds)


## True if damage was dealt.
func apply_damage(info: DamageInfo) -> bool:
	if info.amount <= 0 or not targetable_by(info.team):
		return false
	if is_invulnerable():
		evaded.emit(info)
		return false
	current = maxi(1 if immortal else 0, current - info.amount)
	if iframes_after_hit > 0.0:
		_invulnerable_until = Game.time + iframes_after_hit
	damaged.emit(info)
	if current == 0:
		died.emit()
	return true


func heal(amount: int) -> bool:
	if amount <= 0 or not is_alive() or current >= max_hp:
		return false
	var before := current
	current = mini(max_hp, current + amount)
	healed.emit(current - before)
	return true
