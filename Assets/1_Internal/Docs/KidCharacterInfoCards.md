# Kid Character Information Cards

Scene chinh `Assets/1_Internal/Scenes/1_Main.unity` co ba card thong tin dung ba
sprite trong `Assets/1_Internal/Prefab/UI/Card_Character`:

- `kid1.png` cho Kid1.
- `kid2.png` cho Kid2.
- `kid3.png` cho Kid3.

Ten nguoi hien tren card trong scene:

- Kid1: `Noah`.
- Kid2: `Ethan`.
- Kid3: `Liam`.

`Kid1/Kid2/Kid3` van duoc giu lam ID ky thuat noi bo de camera, emotion va
device controller khong mat reference; nguoi choi chi thay ten nguoi o dong
`Name` cua Character Note.

## Cau truc scene

Tat ca object duoc tao va gan reference san truoc Play:

```text
UI
+-- Kid Character Info Cards            KidCharacterInfoCards
    +-- Kid1 Character Card              Image + CanvasGroup
    |   +-- Name Value                   TextMeshProUGUI
    |   +-- Personality Value            TextMeshProUGUI
    |   +-- Mood Value                   TextMeshProUGUI
    |   +-- Special Trait Value          TextMeshProUGUI
    +-- Kid2 Character Card
    +-- Kid3 Character Card
```

Ba card cung nam tai goc duoi ben trai, anchor/pivot `(0, 0)`, cach canh trai
`28 px` va canh duoi `18 px` de can ngang day giao dien PhoneScreen. Trong scene,
ca ba card root duoc luu san
o trang thai inactive; sprite van duoc serialize truc tiep tren `Image`. Khi focus,
chi card dung Kid duoc active va hai card con lai inactive. Vi vay chung khong
chong len nhau. UI khong nhan raycast va khong chan thao tac camera/phone.

## Quy tac hien thi

- Khi `Kid_Forcus` dang theo doi Kid1, Kid2 hoac Kid3, card trung `kidId` moi
  duoc hien.
- Khi quay ve `Main_room` hoac chuyen sang `TV_Forcus`, tat ca card duoc an.
- `Mood Value` doc `VisualEmotion`, nen co the hien `Stable`, `Happy`,
  `Suspicious`, `Anxious` hoac `Panic` dung theo trang thai hien tai.
- Neu Kid dang xem thiet bi, mood noi them `/ Phone` hoac `/ TV`.
- `Hide While Phone Visible` tren Inspector cho phep an card khi mo PhoneScreen;
  scene mac dinh de tat de card van hien trong suot luc focus Kid.

## Chinh trong Inspector

Chon `UI/Kid Character Info Cards`:

- Moi phan tu `Kids` expose `Display Name`, `Personality`, `Special Trait`,
  activity/device controller va toan bo reference UI cua card.
- Nhom `Mood Text` expose text cho nam state, suffix thiet bi va checkbox noi
  trang thai thiet bi vao mood.
- Kich thuoc/vi tri card chinh truc tiep tren RectTransform cua tung
  `KidN Character Card`; scene mac dinh `720 x 720`, cach trai `28 px`, cach
  day `18 px`.
- Vi tri, font, mau va noi dung preview cua bon text chinh tren cac child
  TextMeshProUGUI. `Name`, `Personality`, `Mood` va `Special Trait` co anchor
  rieng cho tung card, can vao phan duong ke trong chinh anh Kid1/Kid2/Kid3;
  khong dung mot toa do chung de tranh text de len label in san. `Name` dung
  offset Y `0 px`; ba dong `Personality`, `Mood`, `Special Trait` dung offset Y
  `+8 px`, de chu gan duong viet nhung khong bi duong ke cat qua. Font toi da
  `44`, auto-size toi thieu `32`
  de cac gia tri dai khong bi co nho qua muc.

Hai badge `Positive/Negative` va `Watching Phone/Watching TV` duoc giu la UI
prebuilt, scale `1.25` tren RectTransform. Icon, background va text cung scale
theo mot root nen khong bi lech nhau.

`KidCharacterInfoCards` khong tao GameObject, khong `AddComponent`, khong load
sprite va khong tim object theo ten luc Play. `Awake` chi validate reference va
inactive ba card root; `LateUpdate` chon binding da serialize theo
`KidFocusCameraController.SelectedKidId`, active dung card va cap nhat text khi
state thay doi.
