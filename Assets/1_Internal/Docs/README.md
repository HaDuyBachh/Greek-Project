# Phone UI - Scene Baseline

Tai lieu nay ghi lai cau truc phone UI trong scene
`Assets/1_Internal/Scenes/1_Main.unity` de lam moc cho cac chuc nang tiep theo.

## Huong trien khai da chot

- Khong con model dien thoai 3D.
- Khong dung RenderTexture, RawImage, camera overlay hoac presenter trung gian.
- Chi `PhoneScreen` va UI con cua no duoc hien thi.
- UI moi phai duoc them ben trong `PhoneScreen/ScreenMask`.

## Cau truc scene

```text
Kid_Forcus                         Camera
+-- PhoneScreen                    World Space Canvas, active
    +-- ScreenMask                 Rounded mask + UI root
        +-- VideoFeedTemplate      UTube template
```

`PhoneScreen` la child truc tiep cua `Kid_Forcus`. Cac object `Phone` va
`PhoneModel` da duoc xoa khoi scene.

### PhoneScreen

- `Canvas.renderMode = WorldSpace`.
- Co `CanvasScaler` va `GraphicRaycaster`.
- Rect size hien tai la `(0.218, 0.44617)`.
- Sorting order hien tai la `100`.
- Local X da bao gom offset cu cua object `Phone`, vi vay UI van giu vi tri da
  can truoc do.

### ScreenMask

- Stretch theo toan bo `PhoneScreen`.
- Dung `RoundedRectGraphic` voi `radiusRatio = 0.106`.
- Co `Mask`, hien mask graphic va clip tat ca child vuot khoi bon goc.
- Co component `PhoneVideoFeedUI`.
- Moi UI con phai nam trong node nay de nhan rounded mask.

## VideoFeedTemplate

`PhoneVideoFeedUI` dang sinh template version `26`:

```text
VideoFeedTemplate
+-- Background
+-- PhoneInputBlocker
+-- Header                         UTube logo + Search
+-- CategoryBar                    All, Gaming, Live, Music
+-- VideoScroll
|   +-- Viewport
|       +-- Content
|           +-- Video 01
|           +-- Video 02
|           +-- Video 03
|           +-- Video 04
|           +-- ... Video 12
+-- BottomNavigation
+-- VideoPlayerView               Viewer chi tiet, duoc bind tu scene
+-- VideoOptionsBackdrop          Lop mo runtime, click de dong menu
+-- VideoOptionsPanel             Hai lua chon can thiep video
```

- Text su dung TextMeshPro.
- `VideoScroll` dung `ScrollRect` theo chieu doc.
- `PhoneInputBlocker` nhan raycast tren toan man hinh de click khong xuyen xuong
  scene.
- Cac thong so card, thumbnail, spacing, avatar va font duoc expose tren
  Inspector cua `PhoneVideoFeedUI`.
- `Rebuild()` co the ghi de cac child sua tay trong `VideoFeedTemplate`.
- Template khong tu rebuild trong Edit Mode hoac khi vao Play. Muon tao lai, chon `ScreenMask`
  va bam `Rebuild Video Feed` tren Inspector.
- Khi Play, `PhoneVideoFeedUI` chi bind lai cac card trong `Content` tu
  `Data/ContentItems/PhoneVideoLibrary.asset`; no khong rebuild toan bo template.

## Video data va player

`PhoneVideoLibrary.asset` hien co 12 video mock. Moi entry gom:

- `Id`, `Title`, `Channel`, `Description`.
- `Views`, `Published`, `Duration`, `Likes`.
- `Thumbnail` de gan Sprite that sau nay.
- `Mock Image Number` va `Mock Color` lam placeholder khi chua co Sprite.

Danh sach dung mock number `01-06` lap lai cho 12 card. Khi click card,
`VideoPlayerView` duoc tao trong `VideoFeedTemplate`, hien thumbnail cua card
lam khung video tam, title, metadata, channel va nut `< Back` co dinh o day.
Nut Back dong player va tra ve dung danh sach dang scroll.

### Video options menu

Ca dau ba cham `More` tren tung video card va nut `More` trong
`VideoPlayerView` deu dung chung `VideoOptionsPanel`. Panel chi co hai nut va
toan bo text hien thi bang tieng Anh:

- `Suggest more videos`, dung icon
  `Assets/1_Internal/Prefab/UI/Icon/square-plus.png`.
- `Don't recommend this video`, dung icon
  `Assets/1_Internal/Prefab/UI/Icon/ban.png`.

Khi menu mo, `VideoOptionsBackdrop` phu mot lop den mo alpha `0.42` phia sau
panel. Backdrop nam tren UI chinh nhung duoi panel, nhan raycast; click vao
backdrop se dong menu ma khong dong video dang xem.

`PhoneVideoFeedUI` luu card da mo menu trong `selectedVideoCard`. Neu chon
`Don't recommend this video` tu card, card do bi `SetActive(false)` va
`Content` duoc danh dau rebuild layout. Neu chon tu `VideoPlayerView`, viewer
va video dang phat cung dong, sau do card tuong ung trong `VideoScroll/Content`
bi an. Anh xa `VideoEntry -> RectTransform card` duoc tao lai moi khi runtime
build danh sach, nen thao tac trong viewer khong dung ten hoac vi tri card de
doan video.

Trang thai an hien tai chi ton tai trong phien runtime; no chua duoc luu vao
save data. `Suggest more videos` hien chi dong menu, chua thay doi recommendation
weight. Khi them `RecommendationModel`, hai nut nay nen goi intervention API
thay vi de `PhoneVideoFeedUI` tu quan ly persistence.

Panel va backdrop luon duoc dua len tren cung khi mo de khong bi `RectMask2D`
cua `VideoScroll` cat. Neu scene cu chua co `VideoOptionsBackdrop`, runtime tao
dung mot backdrop truoc panel va bind nut dong; no khong rebuild
`VideoPlayerView` hay ghi lai RectTransform nguoi dung da can tay.

### GIF source va image-sequence playback

- Video goc duoc giu trong `Data/Video_Raw`.
- `Data/Video_Processed` chua PNG frame dau dung lam thumbnail va MP4 10 FPS.
- Runtime khong dung `VideoPlayer`. Moi MP4 duoc tach thanh sprite sheet 8x8,
  moi frame 256x144, luu trong `Assets/1_Internal/Resources/VideoFrames`.
- `manifest.json` luu so frame; player doi `RawImage.texture` va `uvRect` o
  10 FPS. Tat ca sheet duoc load trong `Awake`, truoc khi nguoi choi chon Kid.
- Nut giua chuyen giua `>` va `II`; thanh progress co the keo de seek.
- Keo progress khong duoc tinh la da xem. Chi thoi gian clip thuc su dang Play
  va dat 80% duration moi ap dung effect mot lan.

De tao lai sheet cho video hien tai va video moi:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Generate-VideoFrameSheets.ps1
```

Script doc tat ca MP4 trong `Data/Video_Processed`, tao lai JPG sheet va cap
nhat `manifest.json`. Ten MP4 phai trung voi stem do `GetVideoStem` tra ve.

### Tac dong cam xuc

- `Brainrot`: tang `Brainrot Exposure`; mac dinh du 3 luot thi Kid chuyen sang
  `Anxious` va dung nhom animation emotional.
- `Normal`: giam mot exposure, on dinh cam xuc va co `Normal Video Happy Chance`
  de chuyen sang `Happy`.
- `Horror`: mot luot xem dat nguong lap tuc dat `Panic`.
- Khi dong phone, Kid doi sang animation phu hop voi tu the dang dung/ngoi.
  Neu Kid con di chuyen, no den dich truoc roi moi ap dung animation cam xuc.
- Animator hien chua co state happy rieng; `Happy` tam dung nhom animation
  neutral. Khi them clip happy vao `Kid.controller`, co the tach mang animation
  happy ma khong can doi video data.

## Controller va input

Tat ca reference khoi tao lien quan den camera va phone duoc hien thi tai:

```text
Controller
+-- CameraController
|   +-- Main_room Controller
|   +-- Kid_Forcus Controller
+-- ChatUIFollowController
+-- SceneInitializer
```

`SceneInitializer` chi expose `Kid Focus Controller` va `Start In Overview`.
Toan bo camera, chat va phone reference da duoc gan san truc tiep tren hai
controller con trong scene truoc khi Play.

Script nam tai
`Assets/1_Internal/Script/Initialization/SceneInitializationController.cs`.

Khong con script `InitializeOnLoad` tu sua hoac tu save scene. `Kid_Forcus` duoc
de active trong Edit Mode de co the quan sat va can chinh. Khi Play,
`SceneInitializer` chi chuyen ve overview neu `Start In Overview` duoc bat.

`MainRoomCameraController` tren `Main_room Controller` chiu trach nhiem zoom,
pan va collision cua camera tong. `KidFocusCameraController` tren
`Kid_Forcus Controller` chiu trach nhiem:

- Dung reference `PhoneScreen` duoc gan san tren Inspector.
- Gan `focusCamera` vao `PhoneScreen.worldCamera`.
- Dieu khien hien/an theo truc local Y.
- Khoa viec doi Kid khi dang xem phone.
- Tam dung random activity cua Kid khi phone hien thi.
- Bo qua click scene khi con tro dang nam tren UI.
- Dung rect cua `ScreenMask` lam vung che chat bubble.

`ChatUiAnchorFollower` an chat bubble khi bubble overlap `ScreenMask`, vi vay
chat cua Kid khong ve de len giao dien dien thoai. Component nay va
`CanvasGroup` nam truc tiep tren `Chat_Kid1` va `Chat_Kid2`; controller khong
tu them component luc Play.

## Logic tu dong da loai bo

- Khong con `KidRandomChatTester` tu bat chat bubble ngau nhien.
- Khong con `WaypointArrivalDetector` khong duoc tham chieu.
- Camera, Kid, chat anchor va PhoneScreen khong con duoc quet theo ten trong scene.
- `PhoneVideoFeedUI` chi rebuild khi bam nut tren Inspector.
- `ChatUiAnchorFollower` khong chay trong Edit Mode.
- `KidWaypointAnimationTester` co `Start On Play` tren Inspector cua `Kid1`;
  tat checkbox nay neu khong muon Kid tu chay chuoi waypoint/animation.
- `RoundedRectGraphic` van dung `ExecuteAlways` de mesh bo goc hien dung trong
  Scene view; component nay khong doi camera, khong save scene va khong rebuild UI.

## Phone va trang thai Kid

- `Kids > Activity Controller` tren `KidFocusCameraController` tro truc tiep den
  `KidWaypointAnimationTester` cua Kid tuong ung.
- Neu mo phone khi Kid dang di chuyen, Kid hoan thanh diem dich hien tai, chuyen
  sang animation cua waypoint do, sau do moi tam dung.
- Neu mo phone khi Kid dang dung, ngoi dat hoac ngoi ghe, animation hien tai duoc
  giu nguyen va khong chon random animation/waypoint moi.
- Khi dong phone, bo dem activity tiep tuc va random loop hoat dong binh thuong.

## Nguyen tac cho chuc nang moi

- Them UI vao trong `ScreenMask`.
- Khong tao lai object `Phone`, `PhoneModel`, camera overlay hay RenderTexture.
- Doi vi tri va kich thuoc bang RectTransform cua `PhoneScreen`.
- Chi mot component duoc phep lam owner cua layout de tranh nhieu script cung
  ghi vao RectTransform.
- Khong ghi de UI nguoi dung can tay neu chua co migration ro rang.
- Control tuong tac phai nam tren `GraphicRaycaster` va khong lam mat
  `PhoneInputBlocker`.

## Checklist kiem thu

- Hierarchy chi con `Kid_Forcus/PhoneScreen` cho phone UI.
- `PhoneScreen` hien dung khi vao camera `Kid_Forcus`.
- Noi dung duoc cat dung theo bon goc cua `ScreenMask`.
- Scroll bang chuot va drag hoat dong.
- Click tren phone khong chon Kid hay object phia sau.
- Chat bubble khong ve de len phone UI.
- Mo phone khong lam Kid doi state.
- Khong co overlay camera, RenderTexture hoac exception khi Play.
