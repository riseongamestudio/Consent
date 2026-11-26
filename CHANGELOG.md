# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.consent` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [Unreleased]

## [2.0.0] - 2026-11-26

Bản lớn vì hai thay đổi game đang dùng pack thấy ngay khi nâng: hộp thoại thông báo
mới ở lần mở game đầu tiên, và danh sách câu xin tracking theo ngôn ngữ bị bỏ.

### Thêm

- Mục **Privacy Policy URL** trên trang settings, điền sẵn trang của RiseOn, tách
  được thành trang riêng cho Android và iOS; `ConsentFlow.PrivacyPolicyUrl` và `ConsentFlow.OpenPrivacyPolicy()` cho nút chính
  sách trong game.
- Trong Editor, khi Play, hộp thoại vẽ trong Game view thay cho form UMP, popup ATT và
  hộp thoại thông báo, theo dáng của bản thật và theo Region của mục Testing; như trên
  máy, chỉ popup ATT dừng game (`IEditorPauseHandler` của `com.riseon.utils`). Lựa
  chọn giữ qua các lần Play; **Editor failure** cho UMP giả lỗi, **Reset** quên các
  lựa chọn. Ở batchmode, hộp thoại thông báo coi như bấm tiếp tục, các hộp thoại khác
  nhận nút thứ hai.
- Phụ thuộc `com.unity.ugui`, cho các hộp thoại trong Editor.

### Đổi

- Ngoài vùng GDPR, lần mở game đầu tiên hiện hộp thoại thông báo chính sách quyền
  riêng tư của hệ điều hành, có nút mở trang **Privacy Policy URL**; người chơi đã mở
  game bản cũ cũng gặp nó một lần sau khi cập nhật.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.utils` 2.0.0.

### Bỏ

- Danh sách câu xin tracking theo ngôn ngữ và các file `InfoPlist.strings` pack tạo:
  câu xin tracking giờ chỉ có một câu trong `Info.plist`. Project từng có danh sách
  thì build iOS kiểu Replace một lần để bỏ các thư mục `.lproj` pack đã tạo.

## [1.0.2] - 2026-11-20

### Đổi

- Cần Unity 6000.3, bản đang dùng để phát triển, thay cho 6000.0 chưa từng được thử:
  `package.json` ghi `unity`, nên Package Manager không cài pack vào project Unity cũ
  hơn.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.utils` 1.0.18.

## [1.0.1] - 2026-11-20

### Sửa

- Project chưa có Odin Inspector: mọi assembly của pack dùng Odin được bỏ qua, và chỉ còn một lỗi
  nói pack cần Odin; trước đó project báo thêm hàng loạt lỗi compile khó hiểu.

## [1.0.0] - 2026-11-20

Bản đầu tiên.