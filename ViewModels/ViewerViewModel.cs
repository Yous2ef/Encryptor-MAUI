using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Encryptor.Models;
using Encryptor.Services;
using System.Threading.Tasks;

namespace Encryptor.ViewModels;

/// <summary>
/// ViewModel for the Viewer page - displays decrypted content in-memory.
/// </summary>
public partial class ViewerViewModel : ObservableObject
{
    private readonly ScopedDataService _scopedDataService;

    public ViewerViewModel(ScopedDataService scopedDataService)
    {
        _scopedDataService = scopedDataService;
    }

    #region Observable Properties

    /// <summary>
    /// The file name being viewed.
    /// </summary>
    [ObservableProperty]
    private string _fileName = "Viewer";

    /// <summary>
    /// The file category for display type selection.
    /// </summary>
    [ObservableProperty]
    private FileCategory _fileCategory;

    /// <summary>
    /// Image source for image files.
    /// </summary>
    [ObservableProperty]
    private ImageSource? _imageSource;

    /// <summary>
    /// Media source for video/audio files.
    /// </summary>
    [ObservableProperty]
    private object? _mediaSource;

    /// <summary>
    /// Text content for text files.
    /// </summary>
    [ObservableProperty]
    private string _textContent = string.Empty;

    /// <summary>
    /// Indicates if loading is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>
    /// Error message if loading fails.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Indicates if an error occurred.
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Visibility Properties

    /// <summary>
    /// True if displaying an image.
    /// </summary>
    public bool IsImage => FileCategory == FileCategory.Image;

    /// <summary>
    /// True if displaying video.
    /// </summary>
    public bool IsVideo => FileCategory == FileCategory.Video;

    /// <summary>
    /// True if displaying audio.
    /// </summary>
    public bool IsAudio => FileCategory == FileCategory.Audio;

    /// <summary>
    /// True if displaying text.
    /// </summary>
    public bool IsText => FileCategory == FileCategory.Text;

    /// <summary>
    /// True if displaying video or audio (uses MediaElement).
    /// </summary>
    public bool IsMedia => FileCategory == FileCategory.Video || FileCategory == FileCategory.Audio;

    /// <summary>
    /// True if file type is not supported for viewing.
    /// </summary>
    public bool IsUnsupported => FileCategory == FileCategory.Document ||
                                  FileCategory == FileCategory.Archive ||
                                  FileCategory == FileCategory.Unknown ||
                                  FileCategory == FileCategory.Encrypted;

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the viewer with data from ScopedDataService.
    /// Call this when the page appears.
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            System.Diagnostics.Debug.WriteLine("ViewerViewModel: InitializeAsync started");
            
            var stream = _scopedDataService.DecryptedStream;
            var file = _scopedDataService.CurrentFile;

            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Stream null? {stream == null}, File null? {file == null}");

            if (stream is null || file is null)
            {
                System.Diagnostics.Debug.WriteLine("ViewerViewModel: No file data available");
                HasError = true;
                ErrorMessage = "No file data available.";
                IsLoading = false;
                return;
            }

            FileName = file.FileName;
            FileCategory = _scopedDataService.GetFileCategory();

            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: FileName={FileName}, Category={FileCategory}");

            // Notify visibility changes
            OnPropertyChanged(nameof(IsImage));
            OnPropertyChanged(nameof(IsVideo));
            OnPropertyChanged(nameof(IsAudio));
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(IsMedia));
            OnPropertyChanged(nameof(IsUnsupported));

            // Reset stream position
            stream.Position = 0;
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Stream length={stream.Length}, position reset");

            await Task.Run(() => LoadContent(stream));
            
            System.Diagnostics.Debug.WriteLine("ViewerViewModel: LoadContent completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: InitializeAsync error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Stack trace: {ex.StackTrace}");
            HasError = true;
            ErrorMessage = $"Error loading file: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadContent(MemoryStream stream)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: LoadContent for category {FileCategory}");
            
            switch (FileCategory)
            {
                case FileCategory.Image:
                    LoadImage(stream);
                    break;

                case FileCategory.Video:
                case FileCategory.Audio:
                    LoadMedia(stream);
                    break;

                case FileCategory.Text:
                    LoadText(stream);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Unsupported file type");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        HasError = true;
                        ErrorMessage = $"File type '{_scopedDataService.OriginalExtension}' is not supported for viewing.";
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: LoadContent error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Stack trace: {ex.StackTrace}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HasError = true;
                ErrorMessage = $"Error loading content: {ex.Message}";
            });
        }
    }

    private void LoadImage(MemoryStream stream)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: LoadImage - stream length: {stream.Length}");
            
            // Create a copy of the stream for the ImageSource
            byte[] data = stream.ToArray();
            
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Image data array created, length: {data.Length}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("ViewerViewModel: Setting ImageSource on main thread");
                    ImageSource = ImageSource.FromStream(() => new MemoryStream(data));
                    System.Diagnostics.Debug.WriteLine("ViewerViewModel: ImageSource set successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Error setting ImageSource: {ex.Message}");
                    HasError = true;
                    ErrorMessage = $"Cannot display image: {ex.Message}";
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: LoadImage error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Stack trace: {ex.StackTrace}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HasError = true;
                ErrorMessage = $"Cannot load image: {ex.Message}";
            });
        }
    }

    private async Task LoadMedia(MemoryStream stream)
    {
        // For MediaElement, we need to save to a temp file
        // because MediaElement doesn't support direct stream binding well
        try
        {
            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: LoadMedia - stream length: {stream.Length}");
            
            string tempDir = Path.Combine(FileSystem.CacheDirectory, "EncryptorTemp");
            Directory.CreateDirectory(tempDir);

            string ext = _scopedDataService.OriginalExtension ?? ".tmp";
            string tempFile = Path.Combine(tempDir, $"temp_{Guid.NewGuid()}{ext}");

            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Writing media to temp file: {tempFile}");

            // Write to temp file
            File.WriteAllBytes(tempFile, stream.ToArray());

            System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Temp file written successfully, size: {new FileInfo(tempFile).Length} bytes");

            await Task.Delay(100); // Small delay to ensure file system is ready
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
#if WINDOWS
                    // On Windows, open the file with the default system media player
                    // This avoids codec issues and provides better user experience
                    System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Opening media file with default Windows player");

                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(tempFile)
                    });

                    System.Diagnostics.Debug.WriteLine("ViewerViewModel: File opened with default player successfully");

                    // Show message to user
                    await Shell.Current.DisplayAlertAsync(
                        "Media Player Opened",
                        "The video/audio file has been opened in your default media player.",
                        "OK");

                    // Navigate back since we're using external player
                    await GoBackAsync();

#else
                    // On other platforms, use in-app MediaElement
                    System.Diagnostics.Debug.WriteLine($"ViewerViewModel: Setting MediaSource to: {tempFile}");
                    MediaSource = tempFile;
#endif

                    System.Diagnostics.Debug.WriteLine("ViewerViewModel: MediaSource set successfully");


                }
                catch (Exception mediaEx)
                {
                    System.Diagnostics.Debug.WriteLine($"MediaElement error: {mediaEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {mediaEx.StackTrace}");
                    HasError = true;
                    ErrorMessage = $"Media playback error: {mediaEx.Message}\n\nYou can save the file and play it with an external player.";
                }
            });

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMedia error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HasError = true;
                ErrorMessage = $"Cannot load media: {ex.Message}\n\nYou can save the file and play it with an external player.";
            });
        }
    }

    private void LoadText(MemoryStream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        string content = reader.ReadToEnd();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TextContent = content;
        });
    }

#endregion

    #region Commands

    /// <summary>
    /// Go back to the main page.
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        // Clean up temp files
        CleanupTempFiles();

        // Clear scoped data
        _scopedDataService.Clear();

        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Save the decrypted file to disk (user picks location).
    /// </summary>
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        try
        {
            var stream = _scopedDataService.DecryptedStream;
            var file = _scopedDataService.CurrentFile;

            if (stream is null || file is null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "No file data to save.", "OK");
                return;
            }

            // Determine default filename
            string defaultFileName;
            string originalPath = file.FilePath;

            if (originalPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            {
                // Remove .enc extension
                defaultFileName = Path.GetFileName(originalPath[..^4]);
            }
            else
            {
                string name = Path.GetFileNameWithoutExtension(originalPath);
                string ext = _scopedDataService.OriginalExtension ?? ".dec";
                defaultFileName = $"{name}_decrypted{ext}";
            }

            // Let user pick save location
            stream.Position = 0;
            var result = await FileSaver.Default.SaveAsync(defaultFileName, stream, CancellationToken.None);

            if (result.IsSuccessful)
            {
                await Shell.Current.DisplayAlertAsync("Saved", $"File saved to:\n{result.FilePath}", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Cancelled", "File save was cancelled.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Cleanup

    private void CleanupTempFiles()
    {
        try
        {
            string tempDir = Path.Combine(FileSystem.CacheDirectory, "EncryptorTemp");
            if (Directory.Exists(tempDir))
            {
                foreach (var file in Directory.GetFiles(tempDir))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch { /* Ignore cleanup errors */ }
    }

    #endregion
}
