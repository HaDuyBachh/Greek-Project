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
- Moi khi `PhoneView` mo hoac dong, `VideoScroll` dung movement hien tai va quay
  ve card dau tien. Co the tat bang `Reset Scroll On Phone Visibility Changed`
  tren Inspector cua `PhoneVideoFeedUI`.
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

`PhoneVideoLibrary.asset` hien co 26 video, tuong ung 1-1 voi 26 MP4 trong
`Data/Video_Raw`. Moi entry gom:

- `Id`, `Source Stem`, `Title`, `Channel`, `Description`.
- `Views`, `Published`, `Duration`, `Likes`.
- `Thumbnail` de gan Sprite that sau nay.
- `Mock Image Number` va `Mock Color` lam placeholder khi chua co Sprite.

Runtime chi chon 6 video hop le cho moi lan hien feed. Khi click card,
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
`Don't recommend this video`, card van giu nguyen vi tri va khong refill video
moi. `NotRecommendedOverlay` phu len card voi ba text tieng Anh: `Video removed`,
`Undo`, `Tell us why`. Hai hang `Undo` va `Tell us why` chi la rounded visual,
khong co component `Button` hoac listener. Overlay dung light theme: nen trang,
vien xam nhat, chu den va hai action mau xam sang. Neu chon tu `VideoPlayerView`, viewer
va video dang phat dong, sau do overlay cua card tuong ung duoc bat.

Video da chon `Don't recommend this video` duoc dua vao blacklist cua phien
runtime. Card bi blacklist duoc giu lai voi overlay cho toi lan reset ke tiep.
Khi reset, card cu va overlay bi loai, slot duoc refill
bang video hop le khac; video da blacklist khong duoc chon lai lam recommendation.
Blacklist chua duoc luu vao save data. `Suggest more videos` hien chi dong menu, chua thay doi recommendation
weight. Khi them `RecommendationModel`, hai nut nay nen goi intervention API
thay vi de `PhoneVideoFeedUI` tu quan ly persistence.

Panel va backdrop luon duoc dua len tren cung khi mo de khong bi `RectMask2D`
cua `VideoScroll` cat. Neu scene cu chua co `VideoOptionsBackdrop`, runtime tao
dung mot backdrop truoc panel va bind nut dong; no khong rebuild
`VideoPlayerView` hay ghi lai RectTransform nguoi dung da can tay.

### GIF source va image-sequence playback

- Video goc duoc giu trong `Data/Video_Raw`.
- `Data/Video_Processed` chua PNG frame dau dung lam thumbnail va MP4 10 FPS,
  duoc sinh tu dong tu tat ca MP4 trong `Data/Video_Raw`.
- Pipeline tu phat hien va cat bo doan khung den keo dai den het file. Vi vay
  duration va frame count dung phan noi dung thuc, khong tinh padding den trong MP4 raw.
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

Script xu ly toan bo pipeline: doc MP4 trong `Data/Video_Raw`, tao MP4 10 FPS va
thumbnail trong `Data/Video_Processed`, tao lai JPG sheet, sau do cap nhat
`manifest.json`. `VideoEntry.sourceStem` phai trung voi stem cua file trong
`Data/Video_Raw`; runtime khong con can hard-code id moi vao `GetVideoStem`.

## Feed rotation

Scene co component `Controller/Kid Feed Cycle Controller` duoc gan san truoc
khi Play:

- `Cycle Interval Seconds = 5`.
- Moi chu ky thay ngau nhien `1-3` trong 6 video dang hien.
- Video trong blacklist `Don't recommend` khong bao gio duoc chon lai.
- Card da bi blacklist giu overlay `Video removed` cho toi chu ky reset ke tiep.
  Reset loai toan bo card cu co overlay va refill slot bang video hop le khac.
- Doc lap voi cac card bi blacklist, moi chu ky van thay ngau nhien them `1-3`
  video binh thuong. Card co overlay khong tinh vao quota `1-3` nay.
- Khi `Kid_Forcus` dang theo doi mot Kid, chu ky van dem nhung khong thay video.
  Sau khi quay lai `Main_room`, lan chu ky tiep theo moi refresh danh sach.
- Chu ky khong con muon chat slot hay hien emote UI cho Kid1. Emotion cua Kid1
  chi do `KidEmotionVfxController` hien bang VFX world-space.
- Feed khong refresh khi `VideoPlayerView` dang mo, tranh thay card ben duoi khi
  nguoi choi dang xem video.

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
|   +-- TV_Forcus Controller
+-- ChatUIFollowController
+-- SceneInitializer
```

`SceneInitializer` chi expose `Kid Focus Controller` va `Start In Overview`.
Toan bo camera, chat, phone va TV reference da duoc gan san truc tiep tren ba
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

`TV_Forcus Controller` chi quan ly viec chuyen sang camera `TV_Forcus` va quyen
tuong tac voi TV UI:

- Click truc tiep vao `TV LED 30¨` tu `Main_room` se vao `TV_Forcus`.
- `Outline` cua TV tat o trang thai binh thuong va chi bat khi con tro chuot nam
  trong vung chon TV tren camera `Main_room`; outline tat ngay khi roi hover hoac
  khi da chuyen vao `TV_Forcus`.
- Click Kid dang ngoi tai mot trong cac `Television Seats` va dang chay animation
  nam trong `Television Animations` cung se vao `TV_Forcus`. Scene hien gan ba
  ghe sofa va animation `SitChairIdle`; cac dieu kien nay deu sua duoc tren Inspector.
- `Esc` hoac chuot phai quay lai `Main_room`.
- `GraphicRaycaster` cua TV duoc luu san trong scene o trang thai tat, chi duoc
  bat khi `TV_Forcus` dang active. Vi vay TV UI khong nhan click tu camera tong.
- Chuyen vao `TV_Forcus` khong random lai feed va khong khoi dong lai player;
  video TV dang phat o camera tong tiep tuc tu dung frame hien tai.

`Chat_Kid1` da duoc xoa khoi scene va Kid1 khong con binding trong chat pool.
`Chat_Kid2` va ha tang `ChatUIFollowController` van duoc giu cho Kid khac;
controller khong tu them component luc Play.

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
- Ngay khi `Kid_Forcus` theo doi Kid, activity cua Kid do duoc pause; mo phone tao
  them mot pause reason rieng, nen dong phone van khong lam Kid chay lai khi camera
  con dang focus.
- Neu bat dau focus khi Kid dang di chuyen, Kid hoan thanh diem dich hien tai,
  chuyen sang animation khong di chuyen cua waypoint do, sau do moi tam dung.
- Neu bat dau focus khi Kid dang dung, ngoi dat hoac ngoi ghe, animation hien tai
  duoc giu nguyen va khong chon random animation/waypoint moi.
- Khi quay lai `Main_room`, bo dem activity tiep tuc va random loop hoat dong lai.

## Nguyen tac cho chuc nang moi

- Them UI vao trong `ScreenMask`.
- Khong tao lai object `Phone`, `PhoneModel`, camera overlay hay RenderTexture.
- Doi vi tri va kich thuoc bang RectTransform cua `PhoneScreen`.
- Chi mot component duoc phep lam owner cua layout de tranh nhieu script cung
  ghi vao RectTransform.
- Khong ghi de UI nguoi dung can tay neu chua co migration ro rang.
- Control tuong tac phai nam tren `GraphicRaycaster` va khong lam mat
  `PhoneInputBlocker`.

## Television video UI

`TV LED 30¨/Screen/ScreenMask` trong `Assets/1_Internal/Scenes/1_Main.unity`
co UI video rieng va khong dung `PhoneVideoFeedUI`. `TV_Forcus Controller` co
mot bridge serialize den `KidFocusCameraController` de dam bao moi thoi diem chi
mot che do camera nhan input.

- `TelevisionVideoFeedUI` duoc gan san tren `ScreenMask`; library, 6 card slot,
  player, progress, play/pause va nut dong deu la reference serialize trong scene.
- Feed dung light theme voi nen trang, card xam rat nhat, chu den; thanh dieu huong doc ben trai, header `Recommended` va
  bo cuc co dinh 3 cot x 2 hang. Moi lan hien chi co toi da 6 video.
- Click card mo `VideoPlayerView` phu toan bo man TV. Viewer chi hien video,
  progress, play/pause va nut `X` nho o goc tren ben phai; click `X` quay lai feed.
- Sau `VideoInfo` cua ca 6 card co nut `More` gan san truoc Play. Nut nay mo
  `VideoOptionsPanel` cua TV. `Suggest more videos` chi dong panel;
  `Don't recommend this video` dua video vao blacklist TV cua phien hien tai,
  dong panel va bat `NotRecommendedOverlay` phu dung toan bo card. Overlay dung
  nen trang opaque de che hoan toan thumbnail va metadata cu, hien `Video removed`
  cung hai hang rounded full-width `Undo`, `Tell us why`; hai action chi la visual.
  Card van giu
  vi tri nhu Phone nhung `OpenButton` va `MoreButton` bi khoa. Video no-recommend
  khong duoc mo, broadcast, thay vao slot khac hay chon lai trong cung phien Play.
- Sau dau ba cham cua 6 card duoc dong bo cung RectTransform, font `0.0085` va
  alignment; khong con truong hop `Video 01` co dau ba cham lon hon cac card khac.
- `VideoOptionsPanel` la menu theo card: mac dinh rong bang 50% chieu rong card,
  cao bang 50% chieu cao card, can phai sat nut `More` va dat ngay ben duoi nut vua bam. Neu khong
  du cho, panel dat phia tren de khong bi cat khoi TV. `Options Panel Width Ratio`,
  `Options Panel Height Ratio` va `Options Panel Vertical Gap` deu sua duoc tren Inspector.
- `VideoOptionsBackdrop` la GameObject co san trong scene, phu toan bo TV bang
  lop den alpha `0.22` va co `Button` serialize san. Khi menu mo, backdrop nam
  tren feed nhung duoi panel; click ra ngoai panel se dong ca backdrop va menu.
- Object `VideoPlayerView/VideoDetails/Close` co component `Button` gan san trong
  scene. Listener duoc bind den `ClosePlayer` trong `Awake`; khong co component
  nao duoc them luc Play.
- Runtime khong tao/huy card, khong `AddComponent` va khong rebuild UI. Tat ca
  object va component tuong tac da ton tai truoc Play.
- Sau khi cac frame duoc nap trong `Awake`, TV chon ngau nhien 6 video va tu phat
  mot video trong danh sach ngay ca khi chua focus. Khi vao `TV_Forcus`, nguoi choi
  xem tiep dung video va dung timestamp dang chay.
- Player TV dung cung sprite sheet 8x8, toc do 10 FPS va `RawImage.uvRect` nhu
  Phone; khong dung `VideoPlayer` va khong chieu MP4 truc tiep.
- Moi broadcast chay ngau nhien trong khoang `Minimum Seconds Before Rotation`
  den `Maximum Seconds Before Rotation` (scene mac dinh `10-15` giay). Khi het thoi gian, player
  dong; video vua phat duoc thay ngay tai slot cu bang mot video ngau nhien chua
  co trong 6 slot. Neu TV khong focus, broadcast moi bat dau ngay; neu dang focus,
  man hinh quay ve feed de nguoi choi chon.
- Bo dem tu dong mo/doi broadcast chi chay khi TV khong focus. Khi nguoi dung vao
  `TV_Forcus`, frame sequence hien tai van phat de xem nhung bo random tam dung:
  no khong tu dong dong player, thay slot hay mo video moi. Khi thoat focus, bo dem tiep tuc.
- Bam nut `X` cung dong player va, khi `Replace Played Video After Close` duoc bat,
  thay video vua dong tai slot cu. Khi thoat `TV_Forcus`, TV dam bao co mot video
  ngau nhien dang phat tren camera tong.
- `Auto Play When Not Focused`, hai gioi han thoi gian va quy tac thay slot deu duoc
  expose tren Inspector cua `TelevisionVideoFeedUI`.
- Cac thong so bo cuc, mau sac va so video duoc expose tren Inspector. Khi muon
  ap dung lai cac gia tri Edit Mode, chon TV `ScreenMask` va bam
  `Apply Television Layout`; thao tac nay chi sua scene trong Edit Mode.
- TV tai toan bo image sequence 10 FPS trong `Awake`, truoc khi nguoi dung chon
  video; khong co delayed loader hay runtime bootstrap gan script.

## Kid emotion VFX

Kid1 co `KidEmotionVfxController` va hai anchor VFX doc lap:
`emotion_Main_room` cho camera tong, `emotion_Kid_Forcus` cho camera focus.
`emotion_Main_room_root` nam tai dinh dau Kid va la moc vi tri on dinh. Anchor
ghi lai world offset va world rotation cua `emotion_Main_room` cung
`emotion_Kid_Forcus` luc vao Play. Moi frame, anchor chi cong offset cu vao vi tri
root hien tai va khoi phuc rotation cu. Vi vay VFX di theo Kid nhung khong quay
vong quanh dau hay doi huong khi character xoay; pose can trong scene truoc Play
la moc duy nhat, khong co logic billboard theo camera.
Camera focus va vung chon Kid dung `kid_focus_point` co dinh rieng, khong con
dung chung transform voi emotion dang di chuyen.
Moi state `Stable`, `Happy`, `Anxious`, `Panic` co nam prefab bien the tren moi
anchor. Tat ca prefab instance va reference duoc gan san trong scene truoc Play;
runtime chi bat/tat object co san. Khi `TV_Forcus` active, controller tat ca hai
anchor nen camera TV khong hien emotion cua Kid. Chi tiet va cach chinh Inspector
nam trong [KidEmotionVfx.md](KidEmotionVfx.md).

## Checklist kiem thu

- Hierarchy chi con `Kid_Forcus/PhoneScreen` cho phone UI.
- `PhoneScreen` hien dung khi vao camera `Kid_Forcus`.
- Noi dung duoc cat dung theo bon goc cua `ScreenMask`.
- Scroll bang chuot va drag hoat dong.
- Click tren phone khong chon Kid hay object phia sau.
- Chat bubble khong ve de len phone UI.
- Mo phone khong lam Kid doi state.
- Khong co overlay camera, RenderTexture hoac exception khi Play.
