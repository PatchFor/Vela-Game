extends SceneTree
## Saves the default content as .tres files under res://data so designers can tune it in the
## inspector (Unity: ConfigDefaults filling empty assets). Existing files are never overwritten.
##   godot --headless --path . -s res://tools/make_data.gd
## Items are saved first as their own files, so every monster / the player / the loot config
## points at the same item resources (stacking compares item identity).


func _initialize() -> void:
	var it := Defaults.items()
	for key in it:
		it[key] = _save(it[key], "res://data/items/%s.tres" % key)
	var monsters := Defaults.monsters(it)
	# Bats are summoned by the boss: save them first so the boss references the same file.
	var order := ["bat", "slime", "archer", "brute", "cultist", "dummy", "boss"]
	for key in order:
		_save(monsters[key], "res://data/monsters/%s.tres" % key)
	_save(Defaults.player(it), "res://data/player.tres")
	_save(Defaults.feel(), "res://data/feel.tres")
	_save(Defaults.loot_config(it), "res://data/loot_config.tres")
	quit()


func _save(res: Resource, path: String) -> Resource:
	if ResourceLoader.exists(path):
		print("keep  " + path)
		return load(path)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(path.get_base_dir()))
	var err := ResourceSaver.save(res, path)
	print(("saved " if err == OK else "FAILED ") + path)
	res.take_over_path(path)
	return res
