using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Encryptor
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true, 
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        AlwaysRetainTaskState = true,
        NoHistory = false,
        ExcludeFromRecents = false)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const string KEY_FILE_PICKER_ACTIVE = "FilePickerActive";
        private const string KEY_FOLDER_PICKER_ACTIVE = "FolderPickerActive";
        private const string KEY_PICKER_TIMESTAMP = "PickerTimestamp";
        private const string KEY_PICKER_REQUEST_CODE = "PickerRequestCode";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            System.Diagnostics.Debug.WriteLine($"MainActivity: OnCreate called (IsFinishing: {IsFinishing})");

            if (savedInstanceState != null)
            {
                RestorePickerState(savedInstanceState);
            }
        }

        private void RestorePickerState(Bundle savedInstanceState)
        {
            try
            {
                // Check if picker was active before recreation
                var filePickerActive = savedInstanceState.GetBoolean(KEY_FILE_PICKER_ACTIVE, false);
                var folderPickerActive = savedInstanceState.GetBoolean(KEY_FOLDER_PICKER_ACTIVE, false);
                var pickerTimestamp = savedInstanceState.GetLong(KEY_PICKER_TIMESTAMP, 0);
                var requestCode = savedInstanceState.GetInt(KEY_PICKER_REQUEST_CODE, 0);

                if (filePickerActive || folderPickerActive)
                {
                    System.Diagnostics.Debug.WriteLine("MainActivity: Picker was active before recreation");
                    
                    // Calculate how long ago the picker was opened
                    var currentTime = Java.Lang.JavaSystem.CurrentTimeMillis();
                    var elapsedSeconds = (currentTime - pickerTimestamp) / 1000.0;
                    
                    System.Diagnostics.Debug.WriteLine($"MainActivity: Picker was opened {elapsedSeconds:F1} seconds ago");
                    System.Diagnostics.Debug.WriteLine($"MainActivity: Request code was: {requestCode}");
                    
                    // If it's been a very long time (>10 minutes), the user likely cancelled
                    if (elapsedSeconds > 600)
                    {
                        System.Diagnostics.Debug.WriteLine("MainActivity: Cancelling stale picker operations");
                        Platforms.Android.AndroidFilePicker.CancelPendingOperation();
                        Platforms.Android.AndroidFolderPicker.CancelPendingOperation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MainActivity: Picker is still valid, waiting for result");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: Error restoring picker state: {ex.Message}");
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            System.Diagnostics.Debug.WriteLine("MainActivity: OnNewIntent called");
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: OnActivityResult called - RequestCode: {requestCode}, ResultCode: {resultCode}, HasData: {data != null}");
                base.OnActivityResult(requestCode, resultCode, data);
                
                // Handle folder picker result (requestCode: 9999)
                Platforms.Android.AndroidFolderPicker.HandleActivityResult(requestCode, resultCode, data);
                
                // Handle file picker result (requestCode: 10001 or 10002)
                Platforms.Android.AndroidFilePicker.HandleActivityResult(requestCode, resultCode, data);
                
                System.Diagnostics.Debug.WriteLine("MainActivity: OnActivityResult completed");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: OnActivityResult exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MainActivity: Stack trace: {ex.StackTrace}");
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            try
            {
                base.OnSaveInstanceState(outState);
                
                // Save picker state
                var filePickerActive = Platforms.Android.AndroidFilePicker.IsPickerActive;
                var folderPickerActive = Platforms.Android.AndroidFolderPicker.IsPickerActive;
                
                outState.PutBoolean(KEY_FILE_PICKER_ACTIVE, filePickerActive);
                outState.PutBoolean(KEY_FOLDER_PICKER_ACTIVE, folderPickerActive);
                
                if (filePickerActive || folderPickerActive)
                {
                    // Store timestamp when picker was opened
                    outState.PutLong(KEY_PICKER_TIMESTAMP, Java.Lang.JavaSystem.CurrentTimeMillis());
                    
                    // Store request code to help identify which picker was active
                    if (filePickerActive)
                        outState.PutInt(KEY_PICKER_REQUEST_CODE, 10001);
                    else if (folderPickerActive)
                        outState.PutInt(KEY_PICKER_REQUEST_CODE, 9999);
                    
                    System.Diagnostics.Debug.WriteLine("MainActivity: Saved picker state - active picker detected");
                }
                
                System.Diagnostics.Debug.WriteLine($"MainActivity: Saved instance state - FilePicker: {filePickerActive}, FolderPicker: {folderPickerActive}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: OnSaveInstanceState exception: {ex.Message}");
            }
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            try
            {
                base.OnRestoreInstanceState(savedInstanceState);
                System.Diagnostics.Debug.WriteLine("MainActivity: OnRestoreInstanceState called");
                
                RestorePickerState(savedInstanceState);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: OnRestoreInstanceState exception: {ex.Message}");
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            System.Diagnostics.Debug.WriteLine("MainActivity: OnStart called");
        }

        protected override void OnPause()
        {
            base.OnPause();
            System.Diagnostics.Debug.WriteLine($"MainActivity: OnPause called (IsFinishing: {IsFinishing})");
            
            // Don't do anything drastic when picker is active
            if (Platforms.Android.AndroidFilePicker.IsPickerActive || 
                Platforms.Android.AndroidFolderPicker.IsPickerActive)
            {
                System.Diagnostics.Debug.WriteLine("MainActivity: OnPause - Picker is active, app is backgrounded normally");
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            System.Diagnostics.Debug.WriteLine($"MainActivity: OnResume called (IsFinishing: {IsFinishing})");
            
            // Check if we're resuming with an active picker
            if (Platforms.Android.AndroidFilePicker.IsPickerActive || 
                Platforms.Android.AndroidFolderPicker.IsPickerActive)
            {
                System.Diagnostics.Debug.WriteLine("MainActivity: OnResume - Picker still active, waiting for result");
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            System.Diagnostics.Debug.WriteLine($"MainActivity: OnStop called (IsFinishing: {IsFinishing})");
            
            // The app is going to background, but this is NORMAL when file picker opens
            // Don't cancel anything here!
            if (Platforms.Android.AndroidFilePicker.IsPickerActive || 
                Platforms.Android.AndroidFolderPicker.IsPickerActive)
            {
                System.Diagnostics.Debug.WriteLine("MainActivity: OnStop - App backgrounded due to file picker (NORMAL BEHAVIOR)");
            }
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            System.Diagnostics.Debug.WriteLine("MainActivity: OnRestart called");
        }

        protected override void OnDestroy()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: OnDestroy called (IsFinishing: {IsFinishing})");
                
                // If activity is being destroyed while picker is active
                if (Platforms.Android.AndroidFilePicker.IsPickerActive || 
                    Platforms.Android.AndroidFolderPicker.IsPickerActive)
                {
                    System.Diagnostics.Debug.WriteLine("MainActivity: Picker is active during OnDestroy");
                    
                    // Only cancel if we're truly finishing (not just configuration change or backgrounding)
                    if (IsFinishing)
                    {
                        System.Diagnostics.Debug.WriteLine("MainActivity: Activity is finishing - cancelling picker operations");
                        Platforms.Android.AndroidFilePicker.CancelPendingOperation();
                        Platforms.Android.AndroidFolderPicker.CancelPendingOperation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MainActivity: Activity not finishing - keeping picker operations alive");
                    }
                }
                
                base.OnDestroy();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainActivity: OnDestroy exception: {ex.Message}");
            }
        }
    }
}
