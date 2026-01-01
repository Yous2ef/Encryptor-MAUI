using Encryptor.Models;

namespace Encryptor.Services;

/// <summary>
/// Singleton service for passing decrypted data between pages.
/// Avoids passing large objects through navigation parameters.
/// </summary>
public class ScopedDataService
{
    private MemoryStream? _decryptedStream;
    private FileModel? _currentFile;
    private string? _originalExtension;

    /// <summary>
    /// The decrypted file data as a MemoryStream.
    /// </summary>
    public MemoryStream? DecryptedStream
    {
        get => _decryptedStream;
        set
        {
            // Dispose previous stream if exists
            _decryptedStream?.Dispose();
            _decryptedStream = value;
        }
    }

    /// <summary>
    /// The current file being viewed.
    /// </summary>
    public FileModel? CurrentFile
    {
        get => _currentFile;
        set => _currentFile = value;
    }

    /// <summary>
    /// Original file extension before encryption (for MIME type detection).
    /// </summary>
    public string? OriginalExtension
    {
        get => _originalExtension;
        set => _originalExtension = value;
    }

    /// <summary>
    /// Sets the data for the viewer page.
    /// </summary>
    /// <param name="stream">The decrypted MemoryStream.</param>
    /// <param name="file">The source file model.</param>
    /// <param name="originalExtension">The original file extension (e.g., ".mp4").</param>
    public void SetViewerData(MemoryStream stream, FileModel file, string originalExtension)
    {
        System.Diagnostics.Debug.WriteLine($"ScopedDataService.SetViewerData: stream length={stream?.Length ?? 0}");
        System.Diagnostics.Debug.WriteLine($"ScopedDataService.SetViewerData: file={file?.FileName}");
        System.Diagnostics.Debug.WriteLine($"ScopedDataService.SetViewerData: extension={originalExtension}");
        
        DecryptedStream = stream;
        CurrentFile = file;
        OriginalExtension = originalExtension;
        
        System.Diagnostics.Debug.WriteLine("ScopedDataService.SetViewerData: Data set successfully");
    }

    /// <summary>
    /// Clears all scoped data. Call after viewer page is done.
    /// </summary>
    public void Clear()
    {
        _decryptedStream?.Dispose();
        _decryptedStream = null;
        _currentFile = null;
        _originalExtension = null;
    }

    /// <summary>
    /// Gets the MIME type based on the original extension.
    /// </summary>
    public string GetMimeType()
    {
        if (string.IsNullOrEmpty(_originalExtension))
            return "application/octet-stream";

        return _originalExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
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
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Gets the file category based on the original extension.
    /// </summary>
    public FileCategory GetFileCategory()
    {
        if (string.IsNullOrEmpty(_originalExtension))
            return FileCategory.Unknown;

        return _originalExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => FileCategory.Image,
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" => FileCategory.Video,
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => FileCategory.Audio,
            ".txt" or ".json" or ".xml" or ".html" or ".htm" or ".css" or ".js" or ".cs" => FileCategory.Text,
            ".pdf" => FileCategory.Document,
            _ => FileCategory.Unknown
        };
    }
}
