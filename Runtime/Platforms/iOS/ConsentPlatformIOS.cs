using System;
using System.Runtime.InteropServices;
using AOT;

namespace RiseOn.Consent.Platforms.IOS {
    internal sealed class ConsentPlatformIOS : IConsentPlatform {
        private delegate void ErrorCallback(IntPtr context, int errorKind, string message);
        private delegate void StatusCallback(IntPtr context, int status);

        [DllImport("__Internal")] private static extern void ROConsentRequest([MarshalAs(UnmanagedType.I1)] bool tagForUnderAgeOfConsent, int debugGeography, string testDeviceHashedIds, IntPtr context, ErrorCallback callback);
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] private static extern bool ROConsentCanRequestAds();
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] private static extern bool ROConsentIsPrivacyOptionsRequired();
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] private static extern bool ROConsentGDPRApplies();
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] private static extern bool ROConsentHasPurposeOneConsent();
        [DllImport("__Internal")] private static extern string ROConsentGPPString();
        [DllImport("__Internal")] private static extern string ROConsentGPPSectionIDs();
        [DllImport("__Internal")] private static extern void ROConsentShowPrivacyOptions(IntPtr context, ErrorCallback callback);
        [DllImport("__Internal")] private static extern int ROConsentTrackingStatus();
        [DllImport("__Internal")] private static extern void ROConsentRequestTracking(IntPtr context, StatusCallback callback);

        public void Request(bool tagForUnderAgeOfConsent, ConsentDebugGeography debugGeography, string[] testDeviceHashedIds, Action<ConsentException> onDone) {
            ROConsentRequest(tagForUnderAgeOfConsent, (int)debugGeography, string.Join("\n", testDeviceHashedIds), Pin(onDone), OnError);
        }

        public bool CanRequestAds => ROConsentCanRequestAds();
        public bool PrivacyOptionsRequired => ROConsentIsPrivacyOptionsRequired();
        public bool GdprApplies => ROConsentGDPRApplies();
        public bool HasPurposeOneConsent => ROConsentHasPurposeOneConsent();
        public string GppString => ROConsentGPPString();
        public string GppSectionIds => ROConsentGPPSectionIDs();

        public void ShowPrivacyOptions(Action<ConsentException> onClosed) {
            ROConsentShowPrivacyOptions(Pin(onClosed), OnError);
        }

        public TrackingStatus TrackingStatus => (TrackingStatus)ROConsentTrackingStatus();

        public void RequestTracking(Action<TrackingStatus> onDone) {
            ROConsentRequestTracking(Pin(onDone), OnStatus);
        }

        private static IntPtr Pin(object callback) {
            return GCHandle.ToIntPtr(GCHandle.Alloc(callback));
        }

        private static T Release<T>(IntPtr context) where T : class {
            var handle   = GCHandle.FromIntPtr(context);
            var callback = handle.Target as T;
            handle.Free();
            return callback;
        }

        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void OnError(IntPtr context, int errorKind, string message) {
            Release<Action<ConsentException>>(context)?.Invoke(ConsentPlatform.Error(errorKind, message));
        }

        [MonoPInvokeCallback(typeof(StatusCallback))]
        private static void OnStatus(IntPtr context, int status) {
            Release<Action<TrackingStatus>>(context)?.Invoke((TrackingStatus)status);
        }
    }
}