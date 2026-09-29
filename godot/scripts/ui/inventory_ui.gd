class_name InventoryUI
extends Control
## Inventory window (Unity: InventoryUI, but with real Controls instead of IMGUI).
## Drag to move / swap, drag onto an equipment cell to wear, drag out of the window to drop on the
## ground, right-click to use. Because these are Controls, clicks here never reach the game.

const EQUIP_SLOTS := ["head", "chest", "hands", "feet"]

var player: Player
var _bag: Array[InvSlot] = []
var _equip := {}
var _window: PanelContainer
var _drop_zone: Control
var _gold: Label


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Full-screen catcher, active only while dragging: dropping here throws the item on the ground.
	_drop_zone = DropZone.new()
	_drop_zone.ui = self
	_drop_zone.set_anchors_preset(Control.PRESET_FULL_RECT)
	_drop_zone.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_drop_zone)

	_window = PanelContainer.new()
	_window.position = Vector2(get_viewport_rect().size.x - 520, 110)
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(0.05, 0.05, 0.07, 0.92)
	sb.border_color = Color(0.5, 0.45, 0.35)
	sb.set_border_width_all(2)
	sb.set_content_margin_all(12)
	_window.add_theme_stylebox_override("panel", sb)
	add_child(_window)

	var v := VBoxContainer.new()
	_window.add_child(v)
	var title := Label.new()
	title.text = "Inventory  (I)"
	v.add_child(title)
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 14)
	v.add_child(h)

	var eq := VBoxContainer.new()
	h.add_child(eq)
	for s in EQUIP_SLOTS:
		var cell := InvSlot.new()
		cell.ui = self
		cell.equip_slot = s
		eq.add_child(cell)
		_equip[s] = cell

	var grid := GridContainer.new()
	grid.columns = 6
	h.add_child(grid)
	for i in player.inventory.capacity():
		var cell := InvSlot.new()
		cell.ui = self
		cell.bag_index = i
		grid.add_child(cell)
		_bag.append(cell)

	_gold = Label.new()
	v.add_child(_gold)
	var hint := Label.new()
	hint.text = "Drag to move · drag onto armour to wear · drag outside to drop · right-click to use"
	hint.add_theme_font_size_override("font_size", 11)
	hint.modulate = Color(1, 1, 1, 0.6)
	v.add_child(hint)

	player.inventory.changed.connect(refresh)
	player.equipment_changed.connect(refresh)
	_window.visible = false
	refresh()


func _process(_delta: float) -> void:
	if Input.is_action_just_pressed("inventory"):
		_window.visible = not _window.visible
		Sfx.play("pickup", null, 0.4)


func is_open() -> bool:
	return _window.visible


func _notification(what: int) -> void:
	if _drop_zone == null:
		return
	if what == NOTIFICATION_DRAG_BEGIN:
		_drop_zone.mouse_filter = Control.MOUSE_FILTER_STOP
	elif what == NOTIFICATION_DRAG_END:
		_drop_zone.mouse_filter = Control.MOUSE_FILTER_IGNORE


func refresh() -> void:
	for i in _bag.size():
		var s = player.inventory.slots[i]
		_bag[i].show_item(s.item if s else null, s.count if s else 0)
	for slot in EQUIP_SLOTS:
		_equip[slot].show_item(player.equipment.get(slot), 1, slot.capitalize())
	_gold.text = "Gold: %d" % player.inventory.gold


func use(cell: InvSlot) -> void:
	if cell.bag_index >= 0:
		player.use_slot(cell.bag_index)
	elif player.equipment.has(cell.equip_slot):
		# Right-click worn gear: take it off into the first free slot.
		var free := player.inventory.slots.find(null)
		if free >= 0:
			player.inventory.place_at(free, player.unequip(cell.equip_slot), 1)
		else:
			Game.message("Inventory full", Color(1, 0.5, 0.4))


func handle_drop(from: InvSlot, to: Control) -> void:
	var inv := player.inventory
	if to == _drop_zone:
		_drop_on_ground(from)
		return
	var target := to as InvSlot
	if target == null or target == from:
		return
	if from.bag_index >= 0 and target.bag_index >= 0:
		inv.move(from.bag_index, target.bag_index)
	elif from.bag_index >= 0 and target.equip_slot != "":
		var s = inv.slots[from.bag_index]
		if s and s.item.is_equipment() and s.item.equipment.slot == target.equip_slot:
			player.use_slot(from.bag_index)
		else:
			Game.message("Doesn't go there", Color(1, 0.6, 0.5), 1.0)
	elif from.equip_slot != "" and target.bag_index >= 0:
		var s = inv.slots[target.bag_index]
		if s == null:
			inv.place_at(target.bag_index, player.unequip(from.equip_slot), 1)
		elif s.item.is_equipment() and s.item.equipment.slot == from.equip_slot:
			player.use_slot(target.bag_index)
	Sfx.play("pickup", null, 0.5)


func _drop_on_ground(from: InvSlot) -> void:
	var item: ItemDef
	var count := 1
	if from.bag_index >= 0:
		var taken := player.inventory.remove_at(from.bag_index)
		if taken.is_empty():
			return
		item = taken.item
		count = taken.count
	else:
		item = player.unequip(from.equip_slot)
	if item:
		Loot.drop_stack(Game.level, item, count, player.global_position)
		Game.message("Dropped " + item.display_name, Color(0.8, 0.8, 0.8), 1.0)


class DropZone extends Control:
	var ui: InventoryUI

	func _can_drop_data(_pos: Vector2, data: Variant) -> bool:
		return data is Dictionary and data.has("from")

	func _drop_data(_pos: Vector2, data: Variant) -> void:
		ui.handle_drop(data.from, self)
