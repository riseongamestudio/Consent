# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.consent` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [Unreleased]

### Thêm

- Mục **Privacy Policy URL** trên trang settings, điền sẵn trang của RiseOn, tách
  được thành trang riêng cho Android và iOS; `ConsentFlow.PrivacyPolicyUrl` và `ConsentFlow.OpenPrivacyPolicy()` cho nút chính
  sách trong game.
- Hộp thoại thông báo chính sách quyền riêng tư của hệ điều hành ở lần mở game đầu
  tiên, ngoài vùng GDPR.
- Trong Editor, hộp thoại thay cho form UMP, popup ATT và hộp thoại thông báo theo
  Region của mục Testing, lựa chọn giữ qua các lần Play; **Editor failure** cho UMP
  giả lỗi, **Reset** quên các lựa chọn.

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