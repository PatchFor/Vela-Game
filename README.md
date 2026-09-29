# Vela: 2.5D Combat Prototype (Unity 6 LTS)

A playable prototype for testing combat look and feel in the style of *Alabaster Dawn*.
Characters are flat pixel sprites standing in a 3D world, seen through a locked 3/4 camera.
It includes:

- Three weapons with different styles: a fast sword combo, a bow, and a slow, heavy
  greatsword. Each weapon also has a charged attack.
- Five monster types, each with its own behavior.
- A boss with two phases and one HP bar across the top of the screen.
- Floating damage numbers, hit-stop, screen shake, slash arcs, hit sparks, and dash afterimages.
- Telegraphed enemy attacks, and scenery that turns see-through when it hides someone.

> **Godot comparison port:** the same prototype also runs in Godot 4.3 under [`godot/`](godot/README.md).
> For engine advice (online, 2.5D, rendering/physics, and what to invest in) see
> [`docs/engine-comparison.md`](docs/engine-comparison.md).

Everything you'd want to tune lives in **config assets** (ScriptableObjects) that you edit in
the Inspector. Every sprite is a placeholder PNG you can replace.

---

## 1. Open and build

1. Install **Unity 6.3 LTS** from Unity Hub. `ProjectVersion.txt` pins `6000.3.24f1`, but any
   6000.3.x patch works. Coming from 6000.0: open the project in 6.3 and let Unity upgrade it,
   then accept the package updates it offers (Input System, Test Framework).
2. In Unity Hub, choose **Add → Add project from disk** and pick this folder. The first import
   takes a few minutes.
3. Wait for compilation to finish (no red errors in the Console). If Unity asks to enable
   the new Input System backend and restart, click **Yes**. The controls work with either
   backend.
4. Press **Play**. On the first Play the combat scene is built automatically, then Play
   starts again. This generates:
   - `Assets/Art/Placeholder/*.png`: pixel-art sprites and tiles
   - `Assets/Config/**`: every config asset (weapons, monsters, boss, camera, feel)
   - `Assets/Materials/*`: world and effect materials
   - `Assets/Scenes/CombatPrototype.unity`: the level, added to Build Settings

   After that, Play always starts `CombatPrototype.unity`, whichever scene is open. Turn this
   off with **Vela → Always Play Combat Scene**. You can also rebuild by hand with
   **Vela → Build Combat Prototype Scene**.

**If there's no Vela menu, or nothing happens on Play:**

- Open **Window → General → Console** and fix or report the first red error. A compile error
  stops every script and the menu from loading.
- If a monster or the player can't be seen but the HUD shows, run
  **Vela → Regenerate Placeholder Art**.

Rebuilding the scene **keeps your config assets and any sprites you swapped in**. Two other
menu items help:

- **Vela → Reset Configs To Defaults** puts every config back to its starting values.
- **Vela → Regenerate Placeholder Art** rewrites the placeholder PNGs.

## 2. Controls

Keyboard plays; the mouse points (aim, pick targets, pick up items) and runs whatever action
you bind to its buttons.

| Input | Action |
| --- | --- |
| `WASD` | Move (4-way facing art) |
| `Space` / `Shift` | Dash with i-frames. **Near a marked river edge: jump across** |
| `J` | Basic attack (weapon combo) |
| Hold `K`, release when full | Charged attack |
| `1` `2` `3` `4` | Skills: Spin Slash, Piercing Shot, Ground Slam, Fan of Knives |
| Left mouse | Bound action (default: basic attack toward the pointer) |
| Right mouse | Bound action (default: charged attack) |
| `Q` / middle mouse | Lock on: the hovered monster, else the best one in front. Press again to release |
| `E` | Next target |
| Hover a monster | Red outline: that's what a click or `Q` will target |
| Click a monster | Lock it (the click also attacks) |
| Click an item / `F` | Pick up. If it's out of reach, you walk there first. Gold: walk over it |
| `I` | Inventory: drag to move/equip, drag outside to drop, right-click to use/equip. Mouse bindings are set here |
| `Tab` | Next weapon |
| Mouse wheel, `+` / `-` | Zoom |
| `O` | Random outfit (test the paper doll) |
| `F5` | Drop test loot around you (test full inventory, rarity looks) |
| `F6` | Sword animation A/B: C full frames + smear → B key poses → A old single frame |
| `F7` | Swap the sword look (long / short / broad). Same animation, works mid-swing |
| `F3` | Animation debug: anchor dots, windup/active/recovery bar with frame counts, hit arc |
| `T` / `B` / `G` / `R` | Respawn monsters / go to the boss / god mode / restart |
| `H` / `F1` | Show or hide the help text |

**Combat rewards:**
- **Perfect dodge:** dash through an attack right as it lands. The world slows down, the
  dash is ready again, and you get a short **counter** window (bonus damage, guaranteed crits).
- **Hit-confirm:** once a hit connects, you can cancel into the next hit, a dash or a skill early.
- **Punish:** hits on a staggered (BREAK!) enemy deal bonus damage.

**Sound:** every event has a generated placeholder sound. Drop real clips into
`Assets/Config/Audio/SfxLibrary.asset` to replace them.

**Online readiness:** see `docs/architecture/online-readiness.md`. To preview how combat
would feel in multiplayer, set `CombatFeel.asset → hitStopMode = LocalVisual`.

Plans, specs, and the agent team: see `docs/plan/week-1.md`, `docs/specs/`, `docs/art-pipeline.md`,
`CLAUDE.md` and `.claude/agents/`.

## 3. The level

The map is laid out south to north. You spawn at the south end, and a dirt footpath leads
north to the boss.

| Area | What's there | What it shows |
| --- | --- | --- |
| Spawn | 3 **training dummies** in a fenced yard, and a tall ruin wall right in front of the camera | Damage numbers and crits on a target that can't die. Walk north of the wall to watch it turn see-through |
| South hub | 3 **slimes** among pillars | Basic melee lunges |
| West grove | 3 **bats** among trees | Swarmers. The trees fade when you walk behind them |
| East ruins | 2 **skeleton archers** behind low cover | Ranged kiting and fan volleys |
| Crossroads | 1 **stone brute** in a walled courtyard | Heavy telegraphs, ground slam, and a charge that stuns itself on walls |
| North-east rocks | 2 **ember cultists** | Fire circles on the ground and a radial fireball nova |
| North-west thicket | One of each monster type | A mixed fight |
| Boss arena | **The Hollow Warden**, inside a ring of pillars | Two-phase boss |

Walls, pillars, rocks, trees and the border walls all turn see-through when they block your
view of the player or a nearby monster. Low fences and cover stay solid.

## 4. Tuning: where every number lives

Select any asset under `Assets/Config` and edit it in the Inspector. Every field has a tooltip.
Changes made **during Play mode are kept**, because they're assets and not scene objects, so
you can tune live.

### Weapons: `Assets/Config/Weapons/{Sword,Bow,Greatsword}.asset`

| Field | Meaning |
| --- | --- |
| `critChance`, `critMultiplier` | Crit numbers are drawn bigger and yellow with a "!" |
| `combo[]` | One entry per click. Add or remove entries to change the combo length |
| `comboResetTime`, `inputBufferTime` | How forgiving combo timing is |
| `repeatWhileHeld` | Hold to keep attacking (turned on for the bow) |
| `canDashCancel` | Whether a dash can cancel attack recovery (off for the greatsword, so heavy swings are a commitment) |
| `chargeTime`, `chargedAttack` | The right-click attack |

Each **attack step** has these fields:

- `kind`: `MeleeArc` or `Projectile`
- `damage`, `damageVariance`
- `windup`, `active`, `recovery`: timing in seconds
- `range`, `arcDegrees`, `lungeDistance`: melee
- `projectileCount`, `spreadDegrees`, `projectileSpeed`, `pierce`: projectiles
- `knockback`, `stagger` (poise damage), `hitStop`, `cameraShake`
- `moveSpeedMultiplier` while attacking, and `superArmor`
- `slashColor`, `slashWidth`, `slashDuration`, `reverseSwing`: the slash effect

Default styles:

- **Sword:** 3-hit combo (slash, backslash, thrust finisher). The charged attack is a
  5.5 m dash strike.
- **Bow:** hold to fire repeatedly, and you can walk while shooting. The charged attack is 3
  piercing arrows.
- **Greatsword:** 2-hit combo with super armor and big hit-stop, and no dash cancel. The
  charged attack is a 360° whirlwind.

### Monsters: `Assets/Config/Monsters/*.asset`

Each monster config holds:

- **Body:** `maxHealth`, `colliderRadius`, `colliderHeight`
- **Hit reactions:**
  - `poise`: stagger damage needed to interrupt it. 0 means every hit interrupts it; a high
    value means it's hard to stagger.
  - `staggerDuration`
  - `knockbackResistance`: 0 takes full knockback, 1 can't be moved.
- `groupAlertRadius`: when it notices you, monsters within this radius wake up too.
- `behaviour.movement`:
  - `Chase`: walks at you.
  - `KeepDistance`: backs off and strafes at `preferredDistance`.
  - `Circle`: orbits you.
  - `Erratic`: darts along random angles.
  - `Stationary`: never moves.
- `behaviour.moveSpeed`, `detectionRange`, `leashRange`, `preferredDistance`, `attackDecisionDelay`
- `behaviour.attacks[]`: each attack has:
  - `kind`:
    - `MeleeArc`
    - `Lunge`
    - `Charge` (stuns itself when it hits a wall)
    - `Projectile`
    - `GroundAoE` (circles under the player)
    - `SelfAoE`
    - `RadialBurst`
    - `Summon`
  - `weight`: chance relative to its other attacks
  - `minRange` / `maxRange`: when it can use the attack
  - `cooldown`
  - `windup`: the telegraph, and the main difficulty dial
  - `damage`, `knockback`, and shape (`radius`, `arcDegrees`, `dashSpeed`)
  - Projectiles: `projectileCount`, `spreadDegrees`, `volleys`
  - `showTelegraph`, and `color` (telegraph and effect color)

| Monster | Style | Attacks |
| --- | --- | --- |
| Green Slime | Melee lunger (`Chase`) | Hop Bite (lunge) |
| Skeleton Archer | Ranged kiter (`KeepDistance`) | Aimed Shot, Scatter Volley (2×5 fan), Kick (pushes you away) |
| Stone Brute | Tank (`Chase`, 220 HP, poise 60, 70% knockback resistance) | Club Swing, Ground Slam, Charge |
| Cave Bat | Swarmer (`Erratic`, hovers) | Dive (fast, weak lunge) |
| Ember Cultist | Caster (`KeepDistance`) | Fire Circles (3 ground AoEs), Ember Nova (radial burst) |
| Training Dummy | Target (`Stationary`, can't die) | none. It heals to full after 2.5 s without being hit |

To make a **new monster**:

1. Duplicate an asset (`Ctrl+D`) and change it.
2. Place it in the level: create an empty GameObject, add `EnemySpawnPoint`, and drag the
   asset into `Config`. Spawn points are also under `EnemySpawns` in the scene.

### Boss: `Assets/Config/Boss/HollowWarden.asset`

The boss has one HP pool (1600) and a list of `phases[]`. Each phase has:

- `startsAtHealthFraction`: phase 2 starts at `0.5`.
- Its own `behaviour`: movement plus attacks, in the same format as monsters.
- `tint` and `spriteScale`.
- A transition:
  - `transitionDuration`: a roar where the boss is invulnerable and a red circle telegraphs
    a shockwave.
  - `announcement` text.
  - The shockwave: `shockwaveRadius`, `shockwaveDamage`, `shockwaveKnockback`.
  - `summonOnEnter`: monsters that appear when the phase starts.

The two phases:

- **Phase I:** Great Sweep, Shoulder Charge, Blade Fan.
- **Phase II (≤50% HP):** faster and red-tinted, with Great Sweep, Rampage Charge, Nova
  Burst (3 rings), Meteor Rain (5 ground AoEs), and Call the Swarm (summons bats). Entering
  phase 2 also summons 2 bats.

The boss bar is at the top center. It has a white tick at each phase threshold, and a trailing
white "chip" bar so big hits read clearly. Add a third phase by adding an entry to `phases[]`
(for example, starting at `0.2`).

### Player: `Assets/Config/Player.asset`

- HP, `invulnerabilityAfterHit`, `hurtStun`, `knockbackTaken`
- Move speed and acceleration
- Dash: speed, duration, cooldown, and extra invulnerability after it ends
- Afterimage: interval, lifetime, color
- `weapons[]`: which weapon goes in slots 1, 2 and 3

### Camera: `Assets/Config/Camera.asset`

- **Angle (locked):** `pitch` (default 50°) and `yaw` (0 = looking north, straight on like
  the reference game).
- **Lens:** a perspective camera with a narrow `fieldOfView` (30°) by default. Tick
  `orthographic` for a completely flat look.
- **Zoom:** `minDistance`, `maxDistance`, `startDistance`, `zoomStep`, `zoomSmoothTime`.
- **Follow:** `followSmoothTime`, and `aimLookAhead` (how far the camera leans toward the
  cursor).
- **Shake:** `maxShakeOffset`, `maxShakeRoll`, `shakeFrequency`, `shakeDecay`.

### Global feel: `Assets/Config/CombatFeel.asset`

This is the main tuning surface for combat feel.

**Hit weight.** Every attack has a `hitWeight`: `Light`, `Medium`, `Heavy`, `Finisher`, or
`Auto`, which guesses from stagger and damage. Each weight has an **impact profile** here:

| Field | What it changes |
| --- | --- |
| `hitStopMultiplier`, `hitStopBonus` | Freeze-frame length on top of the attack's own `hitStop` |
| `extraShake`, `zoomPunch` | Camera shake, and a quick zoom-in kick |
| `squash`, `trembleAmount`, `trembleDuration` | Victim squash, and a sideways tremble that plays during the freeze |
| `knockbackMultiplier` | How far the victim is pushed |
| `sparkMultiplier`, `impactRing`, `dustCount` | Particles at the impact point |
| `numberScale`, `numberColor` | Damage number size and color |

Defaults: light hits are small, snappy and white. Heavy and finisher hits freeze longer,
zoom the camera, throw rings and dust, and show big orange numbers.

**Critical hits** add on top of the profile:
- extra freeze, then a short **slow-motion** tail (`critSlowMoDuration`, `critSlowMoScale`)
- zoom punch, a white/gold screen pop (`critScreenFlash`)
- a star-shaped spark burst
- a gold number that hangs, shakes, and carries a `CRITICAL` label

**Poise break.** When enough stagger breaks an armored enemy (poise above 0), it shows
`BREAK!` in blue with a ring, a freeze and a shake. This is the payoff for heavy attacks: light
hits won't break a Brute or the boss, heavy ones will. Enemy HP bars show a blue poise meter
under the HP.

**Player getting hit:**
- hit-stop, shake and zoom punch
- red screen edges
- the sprite flashes white, fades from red, and trembles
- the sprite blinks during i-frames (`invulnerableBlinkRate`)
- the screen edges pulse red at low HP (`lowHealthWarning`)
- your combo resets

**Other settings:**
- **Damage numbers:** size, rise speed and lifetime. Quick hits on the same target stack upward
  (`stackWindow`, `stackOffset`) instead of overlapping.
- **Combo counter:** the count on the right side of the screen. `comboTimeout` is how long
  before it resets; `comboMinimum` is how many hits before it appears.
- **Enemy HP bars:** shown for `enemyBarLinger` seconds after an enemy is hit.
- **Kills:** a small freeze on every kill; a long slow-motion when the boss dies.
- **Global multipliers:**
  - `hitStopScale`, `cameraShakeScale`, `zoomPunchScale`, `knockbackScale`: set any of them to
    0 to turn that effect off.
  - `enemyDamageScale`: a difficulty dial.

**Player aim assist** (`Player.asset`): melee swings turn toward the nearest enemy within
`meleeAimAssistAngle` degrees of the cursor, so near misses still connect. Set it to 0 to
turn it off.

> The committed config assets already have their `hitWeight` values set. If a local copy
> shows `Auto` everywhere, pull again, or run **Vela → Reset Configs To Defaults**.

## 5. Replacing the placeholder art

Every character's look is set by the `visual` block of its config (player, monster or boss):

- `sprite`: **the one field you need.** Drag in any sprite.
- `idleFrames`, `moveFrames`, `attackFrames`, `hurtFrames`, `framesPerSecond`: optional
  animation frames. Leave them empty to use `sprite`.
- `worldHeight`: the sprite is scaled to this height, whatever its pixel size.
- `artFacesRight`: the sprite is mirrored automatically when the character faces the other way.
- `shadowSize`, `hoverHeight` (for flyers), `tint`
- `moveBob`, `idleBreath`, `moveLean`: procedural motion that makes single-frame art feel
  alive. Set them to 0 once you have real animation.

Import settings for pixel art:

- Texture Type: **Sprite**
- Filter Mode: **Point**
- Compression: **None**
- **Pivot at the bottom center** (the feet). Other pivots also work: the billboard puts the
  sprite's lowest point on the ground.

Two other ways to swap art:

- Overwrite a PNG in `Assets/Art/Placeholder/` with your own. It updates everywhere.
- Arrows and enemy bullets use the `arrowSprite` and `orbSprite` fields in
  `Assets/Config/FxLibrary.asset`.
- Weapons have an optional `icon` field that the HUD shows.

Ground, path, wall and tree textures are placeholder PNGs too (`Grass.png`, `Path.png`,
`Stone.png`, and so on), used by the materials in `Assets/Materials`.

## 6. How it works

```
Assets/Scripts/
  Config/     WeaponConfig, PlayerConfig, MonsterConfig, BossConfig, EnemyConfigBase,
              CameraConfig, CombatFeelConfig, FxLibrary, CharacterVisual
  Core/       Health, DamageInfo, HitStop, VelaInput (works with the legacy Input Manager or the Input System package)
  Player/     PlayerController (move, dash, knockback), PlayerCombat (combo, charge, weapon swap)
  Enemies/    EnemyBrain (all monster AI), BossController (phases), EnemyFactory (builds from config)
  Combat/     CombatUtility (arc and circle hit tests), Projectile, DamageFeedback (hit → feel)
  FX/         FxManager (sparks, dust, rings, slashes, afterimages), SlashArc, RingPulse,
              Telegraph (ground warnings), DamageNumbers, ProceduralMeshes
  Visual/     SpriteBillboard (2.5D sprite: faces the camera, flips, bob/squash, flash, blob shadow)
  World/      FadeableObject + OcclusionFader (see-through scenery), TextureTiling
  Camera/     CombatCameraRig (locked angle, zoom, follow, trauma shake)
  Gameplay/   CombatGameManager (hotkeys, spawning), EnemySpawnPoint, CombatRegistry, VelaSettings
  UI/         CombatHUD (HP, dash, weapon slots, charge meter, boss bar, banners)
Assets/Editor/
  PrototypeSceneBuilder   Vela menu: builds the level
  ConfigDefaults          starting values for every config asset
  PlaceholderArt          draws the placeholder pixel art
```

- **2.5D look.** `SpriteBillboard` turns the sprite to face the locked camera and leans it
  back by `tiltTowardCamera` (35% of the camera pitch). The feet stay planted and a blob
  shadow sits on the floor.
- **One enemy brain, data-driven.** Every monster and the boss run `EnemyBrain`, which loops
  through these states:

  1. **Idle**: waits for the player.
  2. **Engage**: moves according to its `MovementStyle`. When `attackDecisionDelay` runs out,
     it picks an attack that is in range and off cooldown, using `weight`.
  3. **Windup**: shows the telegraph and pulses its tint.
  4. **Active**: the attack happens.
  5. **Recovery**: a window where you can punish it, then back to Engage.

  Enough poise damage causes a **Stagger**, which cancels the attack in progress. A boss is
  the same brain plus `BossController`, which swaps in a new behaviour at each HP threshold.
- **Hit feel.** An attack builds a `DamageInfo` from its config (damage, crit, knockback,
  stagger, hit-stop, shake). `Health` applies it, and `DamageFeedback` turns it into:
  - a white sprite flash and squash
  - sparks and a floating number
  - hit-stop (`Time.timeScale` drops for a few milliseconds)
  - trauma-based camera shake
- **See-through scenery.** `OcclusionFader` on the camera casts toward the player and nearby
  monsters, checking both the feet and the head. Any `FadeableObject` in the way switches to
  a transparent copy of its material and fades down.
- **No NavMesh.** Monsters steer directly toward or around you and slide along obstacles. This
  is fine for a combat test; add NavMeshAgent steering if you need real pathfinding.

## 7. Notes and limits

- The C# was compile-checked against Unity reference assemblies, but it **has not been run in
  the Unity editor yet**. If the first import shows a Console error, it should be small; fix
  it and commit.
- The generated folders (`Assets/Art`, `Assets/Config`, `Assets/Materials`, `Assets/Scenes`)
  are not committed. Commit `Assets/Config` (with its `.meta` files) once you have tuning worth
  keeping.
- The project uses the built-in render pipeline. The see-through fade also handles URP Lit
  materials, but other effect shaders (`Sprites/Default`, `Legacy Shaders/Particles/Additive`,
  `GUI/Text Shader`) are built-in shaders and would need URP equivalents.
- The HUD and damage numbers use IMGUI for speed of iteration. Move them to UGUI or
  TextMeshPro once the feel is settled.
- Never commit `Library/`, `Temp/`, `Logs/` or `Builds/`; `.gitignore` covers them.
