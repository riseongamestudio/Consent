package com.riseon.consent;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;

import com.google.android.ump.ConsentDebugSettings;
import com.google.android.ump.ConsentInformation;
import com.google.android.ump.ConsentRequestParameters;
import com.google.android.ump.FormError;
import com.google.android.ump.UserMessagingPlatform;

/**
 * Google's UMP for the C# consent flow. Every call hops to the UI thread, as UMP asks; the listener comes back
 * there too, which is safe because the flow runs while Unity is awake.
 */
public final class Ump {
    public interface Listener {
        /** @param errorKind 0 when it went through, else the ordinal of the C# ConsentErrorKind plus one. */
        void onDone(int errorKind, String message);
    }

    // The ordinals of the C# ConsentDebugGeography.
    private static final int GEOGRAPHY_DISABLED = 0;
    private static final int GEOGRAPHY_EEA = 1;
    private static final int GEOGRAPHY_REGULATED_US_STATE = 2;
    private static final int GEOGRAPHY_OTHER = 3;

    // ConsentErrorKind + 1.
    private static final int ERROR_NONE = 0;
    private static final int ERROR_NETWORK = 1;
    private static final int ERROR_MISCONFIGURATION = 2;
    private static final int ERROR_INTERNAL = 3;

    private Ump() { }

    public static void request(
            final Activity activity,
            final boolean tagForUnderAgeOfConsent,
            final int debugGeography,
            final String[] testDeviceHashedIds,
            final Listener listener) {
        activity.runOnUiThread(() -> {
            ConsentRequestParameters.Builder parameters = new ConsentRequestParameters.Builder()
                    .setTagForUnderAgeOfConsent(tagForUnderAgeOfConsent);

            if (debugGeography != GEOGRAPHY_DISABLED) {
                ConsentDebugSettings.Builder debug = new ConsentDebugSettings.Builder(activity)
                        .setDebugGeography(toUmpGeography(debugGeography));
                if (testDeviceHashedIds != null) {
                    for (String id : testDeviceHashedIds) debug.addTestDeviceHashedId(id);
                }
                parameters.setConsentDebugSettings(debug.build());
            }

            ConsentInformation information = UserMessagingPlatform.getConsentInformation(activity);
            information.requestConsentInfoUpdate(
                    activity,
                    parameters.build(),
                    () -> UserMessagingPlatform.loadAndShowConsentFormIfRequired(activity, formError -> done(listener, formError)),
                    requestError -> done(listener, requestError));
        });
    }

    public static boolean canRequestAds(Activity activity) {
        return UserMessagingPlatform.getConsentInformation(activity).canRequestAds();
    }

    public static boolean isPrivacyOptionsRequired(Activity activity) {
        return UserMessagingPlatform.getConsentInformation(activity).getPrivacyOptionsRequirementStatus()
                == ConsentInformation.PrivacyOptionsRequirementStatus.REQUIRED;
    }

    public static void showPrivacyOptions(final Activity activity, final Listener listener) {
        activity.runOnUiThread(() -> UserMessagingPlatform.showPrivacyOptionsForm(activity, formError -> done(listener, formError)));
    }

    /** The IAB TCF and GPP keys UMP writes into the app's default shared preferences. */
    public static boolean gdprApplies(Activity activity) {
        return preferences(activity).getInt("IABTCF_gdprApplies", 0) == 1;
    }

    public static boolean hasPurposeOneConsent(Activity activity) {
        String purposes = preferences(activity).getString("IABTCF_PurposeConsents", "");
        return purposes != null && !purposes.isEmpty() && purposes.charAt(0) == '1';
    }

    public static String gppString(Activity activity) {
        return text(activity, "IABGPP_HDR_GppString");
    }

    public static String gppSectionIds(Activity activity) {
        return text(activity, "IABGPP_GppSID");
    }

    // A CMP may store a single id as a number.
    private static String text(Context context, String key) {
        Object value = preferences(context).getAll().get(key);
        return value == null ? "" : value.toString();
    }

    private static SharedPreferences preferences(Context context) {
        return context.getSharedPreferences(context.getPackageName() + "_preferences", Context.MODE_PRIVATE);
    }

    private static void done(Listener listener, FormError error) {
        if (error == null) {
            listener.onDone(ERROR_NONE, null);
            return;
        }

        listener.onDone(errorKind(error.getErrorCode()), error.getMessage());
    }

    private static int errorKind(int code) {
        switch (code) {
            case FormError.ErrorCode.INTERNET_ERROR:
            case FormError.ErrorCode.TIME_OUT:
                return ERROR_NETWORK;
            case FormError.ErrorCode.INVALID_OPERATION:
                return ERROR_MISCONFIGURATION;
            default:
                return ERROR_INTERNAL;
        }
    }

    private static int toUmpGeography(int geography) {
        switch (geography) {
            case GEOGRAPHY_EEA:
                return ConsentDebugSettings.DebugGeography.DEBUG_GEOGRAPHY_EEA;
            case GEOGRAPHY_REGULATED_US_STATE:
                return ConsentDebugSettings.DebugGeography.DEBUG_GEOGRAPHY_REGULATED_US_STATE;
            case GEOGRAPHY_OTHER:
                return ConsentDebugSettings.DebugGeography.DEBUG_GEOGRAPHY_OTHER;
            default:
                return ConsentDebugSettings.DebugGeography.DEBUG_GEOGRAPHY_DISABLED;
        }
    }
}