using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// The package's own tracking prompts, one for each language App Store Connect localizes into; a regional variant (English (U.K.), French (Canada), Spanish (Mexico)) falls back to its language's file on the device, so it has no row unless its wording differs (Portuguese).<br/>
    /// A project's empty text takes these, so it gets their fixes.<br/>
    /// The default text, in Info.plist, is for every player whose language has no row; it is English until the project writes its own.
    /// </summary>
    internal static class TrackingPrompts {
        public const string English = "This lets the game show ads that suit you better, which keeps it free.";

        // Apple's language ids: a language, then an optional script and region, as .lproj folders name them.
        private static readonly Regex Format = new(@"^[a-z]{2,3}(-[A-Z][a-z]{3})?(-([A-Z]{2}|[0-9]{3}))?$");

        public static readonly (string code, string name, string text)[] Builtin = {
            ("en", "English", English)
          , ("ar", "Arabic", "يتيح هذا للعبة عرض إعلانات تناسبك أكثر، مما يبقيها مجانية.")
          , ("bn", "Bangla", "এটি গেমটিকে আপনার জন্য আরও উপযুক্ত বিজ্ঞাপন দেখাতে দেয়, যা গেমটিকে বিনামূল্যে রাখে।")
          , ("ca", "Catalan", "Això permet que el joc mostri anuncis més adequats per a tu i que continuï sent gratuït.")
          , ("zh-Hans", "Chinese (Simplified)", "这样游戏可以展示更适合你的广告，并保持免费。")
          , ("zh-Hant", "Chinese (Traditional)", "這樣遊戲可以展示更適合你的廣告，並保持免費。")
          , ("hr", "Croatian", "Ovo igri omogućuje prikazivanje oglasa koji ti više odgovaraju, a tako ostaje besplatna.")
          , ("cs", "Czech", "Díky tomu může hra zobrazovat reklamy, které ti lépe sedí, a zůstat zdarma.")
          , ("da", "Danish", "Det lader spillet vise annoncer, der passer bedre til dig, så det kan forblive gratis.")
          , ("nl", "Dutch", "Hiermee kan de game advertenties tonen die beter bij je passen, zodat hij gratis blijft.")
          , ("fi", "Finnish", "Näin peli voi näyttää sinulle sopivampia mainoksia ja pysyä ilmaisena.")
          , ("fr", "French", "Cela permet au jeu d'afficher des publicités plus adaptées à vous et de rester gratuit.")
          , ("de", "German", "Damit kann das Spiel Werbung zeigen, die besser zu dir passt, und bleibt kostenlos.")
          , ("el", "Greek", "Αυτό επιτρέπει στο παιχνίδι να εμφανίζει διαφημίσεις που σου ταιριάζουν καλύτερα, ώστε να παραμένει δωρεάν.")
          , ("gu", "Gujarati", "આનાથી ગેમ તમને વધુ અનુકૂળ જાહેરાતો બતાવી શકે છે, જેથી તે મફત રહે છે.")
          , ("he", "Hebrew", "כך המשחק יכול להציג מודעות שמתאימות לך יותר, וזה שומר עליו חינמי.")
          , ("hi", "Hindi", "इससे गेम आपके लिए ज़्यादा सही विज्ञापन दिखा पाता है, जिससे यह मुफ़्त बना रहता है।")
          , ("hu", "Hungarian", "Így a játék jobban hozzád illő hirdetéseket jeleníthet meg, és ingyenes maradhat.")
          , ("id", "Indonesian", "Ini memungkinkan game menampilkan iklan yang lebih sesuai untukmu, sehingga game tetap gratis.")
          , ("it", "Italian", "Così il gioco può mostrarti annunci più adatti a te e restare gratuito.")
          , ("ja", "Japanese", "あなたに合った広告を表示し、ゲームを無料で提供し続けるために使用されます。")
          , ("kn", "Kannada", "ಇದರಿಂದ ಗೇಮ್ ನಿಮಗೆ ಹೆಚ್ಚು ಸೂಕ್ತವಾದ ಜಾಹೀರಾತುಗಳನ್ನು ತೋರಿಸಬಹುದು, ಇದು ಗೇಮ್ ಅನ್ನು ಉಚಿತವಾಗಿರಿಸುತ್ತದೆ.")
          , ("ko", "Korean", "회원님에게 더 잘 맞는 광고를 표시하여 게임을 무료로 유지하는 데 사용됩니다.")
          , ("ms", "Malay", "Ini membolehkan permainan memaparkan iklan yang lebih sesuai untuk anda, supaya ia kekal percuma.")
          , ("ml", "Malayalam", "ഇതുവഴി ഗെയിമിന് നിങ്ങൾക്ക് കൂടുതൽ അനുയോജ്യമായ പരസ്യങ്ങൾ കാണിക്കാനാകും, അതുവഴി ഗെയിം സൗജന്യമായി തുടരും.")
          , ("mr", "Marathi", "यामुळे गेम तुम्हाला अधिक योग्य जाहिराती दाखवू शकतो, ज्यामुळे तो मोफत राहतो.")
          , ("nb", "Norwegian", "Dette lar spillet vise annonser som passer bedre for deg, slik at det kan forbli gratis.")
          , ("or", "Odia", "ଏହା ଦ୍ୱାରା ଗେମ୍ ଆପଣଙ୍କ ପାଇଁ ଅଧିକ ଉପଯୁକ୍ତ ବିଜ୍ଞାପନ ଦେଖାଇପାରେ, ଯାହା ଏହାକୁ ମାଗଣା ରଖେ।")
          , ("pl", "Polish", "Dzięki temu gra może wyświetlać reklamy lepiej dopasowane do Ciebie i pozostać darmowa.")
          , ("pt-BR", "Portuguese (Brazil)", "Isso permite que o jogo mostre anúncios mais adequados para você e continue gratuito.")
          , ("pt-PT", "Portuguese (Portugal)", "Isto permite que o jogo mostre anúncios mais adequados a si e continue gratuito.")
          , ("pa", "Punjabi", "ਇਸ ਨਾਲ ਗੇਮ ਤੁਹਾਨੂੰ ਵਧੇਰੇ ਢੁਕਵੇਂ ਇਸ਼ਤਿਹਾਰ ਦਿਖਾ ਸਕਦੀ ਹੈ, ਜਿਸ ਨਾਲ ਇਹ ਮੁਫ਼ਤ ਰਹਿੰਦੀ ਹੈ।")
          , ("ro", "Romanian", "Astfel, jocul îți poate arăta reclame mai potrivite pentru tine și rămâne gratuit.")
          , ("ru", "Russian", "Так игра сможет показывать рекламу, которая подходит вам больше, и оставаться бесплатной.")
          , ("sk", "Slovak", "Vďaka tomu môže hra zobrazovať reklamy, ktoré ti viac sedia, a zostať zadarmo.")
          , ("sl", "Slovenian", "Tako lahko igra prikazuje oglase, ki ti bolj ustrezajo, in ostane brezplačna.")
          , ("es", "Spanish", "Esto permite que el juego muestre anuncios más adecuados para ti y siga siendo gratis.")
          , ("sv", "Swedish", "Det låter spelet visa annonser som passar dig bättre, så att det kan förbli gratis.")
          , ("ta", "Tamil", "இதன் மூலம் கேம் உங்களுக்கு மிகவும் பொருத்தமான விளம்பரங்களைக் காட்ட முடியும், இதனால் அது இலவசமாகவே இருக்கும்.")
          , ("te", "Telugu", "దీని ద్వారా గేమ్ మీకు మరింత సరిపోయే ప్రకటనలను చూపగలదు, దాంతో అది ఉచితంగా ఉంటుంది.")
          , ("th", "Thai", "การอนุญาตนี้ช่วยให้เกมแสดงโฆษณาที่เหมาะกับคุณมากขึ้น และทำให้เกมยังคงเล่นได้ฟรี")
          , ("tr", "Turkish", "Bu, oyunun sana daha uygun reklamlar göstermesini ve ücretsiz kalmasını sağlar.")
          , ("uk", "Ukrainian", "Так гра зможе показувати рекламу, яка більше вам підходить, і залишатися безкоштовною.")
          , ("ur", "Urdu", "اس سے گیم آپ کو زیادہ موزوں اشتہارات دکھا سکتی ہے، جس سے یہ مفت رہتی ہے۔")
          , ("vi", "Vietnamese", "Cho phép để game hiển thị quảng cáo phù hợp với bạn hơn, giúp game luôn miễn phí.")
        };

        /// <summary>One language however a folder or a person writes it: zh_Hans and zh-hans are zh-Hans.</summary>
        public static string Key(string code) => code.Replace('_', '-').ToLowerInvariant();

        public static bool IsValid(string code) => Format.IsMatch(code);

        public static (string code, string name, string text)? Find(string code) {
            var key = Key(code);
            foreach (var entry in Builtin) {
                if (Key(entry.code) == key) return entry;
            }

            return null;
        }

        /// <summary>The text in Info.plist, for every language without a row.</summary>
        public static string DefaultOf(ConsentSettings settings) => settings.TrackingPrompt.Length > 0 ? settings.TrackingPrompt : English;

        public static string TextOf(ConsentSettings.TrackingTranslation translation) => translation.Text.Length > 0 ? translation.Text : Find(translation.Language)?.text ?? "";

        /// <summary>Every language with a row, each with the text it shows; each gets its .lproj folder.</summary>
        public static IEnumerable<(string code, string text)> All(ConsentSettings settings) =>
            settings.TrackingTranslations.Select(translation => (translation.Language, TextOf(translation)));

        /// <summary>What keeps the list from building: a code Apple does not read, a language twice, or no text.</summary>
        public static IEnumerable<string> Problems(ConsentSettings settings) {
            var translations = settings.TrackingTranslations;
            for (var i = 0; i < translations.Count; i++) {
                var code = translations[i].Language;
                var row  = $"Tracking prompt row {i + 1}";
                if (code.Length == 0) {
                    yield return $"{row} has no language code.";
                } else if (!IsValid(code)) {
                    yield return $"{row}: \"{code}\" is not an Apple language code; write it like vi, pt-BR or zh-Hans.";
                } else if (TextOf(translations[i]).Length == 0) {
                    yield return $"{row}: Consent has no {code} translation, so the row needs a text.";
                }
            }

            var rows = translations.Select((translation, i) => (key: Key(translation.Language), row: i + 1)).Where(entry => entry.key.Length > 0);
            foreach (var group in rows.GroupBy(entry => entry.key).Where(group => group.Count() > 1)) {
                yield return $"Tracking prompt rows {string.Join(", ", group.Select(entry => entry.row))} are all {group.Key}; keep one.";
            }
        }
    }
}