# Kid Help Cards

## Scene layout

`1_Main.unity` contains a prebuilt `UI/Kid Help Cards` hierarchy. Its `Help Card Area`
occupies the bottom of `Kid_Forcus`, immediately to the right of the Character Note.
All eight PNG assets from `Assets/1_Internal/Prefab/UI/Help_Card` remain assigned to
prebuilt `Image` objects in the scene, but only the five actionable cards are active:
`1`, `3`, `5`, `6`, and `7`. Cards `2`, `4`, and `8` remain prebuilt and inactive.

The cards are bottom anchored and hidden when Kid focus first opens. Press `T` to
toggle their headers. At rest, `Collapsed Anchored Y` leaves only the card header
peeking above the bottom edge. Hovering a visible header moves that card to
`Expanded Anchored Y` with `Hover Smooth Time`; leaving it returns the card to its
collapsed position. These three values are editable on `Kid Help Cards` in the
Inspector, together with `Reveal Key` and the focus-reset behavior.

The area is visible only while a Kid is selected and `Kid_Forcus` is active. It is
hidden for `Main_room`, `TV_Forcus`, and while the selected Kid's phone UI is open.

## Card actions

The `Cards` array on `KidHelpCardController` owns every prebuilt reference and action
setting:

- `1_DeepBreath`: nearest available `walk_place`, then `Breathing Idle` for 20 seconds.
- `2_TakeItOut`: visual/information card only.
- `3_TakeScreenBreak`: nearest available `walk_place`, then a calm idle for 20 seconds.
- `4_NameEmotion`: visual/information card only.
- `5_QuitePlace`: nearest available `walk_place`, then a calm idle for 25 seconds.
- `6_HugFavorite`: `hug_place`, then the controller's `PettingAnimal` state for 18 seconds.
- `7_RelaxingMusic`: nearest available `walk_place`, then one of `Dance01`-`Dance04`
  for 20 seconds.
- `8_DrawFeeling`: visual/information card only.

Clicking an actionable card affects only the Kid currently selected by
`KidFocusCameraController`. `KidWaypointAnimationTester` reserves an unoccupied
waypoint, ends phone/TV activity, travels there, plays the configured state for the
configured duration, releases the reservation, and resumes its normal activity loop.
The guided action continues while the Kid camera is focused.

Each actionable card also owns a separate, inactive `Applied Preview` Image in the
scene. After a successful click, this prebuilt preview appears in the center of the
screen, scales from `Preview Start Scale` to `Preview End Scale`, then fades out over
`Preview Duration Seconds`. No preview is cloned or loaded at runtime.

As soon as an actionable card is accepted, the selected Kid becomes `Happy` and the
recovery is also confirmed again after the action completes. Brainrot exposure,
suspicion, `0/2` unresolved harmful progress, and progress toward the normal-video
reset are cleared immediately, so the previous tracked video cannot reapply the old
negative state while the Kid is travelling. The card also starts a random harmful-
content protection window between `Minimum Help Protection Seconds = 5` and
`Maximum Help Protection Seconds = 10`. During that window Brainrot/Horror cannot
enable Suspicious, increment exposure/counters, or change the Kid's emotion. If a
harmful video is still current when protection expires, its consumption starts again
from zero rather than inheriting hidden progress from the protected interval.

## Pre-Play requirement

No card, component, sprite, animation, or waypoint is created or loaded after Play.
`Awake` only validates serialized references, binds listeners to the existing Buttons,
and toggles the existing card area/previews. Informational cards remain prebuilt and
inactive; their disabled tint is white so the source art is not dimmed if re-enabled.
