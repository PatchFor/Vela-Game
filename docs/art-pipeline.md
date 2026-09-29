# Art pipeline: layered (paper-doll) pixel characters

The player is drawn as a stack of sprites so every equipment piece changes the look:

```
Weapon  ← in front facing Down/Side, BEHIND the body facing Up
Head    (helmet / hood / crown — may hide Hair)
Hair
Hands   (gloves)
Chest   (armor)
Feet    (boots)
Body    (base: skin, underclothes, face)
```

The exact order per facing lives in `Assets/Config/Characters/PlayerRig.asset` and can be edited.

## The contract (every layer must follow it)
| Rule | Value in the prototype |
|---|---|
| Canvas size | Same as the body: **16 × 24 px** |
| Pivot | **Bottom center** (0.5, 0) — the feet |
| Pixels per unit | 16 (the game scales to `worldHeight` anyway) |
| Filter / compression | Point / None |
| Facings | **down**, **up**, **side (facing right)** — left is mirrored |
| Empty pixels | Fully transparent |

Because every layer shares the canvas and pivot, pieces line up with zero offsets. A piece that
doesn't touch some facing (e.g. a crown seen from below) can leave that facing empty.

## Colors
Equipment can be drawn in light grey and colored by `EquipmentVisual.tint` (palette swap):
one helmet drawing gives iron, gold and bronze variants. Weapons are drawn in full color with
tint white.

## Animation (next step)

> Being replaced: the plan for real frames (64×64 canvas, anchors for hats and weapons,
> Aseprite pipeline, phase-tagged attacks) is in `docs/specs/sword-animation-paperdoll.md`.
Today each facing has a single frame; SpriteBillboard adds bob, squash and lean in code. When
real frames arrive:

- Every layer gets the **same frame count per state** (idle, move, attack, hurt) per facing.
- Export from Aseprite: one layer group per equipment piece, one tag per state, 3 facing rows.
- The (planned) importer turns that into `DirectionalSprites` + frame arrays automatically.
- Weapon types change the body's attack animation set; weapon *skins* only swap the weapon layer.

## Budget
frames = layers × facings × states × frames per state.
Example: 7 × 3 × 4 × 6 = **504 frames** for one full outfit set. Keep weapon anchors / shapes
simple until the style is locked.

## Replacing placeholders
- Edit `Assets/Art/Placeholder/Doll_*.png` in place (keep the size), or
- Point an `EquipmentVisual` (Assets/Config/Equipment) at your own sprites, or
- Create a new EquipmentVisual + ItemDefinition and add it to a loot table.
