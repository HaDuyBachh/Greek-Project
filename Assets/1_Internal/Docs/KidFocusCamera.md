# Kid Focus Camera

## Muc tieu

- `Main_room` la camera tong de nguoi choi quan sat toan phong.
- `Kid_Forcus` la camera can canh, bam theo mot Kid duoc nguoi choi chon.
- Moi Kid dung `focus_point` (tag `focus`) lam tam khung hinh.
- Chat UI luon tinh vi tri theo camera dang hien thi.

## Tuong tac

- Click gan Kid tren man hinh: chuyen sang `Kid_Forcus` va theo doi Kid do.
- Re chuot gan Kid: bat `Outline`; roi chuot: tat `Outline`.
- Khi dang focus, giu chuot trai va keo hoac cham-keo de xoay camera quanh Kid.
- Khi dang focus, nhan `Space` de dua `PhoneScreen` len; nhan lan nua de ha xuong.
- Click mot Kid khac dang nhin thay: doi muc tieu focus.
- Khi focus mot Kid, activity/animation ngau nhien cua Kid do tam dung. Neu Kid
  dang di chuyen, Kid van den waypoint hien tai, doi sang animation dung/ngoi tai
  waypoint, roi moi dung. Khi ve camera tong, activity tiep tuc.
- Chuot phai hoac phim `Esc`: quay lai `Main_room`.
- Click tren UI khong lam thay doi camera.

Trong camera tong:

- Cuon chuot hoac pinch hai ngon de phong to/thu nho.
- Khi dang phong to, giu chuot trai va keo hoac cham-keo mot ngon de di chuyen tam nhin.
- Khi thu ve muc rong nhat, camera tu dua tam nhin ve vi tri goc.

### Chinh zoom `Main_room` tren Inspector

Chon `Controller/CameraController/Main_room Controller`, sau do chinh component `MainRoomCameraController`:

- `Minimum Fov`: muc phong to toi da; so cang nho thi camera cang zoom gan.
- `Wheel Zoom Sensitivity`: he so quy doi input cuon chuot sang FOV.
- `Wheel Zoom Speed Multiplier`: toc do zoom tong; so cang lon thi zoom cang nhanh.
- `Pinch Zoom Sensitivity`: toc do zoom khi pinch hai ngon.
- `Zoom Smooth Time`: do tre khi camera tien den FOV dich; so cang nho thi phan hoi cang nhanh, dat `0` de zoom ngay. Scene hien dat `0.15` giay de zoom muot.
- `Pan Smooth Time`: chi dieu khien do muot cua pan, khong lam cham zoom. Scene hien dat `0.05` giay de thao tac keo bam tay, it quan tinh.
- `Limit To Original View`: khoa zoom-out va pan trong footprint ma `Main_room` nhin thay truoc khi Play.
- `Navigation Plane Y`: do cao world-space cua mat san dung de tinh footprint; scene hien dung `Y = 0`.

Tat ca component va reference camera phai duoc gan san trong scene truoc khi Play. Controller khong tu them component, khong tu nap asset va khong tu gan vao object luc runtime.

`Main_room` lay transform, FOV va footprint tren `Navigation Plane Y` trong scene tai luc khoi dong lam gioi han goc. Zoom-out khong the vuot FOV nay. Khi zoom vao, khoang pan hop le duoc co theo footprint con lai; camera khong duoc keo de khung hinh vuot ra ngoai footprint goc. Khi ve muc rong nhat, pan offset duoc xoa va camera tro ve chinh xac vi tri/rotation ban dau.

Vung click duoc tinh quanh vi tri man hinh cua `focus_point`, khong phu thuoc vao collider cua model.
Vung hover dung chung cach tinh nay va chi cho phep mot Kid hien outline tai mot thoi diem.

## Cau truc scene

Ba camera duoc tach thanh ba GameObject quan ly rieng trong scene:

```text
Controller
+-- CameraController
    +-- Main_room Controller     MainRoomCameraController
    +-- Kid_Forcus Controller    KidFocusCameraController
    +-- TV_Forcus Controller     TelevisionFocusCameraController
```

`Main_room Controller` chi quan ly zoom, pan va collision cua camera tong.
`Kid_Forcus Controller` chi quan ly chon Kid, orbit, collision camera focus va
PhoneScreen. `TV_Forcus Controller` chi quan ly chon TV/Kid dang xem TV, camera
TV va quyen raycast cua TV UI. Chuyen camera khong reset video TV dang phat;
`TelevisionVideoFeedUI` tu quan ly broadcast 10 FPS va rotation. Object `Cameras` chi dung lam parent transform
cua ba camera that:

- `MainRoomCameraController > Controlled Camera`: `Main_room`
- `KidFocusCameraController > Main Room Controller`: `Main_room Controller`
- `KidFocusCameraController > Focus Camera`: `Kid_Forcus`
- `TelevisionFocusCameraController > Television Camera`: `TV_Forcus`
- `TelevisionFocusCameraController > Television Canvas`: `TV LED 30¨/Screen`
- `Chat Ui Controller`: `Controller/ChatUIFollowController`
- `Kids`: danh sach Kid co the focus, moi phan tu gom `kidId`, `kidRoot`, `focusPoint`
- `Outline`: component QuickOutline tren root cua Kid

Khi them Kid moi, gan Kid do vao ca `ChatUIFollowController.kids` va danh sach `Kids` tren `Kid_Forcus Controller` truoc khi Play. Kid moi can co child `focus_point` voi tag `focus`, dat o tam mat/nguc tuy khung hinh mong muon, va component `Outline` duoc gan san tren root Kid.

Vi tri focus mac dinh duoc tinh tu `focus_point.forward`: camera nam phia truoc Kid va luon quay nguoc lai nhin vao `focus_point`. Goc orbit cua nguoi choi duoc giu theo huong cua Kid khi Kid xoay hoac di chuyen.

Project hien khong co package Cinemachine. Orbit duoc xu ly boi `KidFocusCameraController`; pan va zoom duoc xu ly rieng boi `MainRoomCameraController`.

## Camera Collision

- Layer `CameraCollision` duoc danh rieng cho tuong.
- Khi khoi dong, collider ben duoi `Walls_01` tu duoc chuyen sang layer nay.
- Ca `Main_room` va `Kid_Forcus` dung SphereCast ban kinh `0.12` va chua khoang dem `0.05` voi tuong.
- Camera focus cast tu `focus_point` den vi tri orbit mong muon, nen tuong nam giua Kid va camera se keo camera vao phia trong phong.
- Camera tong cast tren tung buoc pan, nen khong di xuyen qua collider tuong.

## Phone Screen

`PhoneScreen` la child cua `Kid_Forcus`, vi vay no luon nam trong khung hinh camera focus. Controller chi thay doi local Y:

- An: `Y = -1`
- Hien: `Y = 0`
- `Phone Slide Smooth Time`: `0.25` giay

Khi quay ve `Main_room` hoac doi sang Kid khac, `PhoneScreen` tu ha xuong. Cac he thong UI khac co the goi `SetPhoneScreenVisible(bool)` thay cho phim `Space`.

Reference `Phone Screen` tren `Kid_Forcus Controller` phai tro truc tiep den `Kid_Forcus/PhoneScreen`. `PhoneScreen.Canvas > World Camera` phai tro den camera `Kid_Forcus`; ca hai reference nay duoc luu san trong scene. Neu `Phone Screen` bi mat reference, UI co the van nam trong khung hinh theo transform nhung controller se coi phone dang dong va `PhoneVideoFeedUI` se tat `VideoPlayerView` ngay sau khi card duoc click.
