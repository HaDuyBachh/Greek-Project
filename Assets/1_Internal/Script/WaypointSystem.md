# Waypoint System

## Muc dich

He waypoint dung de tao cac vi tri trong scene, dat nhan cho tung vi tri, va kich hoat hanh dong khi character di den dung vi tri do.

## Cau truc script

### `WaypointGroup`

Gan vao GameObject cha, vi du `Waypoints`.

Chuc nang:
- Sinh waypoint con theo hang ngang (`Line`) hoac vong tron (`Circle`).
- Luu danh sach cac `LabeledWaypoint` con.
- Tim waypoint bang label.
- Tim waypoint gan nhat voi mot vi tri.

Ham hay dung:

```csharp
LabeledWaypoint point = waypointGroup.GetByLabel("SitPoint");
bool found = waypointGroup.TryGetByLabel("PhonePoint", out LabeledWaypoint phonePoint);
LabeledWaypoint nearest = waypointGroup.GetNearest(transform.position);
```

### `LabeledWaypoint`

Gan tren tung waypoint con.

Du lieu:
- `Label`: ten vi tri, vi du `SitPoint`, `PhonePoint`, `TalkPoint`.
- `Arrive Radius`: ban kinh tinh la da den noi.
- `On Arrived`: event goi khi character den waypoint.
- `On Character Arrived`: event co truyen vao `GameObject` character.

Ham hay dung:

```csharp
Vector3 targetPosition = waypoint.Position;
bool isArrived = waypoint.IsInside(character.transform.position);
waypoint.Arrive(character);
```

### `WaypointArrivalDetector`

Gan vao character.

Chuc nang:
- Kiem tra character co nam trong ban kinh waypoint nao khong.
- Neu da den waypoint, goi event cua `LabeledWaypoint`.
- Co tuy chon `Trigger Only Once Per Waypoint` de tranh goi lap lien tuc khi character dung yen trong waypoint.

## Cach thiet lap trong Unity

1. Tao empty GameObject ten `Waypoints`.
2. Gan component `WaypointGroup`.
3. Chinh:
   - `Generate Mode`
   - `Amount`
   - `Spacing` hoac `Radius`
   - `Label Prefix`
4. Bam menu ba cham tren component `WaypointGroup`.
5. Chon `Generate Waypoints`.
6. Chon tung waypoint con va sua `Label` theo y muon.
7. Gan `WaypointArrivalDetector` vao character.
8. Keo object `Waypoints` vao field `Waypoint Group`.
9. Gan action trong `On Arrived` cua tung waypoint.

## Tao waypoint trong Scene Editor

Chon GameObject co component `WaypointGroup`.

Trong Inspector se co cac nut:
- `Generate Waypoints`: sinh nhieu waypoint theo cau hinh Line/Circle.
- `Create Waypoint At Group Position`: tao 1 waypoint tai vi tri cua group.
- `Scene Placement: Ctrl + Left Click`: bat/tat che do tao waypoint bang chuot trong Scene View.

Khi `Scene Placement` dang bat:

1. Dua chuot vao Scene View.
2. Giu `Ctrl`.
3. Bam chuot trai vao mat dat hoac object co collider.
4. Waypoint con se duoc tao tai vi tri raycast hit.

Neu raycast khong cham collider nao, tool se dat waypoint tren mat phang ngang cung do cao voi `WaypointGroup`.

## Vi du label

```text
SitPoint
PhonePoint
TalkPoint
LookAtWindowPoint
ExitPoint
```

## Huong mo rong tiep theo

Neu sau nay can character den waypoint roi lam hanh dong rieng, co the tao script rieng, vi du:

```csharp
public class CharacterActionPlayer : MonoBehaviour
{
    public void PlaySitAction()
    {
        // Play animation sitting.
    }

    public void PlayPhoneAction()
    {
        // Play animation using phone.
    }
}
```

Sau do gan function vao `On Arrived` trong Inspector cua waypoint tuong ung.

Neu sau nay can dieu khien character di den waypoint theo label, nen tao them script rieng, vi du `CharacterWaypointMover`, va cho no goi:

```csharp
LabeledWaypoint target = waypointGroup.GetByLabel("SitPoint");
```

roi di chuyen character den `target.Position`.
