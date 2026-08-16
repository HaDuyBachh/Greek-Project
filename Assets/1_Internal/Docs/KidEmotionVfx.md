# Kid Emotion VFX

## Scene setup

`Kid1`, `Kid2` and `Kid3` in `Assets/1_Internal/Scenes/1_Main.unity` each have an
independent prebuilt `KidEmotionVfxController`. Every controller uses that Kid's
own two display anchors and head root:

- `emotion_Main_room_root`: stable tracking point at the top of that Kid's head.
- `emotion_Main_room`: VFX visible from the overview camera.
- `emotion_Kid_Forcus`: VFX visible only while `Kid_Forcus` follows that Kid.

At `Awake`, each controller records the pre-Play world offset and world rotation
of its own display anchors relative to its `emotion_Main_room_root`. Every
`LateUpdate`, it applies `current root position + recorded world offset` and the
recorded world rotation. The anchors therefore move with their Kid but do not
orbit or rotate when that Kid turns. No camera-facing or billboard placement logic is
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
| `Suspicious` | Indifference, curiosity, expectation before consuming a video |

The four persistent states have five variants on each camera anchor. The
temporary `Suspicious` state reuses three prebuilt suitable variants on each
anchor. The controller chooses randomly and can avoid immediately repeating
the previous variant.

## Camera rules

- `Main_room` active: only a variant below `emotion_Main_room` may be visible.
- `Kid_Forcus` following one Kid: only that Kid's variant below
  `emotion_Kid_Forcus` may be visible; the other focus pools stay hidden.
- `TV_Forcus` active: both anchors are immediately hidden. This rule must be
  used by all three Kid emotion controllers, so TV never shows any Kid emotion.
- If no gameplay camera is active, all VFX stay hidden.

The status badge uses white arrows and text on dark green `POSITIVE` or dark
red `NEGATIVE` backgrounds. Its text region is wide enough to keep the full
`NEGATIVE` label aligned inside the badge without clipping.

## Inspector settings

`KidEmotionVfxController` exposes all references, both prefab pools and timing:

- `Display Duration Seconds`
- `Positive Minimum/Maximum Repeat Interval Seconds`: scene mac dinh `5-7`
  giay, nen trang thai `Stable` hien VFX cung nhip voi `Happy`.
- `Happy Minimum/Maximum Repeat Interval Seconds`: scene mac dinh `5-7` giay,
  nen trang thai vui ve hien VFX thuong xuyen hon `Stable`.
- `Negative Minimum/Maximum Repeat Interval Seconds`: scene mac dinh `3-5`
  giay, nen bieu cam xau xuat hien day hon.
- `Show Immediately When Negative`: bat; emotion xau moi hien phan ung ngay.
- `Show Immediately When Positive`: tat; emotion on dinh doi theo interval.
- `Show Immediately When Suspicious`: bat; dau hoi/cho doi hien ngay truoc khi
  bo dem tieu thu video bat dau.
- `Avoid Immediate Variant Repeat`
- `Preserve Scene Pose`: keep enabled to preserve each anchor's pre-Play world
  offset and rotation while following the head root position.

To add or replace an effect, place the prefab instance under the correct anchor
in Edit Mode and assign that existing GameObject to the matching pool. Keep the
prefab instance inactive in the saved scene.
