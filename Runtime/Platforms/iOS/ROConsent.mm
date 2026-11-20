#import <UIKit/UIKit.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <UserMessagingPlatform/UserMessagingPlatform.h>

// Google's UMP and App Tracking Transparency for the C# consent flow. Calls hop to the main queue, as both ask.
// UMP calls back there; ATT calls back on a queue of its own, so the C# side takes any thread.

extern "C" UIViewController *UnityGetGLViewController(void);

// errorKind: 0 when it went through, else the ordinal of the C# ConsentErrorKind plus one.
typedef void (*ROConsentErrorCallback)(intptr_t context, int errorKind, const char *message);
typedef void (*ROConsentStatusCallback)(intptr_t context, int status);

static const int ROConsentErrorNone = 0;
static const int ROConsentErrorNetwork = 1;
static const int ROConsentErrorMisconfiguration = 2;
static const int ROConsentErrorInternal = 3;

// The ordinals of the C# ConsentDebugGeography.
static UMPDebugGeography ROConsentUMPGeography(int geography) {
    switch (geography) {
        case 1: return UMPDebugGeographyEEA;
        case 2: return UMPDebugGeographyRegulatedUSState;
        case 3: return UMPDebugGeographyOther;
        default: return UMPDebugGeographyDisabled;
    }
}

// The codes of UMPRequestErrorCode and UMPFormErrorCode, which share UMPErrorDomain.
static int ROConsentErrorKind(NSError *error) {
    switch (error.code) {
        case UMPRequestErrorCodeNetwork:
        case UMPFormErrorCodeTimeout:
            return ROConsentErrorNetwork;
        case UMPRequestErrorCodeInvalidAppID:
        case UMPRequestErrorCodeMisconfiguration:
        case UMPFormErrorCodeUnavailable:
            return ROConsentErrorMisconfiguration;
        default:
            return ROConsentErrorInternal;
    }
}

static void ROConsentCallError(ROConsentErrorCallback callback, intptr_t context, NSError *error) {
    if (error == nil) {
        callback(context, ROConsentErrorNone, NULL);
        return;
    }

    callback(context, ROConsentErrorKind(error), error.localizedDescription.UTF8String);
}

// Strings returned to C# must be malloc'ed: the marshaller frees them.
static char *ROConsentCopy(NSString *text) {
    const char *utf8 = (text ?: @"").UTF8String;
    char *copy = (char *)malloc(strlen(utf8) + 1);
    strcpy(copy, utf8);
    return copy;
}

// A CMP may store a single id as a number.
static NSString *ROConsentText(NSString *key) {
    id value = [[NSUserDefaults standardUserDefaults] objectForKey:key];
    return value == nil ? @"" : [value description];
}

extern "C" {
    // testDeviceHashedIds: the ids joined by '\n', or empty.
    void ROConsentRequest(bool tagForUnderAgeOfConsent, int debugGeography, const char *testDeviceHashedIds, intptr_t context, ROConsentErrorCallback callback) {
        NSString *ids = [NSString stringWithUTF8String:testDeviceHashedIds ?: ""];

        dispatch_async(dispatch_get_main_queue(), ^{
            UMPRequestParameters *parameters = [[UMPRequestParameters alloc] init];
            parameters.tagForUnderAgeOfConsent = tagForUnderAgeOfConsent;

            if (debugGeography != 0) {
                UMPDebugSettings *debug = [[UMPDebugSettings alloc] init];
                debug.geography = ROConsentUMPGeography(debugGeography);
                if (ids.length > 0) debug.testDeviceIdentifiers = [ids componentsSeparatedByString:@"\n"];
                parameters.debugSettings = debug;
            }

            [UMPConsentInformation.sharedInstance requestConsentInfoUpdateWithParameters:parameters completionHandler:^(NSError *requestError) {
                if (requestError != nil) {
                    ROConsentCallError(callback, context, requestError);
                    return;
                }

                [UMPConsentForm loadAndPresentIfRequiredFromViewController:UnityGetGLViewController() completionHandler:^(NSError *formError) {
                    ROConsentCallError(callback, context, formError);
                }];
            }];
        });
    }

    bool ROConsentCanRequestAds(void) {
        return UMPConsentInformation.sharedInstance.canRequestAds;
    }

    bool ROConsentIsPrivacyOptionsRequired(void) {
        return UMPConsentInformation.sharedInstance.privacyOptionsRequirementStatus == UMPPrivacyOptionsRequirementStatusRequired;
    }

    // The IAB TCF and GPP keys UMP writes into the standard user defaults.
    bool ROConsentGDPRApplies(void) {
        return [[NSUserDefaults standardUserDefaults] integerForKey:@"IABTCF_gdprApplies"] == 1;
    }

    bool ROConsentHasPurposeOneConsent(void) {
        NSString *purposes = [[NSUserDefaults standardUserDefaults] stringForKey:@"IABTCF_PurposeConsents"];
        return purposes.length > 0 && [purposes characterAtIndex:0] == '1';
    }

    char *ROConsentGPPString(void) {
        return ROConsentCopy(ROConsentText(@"IABGPP_HDR_GppString"));
    }

    char *ROConsentGPPSectionIDs(void) {
        return ROConsentCopy(ROConsentText(@"IABGPP_GppSID"));
    }

    void ROConsentShowPrivacyOptions(intptr_t context, ROConsentErrorCallback callback) {
        dispatch_async(dispatch_get_main_queue(), ^{
            [UMPConsentForm presentPrivacyOptionsFormFromViewController:UnityGetGLViewController() completionHandler:^(NSError *formError) {
                ROConsentCallError(callback, context, formError);
            }];
        });
    }

    // The ordinals match ATTrackingManagerAuthorizationStatus and the C# TrackingStatus.
    int ROConsentTrackingStatus(void) {
        if (@available(iOS 14, *)) return (int)ATTrackingManager.trackingAuthorizationStatus;
        return 3;
    }

    void ROConsentRequestTracking(intptr_t context, ROConsentStatusCallback callback) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (@available(iOS 14, *)) {
                [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                    callback(context, (int)status);
                }];
            } else {
                callback(context, 3);
            }
        });
    }
}