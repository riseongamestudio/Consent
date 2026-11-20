using System;

namespace RiseOn.Consent {
    /// <summary>
    /// What the consent flow learned, for the packages that pass consent to SDKs which do not read the CMP's IAB strings themselves.
    /// </summary>
    public readonly struct ConsentState : IEquatable<ConsentState> {
        /// <summary>The player is somewhere GDPR applies (EEA, UK, Switzerland).</summary>
        public readonly bool GdprApplies;

        /// <summary>The CMP lets ads be requested at all: a decision exists, or none is needed where the player is.</summary>
        public readonly bool CanRequestAds;

        /// <summary>The player let the game store and read information on the device (TCF Purpose 1), the consent ad SDKs take as a whole, where they had to be asked; true where nobody asks.</summary>
        public readonly bool HasUserConsent;

        /// <summary>The iOS App Tracking Transparency answer; <see cref="RiseOn.Consent.TrackingStatus.Authorized"/> on other platforms.</summary>
        public readonly TrackingStatus TrackingStatus;

        /// <summary>The player opted out of the sale or sharing of their data, or of targeted ads, under a US state law.</summary>
        public readonly bool DoNotSell;

        /// <summary>The game is made for children: no personalization, no tracking.</summary>
        public readonly bool ChildDirected;

        /// <summary>The law where the player is requires a way to change their choices; the game then shows a privacy button.</summary>
        public readonly bool PrivacyOptionsRequired;

        public bool TrackingAuthorized => TrackingStatus is TrackingStatus.Authorized;

        public ConsentState(bool gdprApplies, bool canRequestAds, bool hasUserConsent, TrackingStatus trackingStatus, bool doNotSell, bool childDirected, bool privacyOptionsRequired) {
            GdprApplies            = gdprApplies;
            CanRequestAds          = canRequestAds;
            HasUserConsent         = hasUserConsent;
            TrackingStatus         = trackingStatus;
            DoNotSell              = doNotSell;
            ChildDirected          = childDirected;
            PrivacyOptionsRequired = privacyOptionsRequired;
        }

        public bool Equals(ConsentState other) =>
            GdprApplies == other.GdprApplies
         && CanRequestAds == other.CanRequestAds
         && HasUserConsent == other.HasUserConsent
         && TrackingStatus == other.TrackingStatus
         && DoNotSell == other.DoNotSell
         && ChildDirected == other.ChildDirected
         && PrivacyOptionsRequired == other.PrivacyOptionsRequired;

        public override bool Equals(object obj) => obj is ConsentState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(GdprApplies, CanRequestAds, HasUserConsent, TrackingStatus, DoNotSell, ChildDirected, PrivacyOptionsRequired);

        public override string ToString() =>
            $"gdpr {GdprApplies}, can request ads {CanRequestAds}, consent {HasUserConsent}, tracking {TrackingStatus}, do not sell {DoNotSell}, child {ChildDirected}, privacy options {PrivacyOptionsRequired}";
    }

    /// <summary>The ordinals match iOS's ATTrackingManagerAuthorizationStatus.</summary>
    public enum TrackingStatus {
        NotDetermined
      , Restricted
      , Denied
      , Authorized
    }
}