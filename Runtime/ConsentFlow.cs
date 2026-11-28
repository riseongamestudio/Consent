using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RiseOn.Consent {
    /// <summary>
    /// The consent flow, run once per launch on its own: Google's UMP shows the GDPR or US-state form where the law asks for it (and the IDFA explainer with the ATT prompt, when the AdMob message is set up for it), then outside the GDPR regions the privacy notice shows on the first launch, then on iOS the ATT prompt shows if nothing asked yet.<br/>
    /// Every SDK that must wait for it awaits <see cref="WaitUntilCompleteAsync"/>; nothing has to start it.<br/>
    /// When UMP cannot be reached, the decision stored by an earlier launch completes the flow, as Google advises; without one, the flow waits for the one caller of <see cref="PopResultAsync"/> to retry or continue.<br/>
    /// On a platform it does not run on (WebGL, desktop players) it logs one error and completes at once with <see cref="ConsentUnsupported"/>.
    /// </summary>
    public static class ConsentFlow {
        // PlayerPrefs; the settings page's Reset deletes it in the Editor.
        internal const string NoticeShownKey = "RiseOn.Consent.NoticeShown";

        private static UniTaskCompletionSource<ConsentState> completion = new();
        private static Attempt                               current    = new();
        private static ConsentSettings                       settings;

        private static ConsentSettings Settings => settings ? settings : settings = ConsentSettings.Load();

        /// <summary>What the flow learned; null until it completes.</summary>
        public static ConsentState? State { get; private set; }

        /// <summary>Unity's thread. The player changed their choices in the privacy options form; the argument is the new <see cref="State"/>.</summary>
        public static event Action<ConsentState> StateChanged;

        /// <summary>The privacy policy page of Project Settings > RiseOn > Consent for the running platform; in the Editor, the build target's.</summary>
        public static string PrivacyPolicyUrl => Settings.PrivacyPolicyUrl(IsIOS);

        /// <summary>Opens <see cref="PrivacyPolicyUrl"/> in the browser: for the game's settings button, which the stores ask every app to have.</summary>
        public static void OpenPrivacyPolicy() {
            Application.OpenURL(PrivacyPolicyUrl);
        }

        private static bool IsIOS => Application.platform is RuntimePlatform.IPhonePlayer || Application.isEditor && ConsentPlatform.EditorAsIOS;

        /// <summary>Completes on Unity's thread with the state once the flow completes, at once if it has.</summary>
        public static UniTask<ConsentState> WaitUntilCompleteAsync() {
            return completion.Task;
        }

        /// <summary>
        /// The result of the current run, for the one caller that handles a failure; a second call for the same run throws, so a failure never reaches two handlers.
        /// </summary>
        public static UniTask<ConsentResult> PopResultAsync() {
            if (current.Popped) throw new InvalidOperationException($"{nameof(ConsentFlow)}: the result of this run was already popped");

            current.Popped = true;
            return current.Result.Task;
        }

        /// <summary>
        /// Opens the form again for the player to change their choices; raises <see cref="StateChanged"/> when they did.<br/>
        /// Throws before the flow completes, on a platform the flow does not run on, and throws the <see cref="ConsentException"/> when the form fails.
        /// </summary>
        public static async UniTask ShowPrivacyOptionsAsync() {
            if (State == null) throw new InvalidOperationException($"{nameof(ConsentFlow)}: the privacy options form needs the flow to complete first");

            var platform = ConsentPlatform.Installed ?? throw new PlatformNotSupportedException($"{nameof(ConsentFlow)}: no privacy options form on {Application.platform}");
            var closed   = new UniTaskCompletionSource<ConsentException>();
            platform.ShowPrivacyOptions(error => closed.TrySetResult(error));

            var error = await closed.Task;
            await UniTask.SwitchToMainThread();
            if (error != null) throw error;

            var state = Read(platform);
            if (state.Equals(State.Value)) return;

            State = state;
            StateChanged?.Invoke(state);
        }

        // Static state outlives a play session when the Editor skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            completion   = new UniTaskCompletionSource<ConsentState>();
            current      = new Attempt();
            settings     = null;
            State        = null;
            StateChanged = null;
        }

        // After the first scene loads: the game shows its first screen under the forms, and iOS has made the app active,
        // which the ATT prompt needs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Run() {
            var platform = ConsentPlatform.Installed;
            if (platform == null) {
                // Nobody can be asked, so nothing is allowed; the privacy button stays hidden, as there is no form behind it.
                Debug.LogError($"{nameof(ConsentFlow)}: the consent flow does not run on {Application.platform}, only on Android and iOS; it completes with nothing allowed");
                var state = new ConsentState(gdprApplies: false, canRequestAds: false, hasUserConsent: false, trackingStatus: TrackingStatus.Denied, doNotSell: true
                                           , childDirected: Settings.ChildDirected, privacyOptionsRequired: false);
                State = state;
                completion.TrySetResult(state);
                current.Result.TrySetResult(new ConsentUnsupported(state));
                return;
            }

            RunAsync(platform, current).Forget();
        }

        private static async UniTaskVoid RunAsync(IConsentPlatform platform, Attempt attempt) {
            // The player can leave the game before the first scene is up; the prompts wait for them to come back.
            if (!Application.isEditor) await UniTask.WaitUntil(() => Application.isFocused);

            attempt.Result.TrySetResult(await AttemptAsync(platform));
        }

        private static async UniTask<ConsentResult> AttemptAsync(IConsentPlatform platform) {
            var error = await RequestAsync(platform);
            if (error == null) return new ConsentSucceeded(await CompleteAsync(platform));

            // UMP keeps the last launch's status: with a decision there, Google says to go on and request ads.
            if (platform.CanRequestAds) {
                Debug.LogError($"{nameof(ConsentFlow)}: UMP failed ({error.Kind}: {error.Message}); going on with the decision stored by an earlier launch");
                return new ConsentSucceeded(await CompleteAsync(platform));
            }

            Debug.LogError($"{nameof(ConsentFlow)}: UMP failed ({error.Kind}: {error.Message}) and no decision is stored; waiting for a retry or continue");
            return new ConsentFailed(error, () => RetryAsync(platform), () => CompleteAsync(platform).AsUniTask());
        }

        // The retrying caller owns the new run, so nobody can pop it.
        private static UniTask<ConsentResult> RetryAsync(IConsentPlatform platform) {
            current = new Attempt { Popped = true };
            RunAsync(platform, current).Forget();
            return current.Result.Task;
        }

        private static async UniTask<ConsentException> RequestAsync(IConsentPlatform platform) {
            var debugGeography = Debug.isDebugBuild ? Settings.DebugGeography : ConsentDebugGeography.Disabled;
            var requested      = new UniTaskCompletionSource<ConsentException>();
            platform.Request(
                tagForUnderAgeOfConsent: Settings.ChildDirected
              , debugGeography: debugGeography
              , testDeviceHashedIds: Settings.TestDeviceHashedIds ?? Array.Empty<string>()
              , onDone: error => requested.TrySetResult(error));

            var error = await requested.Task;
            await UniTask.SwitchToMainThread();
            return error;
        }

        private static async UniTask<ConsentState> CompleteAsync(IConsentPlatform platform) {
            // In the GDPR regions UMP's form is the notice; elsewhere UMP shows none at launch.
            if (!platform.GdprApplies && PlayerPrefs.GetInt(NoticeShownKey) == 0) {
                await ShowNoticeAsync(platform);
                PlayerPrefs.SetInt(NoticeShownKey, 1);
                PlayerPrefs.Save();
            }

            if (platform.TrackingStatus is TrackingStatus.NotDetermined && !Settings.ChildDirected) {
                var tracked = new UniTaskCompletionSource<TrackingStatus>();
                platform.RequestTracking(status => tracked.TrySetResult(status));
                await tracked.Task;
                await UniTask.SwitchToMainThread();
            }

            var state = Read(platform);
            State = state;
            Debug.Log($"{nameof(ConsentFlow)}: {state}");
            completion.TrySetResult(state);
            return state;
        }

        private static async UniTask ShowNoticeAsync(IConsentPlatform platform) {
            var continued = new UniTaskCompletionSource();
            platform.ShowNotice(
                title: "Privacy Policy"
              , message: $"To play {Application.productName}, please confirm that you have read our Privacy Policy. It explains how the game and its ad partners collect and use your data."
              , policyButton: "Read Privacy Policy"
              , continueButton: "Continue"
              , url: PrivacyPolicyUrl
              , onContinued: () => continued.TrySetResult());

            await continued.Task;
            await UniTask.SwitchToMainThread();
        }

        private static ConsentState Read(IConsentPlatform platform) {
            var gdprApplies = platform.GdprApplies;
            return new ConsentState(
                gdprApplies: gdprApplies
              , canRequestAds: platform.CanRequestAds
              , hasUserConsent: !gdprApplies || platform.HasPurposeOneConsent
              , trackingStatus: platform.TrackingStatus
              , doNotSell: DoNotSell(platform)
              , childDirected: Settings.ChildDirected
              , privacyOptionsRequired: platform.PrivacyOptionsRequired);
        }

        private static bool DoNotSell(IConsentPlatform platform) {
            try {
                return GppUSOptOut.Read(platform.GppString, platform.GppSectionIds);
            } catch (FormatException e) {
                // An unreadable choice counts as an opt-out: wrongly honoring one only costs personalized ads.
                Debug.LogException(e);
                return true;
            }
        }

        private sealed class Attempt {
            public readonly UniTaskCompletionSource<ConsentResult> Result = new();
            public          bool                                   Popped;
        }
    }
}