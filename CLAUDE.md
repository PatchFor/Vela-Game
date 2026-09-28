# Vela — project rules for Claude Code

2.5D action RPG prototype (reference: *Alabaster Dawn*). Pixel-art sprites standing in a 3D
world, locked 3/4 camera, Unity 6 LTS (6000.0.x), built-in render pipeline.

The person you're working with is the **game director**. They decide how things *feel*;
you make everything tunable and prove logic with tests. Never claim something "feels good" —
say what changed and which numbers to tune.

## Layout

```
Assets/Scripts/            Vela.Runtime.asmdef (all gameplay code)
  Core/        Health, DamageInfo, HitStop, VelaInput (ALL input goes through here)
  Config/      ScriptableObject configs: Weapon, Skill, Player, Monster, Boss, Camera, CombatFeel
  Player/      PlayerController (move/dash/jump/walk-to), PlayerCombat, PlayerTargeting,
               PlayerInventory, ItemPickupController
  Enemies/     EnemyBrain (data-driven AI), BossController, EnemyFactory
  Combat/      CombatUtility, Projectile, DamageFeedback, ComboTracker
  Items/       ItemDefinition, Inventory (pure C#), LootTable, LootConfig, WorldItem, LootSpawner
  Visual/      SpriteBillboard (4-way facing), PaperDoll, CharacterRig, EquipmentVisual
  FX/          FxManager, DamageNumbers, Telegraph, SlashArc, RingPulse, TargetReticle
  World/       JumpLink, FadeableObject, OcclusionFader, TextureTiling, ProjectilePassThrough
  Camera/      CombatCameraRig, CameraShake
  Gameplay/    CombatGameManager, VelaSettings, CombatRegistry, HudMessages, EnemySpawnPoint
  UI/          CombatHUD, InventoryUI (IMGUI placeholders)
Assets/Editor/             Vela.Editor.asmdef — scene builder, placeholder art, config defaults
Assets/Tests/EditMode/     Vela.Tests.EditMode.asmdef — NUnit EditMode tests
Assets/Config/             Tuning assets (committed). Designers edit these, not code.
docs/                      Plan, specs, art pipeline
.claude/agents/            Role definitions for the agent team
```

## Rules

1. **Every tunable number lives in a config asset** (ScriptableObject), never hard-coded.
   Add a `[Tooltip]` that says what the number does in player terms.
2. **Defaults go in `Assets/Editor/ConfigDefaults*.cs`.** Existing assets are never overwritten
   (only empty fields are filled), so a rebuild keeps the designer's tuning.
3. **Input only through `VelaInput`.** Keyboard plays, mouse points. Respect
   `VelaInput.PointerOverUI` and `ConsumeClick` so UI clicks never become attacks.
4. **Systems talk through events / registries**, not by reaching into each other
   (`Health.Damaged`, `EnemyBrain.AnyDefeated`, `CombatRegistry`, `HudMessages`).
5. **Scene is generated.** `Vela ▸ Build Combat Prototype Scene` rebuilds
   `Assets/Scenes/CombatPrototype.unity`. When you change the level or add components to the
   player, update `PrototypeSceneBuilder` and bump `SceneVersion`.
6. **Paper-doll art contract:** every layer is a 16×24 canvas (or the rig's size), bottom-center
   pivot, point filter, 3 facings (down/up/side-facing-right). See `docs/art-pipeline.md`.
7. **Frame data is quoted at 60 fps** when talking to animation (windup/active/recovery frames).
8. **One task = one branch = one PR**, with tests. Don't edit files outside your role's folders
   (see `.claude/agents/`); open an issue / leave a note for the owner instead.
9. Keep code comments to *why*, match the existing style (file-level `///` summary per class).

## Running tests

EditMode tests (pure logic: inventory, loot tables, hit weights, paper-doll layer order):

```bash
# Windows
"C:/Program Files/Unity/Hub/Editor/6000.0.32f1/Editor/Unity.exe" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
# macOS
/Applications/Unity/Hub/Editor/6000.0.32f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
```

Unity must be closed for batchmode on the same project. In the editor: Window ▸ General ▸
Test Runner ▸ EditMode ▸ Run All.

Write a test for every rule in a spec's **Edge cases** section that doesn't need a rendered
frame. Anything about *feel* goes in the spec's **Tuning** table instead.

## Definition of done

- Behavior matches the spec in `docs/specs/`, edge cases covered by tests where possible.
- All new numbers are in a config asset with defaults in `ConfigDefaults`.
- Tests pass; no new Console errors or warnings on Play.
- README / spec updated if controls or tuning fields changed.
- PR description lists: what changed, how to try it in the scene, which numbers to tune.
