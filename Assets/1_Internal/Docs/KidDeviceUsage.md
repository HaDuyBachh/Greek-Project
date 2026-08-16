# Kid Device Usage

## Component

Every Kid that can use a device must have a prebuilt
`KidDeviceUsageController`. Choose one `Usage Type` in the Inspector:

| Usage Type | Seated behavior | Enter while Kid is focused |
| --- | --- | --- |
| `Phone Only` | Ground and chair use the configured phone animations | Opens PhoneScreen |
| `Television Only` | Ground stays idle; chair uses the configured TV animation | Opens `TV_Forcus` |
| `Phone And Television` | Ground uses phone; chair alternates phone and TV | Opens only the device represented by the current phone/TV animation; does nothing in another animation |

Kid1 in `Assets/1_Internal/Scenes/1_Main.unity` is configured as
`Phone And Television`. `First Dual Chair Activity` is `Television`, so the
first chair activity uses TV and the next chair activity uses phone.

## Input

- Select/focus a Kid first.
- Press main Enter or Numpad Enter.
- `Phone Only` always opens the phone.
- `Phone And Television` opens the phone only during
  `SitGroundUsingPhone` or `SitChairUsingPhone`, and opens TV only during the
  configured TV-watching animation. A non-device animation does nothing.
- `Television Only` opens TV.
- Enter, Escape or right click closes an already open PhoneScreen according to
  the existing focus rules.

Space remains a direct phone shortcut only for Kid types that can use a phone.

## Device activity badge

The overlay Canvas has a prebuilt `Kid1 Device Activity Status` card managed by
`KidEmotionStatusIndicator`. While the pointer hovers Kid1 or `Kid_Forcus`
follows Kid1, it displays `WATCHING PHONE` in dark blue or `WATCHING TV` in dark
purple according to the animation that is actually playing. The card is hidden
while travelling, during a non-device animation, and in `TV_Forcus`.

All badge objects, text, backgrounds and references are serialized in the scene
before Play. The script only changes visibility, text and color at runtime.

The Enter handler resolves exactly one `DeviceActivity`. Opening Phone is
blocked while `TV_Forcus` owns the cameras; entering TV closes Phone before the
TV camera is enabled, so Phone and TV focus cannot overlap.

## phone_handle

The Kid component has a serialized reference to the existing child GameObject
tagged `phone_handle`. It is active only while a configured phone animation is
actually playing and the Kid is not travelling. It is disabled for TV, travel,
standing and emotional interruption animations.

No object, component or asset is created or searched by name after Play starts.
All references, animation names and device-type settings are serialized in the
scene before Play.
