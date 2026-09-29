# Spec: Loot drops and inventory

**Owner:** items-economy (logic), ui-ux (window) · **Status:** prototype in place

## Goal
Killing a monster is rewarded by a satisfying burst of loot whose rarity you can read at a
glance. Picking up is deliberate (click) except gold, which is collected by walking over it.

## Drop
1. On death, the monster's `LootTable` rolls: gold (chance, min–max), guaranteed entries, then
   `rolls` weighted picks (`nothingWeight` = empty roll).
2. Items fan out evenly around the body (no stacking), never through walls or into water.
3. Each item pops up in an arc, bounces once, settles and bobs. Rare+ land last (`rarityDelay`).
4. Rarity cues: Common grey · Uncommon green · Rare blue · Epic purple · Legendary orange.
   Uncommon+ get a glow ring; Uncommon+ get a light pillar that grows with rarity; Rare+ burst on landing
   and show their name when you're near.

## Pick up
| Action | Result |
|---|---|
| Hover item | Grows, name + "Click / F to pick up" |
| Click (in range) | Into the bag |
| Click (out of range) | Walk there, then pick up |
| F | Nearest item in range |
| Walk near gold | Gold flies to you, "+N G" |

## Inventory (I)
24 slots, stacking by `maxStack`. Equipment: Head, Chest, Hands, Feet (paper-doll updates
immediately). Drag to move/merge/swap; drag onto an equipment slot to equip; drag outside the
window to drop; right-click to use/equip/unequip. The game keeps running while it's open.

## Edge cases
| Case | Expected | Test |
|---|---|---|
| Bag full, item not stackable | Stays on the ground, "Inventory full", item wiggles | EditMode (`Inventory`) + PlayMode |
| Bag full, but a stack of that item has room | Picked up into the stack | EditMode |
| Only part fits (3 of 5) | 3 picked up, 2 stay, message "picked up 3/5" | EditMode + PlayMode |
| Gold with a full bag | Always collected (no slot) | EditMode |
| Equip with full bag | Old piece goes into the slot the new one came from | EditMode |
| Unequip with full bag | Refused, "Inventory full" | PlayMode |
| Drink potion at full HP | Refused, message | PlayMode |
| Drag stack onto same item stack | Merge, overflow stays in source | EditMode |
| Drag onto wrong equipment slot | Refused, message | PlayMode |
| Many monsters die at once | Items don't overlap, drops stay out of walls | Manual |
| Item dropped near water | Lands on the near bank | Manual |
| Monster killed while inventory is open | Drops normally | Manual |

## Tuning (LootConfig.asset, Loot/*.asset)
| Field | Default |
|---|---|
| scatterDistance | 0.8–2.2 m |
| popHeight / popDuration / bounceHeight | 1.6 m / 0.45 s / 0.3 m |
| rarityDelay | 0.08 s per tier |
| pickupRange / goldMagnetRadius | 2 m / 1.4 m |
| inventorySize | 24 |
| beamHeights (Common→Legendary) | 0 / 0.8 / 1.6 / 2.6 / 4 m |

## Open questions for the director
- Should items despawn? (`despawnSeconds`, default never)
- Pause the game while the inventory is open?
- Do equipment pieces get stats (defense, etc.) or stay cosmetic in the prototype?
