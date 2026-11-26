using System;
using RiseOn.Utils;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Platforms.Editor {
    /// <summary>
    /// The Editor's stand-in for UMP, App Tracking Transparency and the privacy notice: dialogs drawn in the Game view in the look of the forms, for the region of the settings page's Testing section, with the choices kept across Play sessions as a device keeps them, until the page's Reset.<br/>
    /// As on a device, only the ATT prompt pauses the game, and it asks only while the build target is iOS. The page can make the request fail, to try the game's handling of a failed flow.<br/>
    /// Batch mode has nobody to answer: each dialog takes its second button at once, and the notice continues.
    /// </summary>
    internal sealed class ConsentPlatformEditor : IConsentPlatform {
        private const string DecidedKey  = ConsentPlatform.EditorDecidedKey;
        private const string ConsentKey  = ConsentPlatform.EditorConsentKey;
        private const string OptOutKey   = ConsentPlatform.EditorOptOutKey;
        private const string TrackingKey = ConsentPlatform.EditorTrackingKey;

        // A GPP string with one US national section (id 7) whose sale, sharing and targeted advertising opt-outs are set:
        // all that GppUSOptOut reads of one.
        private const string OptedOutGpp = "DBABL~AAAV";

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
                PlayerPrefs.Save();
            } else if (PlayerPrefs.GetInt(DecidedKey) != 2) {
                // Under the age of consent, UMP asks nothing and consent stays withheld.
                if (!tagForUnderAgeOfConsent) {
                    AskConsent(() => onDone(null));
                    return;
                }

                Decide(false);
            }

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
                    AskConsent(() => onClosed(null));
                    break;
                case ConsentDebugGeography.RegulatedUSState:
                    AskOptOut(() => onClosed(null));
                    break;
                default:
                    Debug.Log($"{nameof(ConsentPlatformEditor)}: outside the regulated regions UMP has no privacy options form; pick a region in Project Settings > RiseOn > Consent > Testing");
                    onClosed(null);
                    break;
            }
        }

        public TrackingStatus TrackingStatus => ConsentPlatform.EditorAsIOS ? (TrackingStatus) PlayerPrefs.GetInt(TrackingKey) : TrackingStatus.Authorized;

        public void RequestTracking(Action<TrackingStatus> onDone) {
            if (!ConsentPlatform.EditorAsIOS) {
                onDone(TrackingStatus.Authorized);
                return;
            }

            // iOS stops Unity while a system alert is up; the Editor's pause stands in for it.
            var paused = !Application.isBatchMode;
            if (paused) IEditorPauseHandler.Pause();
            Ask(DialogLook.IOSAlert, $"Allow “{Application.productName}” to track your activity across other companies’ apps and websites?", ConsentSettings.Load().TrackingPrompt, "Allow", "Ask App Not to Track", allowed => {
                if (paused) IEditorPauseHandler.Resume();
                var status = allowed ? TrackingStatus.Authorized : TrackingStatus.Denied;
                PlayerPrefs.SetInt(TrackingKey, (int) status);
                PlayerPrefs.Save();
                onDone(status);
            }, yesFirst: false);
        }

        public void ShowNotice(string title, string message, string policyButton, string continueButton, string url, Action onContinued) {
            if (Application.isBatchMode) {
                onContinued();
                return;
            }

            // The order and the preferred button of the platforms' own: the policy first, continue last.
            ConsentEditorDialog.Show(ConsentPlatform.EditorAsIOS ? DialogLook.IOSAlert : DialogLook.AndroidAlert, title, message
              , new DialogChoice(policyButton, () => Application.OpenURL(url), stays: true)
              , new DialogChoice(continueButton, onContinued, preferred: true));
        }

        // Batch mode does not consent.
        private static void AskConsent(Action done) {
            if (Application.isBatchMode) {
                Decide(false);
                done();
                return;
            }

            ConsentEditorDialog.Gdpr(() => {
                Decide(true);
                done();
            }, () => {
                Decide(false);
                done();
            });
        }

        // Batch mode does not opt out.
        private static void AskOptOut(Action done) {
            if (Application.isBatchMode) {
                OptOut(false);
                done();
                return;
            }

            ConsentEditorDialog.UsStates(PlayerPrefs.GetInt(OptOutKey) == 1, optOut => {
                OptOut(optOut);
                done();
            });
        }

        private static void OptOut(bool optOut) {
            PlayerPrefs.SetInt(OptOutKey, optOut ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void Decide(bool consent) {
            PlayerPrefs.SetInt(ConsentKey, consent ? 1 : 0);
            PlayerPrefs.SetInt(DecidedKey, 2);
            PlayerPrefs.Save();
        }

        // A dialog of two buttons, true for the yes one; batch mode answers no. A device may show the no button first, as iOS's tracking prompt does.
        private static void Ask(DialogLook look, string title, string message, string yes, string no, Action<bool> answered, bool yesFirst = true) {
            if (Application.isBatchMode) {
                answered(false);
                return;
            }

            var yesChoice = new DialogChoice(yes, () => answered(true));
            var noChoice  = new DialogChoice(no, () => answered(false));
            if (yesFirst) ConsentEditorDialog.Show(look, title, message, yesChoice, noChoice);
            else ConsentEditorDialog.Show(look, title, message, noChoice, yesChoice);
        }
    }
}