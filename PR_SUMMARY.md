Commit message (one line)
------------------------
Add turn/round system, scaled enemy waves, and HUD/continue UI


PR description
--------------
This PR adds a simple round-based game flow and turn manager, ties enemy spawning to rounds, scales enemy stats per round, and provides small UI helpers for displaying round/score and continuing between rounds.

Files added
- Assets/_Scripts/RoundManager.cs        (round/wave controller; spawns waves and waits for player continue)
- Assets/_Scripts/TurnManager.cs         (turn / phase controller: Player, Allies, Enemies, EndOfTurn)
- Assets/_Scripts/UI/RoundUI.cs          (TMP text for round/enemies display)
- Assets/_Scripts/UI/RoundContinueUI.cs  (controls Continue button visibility and action)
- Assets/_Scripts/UI/HUDAligner.cs       (aligns TMP texts to same width)

Files modified
- Assets/_Scripts/Enemy/Enemy.cs         (per-round multipliers, notify RoundManager on death, phase-gated movement)
- Assets/_Scripts/UI/ScoreUI.cs          (score padding/formatting)
- Assets/_Scripts/UI/RoundUI.cs         (formatting/padding; subscribe to RoundUpdated)
- Assets/_Scripts/RoundManager.cs       (debug logs + reconciliation; auto-start enemy phase option)

Files removed
- Assets/_Scripts/UI/RoundEndUI.cs       (replaced by RoundContinueUI)
- Assets/_Scripts/UI/EndTurnButton.cs    (removed during iteration)

Scene wiring / testing notes
- Create/verify a SpawnManager GameObject: assign enemy prefab, arena BoxCollider2D and obstacle mask. Set autoSpawn = false.
- Add a RoundManager GameObject and assign the SpawnManager to its spawner field.
- Add a TurnManager GameObject (tweak phase durations or enable autoAdvanceOnStart for testing).
- Create a Continue Button under Canvas (Button with TMP child). Can be inactive at start.
- Attach RoundContinueUI to an always-active object (Canvas or UIManager) and assign the Continue Button to the panel field.
- Attach RoundUI and ScoreUI to text elements (TMP) and use HUDAligner to keep columns aligned.
- Play: RoundManager will spawn waves; when all enemies are dead the Continue button will appear and call ContinueToNextRound().

Known issues / TODO
- Reward/upgrade selection UI not implemented yet (suggested next task).
- Enemy AI is simple direct-chase; can be expanded to include targeting base/defendable objects.
- Some initialization order fallbacks and reconciliation checks were added to handle lifecycle issues; consider simplifying once scene wiring is stable.
- Add simple menu and some sound effects
- Boss waves
- Brainstrom upgrades/refine game idea more

Testing steps
1. Open the scene and ensure the objects listed in Scene wiring exist and are assigned.
2. Play and confirm Console shows spawn/kill logs from RoundManager.
3. Kill all enemies: Continue button should appear. Click to start the next round.

Small commit message for git
----------------------------
Add round/turn system, UI for round/score and Continue button; scale enemies per round
