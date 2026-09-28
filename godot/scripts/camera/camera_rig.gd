class_name CameraRig
extends Camera3D
## Locked 3/4 camera (Unity: CombatCameraRig + CameraShake): fixed pitch and yaw, follows the
## player with a small look-ahead toward the aim, mouse-wheel / +/- zoom, trauma shake and zoom punch.

var pitch := 50.0 ## degrees down from horizontal
var min_distance := 12.0
var max_distance := 36.0
var distance := 22.0 ## current zoom
var zoom_step := 2.0
var zoom_smooth := 10.0
var follow_smooth := 8.0
var look_ahead := 1.5 ## metres toward the aim / movement

var _zoom_goal := 22.0
var _focus := Vector3.ZERO
var _noise := FastNoiseLite.new()


func _ready() -> void:
	fov = 30.0
	near = 0.3
	far = 200.0
	current = true
	Game.camera = self
	_noise.frequency = 2.5
	_zoom_goal = distance


func snap_to(pos: Vector3) -> void:
	_focus = pos
	_place(0.0)


func _unhandled_input(event: InputEvent) -> void:
	var mb := event as InputEventMouseButton
	if mb and mb.pressed:
		if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
			_zoom_goal -= zoom_step
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_zoom_goal += zoom_step
	_zoom_goal = clampf(_zoom_goal, min_distance, max_distance)


func _process(delta: float) -> void:
	var real_dt := minf(delta / maxf(Engine.time_scale, 0.001), 0.1)
	if Input.is_action_just_pressed("zoom_in"):
		_zoom_goal = clampf(_zoom_goal - zoom_step, min_distance, max_distance)
	if Input.is_action_just_pressed("zoom_out"):
		_zoom_goal = clampf(_zoom_goal + zoom_step, min_distance, max_distance)
	distance = lerpf(distance, _zoom_goal, 1.0 - exp(-zoom_smooth * real_dt))
	var p = Game.player
	if p and is_instance_valid(p):
		var goal: Vector3 = p.global_position
		if p.has_method("facing_dir"):
			goal += p.facing_dir() * look_ahead
		_focus = _focus.lerp(goal, 1.0 - exp(-follow_smooth * real_dt))
	_place(real_dt)


func _place(_dt: float) -> void:
	var d := distance * (1.0 - Game.zoom_punch)
	var r := deg_to_rad(pitch)
	var offset := Vector3(0, sin(r), cos(r)) * d
	var shake := Vector3.ZERO
	var t := Game.trauma * Game.trauma
	if t > 0.0:
		var now := Game.real_time() * 30.0
		shake = Vector3(_noise.get_noise_2d(now, 0.0), _noise.get_noise_2d(0.0, now), 0.0) * t * Game.feel.shake_max_offset
	global_position = _focus + offset
	global_rotation = Vector3(-r, 0, 0)
	global_position += global_transform.basis * shake
