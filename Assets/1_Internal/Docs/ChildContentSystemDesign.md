# Child Content System Design

## Mục tiêu thiết kế

Game nên hỗ trợ nhiều trẻ cùng lúc. Mỗi trẻ có video đang xem, lịch sử xem, thuật toán đề xuất và trạng thái tâm lý riêng. Tất cả trẻ dùng chung một thư viện video, nhưng mỗi trẻ nhận feed khác nhau dựa trên hành vi xem và can thiệp của người chơi.

## Child Emotion

### Danh sách cảm xúc chính

Đây là enum nên dùng cho gameplay:

```csharp
public enum ChildEmotion
{
    Calm,
    Focused,
    Curious,
    Amused,
    Anxious,
    Fearful,
    Agitated,
    Overstimulated,
    Distracted,
    Attached,
    Sad,
    Distressed
}
```

### Ý nghĩa từng cảm xúc

| Emotion | Ý nghĩa gameplay | Khi nào xuất hiện | Animation gợi ý |
| --- | --- | --- | --- |
| `Calm` | Bình thường, ổn định | Không có tác động xấu hoặc vừa được can thiệp tốt | `Breathing Idle`, `SitGround`, `SitChairIdle` |
| `Focused` | Tập trung lành mạnh | Xem nội dung giáo dục/tích cực | `SitGroundUsingPhone`, `SitChairUsingPhone` |
| `Curious` | Tò mò | Nội dung khám phá, học hỏi, sáng tạo | `SitGroundUsingPhone`, `SitChairUsingPhone` |
| `Amused` | Vui vẻ/giải trí nhẹ | Nội dung hài, hoạt hình nhẹ, giải trí bình thường | `SitGroundUsingPhone`, `SitChairUsingPhone` |
| `Anxious` | Lo lắng | Xem nội dung căng thẳng, gây bất an | `SitChairFear` |
| `Fearful` | Sợ hãi | Xem horror/jumpscare/thumbnail đáng sợ | `SitChairFear`, `Panic` |
| `Agitated` | Kích động/cáu | Nội dung bạo lực, toxic, tranh cãi | `SitChairYell`, `AngryStandNormal`, `AngryStandNormal_1` |
| `Overstimulated` | Quá kích thích | Nội dung brainrot, cắt nhanh, âm thanh dồn dập | `Panic`, `RunForward` |
| `Distracted` | Mất tập trung | Xem quá lâu hoặc nội dung gây nghiện ngắn | `SitGroundUsingPhone`, `SitChairUsingPhone` |
| `Attached` | Bị cuốn vào màn hình | Xem liên tục cùng nhóm nội dung, khó dứt ra | `SitGroundUsingPhone`, `SitChairUsingPhone` |
| `Sad` | Buồn | Nội dung tiêu cực, bị mắng/chê, cô lập | `Crying` |
| `Distressed` | Mất kiểm soát/căng thẳng nặng | Nhiều chỉ số xấu vượt ngưỡng | `Panic`, `Crying`, `GroundPain` |

## Emotion Icon

Icon cảm xúc đang nằm ở:

```text
Assets/1_Internal/Prefab/Icon_Emotion/
```

Folder này nên được xem như thư viện sprite cảm xúc. UI không nên kéo icon trực tiếp theo tên file ở nhiều nơi và không được map cứng `1 emotion -> 1 icon`. Thiết kế đầy đủ về mức độ, icon giao thoa, context và thuật toán chọn nằm trong [EmotionEmoteIntensityDesign.md](EmotionEmoteIntensityDesign.md).

### Pool icon tóm tắt

| Emotion | Nhẹ | Vừa | Nặng |
| --- | --- | --- | --- |
| `Calm` | `smileNormal.png`, `smile.png` | `smile_1.png`, `smilinghalo.png` | `coolface.png` |
| `Focused` | `smileNormal.png`, `coolface.png` | `smartface.png`, `Judging.png` | `smartface.png` |
| `Curious` | `smile_1.png`, `surprised.png` | `surprised.png`, `wow.png` | `wow.png`, `Suspicious.png`, `shock.png` |
| `Amused` | `smile.png`, `smirking.png` | `funny.png`, `joke.png`, `WinkTongue.png` | `laughing.png`, `funny.png` |
| `Anxious` | `SweatSmile.png`, `aBitWorried.png` | `aBitWorried.png`, `Grimace.png`, `upset.png` | `Grimace.png`, `shock.png`, `scared.png`, `Terrified.png` |
| `Fearful` | `aBitWorried.png`, `Grimace.png` | `scared.png`, `coldscrary.png` | `Terrified.png`, `shock.png`, `warning.png` |
| `Agitated` | `Judging.png`, `Suspicious.png`, `upset.png` | `angrynormal.png`, `angry.png`, `reallyUpset.png` | `veryangry.png`, `explodeFace.png`, `warning.png` |
| `Overstimulated` | `hotface.png`, `funny.png` | `dizzy.png`, `hotface.png`, `crazy.png` | `crazy.png`, `explodeFace.png`, `deadFace.png` |
| `Distracted` | `Judging.png`, `boring.png` | `boring.png`, `sleep.png` | `sleep.png`, `dizzy.png`, `deadFace.png` |
| `Attached` | `smirking.png`, `love.png` | `love.png`, `verylove.png` | `verylove.png`, `warning.png` |
| `Sad` | `upset.png`, `very upset.png` | `very upset.png`, `cry.png` | `cryhard.png`, `reallyUpset.png`, `warning.png` |
| `Distressed` | `Grimace.png`, `reallyUpset.png` | `injuryFace.png`, `dizzy.png`, `cryhard.png` | `deadFace.png`, `skullFace.png`, `warning.png` |

### Icon phụ dùng cho tag/nội dung

Một số icon không nên map trực tiếp vào cảm xúc chính, nhưng có thể dùng trong inspect panel hoặc Solution Card:

| Icon | Dùng cho |
| --- | --- |
| `evilface.png` | Nội dung độc hại, nhân vật xấu, toxic |
| `vomit.png` | Nội dung gây ghê/sốc/khó chịu |
| `zombie.png` | Horror hoặc nội dung đáng sợ kiểu quái vật |
| `Shhh.png` | Nội dung bí mật/ẩn rủi ro, cần kiểm tra |
| `Suspicious.png` | Video đáng ngờ, clickbait, không rõ nguồn |
| `kid.png` | Icon đại diện trẻ mặc định |
| `smilinghalo.png` | Nội dung an toàn/tích cực |
| `yummy.png` | Nội dung ăn uống/food |

### Emotion Icon Data

Nên tạo `EmotionIconProfile` dạng `ScriptableObject`. Mỗi variant hỗ trợ nhiều emotion và một khoảng intensity, không phải một cặp emotion-icon:

```csharp
[CreateAssetMenu(menuName = "Greek Project/UI/Emotion Icon Profile")]
public class EmotionIconProfile : ScriptableObject
{
    public EmotionIconVariant[] variants;
    public Sprite warningOverlay;
}

[System.Serializable]
public class EmotionIconVariant
{
    public Sprite icon;
    public ChildEmotion[] emotions;
    [Range(0, 100)] public int minIntensity;
    [Range(0, 100)] public int maxIntensity = 100;
    public EmotionContext[] requiredAnyContexts;
    [Min(0f)] public float weight = 1f;
}
```

Resolver lọc variant bằng emotion, intensity và context, sau đó random theo `weight` và tránh các icon vừa dùng. `warning.png` là overlay cho mức Severe, không thay thế bắt buộc icon khuôn mặt.

### Quy tắc hiển thị icon trong UI

- Icon nhỏ trên đầu trẻ: dùng một variant phù hợp với `ChildEmotion`, intensity và context hiện tại.
- Inspect panel: hiện icon đã chọn + emotion + intensity cụ thể.
- Solution Card: nếu trạng thái Severe hoặc cần can thiệp, thêm `warning.png`.
- Feed item/debug mode: có thể dùng icon phụ theo tag, ví dụ `Horror -> zombie.png`, `Brainrot -> dizzy.png`, `Toxic -> evilface.png`.
- Không đổi icon mỗi frame; chỉ chọn lại khi emotion đổi, intensity đổi band hoặc context quan trọng đổi.

### Chỉ số nội bộ

Emotion không nên tính trực tiếp từ một tag duy nhất. Nên dùng nhiều chỉ số chạy ngầm:

```csharp
public enum ChildStat
{
    Anxiety,
    Fear,
    Agitation,
    Focus,
    Attachment,
    Mood,
    Fatigue
}
```

Gợi ý ngưỡng:

| Điều kiện | Emotion |
| --- | --- |
| `Fear >= 70` | `Fearful` |
| `Anxiety >= 60` | `Anxious` |
| `Agitation >= 60` | `Agitated` |
| `Attachment >= 70` | `Attached` |
| `Focus <= 30` | `Distracted` |
| `Fear + Anxiety + Agitation >= 170` | `Distressed` |
| Không có chỉ số xấu cao | `Calm` |

## Video Tag

### Nhóm tag nội dung

```csharp
public enum ContentTag
{
    Education,
    Cartoon,
    Music,
    Comedy,
    Game,
    Challenge,
    Prank,
    Horror,
    JumpScare,
    Violence,
    Weapon,
    ToxicLanguage,
    Bullying,
    Brainrot,
    FastCut,
    LoudSound,
    Clickbait,
    FakeNews,
    Gambling,
    Shopping,
    AgeInappropriate,
    PositiveMessage,
    Relaxing,
    Creativity,
    Sport,
    Animal,
    Food,
    FamilyFriendly
}
```

### Nhóm phân loại chính

`ContentTag` có thể có nhiều tag. Nhưng mỗi video nên có một category chính:

```csharp
public enum ContentCategory
{
    Positive,
    NeutralEntertainment,
    Educational,
    Horror,
    Violent,
    Brainrot,
    AgeInappropriate,
    Commercial,
    Misinformation
}
```

### Tag theo mức nguy cơ

```csharp
public enum RiskLevel
{
    Safe,
    Low,
    Medium,
    High,
    Severe
}
```

Gợi ý:

| RiskLevel | Ý nghĩa |
| --- | --- |
| `Safe` | Có lợi hoặc phù hợp lứa tuổi |
| `Low` | Giải trí bình thường, ít rủi ro |
| `Medium` | Có dấu hiệu gây nghiện, clickbait, hơi căng |
| `High` | Gây sợ, kích động, không phù hợp tuổi |
| `Severe` | Bạo lực nặng, nội dung độc hại rõ ràng |

## Video Data Schema

Một video nên là `ScriptableObject` tên `ContentItem`.

```csharp
public class ContentItem : ScriptableObject
{
    public string id;
    public string title;
    public string channelName;
    public Sprite thumbnail;
    public ContentCategory category;
    public RiskLevel riskLevel;
    public ContentTag[] tags;
    public int minimumAge;
    public float durationSeconds;
    public PsychologicalEffect effect;
    public string solutionHint;
}
```

Effect nên tách riêng:

```csharp
[System.Serializable]
public struct PsychologicalEffect
{
    public float anxietyDelta;
    public float fearDelta;
    public float agitationDelta;
    public float focusDelta;
    public float attachmentDelta;
    public float moodDelta;
    public float fatigueDelta;
}
```

## Video Tag Preset

### Video tích cực

- Category: `Positive`
- Risk: `Safe`
- Tags: `PositiveMessage`, `FamilyFriendly`, `Relaxing`
- Effect: giảm anxiety/fear/agitation, tăng mood/focus nhẹ.

### Video giáo dục

- Category: `Educational`
- Risk: `Safe`
- Tags: `Education`, `Creativity`, `FamilyFriendly`
- Effect: tăng focus, tăng mood nhẹ, fatigue tăng chậm.

### Video giải trí bình thường

- Category: `NeutralEntertainment`
- Risk: `Low`
- Tags: `Cartoon`, `Comedy`, `Music`, `Game`
- Effect: mood tăng nhẹ, attachment tăng nhẹ nếu xem lâu.

### Video kinh dị

- Category: `Horror`
- Risk: `High`
- Tags: `Horror`, `JumpScare`, `LoudSound`, `Clickbait`
- Effect: tăng fear/anxiety nhanh, giảm mood.

### Video bạo lực/kích động

- Category: `Violent`
- Risk: `High` hoặc `Severe`
- Tags: `Violence`, `Weapon`, `ToxicLanguage`, `Bullying`
- Effect: tăng agitation, tăng anxiety, giảm mood.

### Video brainrot

- Category: `Brainrot`
- Risk: `Medium` hoặc `High`
- Tags: `Brainrot`, `FastCut`, `LoudSound`, `Clickbait`
- Effect: tăng attachment, tăng fatigue, giảm focus, có thể tăng agitation.

### Video không phù hợp tuổi

- Category: `AgeInappropriate`
- Risk: `High`
- Tags: `AgeInappropriate`, `Clickbait`, tag phụ tùy nội dung
- Effect: phụ thuộc tag phụ, nhưng luôn tăng anxiety hoặc attachment.

## Child Session

Mỗi trẻ có một session độc lập.

```csharp
public class ChildSessionController : MonoBehaviour
{
    public string childId;
    public string childName;
    public DeviceScreenController currentDevice;
    public ContentItem currentVideo;
    public ChildStatusController status;
    public RecommendationProfile recommendationProfile;
}
```

### Luồng mỗi trẻ

1. `ChildSessionController` yêu cầu video tiếp theo.
2. `RecommendationProfile` chọn video theo weight riêng của trẻ.
3. `DeviceScreenController` hiển thị video đó trên phone/TV.
4. `ChildStatusController` nhận effect từ video đang xem.
5. `KidAnimationController` đổi animation theo emotion.
6. Người chơi inspect đúng trẻ hoặc đúng thiết bị.
7. Intervention chỉ cập nhật session của trẻ đó.

## Kid Animation Instances

`Assets/1_Internal/Animation/Kid.controller` đang có các animation state sau:

| Instance Id | Animator State | File nguồn | Dùng cho |
| --- | --- | --- | --- |
| `kid_idle_breathing` | `Breathing Idle` | `Breathing Idle 1.fbx` | Đứng/ngồi chờ bình thường |
| `kid_walk` | `Walking` | `X Bot@Walking.fbx` | Di chuyển giữa các vị trí |
| `kid_run_forward` | `RunForward` | `X Bot@Drunk Run Forward.fbx` | Bỏ chạy, quá kích thích, panic |
| `kid_sit_ground` | `SitGround` | `SitGround.fbx` | Ngồi dưới sàn, không dùng thiết bị |
| `kid_sit_ground_phone` | `SitGroundUsingPhone` | `SitGroundUsingPhone.fbx` | Ngồi dưới sàn xem điện thoại |
| `kid_sit_chair_idle` | `SitChairIdle` | `X Bot@SitChairIdle.fbx` | Ngồi ghế bình thường |
| `kid_sit_chair_phone` | `SitChairUsingPhone` | `X Bot@Sitting.fbx` | Ngồi ghế xem điện thoại |
| `kid_sit_chair_fear` | `SitChairFear` | `X Bot@SitFear.fbx` | Sợ hãi khi đang ngồi |
| `kid_sit_chair_yell` | `SitChairYell` | `X Bot@SitChairYell.fbx` | Cáu/kích động khi đang ngồi |
| `kid_angry_stand` | `AngryStandNormal` | `X Bot@AngryStandNormal.fbx` | Tức giận khi đứng |
| `kid_angry_normal` | `AngryStandNormal_1` | `X Bot@AngryNormal_1.fbx` | Tức giận nhẹ/idle angry |
| `kid_panic` | `Panic` | `X Bot@Panic.fbx` | Hoảng sợ/quá tải |
| `kid_crying` | `Crying` | `X Bot@Crying.fbx` | Buồn/khóc |
| `kid_ground_pain` | `GroundPain` | `X Bot@Writhing In Pain.fbx` | Distressed nặng hoặc phản ứng đặc biệt |

### Animation Controller Instance

Nên tạo một asset data để map emotion sang animation, ví dụ `KidAnimationProfile`.

```csharp
[System.Serializable]
public class KidAnimationInstance
{
    public string id;
    public ChildEmotion emotion;
    public string animatorStateName;
    public bool requiresSitting;
    public bool requiresPhone;
    public float crossFadeSeconds;
}
```

Instance mẫu:

| Emotion | State ưu tiên | State thay thế |
| --- | --- | --- |
| `Calm` | `Breathing Idle` | `SitGround`, `SitChairIdle` |
| `Focused` | `SitGroundUsingPhone` | `SitChairUsingPhone` |
| `Curious` | `SitGroundUsingPhone` | `SitChairUsingPhone` |
| `Amused` | `SitGroundUsingPhone` | `SitChairUsingPhone` |
| `Anxious` | `SitChairFear` | `SitGroundUsingPhone` |
| `Fearful` | `SitChairFear` | `Panic` |
| `Agitated` | `SitChairYell` | `AngryStandNormal`, `AngryStandNormal_1` |
| `Overstimulated` | `Panic` | `RunForward` |
| `Distracted` | `SitGroundUsingPhone` | `SitChairUsingPhone` |
| `Attached` | `SitGroundUsingPhone` | `SitChairUsingPhone` |
| `Sad` | `Crying` | `SitChairFear` |
| `Distressed` | `Panic` | `Crying`, `GroundPain` |

## Folder Structure

Đã chia folder theo cấu trúc sau:

```text
Assets/1_Internal
  Animation/
  Art/
    Thumbnails/
    UI/
  Audio/
    Feedback/
    UI/
  Data/
    Children/
    ContentItems/
    RecommendationProfiles/
    SolutionCards/
  Docs/
  Materials/
    Devices/
  Prefab/
    Children/
    Devices/
    Gameplay/
    UI/
  RenderTextures/
  Scenes/
  Script/
    Children/
    Content/
    Core/
    Devices/
    Editor/
    Intervention/
    Recommendation/
    UI/
    Utilities/
```

### Vai trò folder

| Folder | Chứa gì |
| --- | --- |
| `Animation` | FBX animation và Animator Controller |
| `Art/Thumbnails` | Ảnh thumbnail giả cho video |
| `Art/UI` | Icon, sprite, texture UI |
| `Audio/UI` | Click, hover, notification |
| `Audio/Feedback` | Âm thanh phản hồi khi trẻ đổi trạng thái |
| `Data/ContentItems` | Asset `ContentItem` cho từng video |
| `Data/Children` | Profile trẻ, ví dụ Kid1/Kid2 |
| `Data/RecommendationProfiles` | Weight/tag preference mặc định |
| `Data/SolutionCards` | Nội dung hướng dẫn can thiệp |
| `Materials/Devices` | Material màn hình phone/TV |
| `Prefab/Children` | Prefab trẻ đã gắn controller/script |
| `Prefab/Devices` | Phone, TV, tablet screen prefab |
| `Prefab/Gameplay` | Game manager/session prefab |
| `Prefab/UI` | Feed UI, inspect panel, solution card |
| `RenderTextures` | Texture render UI lên phone/TV |
| `Script/Children` | Child session/status/animation |
| `Script/Content` | Content item/library/feed |
| `Script/Core` | Game session, event bus, bootstrap |
| `Script/Devices` | Phone/TV screen binding |
| `Script/Intervention` | Logic remove/reduce/recommend/observe |
| `Script/Recommendation` | Recommendation weight model |
| `Script/UI` | Feed UI, inspect UI, solution card UI |
| `Script/Utilities` | Helper dùng chung |

## Thứ tự implement sau tài liệu này

1. Tạo enum `ChildEmotion`, `ChildStat`, `ContentTag`, `ContentCategory`, `RiskLevel`.
2. Tạo `ContentItem` và `PsychologicalEffect`.
3. Tạo `ChildStatusController`.
4. Tạo `KidAnimationController` dùng mapping state ở trên.
5. Tạo `RecommendationProfile` riêng cho từng trẻ.
6. Tạo `ChildSessionController`.
7. Tạo feed UI mock cho phone.
8. Bind Kid1 với `PhoneScreen`.
9. Thêm Kid2 để kiểm tra mỗi trẻ xem video khác nhau.

## Chat UI Anchor Binding

Scene `Assets/1_Internal/Scenes/1_Main.unity` hiện chỉ giữ object UI
`Chat_Kid2`. `Chat_Kid1` và binding chat của Kid1 đã được xóa; Kid1 dùng
`KidEmotionVfxController` để hiện emotion world-space.

Mỗi chat đại diện cho video/cảm xúc hiện tại của một trẻ. Bên trong mỗi chat có:

- `Talk`: TextMeshPro text của trẻ.
- `Emote`: Image icon cảm xúc.
- `Video`: Image/animated placeholder cho video đang xem.

Quy ước binding mặc định:

```text
Chat_Kid2 -> Kid2/ui_anchor
```

Camera dùng để chiếu vị trí world sang UI là camera người chơi đang nhìn: `Main_room` ở chế độ tổng quan và `Kid_Forcus` khi đang inspect một trẻ.

Script runtime:

- `ChatUIFollowController`: gắn tại `Controller/ChatUIFollowController`, quản lý danh sách trẻ, chat slot nào trẻ được phép dùng, và chat slot nào đang được ai sử dụng.
- `ChatUiAnchorFollower`: cập nhật vị trí RectTransform theo anchor trong `LateUpdate`.
- `ChatUiAnchorUtility`: helper tìm camera, chat, anchor.
- `KidRandomChatTester`: component prototype tạo khoảng im lặng, mượn chat rảnh, hiển thị một nội dung mẫu rồi trả slot về pool.

Anchor nên có:

- Name: `ui_anchor`
- Tag: `ui_anchor`

Nếu object anchor đang viết hoa kiểu `UI_anchor`, script vẫn nhận được, nhưng nên dùng `ui_anchor` thống nhất.

### Chat Pool

Một chat slot không nhất thiết chỉ thuộc một trẻ cố định. `ChatUIFollowController` có hai danh sách chính:

- `kids`: mỗi entry có `kidId`, `kidRoot`, `uiAnchor`, `allowedChatIds`.
- `chats`: mỗi entry có `chatId`, `chatRoot`, `talkRoot`, `emoteRoot`, `videoRoot`, `videoContentRoot`, `activeUserKidId`.

Trạng thái scene hiện tại:

```text
kids: empty
chats: Chat_Kid2
Kid1: no chat binding; emotion uses KidEmotionVfxController
```

Vị trí chèn nội dung trong mỗi chat:

| Field | Object trong chat | Dùng để |
| --- | --- | --- |
| `talkRoot` | `Talk` | Chèn text/TextMeshPro lời nói hoặc caption |
| `emoteRoot` | `Emote` | Chèn icon cảm xúc |
| `videoRoot` | `Video` | Bật/tắt khung video |
| `videoContentRoot` | `Video/Vid` | Chèn frame/gif/video sprite bên trong khung |

### Quy tắc nội dung bong bóng

Một lần hiển thị chỉ được bật đúng một loại nội dung:

- `Talk`: bật `talkRoot`, tắt `emoteRoot` và `videoRoot`.
- `Emote`: bật `emoteRoot`, tắt `talkRoot` và `videoRoot`.
- `Video`: bật `videoRoot`, tắt `talkRoot` và `emoteRoot`.

Không ghép text và icon trong cùng một lượt. `Video` chỉ được chọn khi `videoContentRoot` đã có media thật; không bật một placeholder trống.

Để giảm lượng chữ người chơi phải đọc:

- Ưu tiên `Emote` hơn `Talk`; prototype dùng weight `Emote 3 : Talk 1`.
- Text hiển thị bằng tiếng Anh, ngắn và mang tính phản ứng tức thời.
- Mục tiêu là 1-2 từ; tránh câu giải thích dài trong bong bóng.
- Text mẫu: `Yay!`, `Whoa!`, `Uh-oh`, `Meh...`, `Not cool`.
- Thông tin giải thích chi tiết dành cho inspect panel hoặc Solution Card, không đặt trong bong bóng trên đầu Kid.

Chat không hiện liên tục. Kid phải có khoảng im lặng giữa các lượt; prototype dùng `2-5` giây im lặng và `2.5-4.5` giây hiển thị. Khi hết lượt, slot phải được release và ẩn.

Kid1 hiện không gọi `RequestChat`, `AssignChatToKid` hoặc
`ReleaseChatUsedByKid`. Nếu Kid khác dùng chat sau này, các API pool chung vẫn
có thể được gọi với `kidId` và `chatId` đã serialize cho Kid đó.

Thiết kế và cấu hình prototype hiện tại nằm trong [RandomChatTest.md](RandomChatTest.md).
