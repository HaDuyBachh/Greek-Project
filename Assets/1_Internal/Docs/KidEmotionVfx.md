# Kid Emotion VFX

## Scene setup

`Kid1` in `Assets/1_Internal/Scenes/1_Main.unity` has a prebuilt
`KidEmotionVfxController`. It uses two separate anchors:

- `emotion_Main_room_root`: stable tracking point at the top of Kid1's head.
- `emotion_Main_room`: VFX visible from the overview camera.
- `emotion_Kid_Forcus`: VFX visible only while `Kid_Forcus` is following Kid1.

At `Awake`, the controller records the pre-Play world offset and world rotation
of both display anchors relative to `emotion_Main_room_root`. Every
`LateUpdate`, it applies `current root position + recorded world offset` and the
recorded world rotation. The anchors therefore move with Kid1 but do not orbit
or rotate when Kid1 turns. No camera-facing or billboard placement logic is
used; the scene pose authored before Play is the source of truth.

`kid_focus_point` keeps the old fixed position previously used by
`emotion_Main_room`. `KidFocusCameraController` and
`TelevisionFocusCameraController` reference this dedicated point, so moving an
emotion anchor cannot move the camera target or the Kid selection point.

Each anchor owns its own inactive prefab instances from
`Assets/0_external/Bitnas/Prefabs/CartoonEmotionPack/Colored`. Runtime only
activates an existing instance. It never loads a prefab, instantiates an object,
adds a component or searches the scene after Play begins.

## Emotion mapping

The controller reads `KidWaypointAnimationTester.CurrentEmotion`, so VFX follows
the same state that video effects use for Kid animation.

| State | Prebuilt variant theme |
| --- | --- |
| `Stable` | Smile, indifference, curiosity, expectation |
| `Happy` | Strong smile, wink, love, showing off |
| `Anxious` | Annoyed, bored, tired, glaring |
| `Panic` | Terrified, hurt, dizzy, angry, monster |

There are five variants for every state on each camera anchor. The controller
chooses randomly and can avoid immediately repeating the previous variant.

## Camera rules

- `Main_room` active: only a variant below `emotion_Main_room` may be visible.
- `Kid_Forcus` following Kid1: only a variant below `emotion_Kid_Forcus` may be visible.
- `Kid_Forcus` following another Kid: Kid1 focus VFX stays hidden.
- `TV_Forcus` active: both anchors are immediately hidden. This rule must be
  used by every future Kid emotion controller, so TV never shows a Kid emotion.
- If no gameplay camera is active, all VFX stay hidden.

## Inspector settings

`KidEmotionVfxController` exposes all references, both prefab pools and timing:

- `Display Duration Seconds`
- `Minimum Repeat Interval Seconds`
- `Maximum Repeat Interval Seconds`
- `Show Immediately On Camera Or Emotion Change`
- `Avoid Immediate Variant Repeat`
- `Preserve Scene Pose`: keep enabled to preserve each anchor's pre-Play world
  offset and rotation while following the head root position.

To add or replace an effect, place the prefab instance under the correct anchor
in Edit Mode and assign that existing GameObject to the matching pool. Keep the
prefab instance inactive in the saved scene.
