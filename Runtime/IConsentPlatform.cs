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
    }

    internal static class ConsentPlatform {
        public static IConsentPlatform Installed { get; private set; }

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