using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Encryptor.Models;

/// <summary>
/// Represents a file item in the vault with encryption status and selection state.
/// </summary>
public partial class FileModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Full path to the file on disk.
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// Display name of the file.
    /// </summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// File extension (e.g., .mp4, .txt, .jpg).
    /// </summary>
    [ObservableProperty]
    private string _extension = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [ObservableProperty]
    private long _fileSize;

    /// <summary>
    /// Indicates whether the file is currently encrypted.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Category))]
    private bool _isEncrypted;

    /// <summary>
    /// Indicates whether this item is selected in the UI (for multi-select).
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Indicates whether the file is currently being processed.
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// Decrypted file data stored in memory (null if not decrypted).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDecryptedDataInMemory))]
    [NotifyPropertyChangedFor(nameof(ThumbnailSource))]
    [NotifyPropertyChangedFor(nameof(Category))]
    private byte[]? _decryptedData;

    /// <summary>
    /// Indicates whether this file has been decrypted to memory.
    /// True = encrypted file that was decrypted.
    /// </summary>
    [ObservableProperty]
    private bool _isDecrypted;

    /// <summary>
    /// Original file extension before encryption (for restoring after decryption).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Category))]
    [NotifyPropertyChangedFor(nameof(ThumbnailSource))]
    private string? _originalExtension;

    /// <summary>
    /// Check if decrypted data exists in memory.
    /// </summary>
    public bool HasDecryptedDataInMemory => DecryptedData != null && DecryptedData.Length > 0;

    /// <summary>
    /// Get image source for thumbnail display (from memory if decrypted, otherwise from file).
    /// </summary>
    public ImageSource? ThumbnailSource
    {
        get
        {
            // Determine extension to check
            string extensionToCheck = HasDecryptedDataInMemory && !string.IsNullOrEmpty(OriginalExtension)
                ? OriginalExtension
                : Extension;
            
            // Check if it's a media file
            bool isMediaFile = IsMediaExtension(extensionToCheck);
            
            if (HasDecryptedDataInMemory && isMediaFile)
            {
                // Load from decrypted data in memory
                return ImageSource.FromStream(() => new MemoryStream(DecryptedData!));
            }
            else if (!IsEncrypted && isMediaFile)
            {
                // Load from file on disk (unencrypted)
                return ImageSource.FromFile(FilePath);
            }
            return null;
        }
    }
    
    private static bool IsMediaExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => true,
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" => true,
            _ => false
        };
    }

    /// <summary>
    /// MIME type of the file for viewer detection.
    /// </summary>
    public string MimeType => GetMimeType(Extension);

    /// <summary>
    /// File category for icon selection.
    /// </summary>
    public FileCategory Category
    {
        get
        {
            // Use original extension if file is decrypted
            string extensionToCheck = HasDecryptedDataInMemory && !string.IsNullOrEmpty(OriginalExtension)
                ? OriginalExtension
                : Extension;
            return GetCategory(extensionToCheck);
        }
    }

    /// <summary>
    /// Human-readable file size.
    /// </summary>
    public string FormattedSize => FormatFileSize(FileSize);

    /// <summary>
    /// Creates a FileModel from a file path.
    /// </summary>
    public static FileModel FromPath(string path)
    {
        var fileInfo = new FileInfo(path);
        return new FileModel
        {
            FilePath = path,
            FileName = fileInfo.Name,
            Extension = fileInfo.Extension.ToLowerInvariant(),
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            IsEncrypted = fileInfo.Extension.Equals(".enc", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".mp4" => "video/mp4",
        ".mkv" => "video/x-matroska",
        ".avi" => "video/x-msvideo",
        ".mov" => "video/quicktime",
        ".webm" => "video/webm",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".flac" => "audio/flac",
        ".aac" => "audio/aac",
        ".ogg" => "audio/ogg",
        ".txt" => "text/plain",
        ".json" => "application/json",
        ".xml" => "text/xml",
        ".html" or ".htm" => "text/html",
        ".css" => "text/css",
        ".js" => "text/javascript",
        ".cs" => "text/x-csharp",
        ".pdf" => "application/pdf",
        ".doc" or ".docx" => "application/msword",
        ".xls" or ".xlsx" => "application/vnd.ms-excel",
        ".zip" => "application/zip",
        ".rar" => "application/x-rar-compressed",
        ".enc" => "application/x-encrypted",
        _ => "application/octet-stream"
    };

    private static FileCategory GetCategory(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => FileCategory.Image,
        ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" => FileCategory.Video,
        ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => FileCategory.Audio,
        ".txt" or ".json" or ".xml" or ".html" or ".htm" or ".css" or ".js" or ".cs" => FileCategory.Text,
        ".pdf" => FileCategory.Document,
        ".doc" or ".docx" or ".xls" or ".xlsx" => FileCategory.Document,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => FileCategory.Archive,
        ".enc" => FileCategory.Encrypted,
        _ => FileCategory.Unknown
    };

    /// <summary>
    /// Clear decrypted data from memory (security cleanup).
    /// </summary>
    public void ClearDecryptedData()
    {
        if (DecryptedData != null)
        {
            Array.Clear(DecryptedData, 0, DecryptedData.Length);
            DecryptedData = null;
        }
        IsDecrypted = false;
        OnPropertyChanged(nameof(HasDecryptedDataInMemory));
        OnPropertyChanged(nameof(ThumbnailSource));
    }

    /// <summary>
    /// Dispose pattern for memory cleanup.
    /// </summary>
    public void Dispose()
    {
        ClearDecryptedData();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Categories of files for icon selection.
/// </summary>
public enum FileCategory
{
    Unknown,
    Image,
    Video,
    Audio,
    Text,
    Document,
    Archive,
    Encrypted
}
