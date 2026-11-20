using System;
using UnityEngine;

namespace RiseOn.Consent.Platforms.Editor {
    /// <summary>
    /// No CMP runs in the Editor: the player counts as outside every regulated region, with ads allowed and tracking authorized, so a game plays through its consent flow without a form.
    /// </summary>
    internal sealed class ConsentPlatformEditor : IConsentPlatform {
        public void Request(bool tagForUnderAgeOfConsent, ConsentDebugGeography debugGeography, string[] testDeviceHashedIds, Action<ConsentException> onDone) {
            if (debugGeography is not ConsentDebugGeography.Disabled) Debug.Log($"{nameof(ConsentPlatformEditor)}: the Editor shows no consent form, debug geography {debugGeography} has no effect here");
            onDone(null);
        }

        public bool CanRequestAds => true;
        public bool PrivacyOptionsRequired => false;
        public bool GdprApplies => false;
        public bool HasPurposeOneConsent => true;
        public string GppString => "";
        public string GppSectionIds => "";

        public void ShowPrivacyOptions(Action<ConsentException> onClosed) {
            Debug.Log($"{nameof(ConsentPlatformEditor)}: the Editor has no privacy options form");
            onClosed(null);
        }

        public TrackingStatus TrackingStatus => TrackingStatus.Authorized;

        public void RequestTracking(Action<TrackingStatus> onDone) {
            onDone(TrackingStatus.Authorized);
        }
    }
}