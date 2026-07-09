This project is a game made using Unity for the game jam by Juniper Dev

# Spin2Win

A 2D top-down wave-based tower defense game built in Unity/C#, designed around data-driven architecture, event-driven systems, and clean separation of concerns.

<img width="1553" height="833" alt="Screenshot 2026-06-26 195358" src="https://github.com/user-attachments/assets/96d1c154-7369-4d81-83e1-8063d8be6829" />
<img width="1087" height="726" alt="Screenshot 2026-06-26 195434" src="https://github.com/user-attachments/assets/1eb1f05f-b850-470d-bfe1-b0c071e7e982" />

## Overview

Spin2Win explores core software engineering patterns through the lens of interactive systems development: ScriptableObject-based data architecture, singleton state management, event-driven UI, and modular runtime systems. Players spin a slot-machine shop to purchase towers and consumable upgrade cards, then defend a base against increasingly difficult enemy waves including boss rounds every 10 waves. While the end product is a game, the underlying focus is on writing maintainable, extensible C# systems.

## Architecture Highlights

- **Data-Driven Design** — Towers, enemies, and shop cards are fully decoupled from logic using Unity `ScriptableObject`s (`TowerSO`, `EnemySO`, `ShopCardSO`), allowing new content to be added without touching code.
- **Singleton State Management** — `GameModifiers`, `RoundManager`, `BaseHealth`, and `ScoreManager` each act as a centralized source of truth for their domain, coordinated across scenes via `DontDestroyOnLoad`.
- **Event-Driven Systems** — Wave progression, health changes, round updates, and UI state are coordinated through decoupled C# `Action` events rather than tight polling loops.
- **Procedural UI Construction** — Shop spin reveal, upgrade drag-and-drop inventory, tower stat popups, pause menu, sell confirmation modal, boss round banners, and the instructions panel are all built programmatically with uGUI and TextMeshPro rather than hand-wired in the Editor.
- **Collision & Layer Management** — Combat, tower targeting, and upgrade drag-drop logic use `LayerMask`-based filtering to keep gameplay systems isolated and predictable.
- **Consumable Upgrade System** — Upgrade cards bought into an inventory and applied by dragging onto specific placed towers, with live validity tinting and per-instance stat tracking independent of shared `ScriptableObject` data.

## Tech Stack

- **Engine:** Unity (2D)
- **Language:** C#
- **UI:** uGUI, TextMeshPro (TMPro)
- **Physics:** Rigidbody2D, Physics2D overlap queries
- **Animation:** PlayableGraph API via custom `AnimationClipPlayer` wrapper
- **Persistence:** PlayerPrefs
- **Version Control:** Git / GitHub

---

## System Breakdown

### Data Layer (`ScriptableObject`s)

#### `TowerSO.cs`
Defines all configuration for a tower type: name, cost, icon, damage, range, fire rate, rotation speed, projectile prefab, fire arc tolerance, melee flag, visual scale multiplier, animations, and sound. Towers read from this at runtime but never write back to it, keeping shared data immutable across all placed instances of the same type.

#### `EnemySO.cs`
Defines enemy configuration: health, move speed, damage, attack range/cooldown, score value, spawn point cost, and animations. Also exposes boss-specific fields (`isBoss`, `extraVisualScale`) so the same enemy prefab can be reused at a larger scale for boss rounds without separate art.

#### `ShopCardSO.cs`
Defines a shop upgrade card: label, description, icon, cost, rarity, `SpinFortRewardType` enum value, and payload fields (`intValue`, `floatValue`, `duration`). The enum drives all branching logic in the shop and UI — adding a new card type means appending to the enum and handling one new case, nothing else.

#### `SpinFortRewardType.cs`
Enum mapping every card effect to an integer index. Values are append-only (existing `.asset` files serialize the int, not the name), so inserting mid-list would silently corrupt all existing card assets.

---

### Tower Systems

#### `Tower.cs`
The core placed-tower runtime. Key responsibilities:
- **Visual/collider separation** — On `Awake()`, detects if the `SpriteRenderer` sits on the root `GameObject` (same as the `Collider2D`) and programmatically moves it to a new `"Visual"` child via `MoveSpriteRendererToVisualChild()`. This ensures aiming rotations never move the collider out from under the clickable area.
- **Targeting** — `FindClosestEnemyInRange()` scans all live enemies each frame using squared-distance comparisons to avoid `Mathf.Sqrt` overhead.
- **Aiming** — `RotateVisualTowards()` uses `Mathf.MoveTowardsAngle` for smooth rotation. `ApplyVisualRotation()` folds the angle into the right-facing half and mirrors via `localScale.x` for left-facing directions, so the sprite never appears upside-down regardless of rotation.
- **Per-instance stats** — `instanceDamageBonus`, `instanceDamageMultiplier`, `instanceAttackSpeedBonus`, and `instanceRangeBonus` track buffs applied to this specific placed tower, computed live through `EffectiveDamage`, `EffectiveRange`, `EffectiveFireRate`, and `EffectiveRotationSpeed` properties.
- **Windup bar** — A self-built floating `SpriteRenderer`-based progress bar (independent of the tower's `GameObject` hierarchy) showing charge progress toward the next shot while a target is in range.
- **Sell** — Calculates a gold refund as a percentage of buy cost, spawns floating gold text, triggers sell explosion if `GameModifiers` has it enabled, and destroys itself.

#### `TowerPlacementManager.cs`
Manages the full tower placement flow:
- Drag-from-inventory ghost preview with rotation (R key) and range ring visualization
- Layer-based placement validation (no placing on obstacles, water, or other towers)
- Right-click sell confirmation modal (self-built Panel with Sell/Cancel buttons, ref-counted so it survives multiple towers being right-clicked)
- `ShowRangeRingAt()` / `HideRangeRing()` called by `TowerDetailPopupUI` to reuse the same range preview ring when inspecting a placed tower

---

### Enemy Systems

#### `Enemy.cs`
Handles pathfinding (waypoint-based), health, damage dealing, animation playback, and death cleanup. `Configure(EnemySO)` is called immediately after `Instantiate` (before `Start()`) to inject the data asset and apply `extraVisualScale` for boss enemies — compensating the collider size inversely so physics isn't affected by the visual scale change.

Per-round multipliers (`healthMultiplier`, `speedMultiplier`, `damageMultiplier`) are set by `RoundManager` after spawning so every enemy in a round scales uniformly without modifying the `ScriptableObject`.

#### `SpawnManager.cs`
Bounds-driven spawning using a collider-defined arena perimeter. `GenerateWave(budget)` fills a point budget by randomly selecting from the enemy roster weighted by `EnemySO.pointCost`, returning a spawn list that `RoundManager` steps through with timed delays.

---

### Round & Game Flow

#### `RoundManager.cs`
Central game loop coordinator:
- `RoundLoop()` coroutine permanently drives the buy-phase → wave-phase → repeat cycle
- `StartRound()` generates a wave via `SpawnManager`, applies per-round scaling to each spawned enemy, and handles boss round injection every `bossRoundInterval` rounds
- `IsBossRound(int round)` — clean predicate used by both spawn logic and UI
- `BossRoundStarted` event — fired before the boss spawns so UI can show a banner without the round manager knowing anything about UI
- `HighestRound` persisted via `PlayerPrefs`; `HighestRoundChanged` event notifies the main menu
- `ResetManagerForRestart()` — stops all coroutines, resets all state fields, and re-starts `RoundLoop()` so a restart is a clean in-place reset without reloading the scene
- `SetTimerPaused(bool)` — lets the pause menu and tower popup freeze the between-wave timer without touching `Time.timeScale`

#### `BaseHealth.cs`
Singleton tracking the player's base HP. Initializes in `Awake()` (not `Start()`) so health bar UI can subscribe before the first `HealthChanged` event fires, avoiding a race condition that previously left the bar stuck at zero after restart. `ResetForRestart()` restores max health and re-fires the event so all health-driven UI updates without a scene reload. `IncreaseMaxHealth()` supports the Coral Barricade upgrade card.

#### `GameModifiers.cs`
Accumulates global run-wide buffs across the full run:
- `globalDamageMultiplier` — multiplicative tower damage buff
- `towerRotationSpeedBonus` — additive rotation speed buff
- `luckMultiplier` — multiplicatively shifts rarity weights toward rarer cards (`luckMultiplier *= multiplier`)
- `goldPerRoundBonus` — flat gold added each round
- `explodeOnSell` — triggers a damage explosion at the tower's position on sell

---

### Shop Systems

#### `SpinFortShopManager.cs`
Manages the slot-machine shop economy:
- `RollRandomOffers()` — selects cards weighted by rarity, boosted by `luckMultiplier` via `Mathf.Pow(luckMultiplier, rarityIndex)` so higher luck exponentially favors rarer cards
- `IsTowerTargetedUpgrade()` — gates whether a card enters the drag-onto-tower flow or the instant-use flow
- `UseUpgradeOnTower()` / `UseUpgrade()` — apply card effects by `SpinFortRewardType`, either to a specific `Tower` instance or globally via `GameModifiers`
- Per-slot lock toggles — chosen offers persist across rerolls when locked
- Dynamic reroll cost — base 10, +10 per spin this round, resets on wave start

#### `SpinFortRunManager.cs`
Handles cross-round reward application (e.g. `MaxTowerHealthBuff` calling `BaseHealth.Instance?.IncreaseMaxHealth()`).

---

### Projectile Systems

#### `BulletBehavior.cs`
Base projectile class. Moves via `Rigidbody2D` velocity, triggers `OnHitEnemy()` on collision, plays impact sound and screen shake, then destroys itself.

#### `LightningBoltBulletBehavior.cs`
Extends `BulletBehavior` with chain lightning — on hit, performs up to `chainCount` additional `Physics2D.OverlapCircle` passes to arc damage to nearby enemies. `LinearDamping` on the prefab's `Rigidbody2D` is set to `0` to prevent the bolt from decelerating mid-flight.

#### `PufferMineBulletBehavior.cs`
Extends `BulletBehavior` with area splash damage. Overrides `Update()` to call `IsAnyEnemyWithinSplashRadius()` each frame using `Physics2D.OverlapCircleAll`, detonating as soon as any enemy enters the splash radius. This fixes a targeting edge case where mines fired at maximum range would sail past enemies that walked off the straight-line trajectory before the mine arrived, only exploding (harmlessly) after the fuse expired.

---

### UI Systems

#### `ShopUI.cs`
Drives the slot-machine card reveal animation:
- `SpinThenReveal()` coroutine cycles label, cost, icon, border rarity art, and background type per tick before settling on the true card
- `ApplyBorderRarity()` and `ApplyCardBackground()` helpers called both during spin and at reveal so the border never spoils the true rarity early
- `PlayBorderAnimation()` guards duplicate `AddClip` calls to prevent `AnimationClip` name conflicts

#### `TowerDetailPopupUI.cs`
Full-screen modal with three overloads: `Show(TowerSO)` for shop cards, `Show(Tower)` for placed towers (also draws the range ring), and `Show(ShopCardSO)` for upgrade cards. `BuildStatsText()` generates contextual stat blocks — placed-tower stats show `EffectiveRange`/`EffectiveDamage`/etc. with `(base X)` annotations when a per-instance buff has changed the value. Fires `Shown`/`Hidden` events so other UI (e.g. health bar) can reposition around it. Pauses the between-wave timer via `RoundManager.SetTimerPaused()` while open.

#### `BaseHealthBarUI.cs`
Subscribes to `BaseHealth.HealthChanged` and updates the HP bar. Uses a ref-counting pin system (`RequestPin()` / `ReleasePin()`) to stay visible while the shop is open or a popup is shown, regardless of how many systems are requesting it simultaneously.

#### `UpgradeDragUI.cs`
Handles drag-and-drop of consumable upgrade cards from inventory onto placed towers:
- `LayerMask` uses lazy initialization (assigned on first use, not at class load) to avoid Unity's restriction on calling `LayerMask.GetMask()` from a static field initializer during deserialization
- On drop, `FindTowerUnderScreenPoint()` raycasts against the Towers layer only
- Shows live green/red tint on the tower under the cursor to indicate valid/invalid drop targets

#### `PauseMenuUI.cs`
Self-building pause panel (5 buttons: Resume, Restart, Instructions, Main Menu, Quit). Calls `RoundManager.Instance?.SetTimerPaused(paused)` on open/close so the between-wave countdown freezes correctly. Hosts the instructions overlay via `InstructionsPanelUI.Build()`.

#### `MainMenuUI.cs`
Displays high score (`ScoreManager.HighScore`) and highest round ever reached (`RoundManager.HighestRound`, persisted via `PlayerPrefs`). Hosts the How To Play button wired to the same `InstructionsPanelUI`.

#### `InstructionsPanelUI.cs`
Static `Build(Transform parent, Color accentColor)` factory — constructs a full-screen backdrop, centered content panel, and close button entirely in code. Shared by both `MainMenuUI` and `PauseMenuUI` so instructions are consistent across entry points.

#### `GameOverUI.cs`
Shows final score and round reached. Both Restart and Main Menu paths call `RoundManager.Instance.ResetManagerForRestart()` and `BaseHealth.Instance.ResetForRestart()` so state is fully clean regardless of which exit the player takes.

#### `ScoreUI.cs`, `FloatingText.cs`
`ScoreUI` subscribes to `ScoreManager.ScoreChanged` and reformats the display. `FloatingText` spawns a self-destroying world-space TMP label that floats upward — used for gold earned, damage numbers, and sell refunds.

---

## Notable Engineering Problems Solved

**Collider rotation desync** — Towers became unclickable and refused upgrade cards once they aimed at certain angles because rotating the root `GameObject` for aiming also rotated its `Collider2D`. Fixed by programmatically re-homing the `SpriteRenderer` onto a `"Visual"` child in `Tower.Awake()` so only the visual rotates.

**Health bar stuck at zero after restart** — `BaseHealthBarUI` subscribed in `Start()` but `BaseHealth` initialized `CurrentHealth` in `Start()` too, so the bar missed the first event. Fixed by moving BaseHealth initialization to `Awake()`, which always runs before any `Start()`.

**Round not resetting on restart/main menu** — `RoundLoop()` permanently exits when `BaseHealth.IsDead` is true. `ResetManagerForRestart()` now calls `StopAllCoroutines()` and restarts `RoundLoop()` so the loop is always live for the new run.

**`LayerMask` static init crash** — `LayerMask.GetMask("Towers")` called from a static field initializer triggered a Unity exception during deserialization. Fixed with lazy initialization — the mask is assigned on first use inside the method that needs it, not at class load time.

**Puffer mine edge-of-range miss** — Mines aimed at maximum-range targets would sail past them because the target walked off the straight-line trajectory during flight. Fixed by checking `Physics2D.OverlapCircleAll` each frame in `Update()` and detonating as soon as any enemy enters splash radius.

---

## Status

Actively in development.

## Getting Started

1. Clone the repo
2. Open in Unity (2D template recommended)
3. Open the main scene and press Play

Available on itch.io:
https://aidenramm.itch.io/fin2win-tower-defense

---

*Built by [Aiden Ramm/Artikos](https://github.com/AidenDoesCode)*
