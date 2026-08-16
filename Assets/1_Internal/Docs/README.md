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
thay vi de tung `KidFeedCycleController` tu quan ly persistence.

Noah va Ethan moi Kid so huu rieng mot danh sach `6` card, blacklist va timer
Phone tren `KidFeedCycleController` cua chinh minh. Moi Phone thay toan bo feed
sau `5` giay khi PhoneScreen cua dung Kid do dang dong. Mo Phone cua Noah chi
pause timer Noah; Phone cua Ethan van reset doc lap. Lan
reset uu tien sau video hoan toan khac danh sach cu. Card `No recommend` duoc giu
overlay den lan reset sau khi roi focus, sau do bi loai va khong bao gio duoc
chon lai trong phien Phone cua Kid do; no khong an video tren Phone Kid con lai.

`PhoneVideoFeedUI` chi lam presenter cho hai owner da serialize san. Khi Enter
mo PhoneScreen, presenter lay `SelectedKidId`, chuyen sang feed dung Kid va mo
thang `CurrentPhoneVideo` tai tien do xem hien tai. Vi vay luc badge `SUSPICIOUS`
hien, nguoi choi vao Phone se thay ngay chinh clip Kid dang xem de xu ly.

Ca Phone va TV bat `Balance Harmful Content`: moi feed 6 card dung ty le muc
tieu gan `3 Normal : 1 Brainrot/Horror` va gioi han cung toi da `2` card xau.
Ba gia tri `Balance Harmful Content`, `Normal Videos Per Harmful Video` va
`Maximum Harmful Videos Per Feed` duoc serialize rieng tren hai feed, nen co the
chinh trong Inspector va khong chia se blacklist/data runtime.

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

## Kid xem video tuan tu

Scene co ba component sequential viewer doc lap duoc gan san truoc khi Play:
Kid1 bat dau index `0`, Kid2 index `1`, Kid3 index `2`. Moi component co bo dem,
trang thai nghi ngo va tien do tieu thu rieng. Kid dung Phone duyet lan luot dung
6 entry trong `PhoneVisibleVideos` cua controller rieng, khong dung feed chung.
Kid xem TV chi doc dung
`TelevisionVideoFeedUI.CurrentBroadcastVideo`, tuc chuong trinh dang hien tren
man TV, khong lay nham card khac trong feed. Mot broadcast TV da hoan tat chi
duoc tinh effect mot lan trong luc frame tiep tuc lap. Neu feed/broadcast doi,
tien do clip cu bi huy truoc khi dem clip moi.

Moi viewer loc `Don't recommend` theo dung thiet bi dang xem: Phone doc blacklist
Phone, TV doc blacklist TV. Kid2 co ca hai reference nhung khong chia se state
giua hai feed. Video bi an giua luc dang xem se bi huy suspicion/watch progress
va bo qua truoc khi ap dung Brainrot/Horror effect.

- Bo dem video chi chay khi Kid dang o vi tri ngoi dat hoac ngoi ghe.
- Thoi gian xem lay tu metadata `duration`; neu metadata khong hop le thi dung
  `Fallback Watch Seconds = 6`.
- Khi `Kid_Forcus` dang theo doi dung Kid do, bo dem video cua Kid do tam dung va tiep tuc sau khi
  quay lai `Main_room`.
- Normal khong bao gio bat `Suspicious`. Chi Brainrot/Horror moi giu nguyen
  tracked video, lap frame va bat VFX SUS trong `8-9` giay; status card van chi
  hien `POSITIVE` hoac `NEGATIVE`.
- Neu harmful video bi `No recommend` truoc khi het cua so tren, tien do bi huy
  va counter khong tang. Neu khong bi go, video thu nhat tang `0/2 -> 1/2`;
  harmful video thu hai tang `2/2` va moi ep Kid sang `Panic/NEGATIVE`.
- Tong cong tam video Normal duoc xem het (khong can lien tiep) se xoa counter
  harmful ve `0/2`. Rieng TV, mot Normal broadcast duoc tinh da xem sau `9`
  giay, truoc chu ky doi chuong trinh `10` giay.
- `Normal` van ap dung sau khi Kid xem het thoi luong metadata.
- `Loop Library` cho phep quay lai video dau tien sau video cuoi. Tat checkbox
  nay neu chi muon duyet thu vien mot lan.

### Tac dong cam xuc

- `Brainrot`: neu khong bi go trong pha `Suspicious`, tang counter harmful mot
  lan; chi video xau thu hai moi chuyen Kid sang `Panic`.
- `Normal`: giam mot exposure. Hai video Normal lien tiep phuc hoi mot bac
  `Panic -> Anxious -> Stable`; hai video Normal tiep theo dua `Stable -> Happy`.
- `Horror`: dung chung counter `2` video voi Brainrot; video xau thu hai moi
  chuyen Kid sang `Panic`.
- `Suspicious` chi la VFX tam thoi, khong ghi de state that. The trang thai tren
  dau chi hien `POSITIVE` hoac `NEGATIVE`, khong hien text/counter SUS. Trong khi
  harmful video chua bi `No recommend`, frame cua chinh video do tiep tuc lap va
  VFX SUS tu khoi dong lai ngay sau moi vong hien thi, khong chen khoang an
  `5-7` giay cua cam xuc binh thuong.
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
- Tam dung activity cua Kid khi phone hien thi.
- Bo qua click scene khi con tro dang nam tren UI.
- Dung rect cua `ScreenMask` lam vung che chat bubble.

`TV_Forcus Controller` chi quan ly viec chuyen sang camera `TV_Forcus` va quyen
tuong tac voi TV UI:

- Click truc tiep vao `TV LED 30¨` tu `Main_room` se vao `TV_Forcus`.
- `Outline` cua TV tat o trang thai binh thuong va chi bat khi con tro chuot nam
  trong vung chon TV tren camera `Main_room`; outline tat ngay khi roi hover hoac
  khi da chuyen vao `TV_Forcus`.
- Click vao bat ky Kid nao, ke ca Kid dang xem TV, luon vao `Kid_Forcus` cua Kid
  do. Neu device activity cua Kid la TV, phai bam Enter sau do moi chuyen sang
  `TV_Forcus`. `TV_Forcus Controller` khong con luu vung click cua Kid.
- Click truc tiep vao TV van vao `TV_Forcus` ngay, doc lap voi luong chon Kid.
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
- `PhoneVideoFeedUI` rebuild feed theo chu ky runtime `5` giay chi khi khong
  focus Kid; timer tam dung trong `Kid_Forcus` va khi viewer video dang mo.
- `ChatUiAnchorFollower` khong chay trong Edit Mode.
- `KidWaypointAnimationTester` co `Start On Play` tren Inspector cua `Kid1`;
  tat checkbox nay neu khong muon Kid tu chay chuoi waypoint/animation.
- Ca ba `KidWaypointAnimationTester` bat `Prevent Shared Positions`, giu truoc
  waypoint va ghe trong ban kinh `0.8`; neu het vi tri trong thi doi `1` giay roi
  thu lai. Vi vay hai Kid khong dung chung activity position hay cung ghe.
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
- Khi quay lai `Main_room`, bo dem activity va video tiep tuc.
- Scene dat `Min/Max Action Duration = 15`, nen Kid chi xem xet doi vi tri sau
  moi 15 giay o diem hien tai. `Visit Waypoints In Order` duoc bat de duyet
  waypoint theo thu tu Inspector thay vi random dich den.

## Card thong tin Kid

`UI/Kid Character Info Cards` co ba card prebuilt su dung ba anh trong
`Prefab/UI/Card_Character`. Khi `Kid_Forcus` theo doi Kid nao, chi card cua Kid do
hien; khi ve `Main_room` hoac vao `TV_Forcus`, ca ba card an. Mood doc truc tiep
`VisualEmotion` va co the noi them trang thai dang xem Phone/TV. Tat ca noi dung,
reference, text state va tuy chon an khi PhoneScreen mo deu expose tren Inspector.
Ca ba anh da nam san tren component `Image` trong scene; card khong dung duoc de
inactive va runtime chi active card dung Kid, khong tao UI/component hay load
sprite. Card nam o goc duoi-trai, can day voi PhoneScreen; text duoc can rieng
theo duong ke tren tung anh. Badge cam xuc va badge dang xem thiet bi duoc phong
to dong deu `1.25x`. Chi tiet xem
[KidCharacterInfoCards.md](KidCharacterInfoCards.md).

Ten hien thi tren ba card la `Noah`, `Ethan`, `Liam`. ID noi bo van la
`Kid1/Kid2/Kid3` de khong pha reference camera, emotion va device logic.

## Loai thiet bi cua Kid

Moi Kid dung `KidDeviceUsageController` de chon mot trong ba loai `Phone Only`,
`Television Only` hoac `Phone And Television`. Scene hien cau hinh Kid1 =
`Phone Only`, Kid2 = `Phone And Television`, Kid3 = `Television Only`. Enter mo
phone neu activity ngoi hien tai thuoc Phone, mo `TV_Forcus` neu activity thuoc
TV. Moi Kid co kha nang xem TV deu nhan `sit_ground` la xem TV khi checkbox
`Watch Television When Sitting On Ground` bat; Kid2 van xem Phone tai cac luot
ghe duoc gan Phone. Animation phan ung cam xuc khong lam mat device activity.
Child object `phone_handle` chi active khi activity hien tai thuoc Phone. Chi
tiet xem [KidDeviceUsage.md](KidDeviceUsage.md).

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

World-space Canvas `TV LED 30¨/Screen` dung `Sorting Order = -1`, thap hon
Particle Renderer cua emotion VFX (`0`). Vi vay VFX cua Kid khong bi man TV ve
de len; Unity Layer 31 cua `ScreenMask` van duoc giu nguyen cho raycast/culling.

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
  O lan broadcast/reset `10` giay ke tiep, toan bo 6 card duoc tao lai bang
  danh sach hop le moi va overlay bien mat.
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
- Moi broadcast chay trong khoang `Minimum Seconds Before Rotation`
  den `Maximum Seconds Before Rotation` (scene dat ca hai bang `10` giay). Trong
  khoang nay frame sequence duoc lap lien tuc. Khi het thoi gian, player
  dong; toan bo 6 card duoc thay bang mot danh sach ngau nhien moi, uu tien khong
  trung bat ky card nao cua danh sach cu. Neu TV khong focus, broadcast moi bat dau ngay; neu dang focus,
  man hinh quay ve feed de nguoi choi chon.
- Bo dem tu dong mo/doi broadcast chi chay khi TV khong focus. Khi nguoi dung vao
  `TV_Forcus`, frame sequence hien tai van phat de xem nhung bo random tam dung:
  no khong tu dong dong player, thay slot hay mo video moi. Khi thoat focus, bo dem tiep tuc.
- Bam nut `X` chi dong player, khong thay danh sach video. Khi thoat
  `TV_Forcus`, TV phat lai mot video trong danh sach hien tai; feed chi duoc thay
  sau khi broadcast chay du chu ky `10` giay.
- `Replace Entire Feed After Timed Rotation` quyet dinh co thay toan bo 6 card
  sau khi chu ky thoi gian hoan tat hay khong.
- `Auto Play When Not Focused`, hai gioi han thoi gian va quy tac thay slot deu duoc
  expose tren Inspector cua `TelevisionVideoFeedUI`.
- Blacklist va bo dem reset cua TV la state rieng trong `TelevisionVideoFeedUI`;
  chung khong doc/ghi blacklist hay timer cua Phone.
- Cac thong so bo cuc, mau sac va so video duoc expose tren Inspector. Khi muon
  ap dung lai cac gia tri Edit Mode, chon TV `ScreenMask` va bam
  `Apply Television Layout`; thao tac nay chi sua scene trong Edit Mode.
- TV tai toan bo image sequence 10 FPS trong `Awake`, truoc khi nguoi dung chon
  video; khong co delayed loader hay runtime bootstrap gan script.

## Animator graph integrity

`Assets/Free Cube Pig Cute Pro Series/FBX/No Root/Quadruped.controller` duoc
tham chieu boi cac Pig prefab trong scene. Asset goc tung chua `17` transition
document mo coi: khong state nao so huu chung va destination state cung khong con
ton tai. Cac document hong da duoc loai bo; hai state hop le `Idle` va
`Run Forward In Place`, animation clip cua chung, va hai transition hai chieu
van duoc giu nguyen. Viec nay ngan `UnityEditor.Graphs.Edge.WakeUp()` dung edge
hong sau moi lan assembly/domain reload va khong them bat ky runtime script nao.

Neu stack trace van xuat hien ngay luc domain reload, nguyen nhan la cache cua
cua so Animator trong `UserSettings/Layouts`, khong phai gameplay script hay
controller asset. Cache controller, breadcrumb va view-transform cua Animator da
duoc xoa khoi `CurrentMaximizeLayout.dwlt` va `default-6000.dwlt`; animation,
scene va runtime logic khong bi thay doi. Can nap lai layout hoac khoi dong lai
Unity mot lan de Editor bo graph object cu dang nam trong bo nho.

## Kid emotion VFX

Kid1, Kid2 va Kid3 moi Kid co mot `KidEmotionVfxController`, activity/device
controller, animator/NavMeshAgent va hai anchor VFX doc lap:
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
anchor. `Suspicious` dung ba VFX co san Indifference/Curious/Expectant tren moi
anchor. Tat ca prefab instance va reference duoc gan san trong scene truoc Play;
runtime chi bat/tat object co san. Khi `TV_Forcus` active, controller tat ca hai
anchor nen camera TV khong hien emotion cua Kid. Chi tiet va cach chinh Inspector
nam trong [KidEmotionVfx.md](KidEmotionVfx.md).

Timing scene hien tai: `Stable = 5-7s`, `Happy = 5-7s`, trang thai xau
`Anxious/Panic = 3-5s`.

UI overlay co object dung san `Kid1 Emotion Status`, gom nen bo goc, text tieng
Anh va hai sprite `arrow-circle-up/down` mau trang. Ten object duoc giu de tranh
pha reference cu, nhung component co mang `Kids` serialize cho ca ba Kid va chi
theo Kid dang hover/focus, nen badge khong chong len nhau. `TV_Forcus` luon an.
`Stable/Happy` hien `POSITIVE` mau xanh, `Anxious/Panic` hien `NEGATIVE` mau do.
Chi tiet logic xem video va cac field Inspector nam trong
[KidSequentialVideoAndStatus.md](KidSequentialVideoAndStatus.md).

## Kid help cards

`Kid_Forcus` co san 8 help card o vung day man hinh, ben phai Character Note, nhung
scene chi active 5 card co hanh dong (`1`, `3`, `5`, `6`, `7`) voi kich thuoc lon.
Nhan `T` moi bat/tat cac header; hover card nao thi card do truot len hien day du
noi dung. Bam card se hien mot preview dung san giua man hinh, phong to roi mo dan,
dieu khien dung Kid dang focus di toi `walk_place` hoac `hug_place`. Khi hanh dong
hoan tat, Kid chuyen sang `Happy`, xoa suspicion, brainrot exposure va bo dem video
xau `0/2`. Toan bo anh, UI, Button, preview va reference deu nam trong scene truoc
Play. Chi tiet bo cuc, action va field Inspector nam trong
[KidHelpCards.md](KidHelpCards.md).

## Checklist kiem thu

- Hierarchy chi con `Kid_Forcus/PhoneScreen` cho phone UI.
- `PhoneScreen` hien dung khi vao camera `Kid_Forcus`.
- Noi dung duoc cat dung theo bon goc cua `ScreenMask`.
- Scroll bang chuot va drag hoat dong.
- Click tren phone khong chon Kid hay object phia sau.
- Chat bubble khong ve de len phone UI.
- Mo phone khong lam Kid doi state.
- Khong co overlay camera, RenderTexture hoac exception khi Play.
