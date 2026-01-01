namespace Encryptor.Services;

/// <summary>
/// Service interface for AES-256 encryption/decryption operations.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using AES-256-CBC with PKCS7 padding.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <param name="password">The password for key derivation.</param>
    /// <returns>Encrypted data with salt and IV prepended.</returns>
    byte[] Encrypt(byte[] data, string password);

    /// <summary>
    /// Decrypts data that was encrypted using this service.
    /// </summary>
    /// <param name="encryptedData">The encrypted data (salt + IV + ciphertext).</param>
    /// <param name="password">The password used during encryption.</param>
    /// <returns>The decrypted plaintext data.</returns>
    byte[] Decrypt(byte[] encryptedData, string password);

    /// <summary>
    /// Encrypts data asynchronously with progress reporting.
    /// </summary>
    Task<byte[]> EncryptAsync(byte[] data, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data asynchronously with progress reporting.
    /// </summary>
    Task<byte[]> DecryptAsync(byte[] encryptedData, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts a file and optionally overwrites or creates a new .enc file.
    /// </summary>
    /// <returns>Path to the encrypted file</returns>
    Task<string> EncryptFileAsync(string sourcePath, string password, bool overwriteOriginal, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a file and returns the result as a MemoryStream (for in-memory viewing).
    /// </summary>
    Task<MemoryStream> DecryptFileToStreamAsync(string sourcePath, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a file and saves to disk.
    /// </summary>
    /// <param name="sourcePath">Path to the encrypted file.</param>
    /// <param name="password">Password for decryption.</param>
    /// <param name="overwriteOriginal">Whether to delete the encrypted file after decryption.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="customOutputPath">Optional custom path to save the decrypted file.</param>
    Task DecryptFileAsync(string sourcePath, string password, bool overwriteOriginal, IProgress<double>? progress = null, CancellationToken cancellationToken = default, string? customOutputPath = null);
}
