extends SceneTree
## Generates the paper-doll placeholder art (same designs as the Unity project's PlaceholderArt).
## Run:  godot --headless --path godot -s res://tools/make_art.gd
## Every doll layer is a 16x24 canvas, feet at the bottom center, 3 facings: down / up / side (faces right).

const OUT := "res://art/"
const W := 16
const H := 24

var OUTLINE := Color.html("#1a1423")
var SKIN := Color.html("#f4c7a1")
var SKIN_SHADE := Color.html("#d9a47f")
var TUNIC := Color.html("#7a8494")
var PANTS := Color.html("#3f4a5a")
var EYE := Color.html("#1a1423")
var LIGHT := Color.html("#e0e0e0")
var MID := Color.html("#b4b4b4")
var DARK := Color.html("#8a8a8a")
var STEEL := Color.html("#dfe6e9")
var STEEL_DARK := Color.html("#9aa5b1")
var WOOD := Color.html("#8b5a2b")
var GOLD := Color.html("#e0b04a")


class Canvas:
	var img: Image
	var w: int
	var h: int

	func _init(width: int, height: int) -> void:
		w = width
		h = height
		img = Image.create(width, height, false, Image.FORMAT_RGBA8)
		img.fill(Color(0, 0, 0, 0))

	# (0,0) is bottom-left, like the Unity painter.
	func px(x: int, y: int, c: Color) -> void:
		if x < 0 or y < 0 or x >= w or y >= h:
			return
		img.set_pixel(x, h - 1 - y, c)

	func get_px(x: int, y: int) -> Color:
		if x < 0 or y < 0 or x >= w or y >= h:
			return Color(0, 0, 0, 0)
		return img.get_pixel(x, h - 1 - y)

	func rect(x0: int, y0: int, x1: int, y1: int, c: Color) -> void:
		for y in range(y0, y1 + 1):
			for x in range(x0, x1 + 1):
				px(x, y, c)

	func ellipse(cx: int, cy: int, rx: int, ry: int, c: Color) -> void:
		for y in range(cy - ry, cy + ry + 1):
			for x in range(cx - rx, cx + rx + 1):
				var dx := (x - cx) / (rx + 0.5)
				var dy := (y - cy) / (ry + 0.5)
				if dx * dx + dy * dy <= 1.0:
					px(x, y, c)

	func clear_ellipse(cx: int, cy: int, rx: int, ry: int) -> void:
		ellipse(cx, cy, rx, ry, Color(0, 0, 0, 0))

	func line(x0: int, y0: int, x1: int, y1: int, c: Color, t: int = 1) -> void:
		var dx := absi(x1 - x0)
		var dy := -absi(y1 - y0)
		var sx := 1 if x0 < x1 else -1
		var sy := 1 if y0 < y1 else -1
		var err := dx + dy
		while true:
			rect(x0, y0, x0 + t - 1, y0 + t - 1, c)
			if x0 == x1 and y0 == y1:
				break
			var e2 := 2 * err
			if e2 >= dy:
				err += dy
				x0 += sx
			if e2 <= dx:
				err += dx
				y0 += sy

	func shade(darkest: float) -> void:
		for y in h:
			for x in w:
				var p := get_px(x, y)
				if p.a == 0.0:
					continue
				var t := lerpf(darkest, 1.0, float(y) / h * 0.7 + float(x) / w * 0.3)
				px(x, y, Color(p.r * t, p.g * t, p.b * t, p.a))

	func outline(c: Color) -> void:
		var copy := img.duplicate() as Image
		for y in h:
			for x in w:
				if _alpha(copy, x, y) > 0.0:
					continue
				if _alpha(copy, x - 1, y) > 0.0 or _alpha(copy, x + 1, y) > 0.0 or _alpha(copy, x, y - 1) > 0.0 or _alpha(copy, x, y + 1) > 0.0:
					px(x, y, c)

	func _alpha(source: Image, x: int, y: int) -> float:
		if x < 0 or y < 0 or x >= w or y >= h:
			return 0.0
		return source.get_pixel(x, h - 1 - y).a


func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT))
	_doll_set("Body", _body_down, _body_up, _body_side)
	_doll_set("Hair", _hair_down, _hair_up, _hair_side)
	_doll_set("Hood", _hood_down, _hood_up, _hood_side)
	_doll_set("Helm", _helm_down, _helm_up, _helm_side)
	_doll_set("Crown", _crown_down, _crown_up, _crown_side)
	_doll_set("Vest", _vest_down, _vest_up, _vest_side)
	_doll_set("Plate", _plate_down, _plate_up, _plate_side)
	_doll_set("Gloves", _gloves_down, _gloves_down, _gloves_side)
	_doll_set("Boots", _boots_down, _boots_up, _boots_side)
	_doll_set("Sword", _sword_down, _sword_up, _sword_side)
	_doll_set("Bow", _bow_down, _bow_up, _bow_side)
	_doll_set("Greatsword", _gs_down, _gs_up, _gs_side)
	_icon("Gold", _icon_gold)
	_icon("Potion", _icon_potion)
	_icon("Gel", _icon_gel)
	_icon("Bone", _icon_bone)
	_icon("Ember", _icon_ember)
	_water()
	print("make_art: done")
	quit()


func _doll_set(name: String, down: Callable, up: Callable, side: Callable) -> void:
	_doll(name + "_Down", down)
	_doll(name + "_Up", up)
	_doll(name + "_Side", side)


func _doll(name: String, paint: Callable) -> void:
	var c := Canvas.new(W, H)
	paint.call(c)
	c.shade(0.82)
	c.outline(OUTLINE)
	c.img.save_png(ProjectSettings.globalize_path(OUT + "Doll_" + name + ".png"))


func _icon(name: String, paint: Callable) -> void:
	var c := Canvas.new(12, 12)
	paint.call(c)
	c.shade(0.8)
	c.outline(OUTLINE)
	c.img.save_png(ProjectSettings.globalize_path(OUT + "Icon_" + name + ".png"))


func _water() -> void:
	var c := Canvas.new(32, 32)
	var rng := RandomNumberGenerator.new()
	rng.seed = 7
	for y in 32:
		for x in 32:
			var n := 1.0 + rng.randf_range(-0.05, 0.05)
			var base := Color.html("#3a7ca5")
			c.px(x, y, Color(base.r * n, base.g * n, base.b * n))
	for i in 10:
		var x := rng.randi_range(0, 27)
		var y := rng.randi_range(0, 31)
		c.rect(x, y, x + 3, y, Color.html("#7fb8d8"))
	c.img.save_png(ProjectSettings.globalize_path(OUT + "Water.png"))


# ------------------------------------------------------------------ body

func _legs_front(c: Canvas) -> void:
	c.rect(5, 1, 6, 5, PANTS)
	c.rect(9, 1, 10, 5, PANTS)
	c.rect(5, 0, 6, 0, EYE)
	c.rect(9, 0, 10, 0, EYE)


func _body_down(c: Canvas) -> void:
	_legs_front(c)
	c.rect(4, 6, 11, 13, TUNIC)
	c.rect(3, 7, 3, 12, SKIN)
	c.rect(12, 7, 12, 12, SKIN)
	c.rect(7, 14, 8, 14, SKIN_SHADE)
	c.ellipse(8, 18, 4, 4, SKIN)
	c.px(6, 18, EYE)
	c.px(10, 18, EYE)
	c.px(8, 16, SKIN_SHADE)


func _body_up(c: Canvas) -> void:
	_legs_front(c)
	c.rect(4, 6, 11, 13, TUNIC)
	c.rect(3, 7, 3, 12, SKIN)
	c.rect(12, 7, 12, 12, SKIN)
	c.rect(7, 14, 8, 14, SKIN_SHADE)
	c.ellipse(8, 18, 4, 4, SKIN)


func _body_side(c: Canvas) -> void:
	c.rect(6, 1, 7, 5, Color.html("#323b49"))
	c.rect(8, 1, 9, 5, PANTS)
	c.rect(6, 0, 7, 0, EYE)
	c.rect(8, 0, 10, 0, EYE)
	c.rect(5, 6, 10, 13, TUNIC)
	c.rect(8, 7, 9, 12, SKIN)
	c.rect(7, 14, 8, 14, SKIN_SHADE)
	c.ellipse(8, 18, 4, 4, SKIN)
	c.px(10, 18, EYE)
	c.px(12, 17, SKIN_SHADE)


func _hair_down(c: Canvas) -> void:
	c.ellipse(8, 20, 4, 3, LIGHT)
	c.rect(4, 16, 4, 20, LIGHT)
	c.rect(12, 16, 12, 20, LIGHT)
	c.rect(5, 19, 11, 19, MID)


func _hair_up(c: Canvas) -> void:
	c.ellipse(8, 18, 4, 4, LIGHT)
	c.rect(5, 14, 11, 15, LIGHT)


func _hair_side(c: Canvas) -> void:
	c.ellipse(7, 20, 4, 3, LIGHT)
	c.rect(4, 15, 6, 20, LIGHT)


# ------------------------------------------------------------------ head

func _hood_down(c: Canvas) -> void:
	c.ellipse(8, 19, 5, 5, LIGHT)
	c.rect(4, 13, 11, 14, MID)
	c.clear_ellipse(8, 17, 3, 3)


func _hood_up(c: Canvas) -> void:
	c.ellipse(8, 19, 5, 5, LIGHT)
	c.rect(4, 13, 11, 15, MID)
	c.rect(8, 16, 8, 22, MID)


func _hood_side(c: Canvas) -> void:
	c.ellipse(7, 19, 5, 5, LIGHT)
	c.rect(4, 13, 10, 14, MID)
	c.clear_ellipse(10, 17, 2, 3)


func _helm_down(c: Canvas) -> void:
	c.ellipse(8, 20, 5, 3, LIGHT)
	c.rect(3, 16, 4, 20, MID)
	c.rect(12, 16, 13, 20, MID)
	c.rect(8, 16, 8, 19, MID)
	c.rect(4, 20, 12, 20, DARK)


func _helm_up(c: Canvas) -> void:
	c.ellipse(8, 19, 5, 4, LIGHT)
	c.rect(4, 15, 12, 16, MID)


func _helm_side(c: Canvas) -> void:
	c.ellipse(8, 20, 5, 3, LIGHT)
	c.rect(3, 15, 6, 20, MID)
	c.rect(11, 16, 11, 19, MID)


func _crown_down(c: Canvas) -> void:
	c.rect(4, 21, 12, 22, LIGHT)
	c.px(4, 23, LIGHT)
	c.px(8, 23, LIGHT)
	c.px(12, 23, LIGHT)
	c.px(8, 21, Color.html("#ff4d6d"))


func _crown_up(c: Canvas) -> void:
	c.rect(4, 21, 12, 22, LIGHT)
	c.px(4, 23, LIGHT)
	c.px(8, 23, LIGHT)
	c.px(12, 23, LIGHT)


func _crown_side(c: Canvas) -> void:
	c.rect(5, 21, 11, 22, LIGHT)
	c.px(5, 23, LIGHT)
	c.px(8, 23, LIGHT)
	c.px(11, 23, LIGHT)


# ------------------------------------------------------------------ chest / hands / feet

func _vest_down(c: Canvas) -> void:
	c.rect(4, 6, 11, 13, LIGHT)
	c.rect(7, 8, 8, 13, MID)
	c.rect(4, 7, 11, 7, DARK)


func _vest_up(c: Canvas) -> void:
	c.rect(4, 6, 11, 13, LIGHT)
	c.rect(4, 7, 11, 7, DARK)


func _vest_side(c: Canvas) -> void:
	c.rect(5, 6, 10, 13, LIGHT)
	c.rect(5, 7, 10, 7, DARK)


func _plate_down(c: Canvas) -> void:
	c.rect(4, 6, 11, 14, LIGHT)
	c.ellipse(3, 12, 2, 2, MID)
	c.ellipse(12, 12, 2, 2, MID)
	c.rect(4, 7, 11, 7, DARK)


func _plate_up(c: Canvas) -> void:
	_plate_down(c)


func _plate_side(c: Canvas) -> void:
	c.rect(5, 6, 10, 14, LIGHT)
	c.ellipse(8, 12, 2, 2, MID)
	c.rect(5, 7, 10, 7, DARK)


func _gloves_down(c: Canvas) -> void:
	c.rect(2, 7, 3, 9, LIGHT)
	c.rect(12, 7, 13, 9, LIGHT)


func _gloves_side(c: Canvas) -> void:
	c.rect(8, 7, 10, 9, LIGHT)


func _boots_down(c: Canvas) -> void:
	c.rect(4, 0, 6, 2, LIGHT)
	c.rect(9, 0, 11, 2, LIGHT)
	c.rect(4, 2, 6, 2, MID)
	c.rect(9, 2, 11, 2, MID)


func _boots_up(c: Canvas) -> void:
	c.rect(4, 0, 6, 2, LIGHT)
	c.rect(9, 0, 11, 2, LIGHT)


func _boots_side(c: Canvas) -> void:
	c.rect(6, 0, 7, 2, MID)
	c.rect(8, 0, 11, 2, LIGHT)


# ------------------------------------------------------------------ weapons

func _sword_down(c: Canvas) -> void:
	c.rect(2, 9, 2, 17, STEEL)
	c.px(2, 18, STEEL)
	c.rect(1, 8, 3, 8, GOLD)
	c.rect(2, 6, 2, 7, WOOD)


func _sword_up(c: Canvas) -> void:
	c.rect(13, 9, 13, 17, STEEL_DARK)
	c.rect(12, 8, 14, 8, GOLD)


func _sword_side(c: Canvas) -> void:
	c.rect(11, 8, 14, 8, STEEL)
	c.px(15, 8, STEEL)
	c.rect(10, 7, 10, 9, GOLD)


func _bow_down(c: Canvas) -> void:
	for y in range(5, 18):
		var bend := roundi(sin((y - 5) / 12.0 * PI) * 2.0)
		c.px(4 - bend, y, WOOD)
	c.rect(4, 5, 4, 17, Color.html("#d8d0c0"))


func _bow_up(c: Canvas) -> void:
	for y in range(5, 18):
		var bend := roundi(sin((y - 5) / 12.0 * PI) * 2.0)
		c.px(12 + bend, y, WOOD)


func _bow_side(c: Canvas) -> void:
	for y in range(3, 16):
		var bend := roundi(sin((y - 3) / 12.0 * PI) * 2.0)
		c.px(11 + bend, y, WOOD)
	c.rect(11, 3, 11, 15, Color.html("#d8d0c0"))


func _gs_down(c: Canvas) -> void:
	c.rect(0, 8, 2, 21, STEEL)
	c.rect(1, 9, 1, 20, Color.WHITE)
	c.rect(0, 7, 3, 7, GOLD)
	c.rect(1, 5, 1, 6, WOOD)


func _gs_up(c: Canvas) -> void:
	c.line(3, 5, 12, 21, STEEL_DARK, 2)
	c.rect(4, 8, 6, 8, GOLD)


func _gs_side(c: Canvas) -> void:
	c.line(10, 8, 14, 22, STEEL, 2)
	c.rect(9, 7, 11, 7, GOLD)


# ------------------------------------------------------------------ icons

func _icon_gold(c: Canvas) -> void:
	c.ellipse(4, 3, 3, 2, Color.html("#e0a82e"))
	c.ellipse(7, 4, 3, 2, Color.html("#f5c542"))
	c.ellipse(6, 7, 3, 2, Color.html("#ffd966"))


func _icon_potion(c: Canvas) -> void:
	c.ellipse(6, 4, 4, 4, Color.html("#e63946"))
	c.rect(5, 8, 7, 10, Color.html("#c0c0c0"))
	c.rect(5, 11, 7, 11, WOOD)


func _icon_gel(c: Canvas) -> void:
	c.ellipse(6, 4, 4, 3, Color.html("#6ab04c"))
	c.px(4, 5, Color.html("#badc58"))


func _icon_bone(c: Canvas) -> void:
	c.line(2, 2, 9, 9, Color.html("#e8e2d0"), 2)


func _icon_ember(c: Canvas) -> void:
	c.ellipse(6, 5, 3, 4, Color.html("#ff6b35"))
	c.ellipse(6, 5, 1, 2, Color.html("#ffd166"))
