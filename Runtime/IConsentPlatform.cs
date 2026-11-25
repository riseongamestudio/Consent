using System;

namespace RiseOn.Consent {
    /// <summary>The platform side of consent: Google's UMP as the CMP and, on iOS, App Tracking Transparency.</summary>
    internal interface IConsentPlatform {
        /// <summary>
        /// Updates the consent info and shows the form where the law asks for it; <paramref name="onDone"/> gets the error, or null, and may come on any thread.
        /// </summary>
        void Request(bool tagForUnderAgeOfConsent, ConsentDebugGeography debugGeography, string[] testDeviceHashedIds, Action<ConsentException> onDone);

        bool CanRequestAds { get; }
        bool PrivacyOptionsRequired { get; }

        /// <summary>Read back from the IAB TCF keys the CMP wrote.</summary>
        bool GdprApplies { get; }

        /// <summary>The TCF purpose 1 consent (store and access information on a device).</summary>
        bool HasPurposeOneConsent { get; }

        /// <summary>IABGPP_HDR_GppString, or empty.</summary>
        string GppString { get; }

        /// <summary>IABGPP_GppSID, or empty.</summary>
        string GppSectionIds { get; }

        /// <summary><paramref name="onClosed"/> gets the error, or null, and may come on any thread.</summary>
        void ShowPrivacyOptions(Action<ConsentException> onClosed);

        TrackingStatus TrackingStatus { get; }

        /// <summary>Shows the system prompt; <paramref name="onDone"/> may come on any thread.</summary>
        void RequestTracking(Action<TrackingStatus> onDone);

        /// <summary>
        /// The privacy notice as the platform's own dialog: its policy button opens <paramref name="url"/> and leaves the dialog up, and <paramref name="onContinued"/>, which may come on any thread, runs once the player continues.
        /// </summary>
        void ShowNotice(string title, string message, string policyButton, string continueButton, string url, Action onContinued);
    }

    internal static class ConsentPlatform {
        public static IConsentPlatform Installed { get; private set; }

        /// <summary>The Editor plays as iOS: its build target is iOS. Set by the Editor's platform, which can read the target.</summary>
        public static bool EditorAsIOS { get; set; }

        /// <summary>The PlayerPrefs keys the Editor's stand-in keeps the choices in; the settings page's Reset deletes them.</summary>
        public static readonly string[] EditorChoiceKeys = { EditorDecidedKey, EditorConsentKey, EditorOptOutKey, EditorTrackingKey };

        // 0 before any decision, 1 for one outside the GDPR regions, 2 for one inside them.
        public const string EditorDecidedKey  = "RiseOn.Consent.Editor.Decided";
        public const string EditorConsentKey  = "RiseOn.Consent.Editor.Consent";
        public const string EditorOptOutKey   = "RiseOn.Consent.Editor.OptOut";
        public const string EditorTrackingKey = "RiseOn.Consent.Editor.Tracking";

        /// <summary>EditorPrefs, so each person picks it for themselves: 0 for none, else a <see cref="ConsentErrorKind"/> plus one, as <see cref="Error"/> reads it.</summary>
        public const string EditorFailureKey = "RiseOn.Consent.Editor.Failure";

        public static void Install(IConsentPlatform platform) {
            Installed = platform;
        }

        /// <summary>The kinds the native bridges report, by ordinal: 0 is no error.</summary>
        public static ConsentException Error(int kind, string message) {
            return kind switch {
                0 => null
              , 1 => new ConsentException(ConsentErrorKind.Network, message)
              , 2 => new ConsentException(ConsentErrorKind.Misconfiguration, message)
              , 3 => new ConsentException(ConsentErrorKind.Internal, message)
              , _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message)
            };
        }
    }
}