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
- Chuot phai hoac phim `Esc`: quay lai `Main_room`.
- Click tren UI khong lam thay doi camera.

Trong camera tong:

- Cuon chuot hoac pinch hai ngon de phong to/thu nho.
- Khi dang phong to, giu chuot trai va keo hoac cham-keo mot ngon de di chuyen tam nhin.
- Khi thu ve muc rong nhat, camera tu dua tam nhin ve vi tri goc.

`Main_room` lay transform va FOV trong scene tai luc khoi dong lam gioi han rong nhat. Zoom-out khong the vuot FOV nay; khi ve muc rong nhat, pan offset duoc xoa va camera tro ve chinh xac vi tri/rotation ban dau.

Vung click duoc tinh quanh vi tri man hinh cua `focus_point`, khong phu thuoc vao collider cua model.
Vung hover dung chung cach tinh nay va chi cho phep mot Kid hien outline tai mot thoi diem.

## Cau truc scene

Component `KidFocusCameraController` duoc gan vao object rieng `Controller/CameraController` de cac tham so camera co the duoc dieu chinh tap trung trong Inspector. Object `Cameras` chi dung lam parent cua hai camera:

- `Overview Camera`: `Main_room`
- `Focus Camera`: `Kid_Forcus`
- `Chat Ui Controller`: `Controller/ChatUIFollowController`
- `Kids`: danh sach Kid co the focus, moi phan tu gom `kidId`, `kidRoot`, `focusPoint`
- `Outline`: component QuickOutline tren root cua Kid

Khi them Kid moi, them Kid vao `ChatUIFollowController.kids`; camera controller se tu dong lay Kid do. Kid moi can co child `focus_point` voi tag `focus`, dat o tam mat/nguc tuy khung hinh mong muon. Neu root Kid chua co `Outline`, controller se tu them va tat no cho den khi nguoi choi re chuot vao Kid.

Vi tri focus mac dinh duoc tinh tu `focus_point.forward`: camera nam phia truoc Kid va luon quay nguoc lai nhin vao `focus_point`. Goc orbit cua nguoi choi duoc giu theo huong cua Kid khi Kid xoay hoac di chuyen.

Project hien khong co package Cinemachine. Orbit, pan va zoom duoc xu ly boi `KidFocusCameraController` de tranh them dependency trong khi van giu duoc chuyen dong damping muot.

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
