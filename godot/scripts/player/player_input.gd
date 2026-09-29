class_name PlayerInput
extends Node
## Reads keyboard + mouse into PlayerCommands (Unity: PlayerInputReader). Mouse buttons arrive
## through _unhandled_input, so clicks on UI (inventory, buttons) never become attacks.

var config: PlayerConfig
var hovered_item: Node = null ## set by Player each frame
var hovered_enemy: Node = null

var _pending := PlayerCommands.new()
var _left_held := false
var _right_held := false


func _unhandled_input(event: InputEvent) -> void:
	var mb := event as InputEventMouseButton
	if mb == null:
		return
	if mb.button_index == MOUSE_BUTTON_LEFT:
		_left_held = mb.pressed
		if mb.pressed:
			_click(config.mouse_left)
	elif mb.button_index == MOUSE_BUTTON_RIGHT:
		_right_held = mb.pressed
		if mb.pressed:
			_click(config.mouse_right)


func _click(binding: String) -> void:
	if hovered_item and is_instance_valid(hovered_item):
		_pending.click_item = hovered_item
		return
	if hovered_enemy and is_instance_valid(hovered_enemy):
		_pending.click_enemy = hovered_enemy
	_press(binding)


func _press(binding: String) -> void:
	match binding:
		"attack": _pending.attack = true
		"skill_1": _pending.skill = 0
		"skill_2": _pending.skill = 1
		"skill_3": _pending.skill = 2
		"skill_4": _pending.skill = 3


func _held(binding: String) -> bool:
	return (_left_held and config.mouse_left == binding) or (_right_held and config.mouse_right == binding)


## Builds this frame's commands and clears the one-shot presses.
func read() -> PlayerCommands:
	var c := _pending
	_pending = PlayerCommands.new()
	c.move = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	c.dash = c.dash or Input.is_action_just_pressed("dash")
	c.attack = c.attack or Input.is_action_just_pressed("attack")
	c.attack_held = Input.is_action_pressed("attack") or _held("attack")
	c.charge_held = Input.is_action_pressed("charge") or _held("charge")
	for i in 4:
		if Input.is_action_just_pressed("skill_%d" % (i + 1)):
			c.skill = i
	c.lock_on = Input.is_action_just_pressed("lock_on")
	c.next_target = Input.is_action_just_pressed("next_target")
	c.pick_up = Input.is_action_just_pressed("pick_up")
	c.cycle_weapon = Input.is_action_just_pressed("cycle_weapon")
	var p = pointer_ground()
	if p != null:
		c.has_aim = true
		c.aim_point = p
	return c


## Mouse position projected onto the ground plane (null when there is no camera / window).
func pointer_ground():
	var vp := get_viewport()
	var cam := vp.get_camera_3d() if vp else null
	if cam == null or DisplayServer.get_name() == "headless":
		return null
	var mouse := vp.get_mouse_position()
	var from := cam.project_ray_origin(mouse)
	var dir := cam.project_ray_normal(mouse)
	if absf(dir.y) < 0.0001:
		return null
	var t := -from.y / dir.y
	return from + dir * t if t > 0.0 else null
