using System;
using UnityEngine;

namespace RiseOn.Consent.Platforms.Android {
    internal sealed class ConsentPlatformAndroid : IConsentPlatform {
        private const string BridgeClass = "com.riseon.consent.Ump";

        private static AndroidJavaObject Activity {
            get {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                return player.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }

        public void Request(bool tagForUnderAgeOfConsent, ConsentDebugGeography debugGeography, string[] testDeviceHashedIds, Action<ConsentException> onDone) {
            using var bridge   = new AndroidJavaClass(BridgeClass);
            using var activity = Activity;
            bridge.CallStatic("request", activity, tagForUnderAgeOfConsent, (int)debugGeography, testDeviceHashedIds, new Listener(onDone));
        }

        public bool CanRequestAds => Call<bool>("canRequestAds");
        public bool PrivacyOptionsRequired => Call<bool>("isPrivacyOptionsRequired");
        public bool GdprApplies => Call<bool>("gdprApplies");
        public bool HasPurposeOneConsent => Call<bool>("hasPurposeOneConsent");
        public string GppString => Call<string>("gppString");
        public string GppSectionIds => Call<string>("gppSectionIds");

        public void ShowPrivacyOptions(Action<ConsentException> onClosed) {
            using var bridge   = new AndroidJavaClass(BridgeClass);
            using var activity = Activity;
            bridge.CallStatic("showPrivacyOptions", activity, new Listener(onClosed));
        }

        // App Tracking Transparency is iOS only.
        public TrackingStatus TrackingStatus => TrackingStatus.Authorized;

        public void RequestTracking(Action<TrackingStatus> onDone) {
            onDone(TrackingStatus.Authorized);
        }

        private static T Call<T>(string method) {
            using var bridge   = new AndroidJavaClass(BridgeClass);
            using var activity = Activity;
            return bridge.CallStatic<T>(method, activity);
        }

        private sealed class Listener : AndroidJavaProxy {
            private readonly Action<ConsentException> callback;

            public Listener(Action<ConsentException> callback) : base(BridgeClass + "$Listener") {
                this.callback = callback;
            }

            // Called by name from Java.
            public void onDone(int errorKind, string message) {
                callback?.Invoke(ConsentPlatform.Error(errorKind, message));
            }
        }
    }
}