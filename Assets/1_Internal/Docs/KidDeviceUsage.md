# Kid Device Usage

## Component

Every Kid that can use a device must have a prebuilt
`KidDeviceUsageController`. Choose one `Usage Type` in the Inspector:

| Usage Type | Seated behavior | Enter while Kid is focused |
| --- | --- | --- |
| `Phone Only` | Ground and chair use the configured phone animations | Opens PhoneScreen |
| `Television Only` | Every ground/chair sitting activity watches TV | Opens `TV_Forcus` |
| `Phone And Television` | Ground watches TV; chair alternates phone and TV | Opens the device owned by the current sitting activity |

`Assets/1_Internal/Scenes/1_Main.unity` has three prebuilt profiles:

| Kid | Usage Type | Initial ordered waypoint | Device rule |
| --- | --- | --- | --- |
| `Kid1` | `Phone Only` | `0` | Enter opens PhoneScreen |
| `Kid2` | `Phone And Television` | `1` | Ground watches TV; chair alternates TV/phone, starting with TV |
| `Kid3` | `Television Only` | `2` | Enter opens `TV_Forcus`; no phone handle is required |

The different `First Activity Waypoint Index` values keep the three ordered
activity cycles offset instead of sending every Kid to the same first waypoint.

## Input

- Select/focus a Kid first.
- Clicking a Kid always enters that Kid's `Kid_Forcus`, including while the Kid
  is watching TV. Only a direct click on the television can bypass Kid focus.
- Press main Enter or Numpad Enter.
- `Phone Only` always opens the phone.
- `Phone And Television` opens the device assigned when the Kid entered the
  sitting activity. A reaction animation does not cancel Phone or TV ownership;
  a genuinely non-device activity still does nothing.
- `Television Only` opens TV.
- Enter, Escape or right click closes an already open PhoneScreen according to
  the existing focus rules.

Space remains a direct phone shortcut only for Kid types that can use a phone.

## Device activity badge

The overlay Canvas has one prebuilt device-activity card managed by
`KidEmotionStatusIndicator`. Its serialized `Kids` array contains Kid1, Kid2 and
Kid3. It follows only the Kid currently hovered or selected, so copied cards
cannot overlap. It displays `WATCHING PHONE` in dark blue or `WATCHING TV` in
dark purple according to the current serialized device activity. The card is
hidden while travelling, at a non-viewing location, and in `TV_Forcus`.

All badge objects, text, backgrounds and references are serialized in the scene
before Play. The script only changes visibility, text and color at runtime.

The Enter handler resolves exactly one `DeviceActivity`. Opening Phone is
blocked while `TV_Forcus` owns the cameras; entering TV closes Phone before the
TV camera is enabled, so Phone and TV focus cannot overlap.

TV activity is authoritative at every completed sitting location. A TV-capable
Kid also watches from `sit_ground` when `Watch Television When Sitting On Ground`
is enabled in the Inspector (enabled for all three scene profiles; ignored by
Phone-only Kid1). The Kid turns toward the serialized television target during
the final part of the approach and keeps that facing while seated. Emotional Sit
animations do not cancel the TV activity.

Noah and Ethan each own a separate serialized Phone feed session on their own
`KidFeedCycleController`: visible cards, reset timer, current video and
Don't-recommend blacklist are independent. The single camera-facing PhoneScreen
only presents the selected Kid's session and opens that Kid's current video.

## phone_handle

Every phone-capable Kid has its own serialized child GameObject tagged
`phone_handle`. Kid1 keeps its original model; Kid2 has a separate prebuilt
model attached to `RightHand.Dummy`. Each handle is active only while that Kid's
current sitting activity owns the Phone and the Kid is not travelling.
Kid3 is TV-only, so its Phone Handle reference is intentionally `None` and does
not generate a validation error.

No object, component or asset is created or searched by name after Play starts.
All references, animation names and device-type settings are serialized in the
scene before Play.
