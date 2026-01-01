using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace Encryptor.Platforms.Android
{
    /// <summary>
    /// Helper for managing Android storage permissions, especially MANAGE_EXTERNAL_STORAGE.
    /// </summary>
    public static class StoragePermissionHelper
    {
        private const string PREF_PERMISSION_ASKED = "storage_permission_asked";
        private const string PREF_PERMISSION_DENIED_COUNT = "storage_permission_denied_count";

        /// <summary>
        /// Check if the app has all files access permission (MANAGE_EXTERNAL_STORAGE).
        /// This is required on Android 11+ to access files outside app-specific directories.
        /// </summary>
        public static bool HasAllFilesAccess()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                // Android 11 and above
                var hasAccess = global::Android.OS.Environment.IsExternalStorageManager;
                System.Diagnostics.Debug.WriteLine($"StoragePermissionHelper: Has all files access: {hasAccess}");
                return hasAccess;
            }
            else
            {
                // Android 10 and below - use legacy storage permissions
                System.Diagnostics.Debug.WriteLine($"StoragePermissionHelper: Using legacy storage (Android < 11)");
                return true; // Legacy permissions handled by MAUI
            }
        }

        /// <summary>
        /// Request all files access permission by opening system settings.
        /// </summary>
        public static void RequestAllFilesAccess()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    System.Diagnostics.Debug.WriteLine("StoragePermissionHelper: Requesting all files access");
                    
                    var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                    var uri = global::Android.Net.Uri.FromParts("package", Platform.CurrentActivity?.PackageName, null);
                    intent.SetData(uri);
                    intent.AddFlags(ActivityFlags.NewTask);
                    
                    Platform.CurrentActivity?.StartActivity(intent);
                    
                    // Mark that we've asked for permission
                    IncrementDeniedCount();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("StoragePermissionHelper: All files access not needed on Android < 11");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StoragePermissionHelper: Error requesting permission: {ex.Message}");
                
                // Fallback to general settings
                try
                {
                    var intent = new Intent(Settings.ActionApplicationDetailsSettings);
                    var uri = global::Android.Net.Uri.FromParts("package", Platform.CurrentActivity?.PackageName, null);
                    intent.SetData(uri);
                    intent.AddFlags(ActivityFlags.NewTask);
                    
                    Platform.CurrentActivity?.StartActivity(intent);
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"StoragePermissionHelper: Fallback error: {fallbackEx.Message}");
                }
            }
        }

        /// <summary>
        /// Get the number of times permission has been denied.
        /// </summary>
        public static int GetDeniedCount()
        {
            return Preferences.Get(PREF_PERMISSION_DENIED_COUNT, 0);
        }

        /// <summary>
        /// Increment the denied count.
        /// </summary>
        private static void IncrementDeniedCount()
        {
            var count = GetDeniedCount();
            Preferences.Set(PREF_PERMISSION_DENIED_COUNT, count + 1);
        }

        /// <summary>
        /// Reset the denied count (when permission is granted).
        /// </summary>
        public static void ResetDeniedCount()
        {
            Preferences.Set(PREF_PERMISSION_DENIED_COUNT, 0);
        }

        /// <summary>
        /// Check if this is the first time asking for permission.
        /// </summary>
        public static bool IsFirstTimeAsking()
        {
            return GetDeniedCount() == 0;
        }

        /// <summary>
        /// Request all files access with appropriate messaging based on previous denials.
        /// Returns: true if granted, false if denied or cancelled.
        /// </summary>
        public static async Task<(bool granted, bool userCancelled)> RequestAllFilesAccessAsync()
        {
            if (HasAllFilesAccess())
            {
                System.Diagnostics.Debug.WriteLine("StoragePermissionHelper: Already have all files access");
                ResetDeniedCount();
                return (true, false);
            }

            var deniedCount = GetDeniedCount();
            string title, message;

            if (deniedCount == 0)
            {
                // First time asking
                title = "Storage Access Required";
                message = "🔐 Encryptor needs access to your device storage to:\n\n" +
                         "✓ Encrypt/decrypt files in their original locations\n" +
                         "✓ Delete original files after encryption\n" +
                         "✓ Save encrypted files where you want them\n\n" +
                         "Without this permission, the app cannot function.\n\n" +
                         "Please grant 'All files access' in the next screen.";
            }
            else if (deniedCount == 1)
            {
                // Second attempt - more detailed explanation
                title = "Permission Required to Continue";
                message = "⚠️ This app MUST have storage access to work.\n\n" +
                         "WHY WE NEED THIS:\n" +
                         "• To read your files for encryption\n" +
                         "• To create encrypted versions\n" +
                         "• To delete unencrypted originals\n\n" +
                         "🛡️ PRIVACY: We only access files YOU select.\n" +
                         "We don't scan or collect any data.\n\n" +
                         "The app will close if you deny this permission.";
            }
            else
            {
                // Third+ attempt - final warning
                title = "Final Permission Request";
                message = "🚫 The app cannot run without storage access.\n\n" +
                         "This is your final chance to grant permission.\n\n" +
                         "If you deny again, the app will close and ask again next time you open it.\n\n" +
                         "Grant 'All files access' → App works\n" +
                         "Deny → App closes";
            }

            // Show explanation dialog
            bool userAccepted = await Shell.Current.DisplayAlert(
                title,
                message,
                "Grant Permission",
                "Deny & Close App");

            if (!userAccepted)
            {
                System.Diagnostics.Debug.WriteLine("StoragePermissionHelper: User declined permission request");
                return (false, true);
            }

            // Open settings
            RequestAllFilesAccess();

            // Wait longer for user to return from settings and check permission multiple times
            for (int i = 0; i < 30; i++) // Check for up to 15 seconds (30 * 500ms)
            {
                await Task.Delay(500);
                
                if (HasAllFilesAccess())
                {
                    ResetDeniedCount();
                    System.Diagnostics.Debug.WriteLine($"StoragePermissionHelper: Permission granted!");
                    return (true, false);
                }
            }
            
            // After 15 seconds, permission still not granted
            System.Diagnostics.Debug.WriteLine($"StoragePermissionHelper: Permission denied (count: {GetDeniedCount()})");
            return (false, false);
        }
    }
}
