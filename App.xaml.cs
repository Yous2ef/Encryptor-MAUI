using Microsoft.Extensions.DependencyInjection;

namespace Encryptor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            
            // Request storage permission on startup for Android
            window.Created += async (s, e) =>
            {
                await RequestStoragePermissionAsync();
            };
            
            // Check permission again when app resumes (user returns from settings)
            window.Resumed += async (s, e) =>
            {
                await CheckPermissionOnResumeAsync();
            };
            
            return window;
        }

        private async Task CheckPermissionOnResumeAsync()
        {
#if ANDROID
            try
            {
                System.Diagnostics.Debug.WriteLine("App: Resumed - checking permissions...");
                
                // Small delay to let the system update
                await Task.Delay(300);
                
                if (Platforms.Android.StoragePermissionHelper.HasAllFilesAccess())
                {
                    System.Diagnostics.Debug.WriteLine("App: Permission granted during resume");
                    Platforms.Android.StoragePermissionHelper.ResetDeniedCount();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App: Error checking permission on resume: {ex.Message}");
            }
#endif
        }

        private async Task RequestStoragePermissionAsync()
        {
#if ANDROID
            try
            {
                System.Diagnostics.Debug.WriteLine("App: Checking storage permissions...");
                
                // Check if we have all files access
                if (!Platforms.Android.StoragePermissionHelper.HasAllFilesAccess())
                {
                    System.Diagnostics.Debug.WriteLine("App: Requesting all files access...");
                    
                    // Give the shell time to load
                    await Task.Delay(800);
                    
                    // Request permission with detailed explanation
                    var (granted, userCancelled) = await Platforms.Android.StoragePermissionHelper.RequestAllFilesAccessAsync();
                    
                    if (granted)
                    {
                        System.Diagnostics.Debug.WriteLine("App: All files access granted!");
                        
                        await Shell.Current.DisplayAlert(
                            "✓ Permission Granted",
                            "Thank you! The app now has full access to encrypt/decrypt your files.",
                            "OK");
                    }
                    else
                    {
                        // Permission denied or user cancelled
                        System.Diagnostics.Debug.WriteLine("App: Permission denied - closing app");
                        
                        // Show final message before closing
                        await Shell.Current.DisplayAlert(
                            "App Closing",
                            "The app cannot function without storage access.\n\n" +
                            "The app will now close.\n\n" +
                            "Open the app again and grant the permission to use it.",
                            "OK");
                        
                        // Close the app
                        CloseApp();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("App: Already have all files access");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App: Error requesting storage permission: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Don't close on error - let user try again
                await Shell.Current.DisplayAlert(
                    "Error",
                    "An error occurred while checking permissions.\n\n" +
                    "Please try restarting the app.",
                    "OK");
            }
#endif
        }

        private void CloseApp()
        {
#if ANDROID
            System.Diagnostics.Debug.WriteLine("App: Closing application");
            
            // Close the app gracefully
            var activity = Platform.CurrentActivity;
            if (activity != null)
            {
                activity.FinishAndRemoveTask();
                Java.Lang.JavaSystem.Exit(0);
            }
#endif
        }
    }
}