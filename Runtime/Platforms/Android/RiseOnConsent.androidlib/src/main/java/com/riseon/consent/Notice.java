package com.riseon.consent;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.net.Uri;
import android.util.Log;

/** The privacy notice for the C# consent flow, as the platform's own dialog. */
public final class Notice {
    public interface Listener {
        void onContinued();
    }

    private Notice() { }

    public static void show(
            final Activity activity,
            final String title,
            final String message,
            final String policyButton,
            final String continueButton,
            final String url,
            final Listener listener) {
        activity.runOnUiThread(() -> {
            AlertDialog dialog = new AlertDialog.Builder(activity, android.R.style.Theme_DeviceDefault_Light_Dialog_Alert)
                    .setTitle(title)
                    .setMessage(message)
                    .setCancelable(false)
                    .setNeutralButton(policyButton, null)
                    .setPositiveButton(continueButton, (shown, which) -> listener.onContinued())
                    .create();

            // A button given no listener still closes the dialog; this one opens the page and leaves it up.
            dialog.setOnShowListener(shown -> dialog.getButton(AlertDialog.BUTTON_NEUTRAL).setOnClickListener(button -> open(activity, url)));
            dialog.show();
        });
    }

    private static void open(Activity activity, String url) {
        try {
            activity.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(url)));
        } catch (ActivityNotFoundException e) {
            Log.e("RiseOnConsent", "No app opens the privacy policy " + url, e);
        }
    }
}