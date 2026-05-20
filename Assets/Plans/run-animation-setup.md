# Project Overview
- Game Title: Snake Connection
- High-Level Concept: Top-down "snake-like" action game where characters are constantly moving/charging.
- Players: Single player
- Target Platform: PC
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
- Player moves constantly, aiming with the mouse.
- Followers form a chain behind the player.
- Enemies charge at the player/followers.
- All characters maintain a "Run" state as they never stop.
- **Pickup objects remain Idle until collected.**

# UI
- N/A (Internal logic change for animations)

# Key Asset & Context
- **Run Animation:** `Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Locomotion/Run/A_Run_F_Masc.fbx`
- **Scripts:** `PlayerMovement.cs`, `EnemyMovement.cs`, `FollowerMovement.cs`

# Implementation Steps

1. **Create "AC_AlwaysCharging" Animator Controller:**
    - A minimalist controller with exactly one state: **Run**.
    - The state uses the `A_Run_F_Masc` animation.
    - No parameters (IsStopped, IsGrounded, etc.) are used or required.

2. **Modify PlayerMovement.cs:**
    - Add `[SerializeField] private Animator _animator;`
    - In `Update()`, set `_animator.speed = _moveSpeed * SpeedMultiplier / 2.5f;` (scaling the playback speed by the actual move speed relative to a base run).

3. **Modify EnemyMovement.cs:**
    - Add `private Animator _animator;`
    - In `Start()`, get the animator using `GetComponentInChildren<Animator>()`.
    - In `FixedUpdate()`, set `_animator.speed = _speed / 2.5f;`.

4. **Modify FollowerMovement.cs:**
    - Add `private Animator _animator;`
    - In `Awake()`, get the animator using `GetComponentInChildren<Animator>()`.
    - At runtime, the playback speed will match the chain's velocity.

5. **Scene/Prefab Setup:**
    - Assign the new `AC_AlwaysCharging` controller to the **Player**, **Enemy Prefabs**, and **Follower Prefab**.
    - **FollowerPickup** will NOT be given this controller, ensuring it remains in a static Idle pose.

# Verification & Testing
- Enter Play mode.
- Confirm all characters (Player, Enemy, Follower) are playing the run animation immediately.
- Confirm animation speed scales with game speed.
- Confirm Pickups stay Idle.
