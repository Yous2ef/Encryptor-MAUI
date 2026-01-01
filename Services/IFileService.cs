using Encryptor.Models;

namespace Encryptor.Services;

/// <summary>
/// Service interface for file picking and folder operations.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Pick a single file.
    /// </summary>
    Task<FileModel?> PickFileAsync();

    /// <summary>
    /// Pick multiple files.
    /// </summary>
    Task<IEnumerable<FileModel>> PickMultipleFilesAsync();

    /// <summary>
    /// Pick a folder and get all files recursively.
    /// </summary>
    Task<IEnumerable<FileModel>> PickFolderAsync();

    /// <summary>
    /// Check and request storage permissions (Android).
    /// </summary>
    Task<bool> CheckAndRequestPermissionsAsync();

    /// <summary>
    /// Get all files from a directory recursively.
    /// </summary>
    IEnumerable<FileModel> GetFilesRecursive(string folderPath);

    /// <summary>
    /// Check if a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Read all bytes from a file.
    /// </summary>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write all bytes to a file.
    /// </summary>
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default);
}
