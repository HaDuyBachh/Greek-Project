# Kid Sequential Video And Status

## Scene setup

`Assets/1_Internal/Scenes/1_Main.unity` contains the prebuilt object
`Controller/Kid Sequential Video Viewer`. Its `KidFeedCycleController` component
is retained for GUID compatibility, but its old random feed-cycle behavior has
been replaced completely.

All references are assigned before Play:

- `Video Library`: `PhoneVideoLibrary.asset`.
- `Activity Controller`: the `KidWaypointAnimationTester` on Kid1.
- `Kid Focus Controller`: `Kid_Forcus Controller`.
- `First Video Index = 0`, `Loop Library = true`.
- `Use Video Metadata Duration = true`, fallback `6` seconds.
- Every video begins with a `Suspicious` preview for a random `2-4` seconds.
- Horror must then be consumed for `3` seconds before it can apply `Panic`.

The controller advances only while Kid1 is at a seated activity location. It
pauses while `Kid_Forcus` is following Kid1. No video, sprite, component or UI
object is loaded or created after Play starts.

## Deterministic content effects

Videos are read in the exact order stored in `VideoLibraryData.Videos`.

- `Horror`: first shows the pre-watch suspicion VFX, then switches to `Panic`
  only after three seconds of actual consumption.
- `Brainrot`: adds one exposure after the video completes. At three exposures,
  Kid1 becomes `Anxious`; the threshold is Inspector-adjustable.
- `Normal`: removes one exposure and builds deterministic recovery. Two Normal
  videos recover one negative level, and two further Normal videos change
  `Stable` to `Happy`.

## Position decision interval

Kid1 has both `Min Action Duration` and `Max Action Duration` set to `15`, so a
new position is considered only after 15 seconds at the current activity.
`Visit Waypoints In Order` is enabled, so destinations follow the serialized
waypoint order rather than random selection.

## Positive/negative badge

The overlay Canvas contains the prebuilt object `Kid1 Emotion Status`:

- rounded `RoundedRectGraphic` background;
- white `arrow-circle-up` for positive / `arrow-circle-down` for negative;
- English `POSITIVE` / `NEGATIVE` text;
- `KidEmotionStatusIndicator` with every scene/UI reference serialized.

The badge is visible only while the pointer hovers Kid1 in `Main_room`, or while
`Kid_Forcus` is following Kid1. It is hidden in `TV_Forcus`. `Stable` and
`Happy` are positive with a dark-green background; `Anxious` and `Panic` are
negative with a dark-red background. Text and icon are always white.

A second prebuilt card below the emotion badge displays `WATCHING PHONE` or
`WATCHING TV` from `KidDeviceUsageController`, so the player can see which
device Enter will open. It follows the same hover/focus and TV-hidden rules.

`Suspicious` is a temporary visual override only. It uses the prebuilt
Indifference, Curious and Expectant VFX on both camera anchors without changing
the Kid's stored positive/negative state or its status badge.
