This project is a game made using Unity for the game jam by Juniper Dev

# SpinFort

A 2D top-down wave-based tower defense game built in Unity/C#, designed around data-driven architecture, event-driven systems, and clean separation of concerns.

## Overview

SpinFort explores core software engineering patterns through the lens of interactive systems development: ScriptableObject-based data architecture, singleton state management, interface-driven UI, and modular runtime systems. While the end product is a game, the underlying focus is on writing maintainable, extensible C# systems.

## Architecture Highlights

- **Data-Driven Design** — Tower configuration (stats, rotation speed, icons, behavior) is decoupled from logic using Unity `ScriptableObject`s (`TowerSO`), allowing new content to be added without touching code.
- **Singleton State Management** — `PlayerTowerInventory` tracks owned towers across scenes as a centralized, single source of truth.
- **Event-Driven Systems** — Wave progression, spawning, and UI updates are coordinated through decoupled event flows rather than tight polling loops.
- **Procedural UI Construction** — Drag-and-drop loadout system (`LoadoutBarUI`, `TowerInventoryUI`, `SlotDropHandler`, `TowerDragUI`) built programmatically with uGUI and TextMeshPro, rather than hand-wired in the Editor.
- **Collision & Layer Management** — Combat and placement logic use `LayerMask`-based filtering (e.g., bullets only interact with the Enemy layer; placement respects no-build zones) to keep gameplay systems isolated and predictable.

## Tech Stack

- **Engine:** Unity (2D)
- **Language:** C#
- **UI:** uGUI, TextMeshPro (TMPro)
- **Version Control:** Git / GitHub

## Core Systems

| System | Description |
|---|---|
| `Tower.cs` | Smooth barrel rotation and targeting logic with configurable fire-angle tolerance |
| `TowerPlacementManager.cs` | Runtime loadout management with layer-based placement validation |
| `SpawnManager.cs` | Bounds-driven enemy spawning using collider-based arena definition |
| `RoundManager.cs` | Wave progression and round state management |
| `BaseHealth.cs` / `Enemy.cs` | Shared health and combat logic |

## Notable Engineering Problem Solved

**Serialization timing bug:** The first tower added to the player's inventory list wasn't rendering in the UI until a second tower was added — a subtle Unity serialization/inspector timing issue. Resolved using `OnValidate()` to correctly hook into Unity's serialization lifecycle. A good example of debugging Unity's internals rather than surface-level symptoms.

## Status

Actively in development. Current focus: implementing the in-game shop system.

## Getting Started

1. Clone the repo
2. Open in Unity (2D template recommended)
3. Open the main scene and press Play

It is avaliable on the itch.io page linked below:
https://aidenramm.itch.io/fin2win-tower-defense

---

*Built by [Aiden Ramm](https://github.com/AidenDoesCode)*


<img width="1553" height="833" alt="Screenshot 2026-06-26 195358" src="https://github.com/user-attachments/assets/96d1c154-7369-4d81-83e1-8063d8be6829" />
<img width="1087" height="726" alt="Screenshot 2026-06-26 195434" src="https://github.com/user-attachments/assets/1eb1f05f-b850-470d-bfe1-b0c071e7e982" />
