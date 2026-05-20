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
- **Animator Controller:** `Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller`
- **Scripts:** `PlayerMovement.cs`, `EnemyMovement.cs`, `FollowerMovement.cs`
- **Parameters:** `MoveSpeed` (float), `CurrentGait` (int), `IsStopped` (bool), `IsGrounded` (bool)

# Implementation Steps

1. **Modify PlayerMovement.cs:**
    - Add `[SerializeField] private Animator _animator;`
    - In `Start()`, set constant parameters:
        - `_animator.SetInteger("CurrentGait", 2);`
        - `_animator.SetBool("IsStopped", false);`
        - `_animator.SetBool("IsGrounded", true);`
    - In `ApplyMovement()`, update `_animator.SetFloat("MoveSpeed", 2.5f * SpeedMultiplier);` to scale animation with game speed.

2. **Modify EnemyMovement.cs:**
    - Add `private Animator _animator;`
    - In `Start()`, get the animator and set constant parameters:
        - `_animator.SetInteger("CurrentGait", 2);`
        - `_animator.SetBool("IsStopped", false);`
        - `_animator.SetBool("IsGrounded", true);`
    - In `FixedUpdate()`, update `_animator.SetFloat("MoveSpeed", _speed / 2.5f);` (or similar scaling).

3. **Modify FollowerMovement.cs:**
    - Add `private Animator _animator;`
    - In `Awake()`, get the animator and set constant parameters:
        - `_animator.SetInteger("CurrentGait", 2);`
        - `_animator.SetBool("IsStopped", false);`
        - `_animator.SetBool("IsGrounded", true);`
    - Animation speed will be constant or linked to player's speed.

4. **Scene/Prefab Setup:**
    - For the **Player** object in `TopDownGame.unity`, assign the `AC_Polygon_Masculine` controller to the `Animator` on the child model and link the `Animator` field in `PlayerMovement`.
    - For the **Enemy Prefabs** and **Follower Prefab** (the one spawned by pickup), assign the controller to the `Animator` on the child model.
    - **FollowerPickup** model will remain without the locomotion controller or logic, staying Idle.

# Verification & Testing
- Enter Play mode.
- Confirm the Player plays the Run animation and it speeds up over time.
- Confirm spawned Enemies play the Run animation.
- Confirm collected Followers switch from Idle (as pickups) to Run (as followers).
