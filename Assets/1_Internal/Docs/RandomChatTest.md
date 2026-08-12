# Random Chat Test

## Hanh vi

`KidRandomChatTester` tao du lieu mau de xem nhanh Talk va Emote trong Play Mode:

1. Kid im lang trong thoi gian ngau nhien.
2. Kid yeu cau mot chat dang ranh tu `ChatUIFollowController`.
3. Controller chon ngau nhien trong cac `allowedChatIds` cua Kid va tranh slot vua dung neu con lua chon khac.
4. Chat chi hien mot trong ba loai `Talk`, `Emote` hoac `Video` trong vai giay.
5. Chat duoc tra lai pool va an di.

Mot chat co `activeUserKidId` se khong duoc cap cho Kid khac. Ba root noi dung loai tru nhau: khi mot root bat, hai root con lai luon tat.

`contentType = Auto` chon ngau nhien trong cac noi dung thuc su co san. Ty le test mac dinh la `Emote 3 : Talk 1`, nen khoang 75% luot hien icon va 25% luot hien text. `Video` chi duoc chon khi `videoAvailable = true`; vi test chua co GIF/video nen no khong duoc bat rong.

## Cau hinh scene

- `assignOneChatPerKidOnStart`: `false`
- Im lang: `2-5` giay
- Hien chat: `2.5-4.5` giay
- Kid test: `Kid1`
- Chat co the dung: `Chat_Kid1`, `Chat_Kid2`

## Du lieu mau

- `Yay!`: `smile`, `laughing`, `love`
- `Whoa!`: `wow`, `surprised`, `shock`
- `Uh-oh`: `aBitWorried`, `upset`, `scared`
- `Meh...`: `boring`, `sleep`, `upset`
- `Not cool`: `angrynormal`, `upset`, `aBitWorried`

## UI Copy Rules

- Chi dung text tieng Anh.
- Uu tien icon; weight mac dinh `Emote 3 : Talk 1`.
- Text chi nen co `1-2` tu hoac mot phan ung rat ngan.
- Khong dua ten emotion, intensity, nguyen nhan video hay huong dan dai vao bong bong.
- Noi dung chi tiet duoc hien trong inspect panel hoac Solution Card.
- Moi luot chi bat mot trong `talkRoot`, `emoteRoot`, `videoRoot`.

Day la component test. Khi he thong cam xuc that bat dau phat chat, tat hoac xoa `KidRandomChatTester` de tranh hai he thong cung yeu cau slot.
