# Implementation Directions

## Mục tiêu gần nhất

Dự án nên làm theo hướng prototype gameplay trước, sau đó mới polish UI/animation. Core loop cần chứng minh được:

1. Mỗi trẻ xem một nội dung riêng trên điện thoại hoặc TV.
2. Feed sinh ra nhiều loại video khác nhau.
3. Nội dung đang xem ảnh hưởng dần đến trạng thái tâm lý của trẻ.
4. Animation của Kid1 đổi theo trạng thái.
5. Người chơi kiểm tra/can thiệp để giảm tác động xấu.
6. Game hiện Solution Card khi phát hiện trạng thái bất ổn.

## Hướng triển khai đề xuất

### Hướng A - MVP nhanh, ít rủi ro

Đây là hướng nên làm trước.

- Dùng một scene chính: `Assets/1_Internal/Scenes/1_Main.unity`.
- Dùng Kid1 có sẵn và `Assets/1_Internal/Animation/Kid.controller`.
- Tạo các script gameplay đơn giản trong `Assets/1_Internal/Script`.
- Tạo dữ liệu video bằng `ScriptableObject`, không hard-code toàn bộ trong UI.
- UI feed kiểu YouTube/TikTok làm bằng UGUI.
- Mỗi trẻ có feed/current video riêng. Phone hoặc TV chỉ là thiết bị hiển thị feed của trẻ đang dùng thiết bị đó.
- Trạng thái trẻ điều khiển animation bằng `Animator.CrossFade()` theo tên state có sẵn.

Ưu điểm: nhanh có bản chơi được, ít phải sửa Animator Controller phức tạp.

Nhược điểm: logic animation chưa đẹp bằng state machine đầy đủ, nhưng đủ tốt cho prototype.

### Hướng B - Sạch hơn cho bản hoàn chỉnh

Sau khi MVP chạy ổn, nâng cấp thành hệ thống rõ hơn:

- `GameSessionController`: quản lý vòng lặp chính.
- `ChildSessionController`: quản lý một trẻ, thiết bị đang dùng, video hiện tại, feed riêng và trạng thái tâm lý riêng.
- `ChildStatusController`: giữ trạng thái trẻ và các chỉ số tâm lý.
- `KidAnimationController`: map trạng thái sang animation.
- `ContentFeedController`: chọn video tiếp theo.
- `RecommendationModel`: tăng/giảm trọng số đề xuất theo hành vi xem/can thiệp.
- `DeviceScreenController`: render UI lên phone/TV.
- `InterventionController`: xử lý remove, reduce similar, recommend positive, observe.
- `SolutionCardController`: hiện hướng dẫn khi trạng thái vượt ngưỡng.

Ưu điểm: dễ mở rộng nhiều trẻ, nhiều thiết bị, nhiều level.

Nhược điểm: lâu hơn, cần đặt tên data và event cẩn thận.

### Hướng C - Cinematic/Timeline

Chỉ nên dùng cho đoạn tutorial hoặc event đặc biệt:

- Timeline điều khiển cảnh trẻ ngồi xem điện thoại.
- Signal trong Timeline gọi đổi video/trạng thái.
- Sau tutorial thì gameplay quay về hệ thống runtime.

Không nên dùng Timeline làm toàn bộ logic feed vì sẽ khó scale.

## Phần khó 1: Ghép animation cho Kid1

`Kid.controller` hiện có nhiều state animation nhưng chưa có parameter/transition. Vì vậy có hai cách:

### Cách nên làm trước

Dùng script gọi trực tiếp state name:

- Bình thường: `Breathing Idle`, `SitGround`, `SitChairIdle`
- Đang dùng điện thoại: `SitGroundUsingPhone`, `SitChairUsingPhone`
- Lo lắng/sợ: `SitChairFear`, `Panic`
- Buồn/khóc: `Crying`
- Kích động/cáu: `SitChairYell`, `AngryStandNormal`, `AngryStandNormal_1`
- Bỏ chạy/mất kiểm soát: `RunForward`
- Tác động mạnh: `GroundPain`
- Di chuyển: `Walking`

Tạo enum:

- `ChildMood.Normal`
- `ChildMood.Focused`
- `ChildMood.Anxious`
- `ChildMood.Scared`
- `ChildMood.Agitated`
- `ChildMood.Crying`
- `ChildMood.Overstimulated`

Sau đó tạo bảng map mood -> animation state. Script chỉ cần:

- nhận mood mới,
- kiểm tra state có tồn tại,
- gọi `animator.CrossFade(stateName, 0.2f)`.

Cách này tránh phải sửa Animator Controller trong giai đoạn đầu.

### Cách nâng cấp sau

Thêm parameter vào Animator:

- `Mood` dạng int.
- `UsingPhone` dạng bool.
- `Sitting` dạng bool.
- `Panic` hoặc `InterventionReaction` dạng trigger.

Sau đó tạo transition có exit time/blend chuẩn. Cách này mượt hơn nhưng mất thời gian tune.

## Phần khó 2: UI giả YouTube cho điện thoại và TV

Không nên dựng UI bằng mesh thủ công. Nên làm bằng UGUI vì project đã có `com.unity.ugui`.

### Điện thoại

Có object `PhoneScreen` trong scene. Có thể làm một trong hai kiểu:

- World Space Canvas gắn làm con của `PhoneScreen`.
- RenderTexture từ một UI Camera, sau đó gán texture lên material của màn hình phone.

Với prototype, World Space Canvas nhanh hơn. Với polish, RenderTexture đẹp và kiểm soát tốt hơn.

UI tối thiểu:

- Header giống YouTube: logo giả, search icon, avatar nhỏ.
- Khu video/thumbnail chính.
- Tiêu đề video.
- Channel name giả.
- Like/view/time giả.
- Danh sách recommendation bên dưới hoặc nút next/scroll.
- Badge ẩn/nhẹ cho debug category trong Editor, có thể tắt ở build.

### TV

TV nên hiển thị feed của trẻ đang xem TV, nhưng layout ngang:

- Thumbnail lớn/current video ở giữa.
- Hàng video đề xuất phía dưới.
- Trạng thái "Autoplay next".

Player nhìn TV từ xa nên chữ phải to hơn phone. TV không cần nhiều interaction; phone là nơi kiểm tra chi tiết.

## Mỗi trẻ xem một video khác nhau

Hướng nên làm là tách thư viện nội dung chung khỏi phiên xem riêng của từng trẻ.

### Dữ liệu dùng chung

`ContentLibrary` giữ toàn bộ video có thể xuất hiện trong game. Các trẻ dùng chung thư viện này để tránh tạo trùng data.

Ví dụ:

- Video A: hoạt hình vui, positive.
- Video B: thử thách kích động, brainrot.
- Video C: clip đáng sợ, horror.
- Video D: nội dung giáo dục, positive.

### Phiên xem riêng cho từng trẻ

Mỗi trẻ có một `ChildSessionController` riêng:

- `childId`
- `childName`
- `currentDevice`
- `currentVideo`
- `personalRecommendationWeights`
- `watchHistory`
- `status`
- `attentionLevel`
- `timeWatchingCurrentVideo`

Như vậy Kid1 có thể đang xem video kinh dị trên phone, trong khi Kid2 xem video giải trí trên TV. Can thiệp vào Kid1 chỉ làm thay đổi feed/weight/history của Kid1, không tự động làm đổi feed của Kid2.

### Thiết bị hiển thị

Mỗi thiết bị nên bind vào một child session:

- `PhoneScreen_Kid1` hiển thị session của Kid1.
- `TabletScreen_Kid2` hoặc `TVScreen_Kid2` hiển thị session của Kid2.

Nếu muốn người chơi mượn/kiểm tra điện thoại, UI inspect có thể đọc `currentVideo` từ child session được chọn.

### Recommendation riêng từng trẻ

Không nên dùng một recommendation model toàn cục cho tất cả trẻ. Nên để mỗi trẻ có weight riêng theo category:

- Kid1 xem nhiều `Horror` thì weight `Horror` của Kid1 tăng.
- Kid2 xem nhiều `Brainrot` thì weight `Brainrot` của Kid2 tăng.
- Người chơi chọn `Reduce similar` cho Kid1 thì chỉ giảm weight tương tự của Kid1.

Điều này làm gameplay rõ hơn: người chơi phải quan sát từng trẻ, không chỉ dọn một feed chung.

### Trạng thái tâm lý riêng từng trẻ

Mỗi trẻ có chỉ số riêng:

- anxiety
- fear
- agitation
- focus
- attachment

Video hiện tại của trẻ nào thì chỉ tác động lên trẻ đó. Solution Card cũng nên gắn với trẻ cụ thể, ví dụ:

- "Kid1 đang lo lắng vì xem nhiều nội dung kinh dị."
- "Kid2 bị cuốn vào nội dung lặp nhanh, nên giảm đề xuất tương tự."

## Data video nên thiết kế thế nào

Tạo `ContentItem` dạng ScriptableObject:

- `id`
- `title`
- `channelName`
- `thumbnail`
- `contentType`
- `ageRating`
- `riskLevel`
- `psychologicalEffects`
- `tags`
- `solutionHint`

`contentType` nên có:

- `Positive`
- `NeutralEntertainment`
- `Horror`
- `Violence`
- `Brainrot`
- `AgeInappropriate`

`psychologicalEffects` có thể gồm:

- anxiety delta
- fear delta
- agitation delta
- focus drain
- attachment/addiction delta

## Gameplay loop đề xuất

1. Mỗi `ChildSessionController` chọn video riêng dựa trên weight riêng.
2. Mỗi trẻ xem video hiện tại trong một khoảng thời gian.
3. Mỗi vài giây, video cộng/trừ chỉ số tâm lý của đúng trẻ đó.
4. `ChildStatusController` đổi mood khi chỉ số vượt ngưỡng.
5. `KidAnimationController` của trẻ đó đổi animation.
6. Người chơi click/inspect một trẻ hoặc thiết bị.
7. Người chơi chọn can thiệp cho trẻ đang được inspect:
   - Remove content
   - Reduce similar
   - Recommend positive
   - Observe
8. Feed weight của trẻ đó thay đổi.
9. Nếu mood xấu vượt ngưỡng, hiện Solution Card gắn với trẻ đó.

## Thứ tự làm nên đi

1. Tạo data model cho content và child status.
2. Tạo 10-15 video mẫu bằng ScriptableObject.
3. Tạo `ChildSessionController` cho Kid1 trước.
4. Tạo feed UI đơn giản chạy trong Canvas thường.
5. Gắn feed UI lên `PhoneScreen`.
6. Tạo `KidAnimationController` đổi animation bằng CrossFade.
7. Tạo `ChildStatusController` và map content effect -> mood.
8. Tạo panel inspect/intervention cho child được chọn.
9. Nhân đôi thử nghiệm thêm Kid2 với session riêng.
10. Gắn TV hoặc thiết bị thứ hai vào session của Kid2.
11. Tạo Solution Card theo từng child.
12. Polish thumbnail, text, animation blend, âm thanh, feedback.

## Rủi ro cần chú ý

- Animation FBX có thể khác rig/avatar, cần test từng clip trên Kid1.
- Nếu state name trong Animator đổi, script CrossFade sẽ lỗi im lặng hoặc không đổi animation.
- World Space Canvas trên phone dễ bị nhỏ/mờ; cần scale và Canvas resolution hợp lý.
- Feed kiểu YouTube nếu làm quá thật sẽ tốn thời gian, nên ưu tiên cảm giác nhận diện hơn là đầy đủ tính năng.
- Trạng thái tâm lý phải có decay theo thời gian, nếu không trẻ sẽ chỉ xấu dần và gameplay thiếu đường hồi phục.
- Khi có nhiều trẻ, cần UI chọn/inspect trẻ rõ ràng để người chơi biết mình đang can thiệp vào ai.

## Kết luận

Nên bắt đầu bằng Hướng A nhưng thiết kế ngay theo multi-child: data-driven feed + mỗi trẻ một `ChildSessionController` + mood system riêng + CrossFade animation + UGUI gắn vào thiết bị. Khi Kid1 chạy ổn thì thêm Kid2 sẽ là nhân bản session và bind thiết bị, không phải viết lại core loop.
