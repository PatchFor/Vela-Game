class_name InvSlot
extends Panel
## One inventory or equipment cell. Uses Godot's built-in drag & drop (_get_drag_data /
## _can_drop_data / _drop_data); right-click or double-click uses the item.

var ui: Node ## InventoryUI
var bag_index := -1 ## >= 0 for bag cells
var equip_slot := "" ## non-empty for equipment cells

var _icon: TextureRect
var _count: Label


func _init() -> void:
	custom_minimum_size = Vector2(52, 52)
	mouse_filter = Control.MOUSE_FILTER_STOP
	_icon = TextureRect.new()
	_icon.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_icon.set_anchors_preset(Control.PRESET_FULL_RECT)
	_icon.offset_left = 6
	_icon.offset_top = 6
	_icon.offset_right = -6
	_icon.offset_bottom = -6
	_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_icon)
	_count = Label.new()
	_count.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	_count.offset_left = -30
	_count.offset_top = -20
	_count.offset_right = -3
	_count.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_count.add_theme_font_size_override("font_size", 12)
	_count.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_count)


func show_item(it: ItemDef, count: int, placeholder := "") -> void:
	_icon.texture = it.icon if it else null
	_icon.modulate = it.equipment.tint if it and it.equipment else Color.WHITE
	_count.text = str(count) if it and count > 1 else ""
	var border := Game.loot_config.rarity_color(it.rarity) if it else Color(0.35, 0.35, 0.4)
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(0.08, 0.08, 0.1, 0.9)
	sb.border_color = border
	sb.set_border_width_all(2 if it else 1)
	add_theme_stylebox_override("panel", sb)
	tooltip_text = _describe(it) if it else placeholder


func _describe(it: ItemDef) -> String:
	var rarity: String = ItemDef.Rarity.keys()[it.rarity].capitalize()
	var line := "%s\n%s %s" % [it.display_name, rarity, ItemDef.Category.keys()[it.category].capitalize()]
	if it.heal > 0:
		line += "\nRestores %d HP (right-click)" % it.heal
	if it.is_equipment():
		line += "\nSlot: %s (right-click or drag to wear)" % it.equipment.slot
	return line


func _get_drag_data(_pos: Vector2) -> Variant:
	if _icon.texture == null:
		return null
	var preview := TextureRect.new()
	preview.texture = _icon.texture
	preview.modulate = _icon.modulate
	preview.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview.size = Vector2(44, 44)
	set_drag_preview(preview)
	return {"from": self}


func _can_drop_data(_pos: Vector2, data: Variant) -> bool:
	return data is Dictionary and data.has("from")


func _drop_data(_pos: Vector2, data: Variant) -> void:
	ui.handle_drop(data.from, self)


func _gui_input(event: InputEvent) -> void:
	var mb := event as InputEventMouseButton
	if mb and mb.pressed and (mb.button_index == MOUSE_BUTTON_RIGHT or mb.double_click):
		ui.use(self)
		accept_event()
