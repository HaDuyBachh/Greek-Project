# Waypoint Animation Test

## Muc dich

`KidWaypointAnimationTester` duoc gan tren `Kid1` trong scene `1_Main` de cho nhan vat tu dong di qua cac waypoint va thu cac animation trong `Kid.controller`.

Component nay chi phuc vu test animation. Khi gameplay chinh dieu khien tre em, co the tat component nay trong Inspector.

## Luong hoat dong

1. Khi `Visit Waypoints In Order` bat, chon waypoint ke tiep theo thu tu Inspector
   trong cac label `walk_place`, `sit_ground` hoac `enter_sofa`.
2. Di den waypoint bang `NavMeshAgent` va animation `Walking` hoac `RunForward`.
3. Chay ngau nhien mot animation trung tinh phu hop voi loai waypoint trong khoang thoi gian da dat trong Inspector.
4. Giu hoat dong `15` giay trong scene hien tai, sau do moi xem xet waypoint ke tiep.

`sit_chair` khong duoc chon truc tiep. Khi nhan vat den `enter_sofa`, controller tim `sit_chair` gan diem vao do nhat, dat nhan vat dung vi tri va huong cua point ghe, sau do chay animation ngoi. Khi roi ghe, nhan vat duoc dua ve `enter_sofa` tuong ung roi moi tiep tuc di tren NavMesh.

## Danh sach animation mac dinh

| Loai | Tu the | Animation |
| --- | --- | --- |
| Neutral, duoc random | Di chuyen | `Walking`, `RunForward` |
| Neutral, duoc random | `walk_place` | `Breathing Idle` |
| Neutral, duoc random | `sit_ground` | `SitGround`, `SitGroundUsingPhone` |
| Neutral, duoc random | `sit_chair` | `SitChairIdle`, `SitChairUsingPhone` |
| Emotional, khong random | Dung | `Panic`, `AngryStandNormal`, `AngryStandNormal_1`, `Crying` |
| Emotional, khong random | Mat dat | `GroundPain` |
| Emotional, khong random | Ghe | `SitChairFear`, `SitChairYell` |

Tat ca danh sach tren deu duoc serialize trong component tren `Kid1`, vi vay co the them, xoa hoac doi animation ngay trong Inspector. Vong di ngau nhien chi doc cac field co ten `Neutral`; cac field `Emotional` duoc giu rieng de he cam xuc chu dong goi sau nay.

## Quy uoc waypoint

- Dat point tren hoac gan NavMesh de nhan vat co duong di den.
- Xoay `sit_ground` va `sit_chair` theo huong nhan vat can nhin khi ngoi.
- Moi `enter_sofa` nen nam gan `sit_chair` cua cung sofa. Controller ghep cap bang khoang cach gan nhat.
- Co the them bao nhieu point cung label tuy y; controller tu dong nap lai danh sach khi bat Play.

## Thong so test

- `Min/Max Action Duration`: thoi gian giu animation tai waypoint; scene dat ca
  hai bang `15` de interval khong random.
- `Visit Waypoints In Order`: bat de duyet waypoint tuan tu; tat de quay lai cach
  chon dich ngau nhien.
- `Travel Timeout`: thoi gian toi da cho mot luot di.
- `Walk/Run Speed`: toc do NavMesh tuong ung voi animation di/chay.
- Bo chon `KidWaypointAnimationTester` tren `Kid1` de dung che do test tu dong.
