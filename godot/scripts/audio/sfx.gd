extends Node
## Autoload "Sfx": `Sfx.play("hit_heavy", position)`. Placeholder sounds are synthesized at startup
## (same recipes as the Unity ProceduralSfx). Put real files at res://audio/<event>.wav/.ogg to
## replace one — they're picked up automatically.

const RATE := 22050
const VOICES := 20

var master_volume := 0.8
var _streams := {}
var _players: Array[AudioStreamPlayer] = []
var _next := 0
var _last := {}
var _noise := 22222
var _filter := 0.0


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	for i in VOICES:
		var p := AudioStreamPlayer.new()
		add_child(p)
		_players.append(p)


func play(event: String, world_pos = null, volume := 1.0) -> void:
	var now := Time.get_ticks_msec() / 1000.0
	if now - _last.get(event, -1.0) < 0.03:
		return
	_last[event] = now
	var stream := _stream(event)
	if stream == null:
		return
	var p := _players[_next]
	_next = (_next + 1) % _players.size()
	p.stream = stream
	p.volume_db = linear_to_db(master_volume * volume)
	p.pitch_scale = 1.0 + randf_range(-0.08, 0.08)
	p.play()


func _stream(event: String) -> AudioStream:
	if _streams.has(event):
		return _streams[event]
	var stream: AudioStream = null
	for ext in ["wav", "ogg"]:
		var path := "res://audio/%s.%s" % [event, ext]
		if ResourceLoader.exists(path):
			stream = load(path)
			break
	if stream == null:
		stream = _synth(event)
	_streams[event] = stream
	return stream


# ------------------------------------------------------------------ synthesis

func _synth(event: String) -> AudioStreamWAV:
	match event:
		"swing_light": return _make(0.13, func(t, d): return _whoosh(t, d, 5000.0, 1500.0) * 0.5)
		"swing_heavy": return _make(0.26, func(t, d): return _whoosh(t, d, 2200.0, 500.0) * 0.7)
		"hit_light": return _make(0.1, func(t, d): return _thump(t, 240.0, 120.0, 0.08) * 0.6 + _nz(t, 0.02) * 0.4)
		"hit_medium": return _make(0.14, func(t, d): return _thump(t, 180.0, 80.0, 0.11) * 0.75 + _nz(t, 0.03) * 0.45)
		"hit_heavy": return _make(0.22, func(t, d): return _thump(t, 130.0, 55.0, 0.18) * 0.9 + _nz(t, 0.05) * 0.5)
		"hit_finisher": return _make(0.32, func(t, d): return _thump(t, 110.0, 40.0, 0.26) + _nz(t, 0.08) * 0.6)
		"crit": return _make(0.35, func(t, d): return _tone(t, 1320.0, 0.3) * 0.35 + _tone(t, 1980.0, 0.22) * 0.25)
		"break": return _make(0.4, func(t, d): return _nz(t, 0.15) * 0.6 + _sweep(t, 900.0, 300.0, 0.35) * 0.4)
		"punish": return _make(0.18, func(t, d): return _tone(t, 880.0, 0.15) * 0.3 + _thump(t, 160.0, 70.0, 0.12) * 0.6)
		"player_hurt": return _make(0.2, func(t, d): return _square(t, lerpf(200.0, 90.0, t / d)) * _env(t, 0.18) * 0.35 + _nz(t, 0.05) * 0.4)
		"dash": return _make(0.12, func(t, d): return _whoosh(t, d, 7000.0, 3000.0) * 0.35)
		"jump": return _make(0.16, func(t, d): return _sweep(t, 300.0, 700.0, 0.15) * 0.35)
		"land": return _make(0.12, func(t, d): return _thump(t, 120.0, 60.0, 0.1) * 0.6 + _nz(t, 0.04) * 0.3)
		"perfect_dodge": return _make(0.5, func(t, d): return _sweep(t, 700.0, 1700.0, 0.45) * 0.3 + _tone(t, 2400.0, 0.4) * 0.12)
		"enemy_windup": return _make(0.22, func(t, d): return _square(t, lerpf(260.0, 420.0, t / d)) * _env(t, 0.2) * 0.12)
		"enemy_windup_heavy": return _make(0.4, func(t, d): return _square(t, lerpf(120.0, 220.0, t / d)) * _env(t, 0.38) * 0.18)
		"kill": return _make(0.3, func(t, d): return _nz(t, 0.06) * 0.5 + _sweep(t, 500.0, 120.0, 0.25) * 0.35)
		"boss_phase": return _make(1.2, func(t, d): return _nz(t, 1.1) * 0.35 + _sweep(t, 90.0, 50.0, 1.1) * 0.5)
		"skill": return _make(0.3, func(t, d): return (_tone(t, 660.0, 0.28) + _tone(t, 990.0, 0.28)) * 0.2)
		"charge_ready": return _make(0.25, func(t, d): return _tone(t, 1050.0, 0.22) * 0.35)
		"pickup": return _make(0.14, func(t, d): return _tone(t, 700.0 if t < 0.06 else 1050.0, 0.14) * 0.3)
		"pickup_rare": return _make(0.4, func(t, d): return _tone(t, 660.0 if t < 0.1 else (880.0 if t < 0.2 else 1320.0), 0.4) * 0.3)
		"gold": return _make(0.18, func(t, d): return (_tone(t, 2100.0, 0.16) + _tone(t, 2700.0, 0.12)) * 0.18)
		"inventory_full": return _make(0.18, func(t, d): return _square(t, 120.0) * _env(t, 0.16) * 0.25)
		"shoot": return _make(0.12, func(t, d): return _whoosh(t, d, 8000.0, 4000.0) * 0.3)
	return _make(0.1, func(t, d): return _tone(t, 800.0, 0.08) * 0.2)


func _make(duration: float, wave: Callable) -> AudioStreamWAV:
	var count := int(duration * RATE)
	var data := PackedByteArray()
	data.resize(count * 2)
	_noise = 22222
	_filter = 0.0
	for i in count:
		var t := float(i) / RATE
		var tail := clampf((duration - t) / 0.01, 0.0, 1.0)
		var v := clampf(float(wave.call(t, duration)) * tail, -1.0, 1.0)
		data.encode_s16(i * 2, int(v * 32767.0))
	var s := AudioStreamWAV.new()
	s.format = AudioStreamWAV.FORMAT_16_BITS
	s.mix_rate = RATE
	s.stereo = false
	s.data = data
	return s


func _rand() -> float:
	_noise ^= (_noise << 13) & 0xFFFFFFFF
	_noise ^= _noise >> 17
	_noise ^= (_noise << 5) & 0xFFFFFFFF
	return float(_noise & 0xFFFFFFFF) / 4294967295.0 * 2.0 - 1.0


func _env(t: float, length: float) -> float:
	return clampf(t / 0.003, 0.0, 1.0) * exp(-5.0 * t / maxf(0.001, length))


func _nz(t: float, length: float) -> float:
	return _rand() * _env(t, length)


func _tone(t: float, f: float, length: float) -> float:
	return sin(TAU * f * t) * _env(t, length)


func _square(t: float, f: float) -> float:
	return signf(sin(TAU * f * t))


func _thump(t: float, from: float, to: float, length: float) -> float:
	var f := lerpf(to, from, exp(-t * 30.0))
	return sin(TAU * f * t) * _env(t, length)


func _sweep(t: float, from: float, to: float, length: float) -> float:
	var k := clampf(t / maxf(0.001, length), 0.0, 1.0)
	return sin(TAU * lerpf(from, to, k) * t) * _env(t, length * 1.5) * (1.0 - k * 0.5)


func _whoosh(t: float, d: float, from: float, to: float) -> float:
	var k := t / d
	var a := clampf(TAU * lerpf(from, to, k) / RATE, 0.0, 1.0)
	_filter += a * (_rand() - _filter)
	return _filter * sin(PI * clampf(k, 0.0, 1.0)) * 2.5
