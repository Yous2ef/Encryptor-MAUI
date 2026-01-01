using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Encryptor.Models;
using Encryptor.Services;

namespace Encryptor.ViewModels;

/// <summary>
/// Main ViewModel for the Dashboard/Main page.
/// Handles file selection, encryption/decryption, and UI state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IEncryptionService _encryptionService;
    private readonly IFileService _fileService;
    private readonly ScopedDataService _scopedDataService;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainViewModel(
        IEncryptionService encryptionService,
        IFileService fileService,
        ScopedDataService scopedDataService)
    {
        _encryptionService = encryptionService;
        _fileService = fileService;
        _scopedDataService = scopedDataService;
        
        // Load theme preference
        IsDarkMode = Preferences.Get("IsDarkMode", false);
        ApplyTheme();
        
        // Default to overwrite original
        OverwriteOriginal = true;
    }

    #region Observable Properties

    /// <summary>
    /// Collection of files loaded for processing.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FileModel> _files = [];

    /// <summary>
    /// Key for encryption/decryption.
    /// </summary>
    [ObservableProperty]
    private string _key = string.Empty;

    /// <summary>
    /// Progress value (0.0 to 1.0) for the progress bar.
    /// </summary>
    [ObservableProperty]
    private double _progressValue;

    /// <summary>
    /// Status message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// True = Encrypt Mode, False = Decrypt Mode.
    /// </summary>
    [ObservableProperty]
    private bool _isEncryptMode = true;

    /// <summary>
    /// If true, overwrite original files. If false, decrypt to RAM only.
    /// </summary>
    [ObservableProperty]
    private bool _overwriteOriginal = true;

    /// <summary>
    /// Indicates if an operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// True = Grid view, False = List view.
    /// </summary>
    [ObservableProperty]
    private bool _isGridView = true;

    /// <summary>
    /// Number of columns in grid layout (responsive based on screen width).
    /// </summary>
    [ObservableProperty]
    private int _gridSpan = 4;

    /// <summary>
    /// Number of selected files.
    /// </summary>
    [ObservableProperty]
    private int _selectedCount;

    /// <summary>
    /// Indicates if multi-selection mode is active.
    /// </summary>
    [ObservableProperty]
    private bool _isMultiSelectMode;

    /// <summary>
    /// Dark mode toggle.
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>
    /// Key visibility toggle.
    /// </summary>
    [ObservableProperty]
    private bool _isKeyHidden = true;

    /// <summary>
    /// Custom save location for decrypted files.
    /// </summary>
    [ObservableProperty]
    private string? _saveLocation;

    /// <summary>
    /// Number of files skipped during processing.
    /// </summary>
    [ObservableProperty]
    private int _skippedCount;

    /// <summary>
    /// Tracks if user added new files after decryption (hides Save All).
    /// </summary>
    [ObservableProperty]
    private bool _newFilesAddedAfterDecryption;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Check if any files have decrypted data in memory.
    /// </summary>
    public bool HasDecryptedFiles => Files.Any(f => f.HasDecryptedDataInMemory);

    /// <summary>
    /// Show Save All button only if files are decrypted and no new files added.
    /// </summary>
    public bool ShowSaveAllButton => HasDecryptedFiles && !NewFilesAddedAfterDecryption;

    /// <summary>
    /// Mode display text for the toggle.
    /// </summary>
    public string ModeDisplayText => IsEncryptMode ? "🔐 Encrypt Mode" : "🔓 Decrypt Mode";

    /// <summary>
    /// View mode display text.
    /// </summary>
    public string ViewModeDisplayText => IsGridView ? "Grid View" : "List View";

    /// <summary>
    /// Theme icon for toggle.
    /// </summary>
    public string ThemeIcon => IsDarkMode ? "☀️" : "🌙";

    /// <summary>
    /// Key visibility icon.
    /// </summary>
    public string KeyVisibilityIcon => IsKeyHidden ? "👁️" : "👁️‍🗨️";

    /// <summary>
    /// Check if files are present.
    /// </summary>
    public bool HasFiles => Files.Count > 0;

    #endregion

    #region Commands

    /// <summary>
    /// Pick files (single or multiple based on platform).
    /// </summary>
    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var files = await _fileService.PickMultipleFilesAsync();
        foreach (var file in files)
        {
            // Avoid duplicates
            if (!Files.Any(f => f.FilePath == file.FilePath))
            {
                Files.Add(file);
            }
        }
        if (files.Any())
        {
            // Hide Save All if adding new files after decryption
            if (HasDecryptedFiles)
            {
                NewFilesAddedAfterDecryption = true;
                OnPropertyChanged(nameof(ShowSaveAllButton));
            }
            
            StatusMessage = $"Added {files.Count()} file(s)";
            OnPropertyChanged(nameof(HasFiles));
        }
    }

    /// <summary>
    /// Pick a folder and load all files recursively.
    /// </summary>
    [RelayCommand]
    private async Task PickFolderAsync()
    {
        StatusMessage = "Loading folder...";
        var files = await _fileService.PickFolderAsync();
        int added = 0;
        foreach (var file in files)
        {
            if (!Files.Any(f => f.FilePath == file.FilePath))
            {
                Files.Add(file);
                added++;
            }
        }        
        if (added > 0)
        {
            // Hide Save All if adding new files after decryption
            if (HasDecryptedFiles)
            {
                NewFilesAddedAfterDecryption = true;
                OnPropertyChanged(nameof(ShowSaveAllButton));
            }
            StatusMessage = $"Added {added} files from folder";
        }
        else
        {
            StatusMessage = "No new files found";
        }
        OnPropertyChanged(nameof(HasFiles));
    }

    /// <summary>
    /// Toggle key visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleKeyVisibility()
    {
        IsKeyHidden = !IsKeyHidden;
        OnPropertyChanged(nameof(KeyVisibilityIcon));
    }

    /// <summary>
    /// Toggle dark/light theme.
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        Preferences.Set("IsDarkMode", IsDarkMode);
        ApplyTheme();
        OnPropertyChanged(nameof(ThemeIcon));
    }

    /// <summary>
    /// Apply the current theme.
    /// </summary>
    private void ApplyTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = IsDarkMode ? AppTheme.Dark : AppTheme.Light;
        }
    }

    /// <summary>
    /// Remove a single file from the list.
    /// </summary>
    [RelayCommand]
    private void RemoveFile(FileModel? file)
    {
        if (file is null) return;
        
        // Clean up memory if file was decrypted
        file.Dispose();
        
        Files.Remove(file);
        StatusMessage = $"Removed {file.FileName}";
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasDecryptedFiles));
        OnPropertyChanged(nameof(ShowSaveAllButton));
    }

    /// <summary>
    /// Toggle between Grid and List view.
    /// </summary>
    [RelayCommand]
    private void ToggleViewMode()
    {
        IsGridView = !IsGridView;
        OnPropertyChanged(nameof(ViewModeDisplayText));
    }

    /// <summary>
    /// Toggle between Encrypt and Decrypt mode.
    /// </summary>
    [RelayCommand]
    private void ToggleMode()
    {
        IsEncryptMode = !IsEncryptMode;
        OnPropertyChanged(nameof(ModeDisplayText));
    }

    /// <summary>
    /// Handle item tap - toggle selection if in multi-select mode.
    /// </summary>
    [RelayCommand]
    private async Task ItemTappedAsync(FileModel? file)
    {
        if (file is null) return;

        if (IsMultiSelectMode)
        {
            file.IsSelected = !file.IsSelected;
            UpdateSelectedCount();
        }
        else
        {
            // Single tap - if not encrypted and not in encrypt mode, view directly
            if (!file.IsEncrypted && !IsEncryptMode)
            {
                await ViewFileAsync(file);
            }
        }
    }

    /// <summary>
    /// Handle long press - enter multi-select mode.
    /// </summary>
    [RelayCommand]
    private void ItemLongPressed(FileModel? file)
    {
        if (file is null) return;

        IsMultiSelectMode = true;
        file.IsSelected = true;
        UpdateSelectedCount();
        StatusMessage = "Multi-select mode. Tap files to select.";
    }

    /// <summary>
    /// Select all files.
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var file in Files)
        {
            file.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    /// <summary>
    /// Deselect all files and exit multi-select mode.
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var file in Files)
        {
            file.IsSelected = false;
        }
        IsMultiSelectMode = false;
        UpdateSelectedCount();
        StatusMessage = "Selection cleared";
    }

    /// <summary>
    /// Remove selected files from the list.
    /// </summary>
    [RelayCommand]
    private void RemoveSelected()
    {
        var toRemove = Files.Where(f => f.IsSelected).ToList();
        foreach (var file in toRemove)
        {
            file.Dispose();
            Files.Remove(file);
        }
        IsMultiSelectMode = false;
        UpdateSelectedCount();
        StatusMessage = $"Removed {toRemove.Count} files";
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasDecryptedFiles));
        OnPropertyChanged(nameof(ShowSaveAllButton));
    }

    /// <summary>
    /// Clear all files from the list.
    /// </summary>
    [RelayCommand]
    private void ClearAll()
    {
        // Clean up memory for all decrypted files
        foreach (var file in Files)
        {
            file.Dispose();
        }
        
        Files.Clear();
        IsMultiSelectMode = false;
        UpdateSelectedCount();
        NewFilesAddedAfterDecryption = false;
        StatusMessage = "Cleared all files";
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasDecryptedFiles));
        OnPropertyChanged(nameof(ShowSaveAllButton));
    }

    /// <summary>
    /// Pick save location for decrypted files.
    /// </summary>
    [RelayCommand]
    private async Task PickSaveLocationAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (result.IsSuccessful && result.Folder is not null)
            {
                SaveLocation = result.Folder.Path;
                StatusMessage = $"Save location: {SaveLocation}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not pick folder: {ex.Message}";
        }
    }

    /// <summary>
    /// Process selected files (encrypt or decrypt based on mode).
    /// Smart processing: Skip files that are already in the target state.
    /// </summary>
    [RelayCommand]
    private async Task ProcessFilesAsync()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            StatusMessage = "⚠️ Please enter a key";
            await Shell.Current.DisplayAlertAsync("Key Required", "Please enter a key for encryption/decryption.", "OK");
            return;
        }

        var filesToProcess = Files.Where(f => f.IsSelected || !IsMultiSelectMode).ToList();
        if (filesToProcess.Count == 0)
        {
            if (IsMultiSelectMode)
            {
                StatusMessage = "⚠️ No files selected";
                return;
            }
            filesToProcess = Files.ToList();
        }

        if (filesToProcess.Count == 0)
        {
            StatusMessage = "⚠️ No files to process";
            return;
        }

        // Smart processing: Filter out files that are already in target state
        var actualFilesToProcess = new List<FileModel>();
        SkippedCount = 0;

        foreach (var file in filesToProcess)
        {
            if (IsEncryptMode && file.IsEncrypted)
            {
                // Skip already encrypted files when encrypting
                SkippedCount++;
            }
            else if (!IsEncryptMode && !file.IsEncrypted)
            {
                // Skip already decrypted files when decrypting
                SkippedCount++;
            }
            else
            {
                actualFilesToProcess.Add(file);
            }
        }

        if (actualFilesToProcess.Count == 0)
        {
            string msg = IsEncryptMode 
                ? "All selected files are already encrypted." 
                : "All selected files are already decrypted.";
            StatusMessage = $"ℹ️ {msg}";
            await Shell.Current.DisplayAlertAsync("Nothing to Process", msg, "OK");
            return;
        }

        IsProcessing = true;
        ProgressValue = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            int processed = 0;
            int failed = 0;
            int total = actualFilesToProcess.Count;
            var failedFiles = new List<(string FileName, string Reason)>();

            foreach (var file in actualFilesToProcess)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                file.IsProcessing = true;
                StatusMessage = $"Processing: {file.FileName}";

                var progress = new Progress<double>(p =>
                {
                    double overallProgress = (processed + failed + p) / total;
                    ProgressValue = overallProgress;
                });

                try
                {
                    if (IsEncryptMode)
                    {
                        string originalPath = file.FilePath;
                        
                        string encryptedPath = await _encryptionService.EncryptFileAsync(
                            file.FilePath,
                            Key,
                            OverwriteOriginal,
                            progress,
                            _cancellationTokenSource.Token);

                        // Update file model to reflect encrypted state
                        file.IsEncrypted = true;
                        file.FilePath = encryptedPath;
                        file.FileName = Path.GetFileName(encryptedPath);
                        file.Extension = ".enc";
                        
                        // Update file size if it's a regular file path
                        if (!encryptedPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(encryptedPath))
                            {
                                var fileInfo = new FileInfo(encryptedPath);
                                file.FileSize = fileInfo.Length;
                            }
                        }
                        else
                        {
                            // For content URIs, try to get size from DocumentFile
#if ANDROID
                            try
                            {
                                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                                var uri = global::Android.Net.Uri.Parse(encryptedPath);
                                var documentFile = AndroidX.DocumentFile.Provider.DocumentFile.FromSingleUri(context, uri);
                                if (documentFile != null)
                                {
                                    file.FileSize = documentFile.Length();
                                }
                            }
                            catch (Exception sizeEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Could not get file size: {sizeEx.Message}");
                            }
#endif
                        }
                        
                        processed++;
                    }
                    else
                    {
                        // ALWAYS decrypt to memory (never auto-save to disk)
                        byte[] encryptedData = await File.ReadAllBytesAsync(file.FilePath, _cancellationTokenSource.Token);
                        ((IProgress<double>)progress).Report(0.3);
                        
                        byte[] decryptedData = await _encryptionService.DecryptAsync(
                            encryptedData, 
                            Key, 
                            progress, 
                            _cancellationTokenSource.Token);
                        
                        // Store original extension for later saving
                        if (file.FileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
                        {
                            file.OriginalExtension = Path.GetExtension(file.FileName[..^4]);
                        }
                        
                        file.IsEncrypted = false;
                        
                        // Store decrypted data in memory (this triggers ThumbnailSource update via ObservableProperty)
                        file.DecryptedData = decryptedData;
                        file.IsDecrypted = true;
                        processed++;
                    }
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    failedFiles.Add((file.FileName, "Wrong key"));
                    failed++;
                }
                catch (Exception ex)
                {
                    failedFiles.Add((file.FileName, ex.Message));
                    failed++;
                }

                file.IsProcessing = false;
                ProgressValue = (double)(processed + failed) / total;
            }

            // Show summary
            string skippedMsg = SkippedCount > 0 ? $", {SkippedCount} skipped" : "";
            
            if (IsEncryptMode)
            {
                StatusMessage = $"✅ Encrypted {processed} files{skippedMsg}";
                if (failed > 0)
                {
                    StatusMessage = $"⚠️ Encrypted {processed}, failed {failed}{skippedMsg}";
                }
            }
            else
            {
                StatusMessage = $"✅ Decrypted {processed} files to memory{skippedMsg}";
                
                // Show detailed summary for decryption
                if (failed > 0)
                {
                    StatusMessage = $"⚠️ Decrypted {processed}, failed {failed}{skippedMsg}";
                    
                    var failedList = string.Join("\n", failedFiles.Select(f => $"❌ {f.FileName} - {f.Reason}"));
                    var successMsg = processed > 0 ? $"✅ Successfully decrypted: {processed}\n\n" : "";
                    
                    await Shell.Current.DisplayAlertAsync(
                        "Decryption Summary", 
                        $"{successMsg}❌ Failed: {failed}\n\n{failedList}", 
                        "OK");
                }
                else if (processed > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Success", 
                        $"✅ All {processed} files decrypted successfully to memory!", 
                        "OK");
                }
            }
            
            // Reset flag after decryption completes
            if (!IsEncryptMode)
            {
                NewFilesAddedAfterDecryption = false;
                OnPropertyChanged(nameof(HasDecryptedFiles));
                OnPropertyChanged(nameof(ShowSaveAllButton));
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "⚠️ Operation cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsProcessing = false;
            ProgressValue = 0;
            foreach (var file in actualFilesToProcess)
            {
                file.IsProcessing = false;
            }
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Cancel the current operation.
    /// </summary>
    [RelayCommand]
    private void CancelOperation()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling...";
    }

    /// <summary>
    /// View a single file (for unencrypted files or after decryption).
    /// </summary>
    [RelayCommand]
    private async Task ViewFileAsync(FileModel? file)
    {
        if (file is null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"ViewFileAsync: Starting for file {file.FileName}");
            System.Diagnostics.Debug.WriteLine($"ViewFileAsync: FilePath = {file.FilePath}");
            System.Diagnostics.Debug.WriteLine($"ViewFileAsync: IsEncrypted = {file.IsEncrypted}, HasDecryptedDataInMemory = {file.HasDecryptedDataInMemory}");
            
            if (file.HasDecryptedDataInMemory)
            {
                System.Diagnostics.Debug.WriteLine("ViewFileAsync: Using decrypted data from memory");
                // View decrypted data from memory
                var stream = new MemoryStream(file.DecryptedData!);
                string ext = file.OriginalExtension ?? file.Extension;
                _scopedDataService.SetViewerData(stream, file, ext);
                System.Diagnostics.Debug.WriteLine($"ViewFileAsync: SetViewerData called with extension {ext}");
            }
            else if (file.IsEncrypted)
            {
                System.Diagnostics.Debug.WriteLine("ViewFileAsync: File is encrypted");
                if (string.IsNullOrWhiteSpace(Key))
                {
                    await Shell.Current.DisplayAlertAsync("Key Required", "Enter key to view encrypted file.", "OK");
                    return;
                }

                IsProcessing = true;
                StatusMessage = $"Decrypting: {file.FileName}";

                var stream = await _encryptionService.DecryptFileToStreamAsync(
                    file.FilePath,
                    Key);

                string originalExt = GetOriginalExtension(file.FilePath);
                _scopedDataService.SetViewerData(stream, file, originalExt);
                System.Diagnostics.Debug.WriteLine($"ViewFileAsync: SetViewerData called with extension {originalExt}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ViewFileAsync: Reading unencrypted file");
                // Read unencrypted file into memory
                var data = await _fileService.ReadAllBytesAsync(file.FilePath);
                System.Diagnostics.Debug.WriteLine($"ViewFileAsync: Read {data.Length} bytes");
                var stream = new MemoryStream(data);
                System.Diagnostics.Debug.WriteLine($"ViewFileAsync: Created MemoryStream");
                _scopedDataService.SetViewerData(stream, file, file.Extension);
                System.Diagnostics.Debug.WriteLine($"ViewFileAsync: SetViewerData called with extension {file.Extension}");
            }

            System.Diagnostics.Debug.WriteLine("ViewFileAsync: Navigating to ViewerPage");
            await Shell.Current.GoToAsync("ViewerPage");
            System.Diagnostics.Debug.WriteLine("ViewFileAsync: Navigation completed");
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewFileAsync: Cryptographic error: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Wrong Key", "The key you entered is incorrect. Please try again.", "OK");
            StatusMessage = "❌ Wrong key";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewFileAsync: Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ViewFileAsync: Stack trace: {ex.StackTrace}");
            await Shell.Current.DisplayAlertAsync("Error", $"Cannot view file: {ex.Message}", "OK");
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Save a single decrypted file from memory to disk.
    /// </summary>
    [RelayCommand]
    private async Task SaveFileAsync(FileModel? file)
    {
        if (file is null || !file.HasDecryptedDataInMemory) return;

        try
        {
            // Get original filename without .enc extension
            string defaultFileName = file.FileName;
            if (defaultFileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            {
                defaultFileName = defaultFileName[..^4];
            }

            // Use file picker to let user choose save location
            var result = await FileSaver.Default.SaveAsync(defaultFileName, new MemoryStream(file.DecryptedData!));

            if (result.IsSuccessful)
            {
                StatusMessage = $"💾 Saved: {Path.GetFileName(result.FilePath)}";
                await Shell.Current.DisplayAlertAsync("Success", $"File saved successfully!", "OK");
            }
            else
            {
                StatusMessage = "Save cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Save failed: {ex.Message}";
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to save file: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Save all decrypted files from memory to disk.
    /// </summary>
    [RelayCommand]
    private async Task SaveAllFilesAsync()
    {
        var decryptedFiles = Files.Where(f => f.HasDecryptedDataInMemory).ToList();
        if (decryptedFiles.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("No Files", "No decrypted files to save.", "OK");
            return;
        }

        try
        {
            // Ask user to select a folder
            var result = await FolderPicker.Default.PickAsync();
            if (result.IsSuccessful)
            {
                int saved = 0;
                foreach (var file in decryptedFiles)
                {
                    try
                    {
                        // Get original filename
                        string fileName = file.FileName;
                        if (fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = fileName[..^4];
                        }

                        string outputPath = Path.Combine(result.Folder.Path, fileName);
                        await File.WriteAllBytesAsync(outputPath, file.DecryptedData!);
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        await Shell.Current.DisplayAlertAsync("Error", $"Failed to save {file.FileName}: {ex.Message}", "OK");
                    }
                }

                StatusMessage = $"💾 Saved {saved}/{decryptedFiles.Count} files";
                await Shell.Current.DisplayAlertAsync("Success", $"Saved {saved} file(s) to {result.Folder.Path}", "OK");
            }
            else
            {
                StatusMessage = "Save cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Save failed: {ex.Message}";
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to save files: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Private Methods

    private void UpdateSelectedCount()
    {
        SelectedCount = Files.Count(f => f.IsSelected);
    }

    private static string GetOriginalExtension(string encryptedPath)
    {
        // If .enc file, try to get the original extension from the name
        // e.g., "photo.jpg.enc" -> ".jpg"
        if (encryptedPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
        {
            string withoutEnc = encryptedPath[..^4];
            return Path.GetExtension(withoutEnc);
        }
        return Path.GetExtension(encryptedPath);
    }

    #endregion
}
