using System;
using RiseOn.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Consent {
    internal enum ConsentDebugGeography {
        Disabled
      , EEA
      , RegulatedUSState
      , Other
    }

    /// <summary>
    /// The project's consent settings, edited in Project Settings > RiseOn > Consent and kept in a Resources asset: the flow reads it in the build, the build steps read the AdMob app ids and the tracking prompt.
    /// </summary>
    internal sealed class ConsentSettings : ScriptableObject {
        public const string ResourceName = "RiseOnConsentSettings";

        /// <summary>The privacy policy a project gets until it writes its own: RiseOn's, as the packs are RiseOn's.</summary>
        public const string DefaultPrivacyPolicyUrl = "https://www.riseongamestudio.com/privacy-policy/";

        /// <summary>The tracking prompt a project gets until it writes its own.</summary>
        public const string DefaultTrackingPrompt = "This lets the game show ads that suit you better, which keeps it free.";

        [SerializeField]
        private string adMobAppIdAndroid;

        [SerializeField]
        private string adMobAppIdIOS;

        [SerializeField]
        private bool adMobAppIdAndroidTest;

        [SerializeField]
        private bool adMobAppIdIOSTest;

        [SerializeField, Tooltip("The game is for children: UMP treats every player as under the age of consent and iOS never asks for tracking.")]
        private bool childDirected;

        [SerializeField]
        private string trackingPrompt;

        // Both platforms' page, or Android's alone while split.
        [SerializeField]
        private string privacyPolicyUrl = DefaultPrivacyPolicyUrl;

        [SerializeField]
        private bool privacyPolicyUrlSplit;

        [SerializeField]
        private string privacyPolicyUrlIOS;

        [SerializeField, LabelText("Region")]
        private ConsentDebugGeography debugGeography;

        [SerializeField, SectionList(Item = "device", Foldable = true), LabelText("Test devices")]
        [Tooltip("The devices the testing region applies to. A device prints its id the first time the game starts on it: search logcat for addTestDeviceHashedId, or the Xcode console for testDeviceIdentifiers.")]
        private string[] testDeviceHashedIds = Array.Empty<string>();

        // Google's sample app ids, from the Google Mobile Ads quick-start pages.
        private const string SampleAppIdAndroid = "ca-app-pub-3940256099942544~3347511713";
        private const string SampleAppIdIOS     = "ca-app-pub-3940256099942544~1458002511";

        /// <summary>The Android app id a build uses: Google's sample app's while <see cref="AdMobAppIdAndroidTest"/> is on, else the one written.</summary>
        public string AdMobAppIdAndroid => adMobAppIdAndroidTest ? SampleAppIdAndroid : adMobAppIdAndroid?.Trim() ?? "";

        /// <summary>The iOS app id a build uses: Google's sample app's while <see cref="AdMobAppIdIOSTest"/> is on, else the one written.</summary>
        public string AdMobAppIdIOS => adMobAppIdIOSTest ? SampleAppIdIOS : adMobAppIdIOS?.Trim() ?? "";

        public bool AdMobAppIdAndroidTest => adMobAppIdAndroidTest;
        public bool AdMobAppIdIOSTest => adMobAppIdIOSTest;
        public bool ChildDirected => childDirected;

        /// <summary>The tracking prompt in Info.plist: the project's, or <see cref="DefaultTrackingPrompt"/>.</summary>
        public string TrackingPrompt => string.IsNullOrWhiteSpace(trackingPrompt) ? DefaultTrackingPrompt : trackingPrompt.Trim();

        /// <summary>The privacy policy page for a platform: iOS's own while split, else the one for both.</summary>
        public string PrivacyPolicyUrl(bool ios) => ((ios && privacyPolicyUrlSplit ? privacyPolicyUrlIOS : privacyPolicyUrl) ?? "").Trim();

        public bool PrivacyPolicyUrlSplit => privacyPolicyUrlSplit;

        public ConsentDebugGeography DebugGeography => debugGeography;
        public string[] TestDeviceHashedIds => testDeviceHashedIds;

        /// <summary>The project's asset, or the defaults when the project never saved one.</summary>
        public static ConsentSettings Load() {
            var asset = Resources.Load<ConsentSettings>(ResourceName);
            return asset ? asset : CreateInstance<ConsentSettings>();
        }
    }
}