using System;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Platforms.Editor {
    /// <summary>
    /// The Editor's stand-in for UMP, App Tracking Transparency and the privacy notice: Editor dialogs in place of the forms, for the region of the settings page's Testing section, with the choices kept across Play sessions as a device keeps them, until the page's Reset.<br/>
    /// The ATT prompt asks only while the build target is iOS; the page can make the request fail, to try the game's handling of a failed flow.
    /// </summary>
    internal sealed class ConsentPlatformEditor : IConsentPlatform {
        private const string DecidedKey  = ConsentPlatform.EditorDecidedKey;
        private const string ConsentKey  = ConsentPlatform.EditorConsentKey;
        private const string OptOutKey   = ConsentPlatform.EditorOptOutKey;
        private const string TrackingKey = ConsentPlatform.EditorTrackingKey;

        // A GPP string with one US national section (id 7) whose sale, sharing and targeted advertising opt-outs are set:
        // all that GppUSOptOut reads of one.
        private const string OptedOutGpp = "DBABL~AAAV";

        private const string OnDevice = "\n\nEditor stand-in: on a device, ";

        private ConsentDebugGeography region = ConsentDebugGeography.Other;

        public void Request(bool tagForUnderAgeOfConsent, ConsentDebugGeography debugGeography, string[] testDeviceHashedIds, Action<ConsentException> onDone) {
            region = debugGeography is ConsentDebugGeography.EEA or ConsentDebugGeography.RegulatedUSState ? debugGeography : ConsentDebugGeography.Other;

            var failure = EditorPrefs.GetInt(ConsentPlatform.EditorFailureKey);
            if (failure != 0) {
                onDone(ConsentPlatform.Error(failure, "the Editor's stand-in fails as Project Settings > RiseOn > Consent > Testing asks"));
                return;
            }

            if (region is not ConsentDebugGeography.EEA) {
                PlayerPrefs.SetInt(DecidedKey, 1);
            } else if (PlayerPrefs.GetInt(DecidedKey) != 2) {
                // Under the age of consent, UMP asks nothing and consent stays withheld.
                if (tagForUnderAgeOfConsent) {
                    PlayerPrefs.SetInt(ConsentKey, 0);
                    PlayerPrefs.SetInt(DecidedKey, 2);
                } else {
                    AskConsent();
                }
            }

            PlayerPrefs.Save();
            onDone(null);
        }

        public bool CanRequestAds => PlayerPrefs.GetInt(DecidedKey) != 0;
        public bool PrivacyOptionsRequired => CanRequestAds && region is ConsentDebugGeography.EEA or ConsentDebugGeography.RegulatedUSState;
        public bool GdprApplies => region is ConsentDebugGeography.EEA && PlayerPrefs.GetInt(DecidedKey) == 2;
        public bool HasPurposeOneConsent => PlayerPrefs.GetInt(ConsentKey) == 1;
        public string GppString => OptedOut ? OptedOutGpp : "";
        public string GppSectionIds => OptedOut ? "7" : "";

        private bool OptedOut => region is ConsentDebugGeography.RegulatedUSState && PlayerPrefs.GetInt(OptOutKey) == 1;

        public void ShowPrivacyOptions(Action<ConsentException> onClosed) {
            switch (region) {
                case ConsentDebugGeography.EEA:
                    AskConsent();
                    break;
                case ConsentDebugGeography.RegulatedUSState:
                    var optOut = EditorUtility.DisplayDialog("Privacy options", $"Opt out of the sale or sharing of your personal data, and of targeted ads?{OnDevice}the AdMob US states message shows here.", "Opt out", "Keep");
                    PlayerPrefs.SetInt(OptOutKey, optOut ? 1 : 0);
                    PlayerPrefs.Save();
                    break;
                default:
                    Debug.Log($"{nameof(ConsentPlatformEditor)}: outside the regulated regions UMP has no privacy options form; pick a region in Project Settings > RiseOn > Consent > Testing");
                    break;
            }

            onClosed(null);
        }

        public TrackingStatus TrackingStatus => ConsentPlatform.EditorAsIOS ? (TrackingStatus) PlayerPrefs.GetInt(TrackingKey) : TrackingStatus.Authorized;

        public void RequestTracking(Action<TrackingStatus> onDone) {
            if (!ConsentPlatform.EditorAsIOS) {
                onDone(TrackingStatus.Authorized);
                return;
            }

            var allowed = EditorUtility.DisplayDialog($"Allow “{Application.productName}” to track your activity across other companies’ apps and websites?", ConsentSettings.Load().TrackingPrompt + OnDevice + "iOS shows this prompt.", "Allow", "Ask App Not to Track");
            var status  = allowed ? TrackingStatus.Authorized : TrackingStatus.Denied;
            PlayerPrefs.SetInt(TrackingKey, (int) status);
            PlayerPrefs.Save();
            onDone(status);
        }

        public void ShowNotice(string title, string message, string policyButton, string continueButton, string url, Action onContinued) {
            while (!EditorUtility.DisplayDialog(title, message + OnDevice + "the platform's own dialog shows this.", continueButton, policyButton)) Application.OpenURL(url);

            onContinued();
        }

        private static void AskConsent() {
            var consent = EditorUtility.DisplayDialog("Consent", $"{Application.productName} asks for your consent to use your personal data for personalized ads.{OnDevice}the AdMob GDPR message shows here.", "Consent", "Do not consent");
            PlayerPrefs.SetInt(ConsentKey, consent ? 1 : 0);
            PlayerPrefs.SetInt(DecidedKey, 2);
            PlayerPrefs.Save();
        }
    }
}