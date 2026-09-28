class_name Hud
extends Control
## Placeholder HUD drawn in code (Unity: CombatHUD). Ignores the mouse so it never eats clicks.
## HP / dash / weapon / gold, skill bar with cooldowns, one boss bar on top, target frame,
## combo counter, HUD messages, enemy HP bars, item labels, charge meter and the help panel.

const HELP := """WASD move · Space/Shift dash (near a glowing pad = jump across)
J / Left click attack · hold K charge · 1-4 / Right click skills
Q lock-on · E next target · click enemy = target
Click item / F pick up · walk over gold · I inventory
Tab weapon · wheel / +- zoom · O random outfit · F5 loot shower
G god mode · B go to boss · T respawn · R restart · H hide help"""

var font: Font


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	font = ThemeDB.fallback_font


func _process(_delta: float) -> void:
	queue_redraw()


func _draw() -> void:
	var p = Game.player
	var cam := Game.camera
	if p == null or cam == null or not is_instance_valid(p):
		return
	var size := get_viewport_rect().size
	_world_overlays(p, cam)
	_player_panel(p)
	_skill_bar(p, size)
	_boss_bar(p, size)
	_target_frame(p, size)
	_combo(size)
	_messages(size)
	if Game.show_help:
		_help(size)
	_text(Vector2(size.x - 12, size.y - 12), "%d FPS · kills %d%s" % [Engine.get_frames_per_second(), Game.kills, " · GOD" if Game.god_mode else ""], 13, Color(1, 1, 1, 0.6), HORIZONTAL_ALIGNMENT_RIGHT)


func _player_panel(p: Player) -> void:
	var x := 16.0
	var y := 16.0
	_bar(Rect2(x, y, 260, 18), p.health.ratio(), Color(0.85, 0.2, 0.25), "%d / %d" % [p.health.current, p.health.max_hp])
	_bar(Rect2(x, y + 22, 260, 6), p.dash_ratio(), Color(0.45, 0.8, 1))
	var w := p.weapon()
	_text(Vector2(x, y + 50), "%s   ·   %d gold" % [w.display_name, p.inventory.gold], 16, w.ui_color)
	if p.counter_active():
		_text(Vector2(x, y + 72), "COUNTER READY", 16, Game.feel.counter_color)


func _skill_bar(p: Player, size: Vector2) -> void:
	var n := p.cfg.skills.size()
	var slot := 64.0
	var gap := 8.0
	var x0 := size.x * 0.5 - (n * slot + (n - 1) * gap) * 0.5
	var y := size.y - slot - 20.0
	for i in n:
		var k: SkillConfig = p.cfg.skills[i]
		var r := Rect2(x0 + i * (slot + gap), y, slot, slot)
		draw_rect(r, Color(0, 0, 0, 0.55))
		var ready := p.skill_ratio(i)
		if ready < 1.0:
			draw_rect(Rect2(r.position + Vector2(0, r.size.y * ready), Vector2(r.size.x, r.size.y * (1.0 - ready))), Color(0, 0, 0, 0.55))
		draw_rect(r, k.color if ready >= 1.0 else Color(0.4, 0.4, 0.4), false, 2.0)
		var key := str(i + 1)
		if p.cfg.mouse_right == "skill_%d" % (i + 1):
			key += " / RMB"
		if p.cfg.mouse_left == "skill_%d" % (i + 1):
			key += " / LMB"
		_text(r.position + Vector2(4, 14), key, 12, Color(1, 1, 1, 0.8))
		_text(r.position + Vector2(slot * 0.5, slot - 8), k.display_name, 11, k.color, HORIZONTAL_ALIGNMENT_CENTER, slot - 4)


func _boss_bar(_p: Player, size: Vector2) -> void:
	for e in Game.enemies:
		if not is_instance_valid(e) or not e.is_boss() or not e.is_alive() or e.state == Enemy.State.IDLE:
			continue
		var w := minf(720.0, size.x - 80.0)
		var r := Rect2(size.x * 0.5 - w * 0.5, 34, w, 16)
		_text(Vector2(size.x * 0.5, 26), e.cfg.display_name + "  —  " + e.phase_name(), 18, Color(1, 0.9, 0.8), HORIZONTAL_ALIGNMENT_CENTER)
		_bar(r, e.health.ratio(), Color(0.75, 0.15, 0.2) if e.phase_index > 0 else Color(0.85, 0.35, 0.25))
		for ph in e.cfg.phases.slice(1):
			var mx: float = r.position.x + r.size.x * ph.starts_at
			draw_line(Vector2(mx, r.position.y - 3), Vector2(mx, r.end.y + 3), Color(1, 1, 1, 0.8), 2.0)
		return


func _target_frame(p: Player, size: Vector2) -> void:
	var t = p.target
	if t == null or not is_instance_valid(t) or t.is_boss():
		return
	var r := Rect2(size.x - 276, 16, 260, 14)
	_text(Vector2(r.position.x, r.position.y - 2), "◎ " + t.cfg.display_name, 15, Color(1, 0.6, 0.55))
	r.position.y += 6
	_bar(r, t.health.ratio(), Color(0.85, 0.3, 0.25), "%d / %d" % [t.health.current, t.health.max_hp])


func _combo(size: Vector2) -> void:
	if not Game.combo_active() or Game.combo < 2:
		return
	var pop := clampf(1.0 - (Game.real_time() - Game.combo_pop) / 0.15, 0.0, 1.0)
	var crit := Game.real_time() - Game.last_crit < 0.4
	var c := Game.feel.crit_color if crit else Color(1, 1, 1)
	_text(Vector2(size.x - 40, size.y * 0.42), "%d" % Game.combo, int(44 + 14 * pop), c, HORIZONTAL_ALIGNMENT_RIGHT)
	_text(Vector2(size.x - 40, size.y * 0.42 + 24), "HITS", 16, Color(1, 1, 1, 0.8), HORIZONTAL_ALIGNMENT_RIGHT)


func _messages(size: Vector2) -> void:
	var y := size.y * 0.24
	for m in Game.messages:
		var age: float = Game.real_time() - m.t
		var a := clampf((m.duration - age) / 0.3, 0.0, 1.0)
		var c: Color = m.color
		c.a = a
		_text(Vector2(size.x * 0.5, y), m.text, 22, c, HORIZONTAL_ALIGNMENT_CENTER)
		y += 30


func _help(size: Vector2) -> void:
	var lines := HELP.split("\n")
	var h := lines.size() * 18 + 16
	var r := Rect2(12, size.y - h - 110, 470, h)
	draw_rect(r, Color(0, 0, 0, 0.5))
	for i in lines.size():
		_text(r.position + Vector2(10, 22 + i * 18), lines[i], 13, Color(1, 1, 1, 0.85))


func _world_overlays(p: Player, cam: Camera3D) -> void:
	# Enemy HP bars (only once hurt), not for the boss (top bar).
	for e in Game.enemies:
		if not is_instance_valid(e) or not e.is_alive() or e.is_boss() or e.health.current >= e.health.max_hp:
			continue
		var wp: Vector3 = e.global_position + Vector3.UP * (e.cfg.world_height + e.cfg.hover + 0.35)
		if cam.is_position_behind(wp):
			continue
		var s := cam.unproject_position(wp)
		_bar(Rect2(s.x - 30, s.y, 60, 6), e.health.ratio(), Color(0.9, 0.3, 0.25))
	# Item labels: nearby or hovered.
	for w in Game.world_items:
		if not is_instance_valid(w) or not w.can_pick_up() or w.is_gold():
			continue
		var near: bool = w.global_position.distance_to(p.global_position) < 5.0
		if not near and not w.hovered:
			continue
		var s := cam.unproject_position(w.anchor() + Vector3.UP * 0.5)
		var c: Color = w.label_color()
		var txt: String = w.label()
		var width := font.get_string_size(txt, HORIZONTAL_ALIGNMENT_LEFT, -1, 14).x + 12
		draw_rect(Rect2(s.x - width * 0.5, s.y - 16, width, 20), Color(0, 0, 0, 0.75 if w.hovered else 0.5))
		if w.hovered:
			draw_rect(Rect2(s.x - width * 0.5, s.y - 16, width, 20), c, false, 1.5)
		_text(Vector2(s.x, s.y - 1), txt, 14, c, HORIZONTAL_ALIGNMENT_CENTER)
	# Charge meter over the player.
	if p.state == Player.State.CHARGING:
		var s := cam.unproject_position(p.global_position + Vector3.UP * 2.3)
		var full := p.charge >= 1.0
		_bar(Rect2(s.x - 30, s.y, 60, 6), p.charge, p.weapon().ui_color if full else Color(0.8, 0.8, 0.8))


func _bar(r: Rect2, ratio: float, color: Color, label := "") -> void:
	draw_rect(r.grow(1), Color(0, 0, 0, 0.7))
	draw_rect(Rect2(r.position, Vector2(r.size.x * clampf(ratio, 0.0, 1.0), r.size.y)), color)
	if label != "":
		_text(Vector2(r.position.x + r.size.x * 0.5, r.end.y - 3), label, int(r.size.y - 4), Color.WHITE, HORIZONTAL_ALIGNMENT_CENTER)


func _text(pos: Vector2, s: String, font_size: int, color: Color, align := HORIZONTAL_ALIGNMENT_LEFT, width := -1.0) -> void:
	var x := pos.x
	if align == HORIZONTAL_ALIGNMENT_CENTER:
		var w := font.get_string_size(s, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x
		x -= (w if width < 0 else minf(w, width)) * 0.5
	elif align == HORIZONTAL_ALIGNMENT_RIGHT:
		x -= font.get_string_size(s, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x
	draw_string_outline(font, Vector2(x, pos.y), s, HORIZONTAL_ALIGNMENT_LEFT, width, font_size, 4, Color(0, 0, 0, color.a * 0.85))
	draw_string(font, Vector2(x, pos.y), s, HORIZONTAL_ALIGNMENT_LEFT, width, font_size, color)
