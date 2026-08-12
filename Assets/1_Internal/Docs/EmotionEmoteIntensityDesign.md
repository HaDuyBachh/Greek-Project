# Emotion Emote Intensity Design

## Mục tiêu

Một `ChildEmotion` không được liên kết cứng với đúng một icon. Emotion là trạng thái gameplay, còn emote là cách UI biểu đạt trạng thái đó tại một thời điểm. Mỗi emotion cần một pool nhiều emote, được chọn theo:

- Mức độ của trạng thái.
- Chỉ số tâm lý đang chi phối.
- Ngữ cảnh video và hành động hiện tại.
- Trọng số biến thể để biểu cảm không lặp máy móc.
- Lịch sử icon vừa hiển thị.

Quan hệ đúng là nhiều-nhiều: một emotion có nhiều emote và một emote có thể phù hợp với nhiều emotion gần nhau.

## Mức độ cảm xúc

Mỗi emotion có `intensity` từ `0-100`. Không dùng trực tiếp `RiskLevel` của video làm intensity; video chỉ tạo delta lên các `ChildStat`, sau đó trạng thái của trẻ mới được tính lại.

| Band | Khoảng | Ý nghĩa UI |
| --- | ---: | --- |
| `Subtle` | `0-24` | Biểu hiện rất nhẹ, gần trạng thái bình thường |
| `Mild` | `25-49` | Người chơi có thể nhận ra nhưng chưa cần cảnh báo |
| `Strong` | `50-74` | Biểu hiện rõ, nên được chú ý |
| `Severe` | `75-100` | Trạng thái nặng, có thể thêm `warning.png` và gợi ý can thiệp |

Khi intensity đi qua ranh giới, nên dùng hysteresis khoảng `5` điểm và giữ trạng thái tối thiểu `2-4` giây để icon không nhấp nháy qua lại.

## Ma trận emotion và emote

Tên file dưới đây giữ nguyên chính tả trong `Assets/1_Internal/Prefab/Icon_Emotion/`.

| Emotion | Subtle | Mild | Strong | Severe |
| --- | --- | --- | --- | --- |
| `Calm` | `smileNormal.png`, `smile.png` | `smile_1.png`, `smilinghalo.png` | `coolface.png` | Không dùng mức Severe; chuyển sang emotion khác nếu chỉ số xấu tăng |
| `Focused` | `smileNormal.png`, `coolface.png` | `smartface.png`, `Judging.png` | `smartface.png` | `Judging.png` nếu tập trung căng; không dùng icon báo động |
| `Curious` | `smile_1.png`, `surprised.png` | `surprised.png`, `wow.png` | `wow.png`, `Suspicious.png` | `shock.png` khi bất ngờ mạnh; nếu có Fear cao thì chuyển `Fearful` |
| `Amused` | `smile.png`, `smirking.png` | `funny.png`, `joke.png`, `WinkTongue.png` | `laughing.png`, `funny.png` | `laughing.png`; nếu quá tải thì chuyển `Overstimulated` |
| `Anxious` | `SweatSmile.png`, `aBitWorried.png` | `aBitWorried.png`, `Grimace.png`, `upset.png` | `Grimace.png`, `shock.png`, `scared.png` | `scared.png`, `Terrified.png`; nếu Fear chi phối thì chuyển `Fearful` |
| `Fearful` | `aBitWorried.png`, `Grimace.png` | `scared.png`, `coldscrary.png` | `scared.png`, `Terrified.png`, `shock.png` | `Terrified.png`, `coldscrary.png`, thêm `warning.png` |
| `Agitated` | `Judging.png`, `Suspicious.png`, `upset.png` | `angrynormal.png`, `angry.png`, `reallyUpset.png` | `angry.png`, `veryangry.png`, `reallyUpset.png` | `veryangry.png`, `explodeFace.png`, thêm `warning.png` |
| `Overstimulated` | `hotface.png`, `funny.png` | `dizzy.png`, `hotface.png`, `crazy.png` | `crazy.png`, `explodeFace.png`, `dizzy.png` | `explodeFace.png`, `deadFace.png`, thêm `warning.png` |
| `Distracted` | `Judging.png`, `boring.png` | `boring.png`, `sleep.png` | `sleep.png`, `dizzy.png` | `deadFace.png` khi Fatigue rất cao; thêm `warning.png` nếu cần can thiệp |
| `Attached` | `smirking.png`, `love.png` | `love.png`, `verylove.png` | `verylove.png`, `love.png` | `verylove.png` kèm `warning.png` khi Attachment quá cao |
| `Sad` | `upset.png`, `very upset.png` | `very upset.png`, `cry.png` | `cry.png`, `cryhard.png`, `reallyUpset.png` | `cryhard.png`, `reallyUpset.png`, thêm `warning.png` |
| `Distressed` | `Grimace.png`, `reallyUpset.png` | `reallyUpset.png`, `injuryFace.png`, `dizzy.png` | `Terrified.png`, `cryhard.png`, `explodeFace.png`, `injuryFace.png` | `deadFace.png`, `skullFace.png`, `injuryFace.png`, luôn thêm `warning.png` |

`Calm`, `Focused`, `Curious` và `Amused` không nên bị đẩy lên Severe chỉ để dùng đủ bốn cột. Khi chỉ số xấu đủ cao, emotion chính phải chuyển sang trạng thái phù hợp hơn.

## Icon giao thoa

Các icon sau cố ý không thuộc riêng một emotion:

| Icon | Emotion có thể dùng | Điều kiện phân biệt |
| --- | --- | --- |
| `Grimace.png` | `Anxious`, `Fearful`, `Distressed` | Anxiety nhẹ-vừa, Fear mới tăng hoặc tổng trạng thái xấu đang tích lũy |
| `shock.png` | `Curious`, `Anxious`, `Fearful` | Surprise tích cực dùng Curious; horror/jumpscare dùng Fearful |
| `reallyUpset.png` | `Agitated`, `Sad`, `Distressed` | Agitation cao là cáu; Mood thấp là buồn; nhiều chỉ số cao là Distressed |
| `dizzy.png` | `Overstimulated`, `Distracted`, `Distressed` | FastCut/LoudSound là Overstimulated; Fatigue cao là Distracted |
| `explodeFace.png` | `Agitated`, `Overstimulated`, `Distressed` | Toxic/Violence thiên Agitated; Brainrot/FastCut thiên Overstimulated |
| `deadFace.png` | `Overstimulated`, `Distracted`, `Distressed` | Fatigue/Focus quyết định quá tải, kiệt sức hay báo động tổng hợp |
| `love.png`, `verylove.png` | `Attached`, `Amused` | Attachment cao dùng Attached; phản ứng vui ngắn hạn dùng Amused |
| `upset.png` | `Anxious`, `Agitated`, `Sad` | Chọn theo Anxiety, Agitation hoặc Mood đang chi phối |

## Icon theo ngữ cảnh

Một số asset mô tả phản ứng hoặc nội dung tốt hơn là emotion chính. Chúng chỉ tham gia pool khi có context tương ứng:

| Icon | Context đề xuất |
| --- | --- |
| `disgusting.png`, `vomit.png` | `Disgust`, nội dung gây ghê hoặc khó chịu |
| `zombie.png`, `skullFace.png` | `Horror`; `skullFace.png` chỉ dùng cho Severe/Distressed hoặc marker nội dung |
| `evilface.png` | `Toxic`, `Violence`, nhân vật xấu; ưu tiên marker nội dung thay vì mặt của trẻ |
| `Shhh.png` | `Secret`, nội dung ẩn giấu hoặc trẻ không muốn bị phát hiện |
| `Suspicious.png` | `Clickbait`, `FakeNews`, nội dung đáng ngờ |
| `yummy.png` | `Food`, phản ứng tích cực với nội dung ăn uống |
| `injuryFace.png` | `Pain`, `PhysicalDiscomfort`, hoặc `Distressed` nặng |
| `hotface.png` | `Overstimulated`, căng thẳng nóng bức hoặc nội dung quá dồn dập |
| `warning.png` | Overlay cảnh báo, không thay thế hoàn toàn biểu cảm khuôn mặt |
| `kid.png` | Avatar mặc định, không phải biểu cảm |

## Dữ liệu đề xuất

Không dùng cấu trúc một entry chứa một `emotion` và một `icon`. Mỗi variant cần chứa nhiều emotion, khoảng intensity và context:

```csharp
public enum EmotionIntensityBand
{
    Subtle,
    Mild,
    Strong,
    Severe
}

public enum EmotionContext
{
    None,
    Phone,
    Horror,
    JumpScare,
    Toxic,
    FastCut,
    LoudSound,
    Fatigue,
    Disgust,
    Pain,
    Clickbait,
    Food
}

[System.Serializable]
public class EmotionIconVariant
{
    public string id;
    public Sprite icon;
    public ChildEmotion[] emotions;
    [Range(0, 100)] public int minIntensity;
    [Range(0, 100)] public int maxIntensity = 100;
    public EmotionContext[] requiredAnyContexts;
    [Min(0f)] public float weight = 1f;
    public bool allowWarningOverlay;
}

[CreateAssetMenu(menuName = "Greek Project/UI/Emotion Icon Profile")]
public class EmotionIconProfile : ScriptableObject
{
    public EmotionIconVariant[] variants;
    public Sprite warningOverlay;
}
```

Một variant có thể khai báo nhiều `ChildEmotion`. Ví dụ `reallyUpset.png` chứa `Agitated`, `Sad`, `Distressed`, sau đó hệ thống lọc tiếp bằng intensity và chỉ số chi phối.

## Thuật toán chọn emote

1. Tính `primaryEmotion` từ toàn bộ `ChildStat`.
2. Tính `intensity 0-100` từ chỉ số chi phối của emotion đó.
3. Tạo context từ video tag, thiết bị, thời lượng xem và hành động hiện tại.
4. Lọc các variant có chứa `primaryEmotion` và khoảng intensity phù hợp.
5. Nếu variant yêu cầu context, chỉ giữ variant khớp ít nhất một context.
6. Loại hai icon vừa hiển thị gần nhất nếu pool vẫn còn lựa chọn.
7. Chọn ngẫu nhiên theo `weight`.
8. Nếu intensity Severe hoặc trạng thái cần can thiệp, giữ icon mặt và thêm `warning.png` làm overlay.

Không đổi emote mỗi frame. Chỉ chọn lại khi emotion đổi, intensity đổi band, context quan trọng đổi, hoặc sau một khoảng giữ tối thiểu.

## Ví dụ

### Cùng là Anxious

- Anxiety `18`, không có tag xấu: `SweatSmile.png` hoặc `aBitWorried.png`.
- Anxiety `42`, video căng thẳng: `aBitWorried.png`, `Grimace.png` hoặc `upset.png`.
- Anxiety `65`, có `JumpScare`: `Grimace.png`, `shock.png` hoặc `scared.png`.
- Anxiety `82` nhưng Fear đã chi phối: chuyển emotion thành `Fearful`, dùng `Terrified.png` và overlay `warning.png`.

### Cùng là Agitated

- Agitation `20`: `Judging.png`, `Suspicious.png` hoặc `upset.png`.
- Agitation `45`: `angrynormal.png`, `angry.png` hoặc `reallyUpset.png`.
- Agitation `68`: `angry.png`, `veryangry.png` hoặc `reallyUpset.png`.
- Agitation `90`: `veryangry.png` hoặc `explodeFace.png`, thêm `warning.png`.

### Chat UI

`ChatUIFollowController.ChatSlot.emoteRoot` nhận sprite đã được resolver chọn. Nếu emote đổi từ một biến thể sang biến thể khác trong cùng emotion, không cần đổi animation nhân vật. Animation chỉ đổi khi state gameplay hoặc mức phản ứng thực sự yêu cầu.

Emote trong bong bóng phải là nội dung độc lập: khi `emoteRoot` bật thì `talkRoot` và `videoRoot` tắt. Bong bóng trạng thái ưu tiên icon để người chơi nhận biết nhanh mà không cần đọc; prototype dùng tỷ lệ `Emote 3 : Talk 1`.

Nếu resolver chọn `Talk` thay cho emote, chỉ dùng phản ứng tiếng Anh rất ngắn như `Yay!`, `Whoa!`, `Uh-oh`, `Meh...` hoặc `Not cool`. Mô tả nguyên nhân, intensity và hướng can thiệp phải nằm ở inspect panel/Solution Card thay vì bong bóng.

Chat không được hiển thị thường trực. Sau thời gian giữ ngắn, hệ thống phải tắt cả ba content root, release chat slot và chờ một khoảng im lặng trước lượt kế tiếp. Chi tiết prototype xem [RandomChatTest.md](RandomChatTest.md).
