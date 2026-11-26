# RiseOn.Consent

Luồng xin đồng ý mà mọi SDK quảng cáo, analytics, attribution phải chờ: form
GDPR hoặc luật bang Mỹ của Google UMP, hộp thoại thông báo chính sách quyền riêng tư
ở lần mở đầu tiên, rồi popup App Tracking Transparency trên iOS. Luồng tự chạy khi
mở game; bên nào cần chờ thì `await ConsentFlow.WaitUntilCompleteAsync()`.

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
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Các mục của trang cài đặt |
| `com.riseon.utils` 2.0.0 | Tự cài theo `package.json` | Các mục của trang cài đặt, dừng game dưới popup ATT trong Editor |
| `com.cysharp.unitask` 2.5.11 | Tự cài theo `package.json`, từ OpenUPM | Các hàm `async` |
| `com.google.external-dependency-manager` 1.2.189 | Tự cài theo `package.json`, từ OpenUPM | Đưa SDK UMP native vào build |
| `com.unity.modules.androidjni` 1.0.0 | Tự cài theo `package.json`, có sẵn trong Unity | Gọi UMP trên Android |
| `com.unity.ugui` 2.0.0 | Tự cài theo `package.json`, có sẵn trong Unity | Hộp thoại thay form trong Editor |
| Một app trên AdMob | Tạo trên web AdMob | UMP tìm form của game theo app id, kể cả khi game không hiện quảng cáo AdMob |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Odin không có trên UPM nên phải cài vào
project trước. Thiếu Odin thì project báo một lỗi từ `RiseOn.Consent.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon`, và
OpenUPM với scope `com.cysharp`, `com.google`, vào `Packages/manifest.json`, rồi thêm
package. Scope nào project đã có thì bỏ qua; registry nào khai scope cụ thể hơn (ví
dụ `com.google.firebase`) vẫn được ưu tiên cho các gói đó.

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon"]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp", "com.google"]
    }
  ],
  "dependencies": {
    "com.riseon.consent": "2.0.0"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry, nên manifest cần OpenUPM như ví
dụ trên; `com.riseon.utils` lấy từ registry RiseOn, hoặc cài bằng git URL trước. Rồi
*Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Consent.git
```

**Thư mục local**: `"com.riseon.consent": "file:D:/path/to/Consent"`.

## Luồng

1. Scene đầu load xong và app đang active (iOS bỏ qua popup ATT khi app chưa
   active), luồng tự bắt đầu.
2. **UMP** hỏi server ở mọi lần mở game, rồi hiện form nếu luật ở vùng đó yêu
   cầu và người chơi chưa chọn. Vùng nào áp luật, form có gì: cấu hình trên web
   AdMob, mục Privacy & messaging. Nếu ở đó bật màn giải thích IDFA thì UMP hiện
   màn đó và tự hỏi ATT.
3. **Thông báo chính sách**: ngoài vùng GDPR (nơi UMP không hiện form lúc mở game),
   lần mở game đầu tiên hiện hộp thoại của hệ điều hành, tiếng Anh: nút
   **Read Privacy Policy** mở trang chính sách và giữ hộp thoại lại, nút **Continue**
   đóng nó. Hiện xong thì pack ghi một cờ vào PlayerPrefs và không hiện lại. Trong
   vùng GDPR, form của UMP đã là thông báo nên hộp thoại không hiện.
4. **ATT** trên iOS: xong các bước trên mà trạng thái tracking vẫn `NotDetermined`
   thì luồng gọi popup hệ thống. Game cho trẻ em không hỏi.
5. Luồng đọc [`ConsentState`](#consentstate), gán vào `ConsentFlow.State` và
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

// Nút chính sách quyền riêng tư trong màn cài đặt: luôn hiện.
policyButton.onClick.AddListener(ConsentFlow.OpenPrivacyPolicy);
```

Google Play và App Store bắt mọi app có link chính sách quyền riêng tư bên trong app,
nên mọi game cần nút gọi `OpenPrivacyPolicy`. Pack không kiểm được game có nút đó hay
không.

| API | Ý nghĩa |
|---|---|
| `WaitUntilCompleteAsync()` | Hoàn thành trên thread của Unity, trả `ConsentState` khi luồng xong |
| `State` | `ConsentState?`; `null` khi luồng chưa xong |
| `ShowPrivacyOptionsAsync()` | Mở lại form để người chơi đổi lựa chọn. Gọi khi luồng chưa xong thì ném `InvalidOperationException`; form lỗi thì ném `ConsentException` |
| `StateChanged` | Chỉ phát khi lựa chọn mới khác lựa chọn cũ; đối số là `State` mới |
| `PrivacyPolicyUrl` | Trang chính sách của nền tảng đang chạy, theo mục Privacy Policy URL; trong Editor là của build target đang chọn |
| `OpenPrivacyPolicy()` | Mở `PrivacyPolicyUrl` bằng trình duyệt |

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
| AdMob App ID | Hai ô Android và iOS trên một dòng; cửa sổ hẹp thì mỗi ô một dòng, nút **Test** liền sau mỗi ô. Lấy ở AdMob > Apps > App settings. Build ghi vào manifest và `Info.plist`. Xem [App id AdMob](#app-id-admob) |
| Child Directed | Game cho trẻ em: UMP coi mọi người chơi là chưa đủ tuổi đồng ý, iOS không hỏi ATT, build không ghi câu xin tracking và trang ẩn mục Tracking prompt |
| Privacy Policy URL | Trang chính sách mà nút trong game và hộp thoại thông báo mở, một trang cho cả hai nền tảng, điền sẵn trang của RiseOn. Nút cuối dòng tách thành hai ô Android và iOS, bắt đầu từ trang đang có; tách rồi thì nút đó nối lại, giữ trang của Android. Ô trống hoặc không phải địa chỉ web thì chặn build |
| Tracking prompt (iOS) | Câu dưới tiêu đề popup ATT, tiếng Anh, ghi vào `Info.plist`. Để trống là câu của pack; nút mũi tên vòng trong đầu phải ô quay về câu đó |
| Testing the form | Gập sẵn; trong có Region, hai dòng cho Editor, và danh sách Test devices ở cuối, gập được. Xem [Thử form](#thử-form) |

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

## Khi build

Pack này là nơi duy nhất quyết định app id AdMob và câu xin tracking. Bước build
của pack chạy sau mọi plugin khác mà nó có thể chạy sau, rồi ghi các giá trị đó:

| Nền tảng | Ghi gì |
|---|---|
| Android | App id vào manifest của `unityLibrary` |
| iOS | App id và câu xin tracking vào `Info.plist`; link yếu framework AppTrackingTransparency |

**Build dừng lại** khi:

| Trường hợp | Sửa |
|---|---|
| Build cho nền tảng khác Android và iOS | Pack chỉ chạy trên Android và iOS: bỏ pack khỏi project khi build cho nền tảng khác |
| Project chưa có asset settings của Consent | Nút **Create** trên trang settings |
| Asset settings nằm ngoài thư mục `Resources`: game không đọc được, chạy với giá trị mặc định | Nút **Move it** |
| Thiếu app id AdMob cho nền tảng đang build, hoặc app id sai dạng `ca-app-pub-<số>~<số>` | Điền ở trang settings |
| URL chính sách của nền tảng đang build trống, hoặc không phải địa chỉ trang web `https://` | Sửa ở trang settings |
| App id trong file settings của Google Mobile Ads, AppLovin MAX hoặc LevelPlay bị sửa riêng, khác của project | Nút **Use it** trên trang settings lấy giá trị đó làm app id của project; hoặc sửa lại trong cửa sổ của SDK. Xem [App id AdMob](#app-id-admob) |
| Google Mobile Ads có câu xin tracking riêng | Nút **Clear it** |
| AppLovin MAX bật Consent Flow riêng (người chơi sẽ gặp hai luồng) | Nút **Turn it off** |
| Một plugin khác đã ghi app id hoặc câu xin tracking vào `Info.plist` với giá trị khác | Bỏ giá trị đó ở plugin kia; log build ghi hai giá trị |

Game cho trẻ em không có câu xin tracking ở đâu cả: plugin khác ghi câu này thì
build cũng dừng, vì App Store coi khóa đó là app có tracking.

Build iOS kiểu Append giữ lại project Xcode cũ. Pack ghi các giá trị của lần
build trước vào `RiseOnConsent.json` cạnh project, để không nhầm giá trị của chính
mình là của plugin khác.

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

Trong Editor, khi Play, pack vẽ trong Game view các hộp thoại thay cho form UMP, popup
ATT và hộp thoại thông báo, theo vùng chọn trong **Region** (Disabled là ngoài mọi
vùng có luật):

| Region | Lúc mở game | Nút privacy (`ShowPrivacyOptionsAsync`) |
|---|---|---|
| EEA | Form đồng ý, nếu chưa chọn ở vùng này | Form đồng ý lần nữa |
| RegulatedUSState | Không có gì | Hỏi từ chối bán hoặc chia sẻ dữ liệu, ra `DoNotSell` |
| Disabled, Other | Không có gì | Không có form, chỉ log |

Popup ATT chỉ hiện khi build target là iOS; target khác thì tracking luôn
`Authorized`. Hộp thoại thông báo hiện theo đúng luật trên thiết bị.

Hộp thoại giống bản thật trên máy:

- Form UMP là thẻ trắng giữa màn hình, nút đồng ý tô xanh ở trên. Popup ATT là alert
  kiểu iOS. Hộp thoại thông báo là alert kiểu iOS khi build target là iOS, kiểu
  Android với target khác; nút mở trang chính sách mở trang và để hộp thoại lại.
- Hộp thoại phủ mờ cả game và nhận mọi lần chạm, trên mọi canvas của game. Scene chưa
  có `EventSystem` thì pack tạo một cái, dùng Input System khi project có.
- Chỉ popup ATT dừng game, như iOS dừng Unity khi alert hệ thống hiện: pack gọi
  `IEditorPauseHandler` của `com.riseon.utils`, game muốn dừng theo cách riêng thì
  xem [EditorPauseHandler](https://github.com/riseongamestudio/Utils/blob/main/Core/Runtime/EditorPauseHandler/README.md).
  Form UMP và hộp thoại thông báo không dừng game.

Khi Unity chạy batchmode (test tự động, build bằng dòng lệnh) không ai bấm được: mọi
hộp thoại nhận nút thứ hai (không đồng ý, `Denied`, giữ bán dữ liệu), riêng hộp thoại
thông báo coi như bấm tiếp tục. Lựa chọn đó ghi vào PlayerPrefs, chung với Editor mở
thường của cùng project (cùng Company Name và Product Name), nên sau đó Play không hỏi
lại cho tới khi bấm **Reset**. Chạy test batchmode trên một bản chép của project thì
đổi Product Name của bản chép để nó có PlayerPrefs riêng.

Lựa chọn được giữ qua các lần Play, như máy thật giữ. Hai dòng dưới Region:

- **Editor failure**: cho lần gọi UMP giả lỗi theo loại chọn, để thử nhánh
  [Khi UMP lỗi](#khi-ump-lỗi). Lưu riêng cho từng người (EditorPrefs), không vào
  project.
- **Editor choices > Reset**: quên mọi lựa chọn và cờ của hộp thoại thông báo, để
  Play lại như lần mở đầu tiên.

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
