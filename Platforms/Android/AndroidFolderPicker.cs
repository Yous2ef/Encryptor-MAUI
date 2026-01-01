using Android.App;
using Android.Content;
using AndroidX.DocumentFile.Provider;
using Encryptor.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Encryptor.Platforms.Android
{
    /// <summary>
    /// Native Android folder picker using Storage Access Framework.
    /// </summary>
    public class AndroidFolderPicker
    {
        private static TaskCompletionSource<global::Android.Net.Uri?>? _pickFolderTaskCompletionSource;
        private static System.Threading.CancellationTokenSource? _timeoutCancellationTokenSource;
        private const int PICKER_TIMEOUT_MINUTES = 10; // 10 minute timeout
        
        /// <summary>
        /// Check if a picker operation is currently active
        /// </summary>
        public static bool IsPickerActive => _pickFolderTaskCompletionSource != null && !_pickFolderTaskCompletionSource.Task.IsCompleted;

        /// <summary>
        /// Pick a folder using Android's native document picker.
        /// </summary>
        public static async Task<global::Android.Net.Uri?> PickFolderAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: Starting PickFolderAsync");
                
                // Cancel any previous pending operation
                if (_pickFolderTaskCompletionSource != null && !_pickFolderTaskCompletionSource.Task.IsCompleted)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: Cancelling previous operation");
                    _pickFolderTaskCompletionSource.TrySetCanceled();
                }
                
                // Cancel any previous timeout
                _timeoutCancellationTokenSource?.Cancel();
                _timeoutCancellationTokenSource = new System.Threading.CancellationTokenSource();
                
                _pickFolderTaskCompletionSource = new TaskCompletionSource<global::Android.Net.Uri?>();

                var activity = Platform.CurrentActivity;
                if (activity == null)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: No current activity");
                    _pickFolderTaskCompletionSource.SetResult(null);
                    return _pickFolderTaskCompletionSource.Task.Result;
                }

                System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Current activity: {activity.GetType().Name}");

                var intent = new Intent(Intent.ActionOpenDocumentTree);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

                System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: Calling StartActivityForResult");
                activity.StartActivityForResult(intent, 9999);
                System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: StartActivityForResult completed");
                
                // Start timeout task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(PICKER_TIMEOUT_MINUTES), _timeoutCancellationTokenSource.Token);
                        
                        // If we reach here, timeout occurred
                        System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Timeout after {PICKER_TIMEOUT_MINUTES} minutes");
                        if (_pickFolderTaskCompletionSource != null && !_pickFolderTaskCompletionSource.Task.IsCompleted)
                        {
                            _pickFolderTaskCompletionSource.TrySetResult(null);
                        }
                    }
                    catch (System.Threading.Tasks.TaskCanceledException)
                    {
                        // Timeout was cancelled, which is fine
                        System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: Timeout cancelled (picker completed)");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Timeout task error: {ex.Message}");
                    }
                }, _timeoutCancellationTokenSource.Token);
                
                return await _pickFolderTaskCompletionSource.Task;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Exception in PickFolderAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Stack trace: {ex.StackTrace}");
                
                if (_pickFolderTaskCompletionSource != null)
                {
                    _pickFolderTaskCompletionSource.SetException(ex);
                    return await _pickFolderTaskCompletionSource.Task;
                }
                
                return null;
            }
        }

        /// <summary>
        /// Handle the result from the folder picker activity.
        /// Call this from MainActivity.OnActivityResult.
        /// </summary>
        public static void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode != 9999)
                return;

            System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: HandleActivityResult - RequestCode: {requestCode}, ResultCode: {resultCode}");

            // Cancel timeout
            _timeoutCancellationTokenSource?.Cancel();

            // Check if we have a pending task
            if (_pickFolderTaskCompletionSource == null || _pickFolderTaskCompletionSource.Task.IsCompleted)
            {
                System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: No pending task or already completed");
                return;
            }

            if (resultCode == Result.Ok && data?.Data != null)
            {
                var uri = data.Data;
                
                // Take persistable permission to access this folder in the future
                var activity = Platform.CurrentActivity;
                if (activity != null && uri != null)
                {
                    try
                    {
                        var takeFlags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                        activity.ContentResolver?.TakePersistableUriPermission(uri, takeFlags);
                        System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Got URI with persistent permission: {uri}");
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Error taking persistent permission: {ex.Message}");
                    }
                }
                
                _pickFolderTaskCompletionSource?.TrySetResult(uri);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: User cancelled or error occurred. ResultCode: {resultCode}");
                _pickFolderTaskCompletionSource?.TrySetResult(null);
            }
        }

        /// <summary>
        /// Cancel any pending picker operation (call this when activity is being destroyed)
        /// </summary>
        public static void CancelPendingOperation()
        {
            System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: Cancelling pending operation due to activity lifecycle");
            _timeoutCancellationTokenSource?.Cancel();
            
            if (_pickFolderTaskCompletionSource != null && !_pickFolderTaskCompletionSource.Task.IsCompleted)
            {
                _pickFolderTaskCompletionSource.TrySetResult(null);
            }
        }

        /// <summary>
        /// Enumerate all files in a folder tree using DocumentFile API.
        /// </summary>
        public static IEnumerable<FileModel> EnumerateFiles(Context context, global::Android.Net.Uri treeUri)
        {
            var files = new List<FileModel>();
            var documentFile = DocumentFile.FromTreeUri(context, treeUri);

            if (documentFile == null || !documentFile.IsDirectory)
            {
                System.Diagnostics.Debug.WriteLine("AndroidFolderPicker: Invalid document tree");
                return files;
            }

            System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Starting enumeration from URI: {treeUri}");
            EnumerateFilesRecursive(context, documentFile, files);
            System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Found {files.Count} total files");

            return files;
        }

        private static void EnumerateFilesRecursive(Context context, DocumentFile folder, List<FileModel> files)
        {
            try
            {
                var children = folder.ListFiles();
                if (children == null || children.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: No children in {folder.Name}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Found {children.Length} items in {folder.Name}");

                foreach (var child in children)
                {
                    if (child == null) continue;

                    if (child.IsDirectory)
                    {
                        EnumerateFilesRecursive(context, child, files);
                    }
                    else if (child.IsFile)
                    {
                        var fileModel = CreateFileModelFromDocument(child);
                        if (fileModel != null)
                        {
                            files.Add(fileModel);
                            System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker: Added file: {fileModel.FileName}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFolderPicker enumeration error: {ex.Message}");
            }
        }

        private static FileModel? CreateFileModelFromDocument(DocumentFile document)
        {
            try
            {
                var uri = document.Uri;
                if (uri == null) return null;

                string fileName = document.Name ?? "unknown";
                long fileSize = document.Length();
                string extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();

                return new FileModel
                {
                    FilePath = uri.ToString(),
                    FileName = fileName,
                    Extension = extension,
                    FileSize = fileSize,
                    IsEncrypted = extension.Equals(".enc", System.StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileModelFromDocument error: {ex.Message}");
                return null;
            }
        }
    }
}
