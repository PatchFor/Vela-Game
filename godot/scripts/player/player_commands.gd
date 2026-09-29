class_name PlayerCommands
extends RefCounted
## Everything the player asked for this frame, as plain data (Unity: PlayerCommands).
## Gameplay only reads this, so a bot, a replay or a network peer can drive the player the same way.

var move := Vector2.ZERO ## x = right, y = down the screen
var has_aim := false ## pointer is over the world
var aim_point := Vector3.ZERO ## pointer on the ground
var dash := false
var attack := false ## pressed this frame
var attack_held := false
var charge_held := false
var skill := -1 ## 0..3 pressed this frame
var lock_on := false
var next_target := false
var pick_up := false
var cycle_weapon := false
var click_item: Node = null ## WorldItem clicked
var click_enemy: Node = null ## Enemy clicked
