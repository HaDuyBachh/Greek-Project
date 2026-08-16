# Kid Sequential Video And Status

## Scene setup

`Assets/1_Internal/Scenes/1_Main.unity` contains three prebuilt sequential-viewer
objects under `Controller`, one for each Kid. Their `KidFeedCycleController`
components are retained for GUID compatibility, but the old random feed-cycle
behavior has been replaced completely.

All references are assigned before Play:

- `Video Library`: `PhoneVideoLibrary.asset`.
- `Activity Controller`: that viewer's own Kid activity controller.
- `Kid Focus Controller`: `Kid_Forcus Controller`.
- `First Video Index`: Kid1 `0`, Kid2 `1`, Kid3 `2`; `Loop Library = true`.
- `Use Video Metadata Duration = true`, fallback `6` seconds.
- Normal video never enables `Suspicious`. Only a tracked `Brainrot` or `Horror`
  video keeps looping under `Suspicious` for `8-9` seconds, giving the player a
  longer intervention window.
- Brainrot/Horror must be hidden before that window ends. Scene threshold is
  fixed at `2-2`: the first missed harmful video records `1/2`; the second
  records `2/2` and changes the Kid to `Panic`.

Each controller advances only while its Kid is at a seated activity location.
It pauses only while `Kid_Forcus` is following that Kid. No video, sprite, component or UI
object is loaded or created after Play starts.

## Device-specific hidden videos

Each `KidFeedCycleController` has prebuilt references to the recommendation feed
used by that Kid. `Skip Videos Hidden On Current Device` is enabled in the scene:

- While watching Phone, the Kid's own `KidFeedCycleController` phone suppression
  state is checked. Noah and Ethan do not share this list.
- While watching TV, only `TelevisionVideoFeedUI` hidden state is checked.
- Kid2 has both references, but the current device selects exactly one source;
  Phone and TV blacklist data remain independent.
- If `Don't recommend` hides the current video during suspicion or consumption,
  its suspicion/watch progress is cancelled before an effect can be applied and
  the viewer advances to the next eligible library entry.
- Turning the checkbox off in Inspector restores unfiltered library traversal.

Scene references are Kid1 -> Phone, Kid2 -> Phone + TV, Kid3 -> TV. Runtime does
not search for either feed by name and does not add a bridge/component.

## Deterministic content effects

Each Phone-capable Kid owns a different six-entry `PhoneVisibleVideos` list on
its own `KidFeedCycleController`. `PhoneVideoFeedUI` is only the presenter for
the currently selected owner; it is not the source of shared feed state.
TV Kids consume only `TelevisionVideoFeedUI.CurrentBroadcastVideo`: this is the
exact frame sequence currently visible on the television, not another card in
the six-card recommendation feed. A feed/broadcast change cancels old progress
before the new entry is evaluated. A completed TV broadcast is evaluated only
once and is not counted repeatedly while its frames loop. The library asset is
only the prebuilt fallback when the current device feed reference is unavailable.

- `Horror` and `Brainrot`: if the device-specific `Don't recommend` list still
  does not contain the video when its `8-9` second Suspicious window ends, the
  harmful counter advances once. Hiding it at any point before completion
  cancels progress and does not increment the counter.
- The second unresolved harmful video changes the Kid to `Panic`. The threshold,
  harmful Suspicious min/max and looping behavior remain Inspector-adjustable.
- Eight fully watched Normal videos (cumulative, not necessarily consecutive)
  clear harmful progress back to `0/2`. A Normal TV broadcast counts after `9`
  seconds, before the TV's `10` second rotation can replace it.
- `Normal`: removes one exposure and builds deterministic recovery. Two Normal
  videos recover one negative level, and two further Normal videos change
  `Stable` to `Happy`.

## Feed content ratio

Each Kid Phone and TV independently select each six-card feed with an approximate
`3 Normal : 1 Brainrot/Horror` target and a hard maximum of `2` harmful cards.
The selector still prioritizes videos not present in the previous feed and
never reintroduces that specific phone/device's hidden videos. Phone ratio and
reset fields are serialized separately on Noah and Ethan's sequential viewer
components; TV fields remain on its prebuilt feed component in `1_Main.unity`.

Each Phone replaces all six of its own cards after `5` seconds whenever that
Kid's PhoneScreen is closed. Opening Noah's phone pauses only Noah's timer;
Ethan's separate timer/feed/blacklist continues independently. TV keeps the
same active broadcast looping for `10` seconds, then
rotates the full TV feed only while TV is not focused. No open device UI is
replaced underneath the player.

When PhoneScreen opens, its serialized two-owner mapping resolves the selected
Kid and immediately opens that owner's `CurrentPhoneVideo` at the Kid's current
playback progress. This makes the exact Suspicious clip visible for intervention.

## Position decision interval

All three Kids have both `Min Action Duration` and `Max Action Duration` set to
`15`, so a new position is considered only after 15 seconds at the current
activity. `Visit Waypoints In Order` is enabled. `First Activity Waypoint Index`
is `0/1/2` for Kid1/Kid2/Kid3 to keep their ordered routes offset.

## Positive/negative badge

The overlay Canvas contains the prebuilt object `Kid1 Emotion Status`:

- rounded `RoundedRectGraphic` background;
- white `arrow-circle-up` for positive / `arrow-circle-down` for negative;
- English `POSITIVE` / `NEGATIVE` text;
- `KidEmotionStatusIndicator` with every scene/UI reference serialized.

The existing UI object is shared by a serialized three-Kid binding array. It is
visible only for the Kid currently hovered in `Main_room` or followed by
`Kid_Forcus`, so status cards never overlap. It is hidden in `TV_Forcus`. `Stable` and
`Happy` are positive with a dark-green background; `Anxious` and `Panic` are
negative with a dark-red background. Text and icon are always white.

A second prebuilt card below the emotion badge displays `WATCHING PHONE` or
`WATCHING TV` from `KidDeviceUsageController`, so the player can see which
device Enter will open. It follows the same hover/focus and TV-hidden rules.

`Suspicious` is a VFX-only temporary override. It uses the prebuilt Indifference,
Curious and Expectant VFX on both camera anchors without changing the stored
positive/negative state. The status badge always reads only `POSITIVE` or
`NEGATIVE` from the Kid's stored emotion. Suspicious VFX restarts immediately
after each configured display duration, so it has no hidden interval while the
harmful video remains unresolved. Phone and TV frame players loop that same
tracked video rather than advancing when its frame sequence reaches the end.
`SetSuspicionVisual` also validates the tracked video's `contentEffect`, so a
Normal entry cannot enable SUS even if another call path requests the visual.
