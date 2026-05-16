# Project Overview - Hive Mind Conga Line Survival Game
- Game Title: Hive Mind Conga Line (Working Title)
- High-Level Concept: A top-down 3D sci-fi arcade survival shooter where you control a leader leading a conga line of survivors that auto-fire at enemies. If a link in the chain breaks, everything behind it is lost.
- Players: Single player
- Inspiration / Reference Games: Snake, Vampire Survivors, Geometry Wars
- Tone / Art Direction: Low-poly sci-fi arcade, neon, flashy, high contrast.
- Target Platform: PC (Standalone Windows)
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: URP (PC_RPAsset detected)

# Game Mechanics 
## Core Gameplay Loop
1. Move the leader with WASD and aim with the mouse.
2. Recruit followers (pickups) to grow the chain.
3. Followers automatically target and shoot the nearest enemies.
4. Protect the chain: if a middle follower dies, all subsequent followers are destroyed.
5. Survive escalating waves of simple AI enemies.

## Controls and Input Methods
- WASD: Move leader relative to camera.
- Mouse: Rotate leader to face cursor.
- Auto-Fire: Followers fire automatically when enemies are in range.

# UI
- HUD: Wave counter, Score, Chain length, Health bar for leader.
- Game Over: Stats summary (waves survived, kills, max chain).

# Key Asset & Context
- `PlayerMovement.cs`: Already exists, handles leader movement.
- `Follower.cs`: New script for the conga-line units.
- `ChainManager.cs`: New script to manage the list of followers, spacing, and death propagation.
- `AutoFire.cs`: New script (or refactored `PlayerFire.cs`) for automatic targeting and shooting.
- `Enemy.cs`: Simple AI for Chasers, Tanks, etc.
- `RecruitPickup.prefab`: For adding units to the chain.

# Implementation Steps
## Phase 1: The Conga Line (Follower System)
1. **Implement `BreadcrumbBuffer`**: Create a component for the Leader to store a list of previous positions and rotations (spaced by distance).
2. **Implement `Follower.cs`**: Followers will follow the "breadcrumbs" left by the unit ahead of them to ensure they follow the exact path.
3. **Implement `ChainManager.cs`**: Handles `AddFollower()` and `RemoveFollower()` logic. 
4. **Verification**: Move the leader in circles; followers should follow the exact path without cutting corners.

## Phase 2: Auto-Fire & Combat
1. **Refactor `PlayerFire.cs` to `AutoFire.cs`**:
   - Add a `detectionRange`.
   - Use `Physics.OverlapSphere` or a Trigger to find enemies.
   - Rotate the unit towards the nearest enemy and fire based on `fireRate`.
2. **Apply `AutoFire.cs` to Followers**: Ensure every follower operates independently.
3. **Implement `Health.cs`**: A generic health system for the Leader and Followers.
4. **Implement Chain Death Propagation**: When a `Health` component on a follower reaches 0, notify `ChainManager` to destroy it and all followers after it in the list.

## Phase 3: Enemies & Waves
1. **Implement `BasicEnemy.cs`**: Simple "move towards player/chain" logic.
2. **Implement `WaveSpawner.cs`**: Spawns enemies around the arena edges.
3. **Verification**: Enemies should be able to damage both the leader and followers.

## Phase 4: Pickups & Polish
1. **Implement `RecruitPickup.cs`**: On collision, calls `ChainManager.AddFollower()`.
2. **Visuals**: Add "Link" lines between followers (LineRenderer or simple beams).
3. **Juice**: Screen shake on death, muzzle flashes, hit sparks.

# Verification & Testing
- **Conga Stability**: Verify followers don't jitter or overlap when the leader stops suddenly.
- **Path Integrity**: Verify followers follow the leader's exact path through tight corners.
- **Chain Break**: Manually destroy the 2nd unit in a 5-unit chain; verify units 3, 4, and 5 are also destroyed.
- **Auto-Targeting**: Verify followers shoot at the closest enemy even if it's behind the leader.
