using CommunityToolkit.Maui.Storage;
using Encryptor.Models;

namespace Encryptor.Services;

/// <summary>
/// File service implementation for picking files/folders and file operations.
/// </summary>
public class FileService : IFileService
{
    /// <inheritdoc />
    public async Task<FileModel?> PickFileAsync()
    {
        try
        {
            if (!await CheckAndRequestPermissionsAsync())
                return null;

#if ANDROID
            // Use native Android file picker for better SAF support
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var uri = await Platforms.Android.AndroidFilePicker.PickSingleFileAsync();
                
                if (uri == null)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFilePicker: User cancelled or no file selected");
                    return null;
                }
                
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Got file URI: {uri}");
                var files = Platforms.Android.AndroidFilePicker.ConvertUrisToFileModels(context, new List<global::Android.Net.Uri> { uri });
                var file = files.FirstOrDefault();
                
                if (file != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Converted to FileModel: {file.FileName}");
                }
                
                return file;
            }
            catch (Exception androidEx)
            {
                System.Diagnostics.Debug.WriteLine($"Android file picker error: {androidEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {androidEx.StackTrace}");
                
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Could not pick file: {androidEx.Message}",
                    "OK");
                
                return null;
            }
#else
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a file to process"
            });

            if (result is null)
                return null;

            return FileModel.FromPath(result.FullPath);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PickFileAsync error: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileModel>> PickMultipleFilesAsync()
    {
        try
        {
            if (!await CheckAndRequestPermissionsAsync())
                return [];

#if ANDROID
            // Use native Android file picker instead of MAUI FilePicker for better SAF support
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                
                // Picker now has built-in timeout handling
                var uris = await Platforms.Android.AndroidFilePicker.PickMultipleFilesAsync();
                
                if (uris == null || uris.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidFilePicker: User cancelled or no files selected");
                    return [];
                }
                
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Got {uris.Count} file URIs");
                var files = Platforms.Android.AndroidFilePicker.ConvertUrisToFileModels(context, uris);
                System.Diagnostics.Debug.WriteLine($"AndroidFilePicker: Converted to {files.Count()} FileModels");
                return files;
            }
            catch (Exception androidEx)
            {
                System.Diagnostics.Debug.WriteLine($"Android file picker error: {androidEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {androidEx.StackTrace}");
                
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Could not pick files: {androidEx.Message}",
                    "OK");
                
                return [];
            }
#else
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select files to process"
            });

            if (results is null || !results.Any())
                return [];

            return results.Select(r => FileModel.FromPath(r.FullPath));
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PickMultipleFilesAsync error: {ex.Message}");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileModel>> PickFolderAsync()
    {
        try
        {
            if (!await CheckAndRequestPermissionsAsync())
            {
                System.Diagnostics.Debug.WriteLine("Permissions not granted for folder picking");
                return [];
            }

            System.Diagnostics.Debug.WriteLine("Starting folder picker...");

#if ANDROID
            // Use native Android folder picker instead of CommunityToolkit
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var treeUri = await Platforms.Android.AndroidFolderPicker.PickFolderAsync();

                if (treeUri == null)
                {
                    System.Diagnostics.Debug.WriteLine("Android folder picker: User cancelled");
                    return [];
                }

                System.Diagnostics.Debug.WriteLine($"Android folder picker: Got URI {treeUri}");
                var files = Platforms.Android.AndroidFolderPicker.EnumerateFiles(context, treeUri);
                System.Diagnostics.Debug.WriteLine($"Android folder picker: Found {files.Count()} files");
                return files;
            }
            catch (Exception androidEx)
            {
                System.Diagnostics.Debug.WriteLine($"Android folder picker error: {androidEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {androidEx.StackTrace}");

                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Could not pick folder: {androidEx.Message}",
                    "OK");

                return [];
            }
#else
            // Use CommunityToolkit FolderPicker for other platforms
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);

            if (result is null)
            {
                System.Diagnostics.Debug.WriteLine("Folder picker returned null");
                return [];
            }

            if (!result.IsSuccessful)
            {
                System.Diagnostics.Debug.WriteLine($"Folder picker not successful. Exception: {result.Exception?.Message}");
                return [];
            }

            if (result.Folder is null)
            {
                System.Diagnostics.Debug.WriteLine("Folder is null");
                return [];
            }

            System.Diagnostics.Debug.WriteLine($"Folder picked: {result.Folder.Path}");

            // Use standard file enumeration for iOS, Windows, Mac
            if (!string.IsNullOrEmpty(result.Folder.Path))
            {
                var files = GetFilesRecursive(result.Folder.Path);
                System.Diagnostics.Debug.WriteLine($"Standard enumeration found {files.Count()} files");
                return files;
            }

            return [];
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PickFolderAsync error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            // Show error to user
            await Shell.Current.DisplayAlertAsync(
                "Error",
                $"Could not pick folder: {ex.Message}",
                "OK");

            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckAndRequestPermissionsAsync()
    {
#if ANDROID
        System.Diagnostics.Debug.WriteLine("Checking Android permissions...");

        // Check Read permission
        var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        System.Diagnostics.Debug.WriteLine($"Read permission status: {readStatus}");

        if (readStatus != PermissionStatus.Granted)
        {
            readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
            System.Diagnostics.Debug.WriteLine($"Read permission after request: {readStatus}");

            if (readStatus != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Storage read permission is required to access files.",
                    "OK");
                return false;
            }
        }

        // Check Write permission
        var writeStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        System.Diagnostics.Debug.WriteLine($"Write permission status: {writeStatus}");

        if (writeStatus != PermissionStatus.Granted)
        {
            writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
            System.Diagnostics.Debug.WriteLine($"Write permission after request: {writeStatus}");

            if (writeStatus != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Storage write permission is required to save encrypted files.",
                    "OK");
                return false;
            }
        }

        System.Diagnostics.Debug.WriteLine("All permissions granted!");
#endif
        return true;
    }

#if ANDROID
    /// <summary>
    /// Android-specific method to enumerate files using Java File API
    /// This works better than .NET Directory.GetFiles on Android 11+
    /// </summary>
    private async Task<IEnumerable<FileModel>> EnumerateAndroidFolderAsync(string folderPath)
    {
        return await Task.Run(() =>
        {
            var files = new List<FileModel>();

            try
            {
                var javaFile = new Java.IO.File(folderPath);

                if (!javaFile.Exists() || !javaFile.IsDirectory)
                {
                    System.Diagnostics.Debug.WriteLine($"Java.IO.File: Path doesn't exist or not a directory");
                    return files;
                }

                System.Diagnostics.Debug.WriteLine($"Java.IO.File: Enumerating {folderPath}");
                EnumerateJavaFiles(javaFile, files);
                System.Diagnostics.Debug.WriteLine($"Java.IO.File: Found {files.Count} files");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnumerateAndroidFolderAsync error: {ex.Message}");
            }

            return files;
        });
    }

    private void EnumerateJavaFiles(Java.IO.File directory, List<FileModel> files)
    {
        try
        {
            var entries = directory.ListFiles();

            if (entries == null || entries.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No entries in {directory.AbsolutePath}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Found {entries.Length} entries in {directory.Name}");

            foreach (var entry in entries)
            {
                if (entry == null) continue;

                try
                {
                    if (entry.IsDirectory)
                    {
                        EnumerateJavaFiles(entry, files);
                    }
                    else if (entry.IsFile)
                    {
                        var fileModel = new FileModel
                        {
                            FilePath = entry.AbsolutePath,
                            FileName = entry.Name,
                            Extension = Path.GetExtension(entry.Name).ToLowerInvariant(),
                            FileSize = entry.Length(),
                            IsEncrypted = entry.Name.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
                        };
                        files.Add(fileModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing {entry.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnumerateJavaFiles error: {ex.Message}");
        }
    }
#endif

    /// <inheritdoc />
    public IEnumerable<FileModel> GetFilesRecursive(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            System.Diagnostics.Debug.WriteLine("GetFilesRecursive: folderPath is null or empty");
            return [];
        }

        System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Checking path: {folderPath}");

        if (!Directory.Exists(folderPath))
        {
            System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Directory does not exist: {folderPath}");
            return [];
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Starting file scan...");
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Found {files.Length} files");

            if (files.Length == 0)
            {
                // Try to list directories to see if we can access the folder at all
                var dirs = Directory.GetDirectories(folderPath);
                System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Found {dirs.Length} subdirectories");

                // Try just top-level files
                var topLevelFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
                System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Found {topLevelFiles.Length} files in top level");
            }

            return files.Select(FileModel.FromPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetFilesRecursive: Access denied - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetFilesRecursive error: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return [];
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
#if ANDROID
        // Check if this is an Android content URI
        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var uri = global::Android.Net.Uri.Parse(path);

                using var inputStream = context.ContentResolver?.OpenInputStream(uri);
                return inputStream != null;
            }
            catch
            {
                return false;
            }
        }
#endif
        return File.Exists(path);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
#if ANDROID
        // Check if this is an Android content URI
        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Reading content URI: {path}");
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var uri = global::Android.Net.Uri.Parse(path);

                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: URI parsed successfully");

                using var inputStream = context.ContentResolver?.OpenInputStream(uri);
                if (inputStream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Could not open input stream");
                    throw new IOException($"Could not open content URI: {path}");
                }

                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Input stream opened, reading data");

                using var memoryStream = new MemoryStream();
                await inputStream.CopyToAsync(memoryStream, cancellationToken);

                var data = memoryStream.ToArray();
                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Read {data.Length} bytes from content URI");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync content URI error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync stack trace: {ex.StackTrace}");
                throw;
            }
        }

        System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Reading file path: {path}");
#endif

        try
        {
            System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Reading file from path: {path}");
            
            // Check if file exists
            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: File does not exist at path: {path}");
                throw new FileNotFoundException($"File not found: {path}");
            }

            // Get file info for logging
            var fileInfo = new FileInfo(path);
            System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: File exists, size: {fileInfo.Length} bytes");

            // Read file using FileStream for better control on Windows
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            
            var data = memoryStream.ToArray();
            System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync: Successfully read {data.Length} bytes");
            
            return data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync ERROR: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ReadAllBytesAsync stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default)
    {
#if ANDROID
        // Check if this is an Android content URI
        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync: Writing to content URI: {path}");
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var uri = global::Android.Net.Uri.Parse(path);

                using var outputStream = context.ContentResolver?.OpenOutputStream(uri);
                if (outputStream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync: Could not open output stream");
                    throw new IOException($"Could not open content URI for writing: {path}");
                }

                System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync: Writing {data.Length} bytes to content URI");
                await outputStream.WriteAsync(data, cancellationToken);
                await outputStream.FlushAsync();

                System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync: Successfully wrote to content URI");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync content URI error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync stack trace: {ex.StackTrace}");
                throw new IOException($"Failed to write to content URI: {ex.Message}", ex);
            }
        }

        System.Diagnostics.Debug.WriteLine($"WriteAllBytesAsync: Writing to file path: {path}");
#endif
        await File.WriteAllBytesAsync(path, data, cancellationToken);
    }
}
