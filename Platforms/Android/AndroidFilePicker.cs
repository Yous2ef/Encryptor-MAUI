using Android.App;
using Android.Content;
using AndroidX.DocumentFile.Provider;
using Encryptor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Encryptor.Platforms.Android
{
    /// <summary>
    /// Native Android file picker using Storage Access Framework.
    /// Supports single and multiple file selection with persistent URI permissions.
    /// </summary>
    public class AndroidFilePicker
    {
        private static TaskCompletionSource<List<global::Android.Net.Uri>>? _pickFilesTaskCompletionSource;
        private static System.Threading.CancellationTokenSource? _timeoutCancellationTokenSource;
        private const int PICKER_TIMEOUT_MINUTES = 10; // 10 minute timeout
        
        /// <summary>
        /// Check if a picker operation is currently active
        /// </summary>
        public static bool IsPickerActive => _pickFilesTaskCompletionSource != null && !_pickFilesTaskCompletionSource.Task.IsCompleted;

        /// <summary>
        /// Pick multiple files using Android's native document picker.
        /// </summary>
        public static async Task<List<global::Android.Net.Uri>> PickMultipleFilesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Starting PickMultipleFilesAsync");
                
                // Cancel any previous pending operation
                if (_pickFilesTaskCompletionSource != null && !_pickFilesTaskCompletionSource.Task.IsCompleted)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Cancelling previous operation");
                    _pickFilesTaskCompletionSource.TrySetCanceled();
                }
                
                // Cancel any previous timeout
                _timeoutCancellationTokenSource?.Cancel();
                _timeoutCancellationTokenSource = new System.Threading.CancellationTokenSource();
                
                _pickFilesTaskCompletionSource = new TaskCompletionSource<List<global::Android.Net.Uri>>();

                var activity = Platform.CurrentActivity;
                if (activity == null)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFilePicker: No current activity");
                    _pickFilesTaskCompletionSource.SetResult(new List<global::Android.Net.Uri>());
                    return _pickFilesTaskCompletionSource.Task.Result;
                }

                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Current activity: {activity.GetType().Name}");

                var intent = new Intent(Intent.ActionOpenDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("*/*"); // Allow all file types
                intent.PutExtra(Intent.ExtraAllowMultiple, true); // Enable multiple selection
                intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

                System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Calling StartActivityForResult");
                activity.StartActivityForResult(intent, 10001);
                System.Diagnostics.Debug.WriteLine("AndroidFilePicker: StartActivityForResult completed");

                // Start timeout task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(PICKER_TIMEOUT_MINUTES), _timeoutCancellationTokenSource.Token);
                        
                        // If we reach here, timeout occurred
                        System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Timeout after {PICKER_TIMEOUT_MINUTES} minutes");
                        if (_pickFilesTaskCompletionSource != null && !_pickFilesTaskCompletionSource.Task.IsCompleted)
                        {
                            _pickFilesTaskCompletionSource.TrySetResult(new List<global::Android.Net.Uri>());
                        }
                    }
                    catch (System.Threading.Tasks.TaskCanceledException)
                    {
                        // Timeout was cancelled, which is fine
                        System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Timeout cancelled (picker completed)");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Timeout task error: {ex.Message}");
                    }
                }, _timeoutCancellationTokenSource.Token);

                return await _pickFilesTaskCompletionSource.Task;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Exception in PickMultipleFilesAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Stack trace: {ex.StackTrace}");

                if (_pickFilesTaskCompletionSource != null)
                {
                    _pickFilesTaskCompletionSource.SetException(ex);
                    return await _pickFilesTaskCompletionSource.Task;
                }

                return new List<global::Android.Net.Uri>();
            }
        }

        /// <summary>
        /// Pick a single file using Android's native document picker.
        /// </summary>
        public static Task<global::Android.Net.Uri?> PickSingleFileAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Starting PickSingleFileAsync");
                var tcs = new TaskCompletionSource<global::Android.Net.Uri?>();

                var activity = Platform.CurrentActivity;
                if (activity == null)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFilePicker: No current activity");
                    tcs.SetResult(null);
                    return tcs.Task;
                }

                var intent = new Intent(Intent.ActionOpenDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("*/*");
                intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

                System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Calling StartActivityForResult for single file");
                activity.StartActivityForResult(intent, 10002);

                // Use the same handler but return only the first file
                _pickFilesTaskCompletionSource = new TaskCompletionSource<List<global::Android.Net.Uri>>();
                _pickFilesTaskCompletionSource.Task.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result.Count > 0)
                    {
                        tcs.TrySetResult(t.Result[0]);
                    }
                    else if (t.IsFaulted)
                    {
                        tcs.TrySetException(t.Exception!);
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                });

                return tcs.Task;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Exception in PickSingleFileAsync: {ex.Message}");
                return Task.FromResult<global::Android.Net.Uri?>(null);
            }
        }

        /// <summary>
        /// Handle the result from the file picker activity.
        /// Call this from MainActivity.OnActivityResult.
        /// </summary>
        public static void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            // Check if this is a file picker request
            if (requestCode != 10001 && requestCode != 10002)
                return;

            System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: HandleActivityResult - RequestCode: {requestCode}, ResultCode: {resultCode}");

            // Cancel timeout
            _timeoutCancellationTokenSource?.Cancel();

            // Check if we have a pending task
            if (_pickFilesTaskCompletionSource == null || _pickFilesTaskCompletionSource.Task.IsCompleted)
            {
                System.Diagnostics.Debug.WriteLine("AndroidFilePicker: No pending task or already completed");
                return;
            }

            var uris = new List<global::Android.Net.Uri>();

            if (resultCode == Result.Ok && data != null)
            {
                var activity = Platform.CurrentActivity;

                // Handle multiple file selection
                if (data.ClipData != null)
                {
                    var clipData = data.ClipData;
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Got {clipData.ItemCount} files from ClipData");

                    for (int i = 0; i < clipData.ItemCount; i++)
                    {
                        var uri = clipData.GetItemAt(i)?.Uri;
                        if (uri != null)
                        {
                            // Take persistable permission for the file
                            if (activity != null)
                            {
                                try
                                {
                                    var takeFlags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                                    activity.ContentResolver?.TakePersistableUriPermission(uri, takeFlags);
                                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Took persistent permission for: {uri}");
                                    
                                    // Try to get and request permission for parent directory
                                    TryRequestParentDirectoryPermission(activity, uri);
                                }
                                catch (System.Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Error taking persistent permission: {ex.Message}");
                                }
                            }

                            uris.Add(uri);
                        }
                    }
                }
                // Handle single file selection
                else if (data.Data != null)
                {
                    var uri = data.Data;
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Got single file: {uri}");

                    // Take persistable permission for the file
                    if (activity != null)
                    {
                        try
                        {
                            var takeFlags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                            activity.ContentResolver?.TakePersistableUriPermission(uri, takeFlags);
                            System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Took persistent permission for: {uri}");
                            
                            // Try to get and request permission for parent directory
                            TryRequestParentDirectoryPermission(activity, uri);
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Error taking persistent permission: {ex.Message}");
                        }
                    }

                    uris.Add(uri);
                }

                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Total URIs collected: {uris.Count}");
                _pickFilesTaskCompletionSource?.TrySetResult(uris);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: User cancelled or error occurred. ResultCode: {resultCode}");
                _pickFilesTaskCompletionSource?.TrySetResult(uris);
            }
        }

        /// <summary>
        /// Try to request persistable permission for the parent directory of a picked file.
        /// This allows creating sibling files (encrypted versions) in the same location.
        /// </summary>
        private static void TryRequestParentDirectoryPermission(Activity activity, global::Android.Net.Uri fileUri)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Attempting to get parent directory permission for: {fileUri}");
                
                // Try to build a tree URI for the parent directory
                var parentTreeUri = AndroidUriHelper.GetParentTreeUri(fileUri);
                if (parentTreeUri != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Requesting permission for parent tree URI: {parentTreeUri}");
                    
                    var takeFlags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                    activity.ContentResolver?.TakePersistableUriPermission(parentTreeUri, takeFlags);
                    
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Successfully got parent directory permission!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Could not determine parent tree URI");
                }
            }
            catch (System.Exception ex)
            {
                // This is expected to fail for single file picks - permissions weren't granted for parent
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Could not get parent directory permission (expected): {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cancel any pending picker operation (call this when activity is being destroyed)
        /// </summary>
        public static void CancelPendingOperation()
        {
            System.Diagnostics.Debug.WriteLine("AndroidFilePicker: Cancelling pending operation due to activity lifecycle");
            _timeoutCancellationTokenSource?.Cancel();
            
            if (_pickFilesTaskCompletionSource != null && !_pickFilesTaskCompletionSource.Task.IsCompleted)
            {
                _pickFilesTaskCompletionSource.TrySetResult(new List<global::Android.Net.Uri>());
            }
        }

        /// <summary>
        /// Convert Android URIs to FileModel objects.
        /// </summary>
        public static IEnumerable<FileModel> ConvertUrisToFileModels(Context context, List<global::Android.Net.Uri> uris)
        {
            var files = new List<FileModel>();

            foreach (var uri in uris)
            {
                try
                {
                    var documentFile = DocumentFile.FromSingleUri(context, uri);
                    if (documentFile != null && documentFile.IsFile)
                    {
                        var fileModel = CreateFileModelFromDocument(context, documentFile, uri);
                        if (fileModel != null)
                        {
                            files.Add(fileModel);
                            System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Converted file: {fileModel.FileName}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Error converting URI {uri}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Converted {files.Count} files");
            return files;
        }

        private static FileModel? CreateFileModelFromDocument(Context context, DocumentFile document, global::Android.Net.Uri uri)
        {
            try
            {
                if (uri == null) return null;

                string fileName = document.Name ?? "unknown";
                long fileSize = document.Length();
                string extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();

                // Try to get real file path if we have full storage access
                string filePath = Platforms.Android.AndroidUriHelper.GetRealPathFromUri(context, uri);
                
                if (string.IsNullOrEmpty(filePath))
                {
                    // Fall back to content URI
                    filePath = uri.ToString();
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Using content URI: {filePath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Using real path: {filePath}");
                }

                return new FileModel
                {
                    FilePath = filePath,
                    FileName = fileName,
                    Extension = extension,
                    FileSize = fileSize,
                    IsEncrypted = extension.Equals(".enc", System.StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: CreateFileModelFromDocument error: {ex.Message}");
                return null;
            }
        }
    }
}
