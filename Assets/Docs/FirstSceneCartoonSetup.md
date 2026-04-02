# First Scene Cartoon Setup

This is the fastest path to a working first scene that still points toward the vintage cartoon look you want.

## 1. Let Unity Resolve Packages

Open the project and let Unity finish importing packages.

The project now includes:
- URP 2D
- 2D Animation
- PSD Importer
- Aseprite Importer
- Tilemap tools
- Cinemachine

Use those to support the look later, but keep the prototype simple right now.

## 2. Fix Jump First

On the `Player` object, verify:
- `Rigidbody2D`
  - `Body Type`: `Dynamic`
  - `Gravity Scale`: `3`
  - `Collision Detection`: `Continuous`
  - `Interpolate`: `Interpolate`
  - `Freeze Rotation Z`: enabled
- `CapsuleCollider2D` or `BoxCollider2D`
- `PlayerMovement`

Under `Player`, create:
- `GroundCheck`
  - place it slightly below the feet, around `Y = -0.72`
- `FirePoint`
  - place it slightly in front of the body, around `X = 0.65`, `Y = 0.05`

On the level objects:
- Put `Ground` and all platforms on the `Ground` layer
- Keep their colliders enabled

The movement script now auto-finds `GroundCheck` and can still detect ground even if the reference was missed, but the setup above is still the correct scene setup.

## 3. Add Cinemachine Camera Follow

Once the package finishes importing:

1. Select `Main Camera`
2. Remove `CameraFollow` if you want to switch fully to Cinemachine
3. Add `CinemachineBrain`
4. Create a Cinemachine camera from the Unity menu
5. Set its `Follow` target to `Player`

Use a simple follow setup first:
- Follow target: `Player`
- Damping: low to medium
- Dead zone: small

That gives you a cleaner camera feel immediately and makes boss framing easier later.

## 4. Make a Cartoon Placeholder Character

Do not chase final art yet. Build a stylized placeholder first.

Use a simple shape language:
- Large pie-cut eyes
- White face area
- Black body or dark torso
- White gloves
- Thin arms and legs
- Slightly oversized shoes

For the first prototype sprite:
- Draw Luca as a simple grayscale character
- Keep outlines thick
- Use only black, white, and 1-2 gray tones
- Avoid tiny details

You can make the first placeholder in:
- Aseprite
- Photoshop
- Krita
- even simple PNG blocks if needed

Import it as a sprite and replace the current placeholder on `Player`.

## 5. Quick Environment Style Pass

To get closer to the vibe without full art production:

- Change the camera background to a slightly warm gray
- Use grayscale blocks for platforms
- Add one crooked sign
- Add one bent lamp post
- Add one warped storefront silhouette in the background

That is enough to signal the visual direction without slowing down development.

## 6. First Scene Goal

By the end of this setup, the scene should do this:
- Player moves left and right
- Player jumps consistently
- Camera follows the player
- Player shoots with `J`
- Bullets hit an enemy
- Scene reads as grayscale cartoon prototype, not generic Unity boxes

## 7. What To Do After This

Once the first scene is stable:
- replace the player placeholder with a rough Luca sprite
- add idle and run animation
- add one enemy animation
- block out one ruined city street scene
- keep everything grayscale until gameplay is stable
