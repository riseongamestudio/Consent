using System;
using System.Collections.Generic;
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
    /// The project's consent settings, edited in Project Settings > RiseOn > Consent and kept in a Resources asset: the flow reads it in the build, the build steps read the AdMob app ids and the tracking prompt texts.
    /// </summary>
    internal sealed class ConsentSettings : ScriptableObject {
        public const string ResourceName = "RiseOnConsentSettings";

        /// <summary>The tracking prompt in one language of the list, English among them.</summary>
        [Serializable]
        public struct TrackingTranslation {
            [SerializeField]
            private string language;

            [SerializeField]
            private string text;

            public TrackingTranslation(string language, string text) {
                this.language = language;
                this.text     = text;
            }

            /// <summary>Apple's language code, the name of the app's .lproj folder: vi, pt-BR, zh-Hans.</summary>
            public string Language => language?.Trim() ?? "";

            /// <summary>Empty for the package's own translation.</summary>
            public string Text => text ?? "";
        }

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

        [SerializeField]
        private List<TrackingTranslation> trackingTranslations = new();

        [SerializeField, LabelText("Region")]
        private ConsentDebugGeography debugGeography;

        [SerializeField, SectionList(Item = "device"), LabelText("Test devices")]
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

        /// <summary>The default tracking prompt, in Info.plist, for every language without a row; empty for the package's English one.</summary>
        public string TrackingPrompt => trackingPrompt ?? "";

        public IReadOnlyList<TrackingTranslation> TrackingTranslations => trackingTranslations;
        public ConsentDebugGeography DebugGeography => debugGeography;
        public string[] TestDeviceHashedIds => testDeviceHashedIds;

        /// <summary>The project's asset, or the defaults when the project never saved one.</summary>
        public static ConsentSettings Load() {
            var asset = Resources.Load<ConsentSettings>(ResourceName);
            return asset ? asset : CreateInstance<ConsentSettings>();
        }
    }
}