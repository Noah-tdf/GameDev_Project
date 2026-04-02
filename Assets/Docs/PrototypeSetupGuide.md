# Ubuntu City Prototype Setup Guide

## 1. Unity Project Setup

### Recommended template
Use **2D URP**.

### Why
- You already have a Unity 6 URP 2D project set up.
- URP gives you a cleaner path for later grayscale lighting, fog, glow, and old-film post-processing.
- It keeps the prototype simple now while leaving room for stronger visual style later.

## 2. Packages To Install

Your project already includes most of these. Keep or install the following:

- **Cinemachine**
  - Best for smooth camera follow, camera zones, and boss-room framing later.
- **2D Animation**
  - Useful for future character rigs, sprite swapping, and rubber-hose style animation.
- **2D Sprite**
  - Core sprite tools for building 2D scenes and characters.
- **2D PSD Importer**
  - Helpful if you later import layered Photoshop files for characters, props, or UI.
- **2D Tilemap Editor**
  - Good for building quick prototype layouts and later city street levels.

## 3. Folder Structure

Use this `Assets` structure:

```text
Assets
├── Art
│   └── Placeholders
├── Audio
├── Docs
├── Materials
├── Prefabs
│   ├── Characters
│   ├── Combat
│   └── Environment
├── Scenes
├── Scripts
│   ├── Camera
│   ├── Combat
│   ├── Core
│   ├── Enemies
│   └── Player
├── Settings
├── Tilemaps
└── UI
```

## 4. Scene Hierarchy

Create a first playable scene named `PrototypeScene`.

```text
PrototypeScene
├── GameManager
├── Level
│   ├── Background
│   ├── Ground
│   ├── Platform_01
│   ├── Platform_02
│   ├── Platform_03
│   └── EnemySpawnArea
├── Player
│   └── FirePoint
├── Enemy_Test
│   ├── LeftPoint
│   └── RightPoint
├── Main Camera
└── Global Light 2D
```

## 5. GameObjects And Components

### Player
- Tag: `Player`
- Layer: `Player`
- Components:
  - `SpriteRenderer`
  - `Rigidbody2D`
  - `BoxCollider2D` or `CapsuleCollider2D`
  - `PlayerMovement`
  - `PlayerShooting`
- Rigidbody2D settings:
  - Body Type: `Dynamic`
  - Gravity Scale: `3`
  - Collision Detection: `Continuous`
  - Interpolate: `Interpolate`
  - Freeze Rotation Z: `On`
- Child object:
  - `FirePoint`
    - `Transform`
    - Position around `(0.6, 0.1, 0)`

### Ground
- Tag: `Ground`
- Layer: `Ground`
- Components:
  - `SpriteRenderer` or `TilemapRenderer`
  - `BoxCollider2D` or `TilemapCollider2D`
- Rigidbody2D:
  - None needed for static ground

### Platforms
- Tag: `Platform`
- Layer: `Ground`
- Components:
  - `SpriteRenderer`
  - `BoxCollider2D`
- Rigidbody2D:
  - None for normal static platforms

### Main Camera
- Tag: `MainCamera`
- Layer: `Default`
- Components:
  - `Camera`
  - `UniversalAdditionalCameraData`
  - `AudioListener`
  - `CameraFollow`
- If using Cinemachine later:
  - Replace `CameraFollow` with `CinemachineBrain`
  - Create a Cinemachine camera that follows `Player`

### FirePoint
- Child of `Player`
- Components:
  - `Transform`
- Purpose:
  - Spawn point for bullets

### Bullet
- Tag: `Bullet`
- Layer: `Projectile`
- Components:
  - `SpriteRenderer`
  - `Rigidbody2D`
  - `CircleCollider2D` set to `Is Trigger`
  - `Bullet`
- Rigidbody2D settings:
  - Body Type: `Dynamic`
  - Gravity Scale: `0`
  - Collision Detection: `Continuous`
  - Interpolate: `Interpolate`

### Enemy_Test
- Tag: `Enemy`
- Layer: `Enemy`
- Components:
  - `SpriteRenderer`
  - `Rigidbody2D`
  - `BoxCollider2D`
  - `Enemy`
- Rigidbody2D settings:
  - Body Type: `Dynamic`
  - Gravity Scale: `3`
  - Freeze Rotation Z: `On`
  - Collision Detection: `Continuous`
- Child objects:
  - `LeftPoint`
  - `RightPoint`

### GameManager
- Tag: `Untagged`
- Layer: `Default`
- Components:
  - `GameManager`
- Purpose:
  - Holds project-wide systems later like score, pause, UI, game state, and scene flow

## 6. Prefabs

Make these into prefabs:

- **Player**
  - Reusable across scenes and easier to update once.
- **Bullet**
  - Must be a prefab because it is instantiated at runtime.
- **Enemy_Test**
  - Lets you duplicate enemies quickly across levels.
- **Ground piece / platform piece**
  - Good if you build levels with reusable blocks before switching fully to tilemaps.

## 7. Tags And Layers

### Tags
Create these tags:
- `Player`
- `Ground`
- `Platform`
- `Enemy`
- `Bullet`

### Layers
Create these layers:
- `Player`
- `Ground`
- `Enemy`
- `Projectile`

Recommended layer use:
- Put player on `Player`
- Put ground and platforms on `Ground`
- Put enemies on `Enemy`
- Put bullets on `Projectile`

## 8. Scripts

Scripts added in this project:

- [PlayerMovement.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Player/PlayerMovement.cs)
- [PlayerShooting.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Player/PlayerShooting.cs)
- [Bullet.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Combat/Bullet.cs)
- [Enemy.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Enemies/Enemy.cs)
- [CameraFollow.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Camera/CameraFollow.cs)
- [GameManager.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Core/GameManager.cs)

### Setup notes for `PlayerMovement`
- Assign `Ground Check` to an empty child object placed under the player’s feet.
- Set `Ground Layer` to the `Ground` layer mask.

### Setup notes for `PlayerShooting`
- Assign `Player Movement`
- Assign `Fire Point`
- Assign the `Bullet` prefab

### Setup notes for `Bullet`
- Add a small placeholder sprite
- Set collider to `Is Trigger`

### Setup notes for `Enemy`
- For patrol:
  - Create `LeftPoint` and `RightPoint`
  - Place them on each side of the patrol zone

### Setup notes for `CameraFollow`
- Drag the `Player` transform into the target field on the camera

## 9. Input Controls

Current control plan:

- `A` / `Left Arrow` = move left
- `D` / `Right Arrow` = move right
- `Space` = jump
- `J` = shoot

This prototype uses the active Unity Input System keyboard API directly to stay simple.

## 10. Physics Setup

### Player
- `Rigidbody2D` should be dynamic.
- Freeze Z rotation.
- Use a `BoxCollider2D` or `CapsuleCollider2D`.
- Player movement uses velocity on X and jump force on Y.

### Bullet
- `Rigidbody2D` should be dynamic with gravity `0`.
- Collider should be `Is Trigger`.
- Bullet should destroy itself on enemy or level collision.

### Platforms
- Static colliders only.
- No `Rigidbody2D` required for simple prototype platforms.

### Enemy
- `Rigidbody2D` should be dynamic.
- Freeze Z rotation.
- Use a standard collider so it interacts with the ground correctly.

## 11. Minimum Playable Prototype

At minimum, the first playable prototype should do all of this:

- Load into one main playable scene
- Spawn the player in a side-scrolling level
- Let the player move left and right
- Let the player jump onto platforms
- Let the camera follow the player
- Let the player shoot bullets using `J`
- Destroy an enemy by hitting it with bullets
- Use collisions correctly so the player stands on the ground and platforms

If all of that works, you already have a valid foundation for the next systems.

## 12. Development Order

Use this order to avoid confusion:

1. Create the main scene and save it as `PrototypeScene`
2. Build ground and platforms with placeholder sprites
3. Create the player object with collider and rigidbody
4. Add `PlayerMovement` and test movement and jumping
5. Add camera follow
6. Create bullet prefab
7. Add `PlayerShooting` and test bullet spawning
8. Create `Enemy_Test` prefab
9. Add enemy patrol and bullet damage
10. Expand the level with more platforms and one test combat section

## 13. Checkpoint 02 Content

### 10 features / work steps
1. Create the first main prototype scene
2. Build the player prefab
3. Implement movement and jumping
4. Set up 2D physics and collisions
5. Implement camera follow
6. Create bullet prefab and shooting
7. Create a simple enemy prefab
8. Build a basic level layout with platforms
9. Organize assets, scripts, and prefabs cleanly
10. Set up GitHub branch workflow and merge process

### Best 4 to implement first
1. Main scene and level layout
2. Player movement and jumping
3. Physics and collisions
4. Shooting system with bullet prefab and one enemy

### Why these 4
- They create a real playable prototype quickly.
- They satisfy the checkpoint requirement for scene, character, movement scripts, physics, and prefab use.
- They give the team something testable before art or advanced systems start.

## 14. Four-Week Plan

### Week 1
- Set up Unity project structure
- Create prototype scene
- Build placeholder level blockout
- Create player object and movement

### Week 2
- Finish jumping and collision tuning
- Add camera follow
- Create bullet prefab and shooting
- Create first enemy prefab

### Week 3
- Expand level with platforming challenge
- Improve enemy placement and combat flow
- Add fail state / restart logic if time allows
- Start grayscale placeholder environment pass

### Week 4
- Test and fix bugs
- Clean scene hierarchy and prefabs
- Merge branches into a stable prototype build
- Prepare checkpoint submission notes and demo

## 15. Team Responsibilities

### Noah Bouffard
- Project setup and folder structure
- Player controller
- Shooting and bullet prefab
- GitHub branch management and merges

### Ryan Liautaud
- Level blockout and platform placement
- Enemy prefab setup and testing
- Scene setup and environment placeholders
- Checkpoint documentation and feature tracking

### Shared responsibilities
- Playtesting
- Bug fixing
- Merge reviews
- Planning next milestone

## 16. GitHub Branch Workflow

Use a simple student-friendly branch structure:

- `main`
  - Stable checkpoint-ready build
- `dev`
  - Shared integration branch
- `feature/player-movement`
- `feature/shooting`
- `feature/enemy`
- `feature/level-blockout`

Suggested workflow:
1. Create a feature branch
2. Finish a focused task
3. Test in Unity
4. Merge into `dev`
5. Merge `dev` into `main` when stable

## 17. Coding Style

Use these rules:

- Keep classes small and focused
- Use clear variable names
- Add comments only where they help beginners understand logic
- Avoid single giant scripts
- Avoid advanced architecture too early
- Build systems that are easy to replace later

For this project, simple and working is better than complex and unfinished.

## 18. Future Style Layering

After the prototype works, add the visual identity in layers:

### Grayscale backgrounds
- Paint or draw city backgrounds in black, white, and gray
- Use simple parallax layers for depth

### Warped props
- Create bent lamp posts, crooked doors, warped windows, and tilted signs
- Keep shapes exaggerated and slightly surreal

### Vintage signage
- Add old storefront names, hand-lettered signs, and broken city billboards

### Rubber-hose animation
- Use exaggerated squash, stretch, bend, and expressive poses
- Focus first on Luca, then enemies, then bosses

### Old-film effects
- Add vignette, film grain, flicker, dust, and light blur later through URP/post-processing

### Post-processing
- Keep it subtle
- The goal is “old cartoon city with eerie charm,” not “hard-to-read filter overload”

## 19. Important Prototype Rule

Do not spend your early checkpoint time chasing final art.

First build:
- movement
- shooting
- collisions
- enemy interaction
- level flow

Then layer the Ubuntu City cartoon style on top of a working game.

## 20. Demo Scene Builder

I added an editor utility that can generate a playable placeholder scene for you.

### File
- [PrototypeSceneBuilder.cs](/C:/Users/noahb/UbuntuCity/Assets/Scripts/Editor/PrototypeSceneBuilder.cs#L1)

### How to use it
1. Open the project in Unity
2. Let Package Manager resolve packages
3. In the top menu, click `Ubuntu City > Build Prototype Demo Scene`
4. Open `Assets/Scenes/PrototypeScene.unity`
5. Press Play

### What it creates
- Main camera with follow script
- Global Light 2D
- Placeholder grayscale background
- Ground and platforms
- Player prefab
- Bullet prefab
- Enemy prefab
- GameManager

### Controls
- `A` / `Left Arrow` = move left
- `D` / `Right Arrow` = move right
- `Space` = jump
- `J` = shoot

## 21. Jump Troubleshooting

If the player does not jump, check these in this order:

1. `Player` has a `Rigidbody2D` and collider
2. `Ground` and platforms are on the `Ground` layer
3. `GroundCheck` exists under `Player` and is placed below the feet
4. Ground and platforms have colliders enabled
5. Unity finished recompiling scripts after import

The current movement script also has:
- a fallback ground test if `GroundCheck` is not wired correctly
- a small jump buffer
- a small coyote time window

That makes the prototype much less fragile while you build the first scene.

## 22. Cartoon Placeholder Setup

Use [FirstSceneCartoonSetup.md](/C:/Users/noahb/UbuntuCity/Assets/Docs/FirstSceneCartoonSetup.md) as the immediate next-step checklist for:
- fixing jump setup
- enabling Cinemachine follow
- building a grayscale cartoon placeholder character
- pushing the first scene toward the vintage city vibe
