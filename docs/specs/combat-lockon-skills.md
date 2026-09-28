# Spec: Combat, skills, lock-on

**Owner:** combat-designer · **Status:** prototype in place

## Goal
Basic attacks feel snappy, heavy attacks feel heavy, skills are big moments with cooldowns.
The keyboard can do everything; the mouse is an optional faster way to aim, pick targets, and
fire bound actions.

## Controls
| Input | Default |
|---|---|
| J | Basic attack (weapon combo) |
| Hold K, release when full | Charged attack |
| 1–4 | Skills |
| Left mouse | Bindable (default: basic attack, aimed at the pointer) |
| Right mouse | Bindable (default: charged attack) |
| Q / middle mouse | Lock on (hovered monster, else best in front) / release |
| E | Next target |
| Click a monster | Lock it (and the click still attacks) |
| Tab | Next weapon |

Mouse bindings are changed in the inventory window (I): Basic, Charged, Skill 1–4, or Nothing.

## Aim rules (in priority order)
1. Locked target → attack toward it.
2. Attack came from the mouse → toward the pointer.
3. Attack came from the keyboard → facing direction.
4. Melee only, not locked: soft aim assist snaps to the nearest monster within `meleeAimAssistAngle`.

## Hit feel
Each attack has a `hitWeight` (Light / Medium / Heavy / Finisher). CombatFeel.asset maps each
weight to hit-stop, shake, zoom punch, squash, tremble, knockback, sparks, rings and number size.
Crits add freeze + slow-mo + star burst + gold "CRITICAL" number. Poise break shows "BREAK!".

## Edge cases
| Case | Expected | Test |
|---|---|---|
| Skill pressed while on cooldown | Nothing, HUD shows remaining seconds once | PlayMode |
| Skill pressed during attack recovery | Cancels recovery into the skill | PlayMode |
| Skill pressed during windup/active | Ignored | PlayMode |
| Click on UI (inventory open) | Never attacks | PlayMode |
| Click on an item | Picks up, never attacks | PlayMode |
| Locked target dies | Lock released | PlayMode |
| Locked target further than lockOnBreakRange | Lock released | PlayMode |
| Q with nothing in range | Nothing happens | PlayMode |
| E with one monster in range | Stays on it | PlayMode |
| Hit during super-armor attack | Damage taken, no stagger/knockback | PlayMode |
| Combo input after comboResetTime | Restarts at step 1 | PlayMode |

## Tuning
Weapons: `Assets/Config/Weapons/*` · Skills: `Assets/Config/Skills/*` · Feel: `CombatFeel.asset` ·
Lock-on: `Player.asset` (lockOnRange 14, lockOnBreakRange 20, hoverRadiusPixels 60).

## Skills (defaults)
| Skill | Type | Cooldown | Weight |
|---|---|---|---|
| Spin Slash | 360° melee, super armor | 4 s | Heavy |
| Piercing Shot | projectile, pierces 5 | 3 s | Heavy |
| Ground Slam | 360° r=4, breaks armor | 8 s | Finisher |
| Fan of Knives | 7 projectiles, 70° fan | 5 s | Medium |
