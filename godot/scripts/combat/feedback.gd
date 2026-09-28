class_name Feedback
extends RefCounted
## Sells a landed hit (Unity: DamageFeedback). Called from the victim's Health.damaged handler,
## so any source of damage (melee, projectile, AoE, network) gets the same numbers, flash,
## sparks, hit-stop, shake and sound — all scaled by the hit weight's ImpactProfile.

const HIT_SOUNDS := ["hit_light", "hit_medium", "hit_heavy", "hit_finisher"]


static func enemy_hit(victim: Node3D, rig: SpriteRig, info: DamageInfo) -> void:
	var feel := Game.feel
	var prof := feel.profile(info.weight)
	var color := prof.number_color
	var label := ""
	var scale := prof.number_scale
	if info.crit:
		color = feel.crit_color
		scale *= feel.crit_number_scale
		label = "CRIT"
	if info.punish:
		color = feel.punish_color
		label = "PUNISH"
	if info.counter:
		color = feel.counter_color
		label = "COUNTER"
	Fx.number(info.hit_point + Vector3.UP * 0.4, str(info.amount), color, scale, info.crit, label)

	rig.flash(Color.WHITE, feel.flash_time)
	rig.punch(Vector2(1.0 + prof.squash, 1.0 - prof.squash))
	rig.tremble(prof.tremble_amount, prof.tremble_time)
	Fx.sparks(info.hit_point, info.direction, Color(1, 0.95, 0.75), int(8 * prof.spark_mult))
	if prof.impact_ring:
		Fx.ring(victim.global_position + Vector3.UP * 0.05, 0.3, prof.ring_radius, 0.25, Color(1, 1, 1, 0.8))
	if prof.dust > 0:
		Fx.dust(victim.global_position, prof.dust)

	var stop := (info.hit_stop * prof.hit_stop_mult + prof.hit_stop_bonus) * feel.hit_stop_scale
	var shake := info.shake + prof.extra_shake
	var zoom := prof.zoom_punch
	if info.crit:
		stop += feel.crit_hit_stop_bonus
		shake += feel.crit_extra_shake
		zoom += feel.crit_zoom
		Fx.crit_burst(info.hit_point, feel.crit_color)
		Game.slow_motion(feel.crit_slow_time, feel.crit_slow_scale)
	Game.hit_stop(stop, victim, info.source)
	Game.shake(shake)
	Game.punch(zoom)
	Sfx.play(HIT_SOUNDS[clampi(info.weight - 1, 0, 3)], info.hit_point)
	if info.crit:
		Sfx.play("crit", info.hit_point, 0.8)
	if info.punish:
		Sfx.play("punish", info.hit_point)
	Game.register_hit(info.crit)


static func player_hurt(player: Node3D, rig: SpriteRig, info: DamageInfo) -> void:
	var feel := Game.feel
	Fx.number(player.global_position + Vector3.UP * 2.0, str(info.amount), feel.taken_color, 1.1)
	rig.flash(Color.WHITE, feel.flash_time)
	rig.hurt_tint(feel.hurt_tint, feel.hurt_tint_time)
	rig.punch(Vector2(1.25, 0.8))
	Fx.sparks(player.global_position + Vector3.UP, info.direction, Color(1, 0.4, 0.35), 10)
	Game.hit_stop(feel.hurt_hit_stop * feel.hit_stop_scale, player, info.source)
	Game.shake(feel.hurt_shake)
	Game.punch(feel.hurt_zoom)
	Sfx.play("player_hurt", player.global_position)
	Game.stats.player_hits += 1
