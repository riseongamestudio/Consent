# RiseOn.Consent

Luồng xin đồng ý mà mọi SDK quảng cáo, analytics, attribution phải chờ: form
GDPR hoặc luật bang Mỹ của Google UMP, rồi popup App Tracking Transparency trên
iOS. Luồng tự chạy khi mở game; bên nào cần chờ thì
`await ConsentFlow.WaitUntilCompleteAsync()`.

Package `com.riseon.consent`, namespace `RiseOn.Consent`. Chỉ build cho Android
và iOS; build cho nền tảng khác bị dừng.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Luồng](#luồng)
- [Dùng trong game](#dùng-trong-game)
- [Khi UMP lỗi](#khi-ump-lỗi)
- [Cài đặt của project](#cài-đặt-của-project)
- [Khi build](#khi-build)
- [`ConsentState`](#consentstate)
- [Thử form](#thử-form)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.0 | | |
| `com.cysharp.unitask` | OpenUPM, tự cài theo `package.json` | Các hàm `async` |
| `com.google.external-dependency-manager` | OpenUPM, tự cài theo `package.json` | Đưa SDK UMP native vào build |
| `com.unity.modules.androidjni` | Có sẵn trong Unity | Gọi UMP trên Android |
| `com.riseon.utils` | Registry RiseOn, tự cài theo `package.json` | Các mục của trang cài đặt |
| Odin Inspector | Cài tay từ Asset Store | `com.riseon.utils` cần nó |
| Một app trên AdMob | Tạo trên web AdMob | UMP tìm form của game theo app id, kể cả khi game không hiện quảng cáo AdMob |

## Cài đặt

Thêm hai registry vào `Packages/manifest.json`. Scope nào project đã có thì
bỏ qua; registry nào khai scope cụ thể hơn (ví dụ `com.google.firebase`) vẫn
được ưu tiên cho các gói đó.

```json
"scopedRegistries": [
  {
    "name": "RiseOn",
    "url": "https://upm.riseongamestudio.com",
    "scopes": [ "com.riseon" ]
  },
  {
    "name": "OpenUPM",
    "url": "https://package.openupm.com",
    "scopes": [ "com.cysharp", "com.google" ]
  }
]
```

rồi thêm gói vào `dependencies`:

```json
"com.riseon.consent": "1.0.0"
```

## Luồng

1. Scene đầu load xong và app đang active (iOS bỏ qua popup ATT khi app chưa
   active), luồng tự bắt đầu.
2. **UMP** hỏi server ở mọi lần mở game, rồi hiện form nếu luật ở vùng đó yêu
   cầu và người chơi chưa chọn. Vùng nào áp luật, form có gì: cấu hình trên web
   AdMob, mục Privacy & messaging. Nếu ở đó bật màn giải thích IDFA thì UMP hiện
   màn đó và tự hỏi ATT.
3. **ATT** trên iOS: xong UMP mà trạng thái tracking vẫn `NotDetermined` thì
   luồng gọi popup hệ thống. Game cho trẻ em không hỏi.
4. Luồng đọc [`ConsentState`](#consentstate), gán vào `ConsentFlow.State` và
   hoàn thành task mà các bên đang chờ.

UMP lưu lựa chọn của người chơi trên máy (khóa `IABTCF_*` và `IABGPP_*` trong
SharedPreferences, NSUserDefaults). Google Mobile Ads, AppLovin MAX, LevelPlay
tự đọc các khóa này. Trong game, chỉ pack này được gọi UMP; code khác gọi UMP
thì hai luồng chạy chồng lên nhau.

UMP chạy qua cầu native riêng của pack (Java, Objective-C), không qua plugin
Google Mobile Ads, nên project chỉ dùng MAX hoặc LevelPlay vẫn có form.

## Dùng trong game

```csharp
using RiseOn.Consent;

// SDK cần đợi người chơi chọn xong: chờ luồng. Luồng đã xong thì trả về ngay.
var state = await ConsentFlow.WaitUntilCompleteAsync();

// Nút privacy trong màn cài đặt: chỉ hiện khi luật bắt buộc.
privacyButton.SetActive(ConsentFlow.State is { PrivacyOptionsRequired: true });
await ConsentFlow.ShowPrivacyOptionsAsync();

// Người chơi đổi lựa chọn trong form privacy.
ConsentFlow.StateChanged += newState => Apply(newState);
```

| API | Ý nghĩa |
|---|---|
| `WaitUntilCompleteAsync()` | Hoàn thành trên thread của Unity, trả `ConsentState` khi luồng xong |
| `State` | `ConsentState?`; `null` khi luồng chưa xong |
| `ShowPrivacyOptionsAsync()` | Mở lại form để người chơi đổi lựa chọn. Gọi khi luồng chưa xong thì ném `InvalidOperationException`; form lỗi thì ném `ConsentException` |
| `StateChanged` | Chỉ phát khi lựa chọn mới khác lựa chọn cũ; đối số là `State` mới |

## Khi UMP lỗi

UMP cần mạng ở mọi lần mở game. Khi gọi lỗi:

- **Máy đã có lựa chọn từ lần mở trước** (người chơi đã chọn, hoặc ở vùng không
  cần hỏi): luồng tự xong với lựa chọn đó, như Google hướng dẫn. Game không phải
  làm gì; lỗi chỉ ghi vào log.
- **Máy chưa có lựa chọn nào:** luồng dừng lại. Mọi bên đang chờ
  `WaitUntilCompleteAsync()` đứng yên cho tới khi game quyết định.

Đúng một chỗ trong game (thường là màn boot) nhận kết quả và quyết định:

```csharp
var result = await ConsentFlow.PopResultAsync();
while (result is ConsentFailed failed) {
    if (failed.Error.Kind is not ConsentErrorKind.Network) {
        // Thử lại không khỏi: chơi tiếp, lần mở này không có quảng cáo.
        await failed.ContinueAsync();
        break;
    }

    await noConnectionPopup.ShowAsync();
    result = await failed.RetryAsync();
}
```

| API | Ý nghĩa |
|---|---|
| `PopResultAsync()` | Kết quả của lần chạy hiện tại: `ConsentSucceeded` hoặc `ConsentFailed`. Mỗi lần chạy chỉ lấy ra được một lần; lần thứ hai ném `InvalidOperationException`, để một lỗi không tới hai chỗ xử lý |
| `ConsentFailed.Error` | `ConsentException`; `Kind` cho biết loại lỗi |
| `ConsentFailed.RetryAsync()` | Chạy lại luồng, trả kết quả mới cho chính bên gọi |
| `ConsentFailed.ContinueAsync()` | Xong luồng mà không có lựa chọn: `CanRequestAds` là `false` tới hết lần mở này, lần mở sau hỏi lại |

| `ConsentErrorKind` | Khi nào | Nên làm |
|---|---|---|
| `Network` | Không có mạng, hoặc server UMP không trả lời kịp | Báo người chơi rồi thử lại |
| `Misconfiguration` | App id AdMob sai, hoặc message trên Privacy & messaging cấu hình sai | Người chơi không sửa được: đi tiếp. Dev sửa cấu hình |
| `Internal` | UMP tự lỗi | Thử lại vài lần rồi đi tiếp |

Mỗi `ConsentFailed` chỉ thử lại hoặc đi tiếp được một lần. Game không lấy kết quả
ra thì luồng chờ mãi; lỗi chỉ hiện trong log.

## Cài đặt của project

Mở bằng menu **RiseOn > Consent**, hoặc Edit > Project Settings > RiseOn >
Consent. Trang này lưu vào `Assets/RiseOn/Resources/RiseOnConsentSettings.asset`;
game đọc asset này lúc chạy. Inspector của asset chỉ có nút mở trang này.

Đầu trang liệt kê những gì đang chặn build (đỏ) và cảnh báo không chặn (vàng), kèm nút
sửa nếu sửa được ngay.

| Mục | Ý nghĩa |
|---|---|
| Child Directed | Game cho trẻ em: UMP coi mọi người chơi là chưa đủ tuổi đồng ý, iOS không hỏi ATT, build không ghi câu xin tracking và trang ẩn mục Tracking prompt |
| AdMob App ID | Hai ô Android và iOS trên một dòng; cửa sổ hẹp thì mỗi ô một dòng, nút **Test** liền sau mỗi ô. Lấy ở AdMob > Apps > App settings. Build ghi vào manifest và `Info.plist`. Xem [App id AdMob](#app-id-admob) |
| Tracking prompt (iOS) | Câu trên popup ATT, theo ngôn ngữ của máy. Xem [Câu xin tracking](#câu-xin-tracking) |
| Testing the form | Gập sẵn; trong có Region và danh sách Test devices. Xem [Thử form](#thử-form) |

### App id AdMob

App id chỉ lưu một bản, trong asset của Consent, vì pack này cài được khi project
không có Google Mobile Ads. Ô này hiện ở trang Consent và dưới từng provider có SDK
cần app id trên trang Ads (AdMob, MAX, và LevelPlay khi bật AdMob Network); sửa ở trang
nào cũng là sửa bản đó.

File settings của Google Mobile Ads, AppLovin MAX và LevelPlay cũng có ô app id. Pack
ghi app id vào các file đó ngay khi sửa, và điền các ô còn trống trước khi build. Ô
trong file của SDK bị sửa riêng (trong cửa sổ của SDK) thì đầu trang báo đỏ, build
dừng, với nút **Use it**: lấy giá trị đó làm app id của project, rồi ghi lại vào mọi
file.

Nút **Test** sau ô của mỗi nền tảng dành cho dev chưa có app trên AdMob mà muốn thử
unit test của Google. Bật thì mọi nơi nhận app id (file settings của ba SDK,
manifest, `Info.plist`) nhận app id mẫu của Google cho nền tảng đó; ô vẫn hiện id
đã điền, tắt thì các nơi đó nhận lại id này. Khi bật, đầu trang và log build có
cảnh báo vàng: form đồng ý và các unit thật của game thuộc app của game, không
thuộc app mẫu, nên tắt trước khi build lên store. App id mẫu lấy từ trang hướng dẫn
Google Mobile Ads:
[Android](https://developers.google.com/admob/android/quick-start),
[iOS](https://developers.google.com/admob/ios/quick-start).

### Câu xin tracking

Ô trên thanh của mục là câu mặc định, ghi vào `Info.plist`: iOS dùng nó cho mọi
ngôn ngữ không có dòng trong danh sách. Để trống thì là câu tiếng Anh của pack; sửa
rồi thì nút mũi tên vòng trong đầu phải ô quay về câu của pack. Mỗi
dòng dưới thanh là một ngôn ngữ; nút **+** trên thanh thêm dòng, nút **×** trên
dòng xóa dòng:

- Chọn một ngôn ngữ trong danh sách: pack có sẵn câu cho 45 ngôn ngữ (cả tiếng Anh),
  theo danh sách ngôn ngữ của App Store Connect. Ô câu hiện câu của pack; sửa thì dùng
  câu của mình, nút mũi tên vòng trong đầu phải ô quay về câu của pack. Câu của pack đổi khi cập
  nhật pack thì project dùng câu mới, trừ dòng đã sửa.
- **Other language**: tự gõ mã ngôn ngữ của Apple (`sw`, `es-419`, `zh-Hant-HK`)
  và câu.

Chỉ thêm ngôn ngữ mà game có. App Store ghi mỗi ngôn ngữ có thư mục `.lproj` trong
app là một ngôn ngữ của game, và mỗi dòng ở đây là một thư mục như thế.

Trang settings báo đỏ, và build iOS dừng, khi:

- Mã sai dạng `vi`, `pt-BR`, `zh-Hans`.
- Một ngôn ngữ xuất hiện hai lần. `VI` và `vi`, `zh_Hans` và `zh-Hans` là một;
  `en` trùng với dòng tiếng Anh. Pack không tự chọn dòng nào, vì hai dòng (ví dụ
  sau khi merge git) có thể có hai câu khác nhau; xóa dòng thừa bằng nút **×**.
- Ngôn ngữ pack không có câu mà ô câu để trống.

## Khi build

Pack này là nơi duy nhất quyết định app id AdMob và câu xin tracking. Bước build
của pack chạy sau mọi plugin khác mà nó có thể chạy sau, rồi ghi các giá trị đó:

| Nền tảng | Ghi gì |
|---|---|
| Android | App id vào manifest của `unityLibrary` |
| iOS | App id và câu tiếng Anh vào `Info.plist`; mỗi ngôn ngữ trong danh sách (cả tiếng Anh) ghi dòng `NSUserTrackingUsageDescription` vào `<ngôn ngữ>.lproj/InfoPlist.strings`; link yếu framework AppTrackingTransparency |

Mỗi ngôn ngữ chỉ được có một file `InfoPlist.strings` vào app. File của ngôn ngữ
đó đã có thì pack chỉ sửa dòng của mình trong file, ví dụ:

- File Unity Localization tạo để dịch tên app (thư mục `zh_Hans.lproj` của nó
  được coi là `zh-Hans`).
- File tiếng Anh có sẵn trong template Xcode của Unity.

Chưa có thì pack tạo file mới, đánh dấu bằng dòng đầu
`/* Generated by com.riseon.consent */`. Bỏ một ngôn ngữ khỏi settings thì lần
build sau xóa dòng đó khỏi file của plugin khác, còn file do pack tạo thì xóa cả
file, thư mục và mục của nó trong project Xcode, để App Store không còn ghi ngôn
ngữ đó.

App có thư mục `.lproj` của một ngôn ngữ (ví dụ Unity Localization dịch tên app)
mà danh sách không có ngôn ngữ đó thì log build có cảnh báo: người chơi ngôn ngữ
đó đọc câu tiếng Anh.

**Build dừng lại** khi:

| Trường hợp | Sửa |
|---|---|
| Build cho nền tảng khác Android và iOS | Pack chỉ chạy trên Android và iOS: bỏ pack khỏi project khi build cho nền tảng khác |
| Project chưa có asset settings của Consent | Nút **Create** trên trang settings |
| Asset settings nằm ngoài thư mục `Resources`: game không đọc được, chạy với giá trị mặc định | Nút **Move it** |
| Thiếu app id AdMob cho nền tảng đang build, hoặc app id sai dạng `ca-app-pub-<số>~<số>` | Điền ở trang settings |
| Danh sách câu xin tracking có mã sai, ngôn ngữ trùng, hoặc dòng thiếu câu (trừ game cho trẻ em) | Sửa ở trang settings, xem [Câu xin tracking](#câu-xin-tracking) |
| App id trong file settings của Google Mobile Ads, AppLovin MAX hoặc LevelPlay bị sửa riêng, khác của project | Nút **Use it** trên trang settings lấy giá trị đó làm app id của project; hoặc sửa lại trong cửa sổ của SDK. Xem [App id AdMob](#app-id-admob) |
| Google Mobile Ads có câu xin tracking riêng | Nút **Clear it** |
| AppLovin MAX bật Consent Flow riêng (người chơi sẽ gặp hai luồng) | Nút **Turn it off** |
| Một plugin khác đã ghi app id, câu xin tracking (kể cả trong `InfoPlist.strings`, kể cả ngôn ngữ danh sách không có) với giá trị khác | Bỏ giá trị đó ở plugin kia, thêm ngôn ngữ đó vào danh sách nếu cần; log build ghi rõ file và hai giá trị |
| Một ngôn ngữ có hơn một file `InfoPlist.strings` vào app (Xcode chỉ giữ một) | Bỏ file thừa ở plugin đã tạo nó; log build liệt kê các file |

Game cho trẻ em không có câu xin tracking ở đâu cả: plugin khác ghi câu này thì
build cũng dừng, vì App Store coi khóa đó là app có tracking.

Build iOS kiểu Append giữ lại project Xcode cũ. Pack ghi các giá trị của lần
build trước, và các ngôn ngữ pack từng tạo file, vào `RiseOnConsent.json` cạnh
project, để không nhầm giá trị của chính mình là của plugin khác. Unity thêm lại
vào project các file mà lần build trước có, kể cả file pack đã xóa; mục đó trỏ tới
file không còn thì Xcode dừng, nên pack gỡ những mục như vậy của mình.

## `ConsentState`

| Trường | Lấy từ |
|---|---|
| `GdprApplies` | `IABTCF_gdprApplies` mà UMP lưu |
| `CanRequestAds` | `canRequestAds` của UMP |
| `HasUserConsent` | Không áp GDPR, hoặc đồng ý Purpose 1 trong `IABTCF_PurposeConsents` |
| `TrackingStatus` | Trạng thái ATT; Android luôn `Authorized` |
| `DoNotSell` | Người chơi chọn không cho bán hoặc chia sẻ dữ liệu theo luật một bang Mỹ, đọc từ chuỗi GPP mà UMP lưu. Chuỗi hỏng thì coi là `true` |
| `ChildDirected` | Mục Child Directed của project |
| `PrivacyOptionsRequired` | UMP yêu cầu game có nút mở lại form |

Các SDK tự đọc khóa IAB thì không cần trường nào ở đây. Pack quảng cáo dùng
`DoNotSell` cho SDK nào yêu cầu truyền tay (AppLovin MAX `SetDoNotSell`,
LevelPlay `SetCCPA`).

## Thử form

Mục **Testing the form** chỉ có tác dụng trên bản Development Build, và chỉ trên
máy có trong danh sách **Test devices** của mục. Máy đó hiện form như đang ở vùng
chọn trong **Region**.

Nút **+** trên thanh **Test devices** thêm một dòng để dán id của máy. Lấy id của
máy: chạy game một lần, rồi tìm trong log dòng UMP in ra.

| Nền tảng | Tìm trong log |
|---|---|
| Android | `addTestDeviceHashedId` (logcat) |
| iOS | `testDeviceIdentifiers` (console Xcode) |

Trong Editor không có form: người chơi được coi là ở ngoài mọi vùng có luật,
được phép quảng cáo và đã cho tracking.

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
