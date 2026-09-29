# Spec: Movement, dash, jump links

**Owner:** movement-camera · **Status:** prototype in place, tuning in progress

## Goal
Moving should feel light and precise; dash is the safe answer to a telegraph; jumping across
water happens only where the level marks it, and uses the same button as dash.

## Behavior
| Action | Input | Result |
|---|---|---|
| Walk | WASD | Camera-relative, accelerates/decelerates, faces the move direction (4-way art) |
| Dash | Space / Shift | Short burst in move direction (or facing), i-frames, afterimages, cooldown |
| Jump link | Dash while near a marked edge, toward the other side | Arc to the paired landing point, invulnerable in the air, dust + squash on landing |
| Walk-to | Click an item out of reach | Walks there and picks it up |
| Face target | While locked on | Faces the target when not attacking |

Facing (4-way): Side if the direction is mostly left/right on screen, else Up/Down. Left = Side mirrored.

## Edge cases
| Case | Expected | Test |
|---|---|---|
| Dash with no input | Dash toward facing | PlayMode |
| Dash into a wall | Stops at the wall, no tunneling | PlayMode |
| Dash near a jump edge but pointing away (> jumpMaxAngle) | Normal dash, no jump | PlayMode |
| Dash near an edge during attack recovery on a no-dash-cancel weapon (greatsword) | Nothing (dash refused) | PlayMode |
| Hit while in the air | No damage (i-frames) | PlayMode |
| Enemy standing on the landing point | Player lands; CharacterController pushes apart | Manual |
| Walk-to interrupted by WASD / dash / attack | Walk-to cancelled | PlayMode |
| Walk into water | Blocked by the invisible wall; arrows still fly over | Manual |
| Death during jump | Jump finishes, then death state | Manual |

## Tuning (Player.asset / Camera.asset)
| Field | Default | Director notes |
|---|---|---|
| moveSpeed / acceleration / deceleration | 6.5 / 60 / 70 | |
| dashSpeed × dashDuration | 20 × 0.16 s (= 3.2 m) | |
| dashCooldown | 0.45 s | |
| jumpArcHeight / jumpDuration | 1.8 m / 0.55 s | |
| jumpMaxAngle | 65° | |
| Camera pitch / FOV / zoom range | 50° / 30° / 10–34 | |

## Animation hand-off (60 fps)
Dash: 10 frames (0.16 s). Jump: 33 frames (0.55 s) — takeoff 3f, air 27f, land 3f.
Update this table whenever the numbers above change.
