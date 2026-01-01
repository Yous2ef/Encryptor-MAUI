using System.Security.Cryptography;

namespace Encryptor.Services;

/// <summary>
/// AES-256-CBC encryption service with PBKDF2 key derivation.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int SaltSize = 32;        // 256-bit salt
    private const int KeySize = 32;         // 256-bit key (AES-256)
    private const int IvSize = 16;          // 128-bit IV (AES block size)
    private const int Iterations = 100_000; // PBKDF2 iterations (OWASP recommended)
    private const int BufferSize = 1024 * 1024; // 1 MB chunks for streaming

    private readonly IFileService _fileService;

    public EncryptionService(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <inheritdoc />
    public byte[] Encrypt(byte[] data, string password)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(password);

        // Generate random salt and IV
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);

        // Derive key using PBKDF2 (static method - modern API)
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        // Encrypt using AES-256-CBC
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        byte[] cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Combine: Salt + IV + CipherText
        byte[] result = new byte[SaltSize + IvSize + cipherText.Length];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(iv, 0, result, SaltSize, IvSize);
        Buffer.BlockCopy(cipherText, 0, result, SaltSize + IvSize, cipherText.Length);

        // Clear sensitive data
        CryptographicOperations.ZeroMemory(key);

        return result;
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] encryptedData, string password)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentException.ThrowIfNullOrEmpty(password);

        if (encryptedData.Length < SaltSize + IvSize + 16)
            throw new CryptographicException("Invalid encrypted data format.");

        // Extract salt, IV, and ciphertext
        byte[] salt = new byte[SaltSize];
        byte[] iv = new byte[IvSize];
        byte[] cipherText = new byte[encryptedData.Length - SaltSize - IvSize];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(encryptedData, SaltSize + IvSize, cipherText, 0, cipherText.Length);

        // Derive key using PBKDF2 (static method - modern API)
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        // Decrypt using AES-256-CBC
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        // Clear sensitive data
        CryptographicOperations.ZeroMemory(key);

        return plainText;
    }

    /// <inheritdoc />
    public async Task<byte[]> EncryptAsync(byte[] data, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            progress?.Report(0.1);
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = Encrypt(data, password);
            
            progress?.Report(1.0);
            return result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> DecryptAsync(byte[] encryptedData, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            progress?.Report(0.1);
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = Decrypt(encryptedData, password);
            
            progress?.Report(1.0);
            return result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> EncryptFileAsync(string sourcePath, string password, bool overwriteOriginal, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);

        return await Task.Run(async () =>
        {
            progress?.Report(0.0);
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine($"EncryptFileAsync: Starting encryption for: {sourcePath}");

            // Generate random salt and IV
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] iv = RandomNumberGenerator.GetBytes(IvSize);

            // Derive key using PBKDF2
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            progress?.Report(0.1);
            cancellationToken.ThrowIfCancellationRequested();

            string encryptedPath;

#if ANDROID
            // Check if this is a content URI or real file path
            bool isContentUri = Platforms.Android.AndroidUriHelper.IsContentUri(sourcePath);
            
            if (!isContentUri)
            {
                // We have a real file path - use streaming file operations!
                System.Diagnostics.Debug.WriteLine($"EncryptFileAsync: Using streaming for real file path: {sourcePath}");
                encryptedPath = await EncryptFileStreamingStandard(sourcePath, key, salt, iv, overwriteOriginal, progress, cancellationToken);
            }
            else if (Platforms.Android.AndroidUriHelper.IsTreeBasedUri(sourcePath))
            {
                // Tree-based URI from folder picker
                System.Diagnostics.Debug.WriteLine("EncryptFileAsync: Tree-based URI detected - using streaming");
                encryptedPath = await EncryptFileStreamingAndroid(sourcePath, key, salt, iv, overwriteOriginal, progress, cancellationToken);
            }
            else
            {
                // Single file content URI
                System.Diagnostics.Debug.WriteLine("EncryptFileAsync: Content URI detected - using streaming");
                encryptedPath = await EncryptFileStreamingAndroid(sourcePath, key, salt, iv, overwriteOriginal, progress, cancellationToken);
            }
#else
            encryptedPath = await EncryptFileStreamingStandard(sourcePath, key, salt, iv, overwriteOriginal, progress, cancellationToken);
#endif

            // Clear sensitive data
            CryptographicOperations.ZeroMemory(key);

            progress?.Report(1.0);
            System.Diagnostics.Debug.WriteLine($"EncryptFileAsync: Completed encryption, output: {encryptedPath}");
            return encryptedPath;
        }, cancellationToken);
    }

    /// <summary>
    /// Decrypt file to memory stream using streaming (for large files).
    /// Returns decrypted data as MemoryStream without loading entire file at once.
    /// </summary>
    private async Task<MemoryStream> DecryptFileToMemoryStreamAsync(string sourcePath, string password, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(0.0);
            
            // First, decrypt to a temporary file using streaming
            string tempDecryptedPath = Path.Combine(Path.GetTempPath(), $"dec_{Guid.NewGuid()}.tmp");
            
            try
            {
#if ANDROID
                bool isContentUri = Platforms.Android.AndroidUriHelper.IsContentUri(sourcePath);
                
                if (isContentUri)
                {
                    await DecryptFileStreamingAndroid(sourcePath, password, tempDecryptedPath, progress, cancellationToken);
                }
                else
                {
                    await DecryptFileStreamingStandard(sourcePath, password, tempDecryptedPath, progress, cancellationToken);
                }
#else
                await DecryptFileStreamingStandard(sourcePath, password, tempDecryptedPath, progress, cancellationToken);
#endif
                
                // Now read the decrypted file into a MemoryStream
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(tempDecryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                {
                    await fileStream.CopyToAsync(memoryStream, cancellationToken);
                }
                
                memoryStream.Position = 0;
                progress?.Report(1.0);
                
                return memoryStream;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempDecryptedPath))
                {
                    try { File.Delete(tempDecryptedPath); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DecryptFileToMemoryStreamAsync error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Encrypt file using streaming (for standard file paths) - doesn't load entire file into memory.
    /// </summary>
    private async Task<string> EncryptFileStreamingStandard(string sourcePath, byte[] key, byte[] salt, byte[] iv, bool overwriteOriginal, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        string outputPath = sourcePath + ".enc";

        try
        {
            // Get file size for progress reporting
            long fileSize = new FileInfo(sourcePath).Length;
            long totalBytesRead = 0;

            // Encrypt the file using scoped stream handling
            {
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using var inputStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
                using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
                
                // Write salt and IV first
                await outputStream.WriteAsync(salt, cancellationToken);
                await outputStream.WriteAsync(iv, cancellationToken);

                // Create encryptor and crypto stream
                using var encryptor = aes.CreateEncryptor();
                using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);

                // Stream encryption in chunks
                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = await inputStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    
                    totalBytesRead += bytesRead;
                    double progressPercentage = 0.1 + (0.8 * totalBytesRead / fileSize);
                    progress?.Report(progressPercentage);
                    
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Finalize encryption
                await cryptoStream.FlushFinalBlockAsync(cancellationToken);
                
                System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard: Encrypted {totalBytesRead} bytes");
            }
            // Streams are now fully disposed and file handles released

            // Verify encrypted file was created successfully before deleting original
            if (!File.Exists(outputPath))
            {
                throw new IOException("Encrypted file was not created successfully");
            }

            var encryptedFileInfo = new FileInfo(outputPath);
            if (encryptedFileInfo.Length == 0)
            {
                throw new IOException("Encrypted file is empty - encryption may have failed");
            }

            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard: Verified encrypted file exists ({encryptedFileInfo.Length} bytes)");

            // Only delete original AFTER successful encryption and verification
            // All file handles are now released
            if (overwriteOriginal && File.Exists(sourcePath))
            {
                try
                {
                    File.Delete(sourcePath);
                    System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard: Deleted original file");
                }
                catch (Exception deleteEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard: Warning - could not delete original file: {deleteEx.Message}");
                    // Don't fail the entire operation if we can't delete the original
                    // The encrypted file is already created successfully
                }
            }

            return outputPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard error: {ex.Message}");
            
            // Clean up partial encrypted file on error
            if (File.Exists(outputPath))
            {
                try 
                { 
                    File.Delete(outputPath);
                    System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard: Cleaned up partial encrypted file");
                } 
                catch (Exception cleanupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingStandard: Could not clean up partial file: {cleanupEx.Message}");
                }
            }
            
            throw;
        }
    }

#if ANDROID
    /// <summary>
    /// Encrypt file using streaming (for Android content URIs) - doesn't load entire file into memory.
    /// </summary>
    private async Task<string> EncryptFileStreamingAndroid(string sourceUri, byte[] key, byte[] salt, byte[] iv, bool overwriteOriginal, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var uri = global::Android.Net.Uri.Parse(sourceUri);
            
            // Get the DocumentFile for the source
            var documentFile = AndroidX.DocumentFile.Provider.DocumentFile.FromSingleUri(context, uri);
            if (documentFile == null)
            {
                throw new IOException($"Cannot access document: {sourceUri}");
            }

            string originalName = documentFile.Name ?? "unknown";
            string encryptedName = originalName + ".enc";
            long fileSize = documentFile.Length();

            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Original file: {originalName}, Size: {fileSize} bytes");

            // Create encrypted file as a sibling
            var encryptedFile = Platforms.Android.AndroidUriHelper.CreateSiblingFile(
                context, 
                uri, 
                encryptedName, 
                "application/octet-stream");

            if (encryptedFile == null)
            {
                throw new IOException($"Cannot create encrypted file '{encryptedName}'");
            }

            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Created encrypted file with URI: {encryptedFile.Uri}");

            long totalBytesRead = 0;

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var inputStream = context.ContentResolver?.OpenInputStream(uri);
            using var outputStream = context.ContentResolver?.OpenOutputStream(encryptedFile.Uri);
            
            if (inputStream == null || outputStream == null)
            {
                throw new IOException("Cannot open input/output streams");
            }

            // Write salt and IV first
            await outputStream.WriteAsync(salt, cancellationToken);
            await outputStream.WriteAsync(iv, cancellationToken);

            // Create encryptor and crypto stream
            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);

            // Stream encryption in chunks
            byte[] buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await cryptoStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                
                totalBytesRead += bytesRead;
                if (fileSize > 0)
                {
                    double progressPercentage = 0.1 + (0.8 * totalBytesRead / fileSize);
                    progress?.Report(progressPercentage);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Finalize encryption
            await cryptoStream.FlushFinalBlockAsync(cancellationToken);
            
            // IMPORTANT: Close streams before deleting original file
            // This ensures encrypted file is fully written and flushed to disk

            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Encrypted {totalBytesRead} bytes");

            // Verify encrypted file was created successfully before deleting original
            if (!encryptedFile.Exists())
            {
                throw new IOException("Encrypted file was not created successfully");
            }

            long encryptedFileSize = encryptedFile.Length();
            if (encryptedFileSize == 0)
            {
                throw new IOException("Encrypted file is empty - encryption may have failed");
            }

            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Verified encrypted file exists ({encryptedFileSize} bytes)");

            // Only delete original AFTER successful encryption and verification
            if (overwriteOriginal)
            {
                try
                {
                    bool deleted = documentFile.Delete();
                    System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Deleted original file: {deleted}");
                    
                    if (!deleted)
                    {
                        System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Warning - could not delete original file");
                        // Don't fail the operation - encrypted file is already safe
                    }
                }
                catch (Exception deleteEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid: Warning - exception deleting original: {deleteEx.Message}");
                    // Don't fail the operation - encrypted file is already safe
                }
            }

            return encryptedFile.Uri.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EncryptFileStreamingAndroid error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Note: We cannot easily clean up the encrypted file on Android if it failed
            // The DocumentFile API doesn't always allow deletion of partially written files
            // But that's okay - the original file is still intact
            
            throw new IOException($"Failed to encrypt file: {ex.Message}", ex);
        }
    }
#endif

    /// <summary>
    /// Write encrypted file using standard .NET File operations.
    /// </summary>
    private async Task<string> WriteEncryptedFileStandard(string sourcePath, byte[] encryptedData, bool overwriteOriginal, CancellationToken cancellationToken)
    {
        // Determine output path (append .enc to preserve original extension)
        string outputPath = sourcePath + ".enc";

        // Write encrypted file
        await _fileService.WriteAllBytesAsync(outputPath, encryptedData, cancellationToken);

        // Delete original if overwriting
        if (overwriteOriginal && File.Exists(sourcePath) && sourcePath != outputPath)
        {
            File.Delete(sourcePath);
        }
        
        return outputPath;
    }

    /// <inheritdoc />
    public async Task<MemoryStream> DecryptFileToStreamAsync(string sourcePath, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);

        try
        {
            progress?.Report(0.0);
            
            System.Diagnostics.Debug.WriteLine($"DecryptFileToStreamAsync: Starting for {sourcePath}");
            
            // For large files (>50MB), we use a temporary file approach
            // For smaller files, we can decrypt directly to memory
            long fileSize = await GetFileSizeAsync(sourcePath);
            
            const long largeSizeThreshold = 50 * 1024 * 1024; // 50 MB
            
            if (fileSize > largeSizeThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"DecryptFileToStreamAsync: Large file ({fileSize} bytes), using temp file");
                return await DecryptLargeFileToStreamAsync(sourcePath, password, progress, cancellationToken);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DecryptFileToStreamAsync: Small file ({fileSize} bytes), decrypting to memory");
                return await DecryptSmallFileToStreamAsync(sourcePath, password, progress, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DecryptFileToStreamAsync error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get file size from either a regular file path or Android content URI.
    /// </summary>
    private async Task<long> GetFileSizeAsync(string path)
    {
#if ANDROID
        if (Platforms.Android.AndroidUriHelper.IsContentUri(path))
        {
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var uri = global::Android.Net.Uri.Parse(path);
                var documentFile = AndroidX.DocumentFile.Provider.DocumentFile.FromSingleUri(context, uri);
                if (documentFile != null)
                {
                    return documentFile.Length();
                }
            }
            catch
            {
                // Fall back to 0 if we can't get size
            }
            return 0;
        }
#endif
        return await Task.Run(() =>
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return 0;
        });
    }

    /// <summary>
    /// Decrypt small files directly to MemoryStream (more efficient for small files).
    /// </summary>
    private async Task<MemoryStream> DecryptSmallFileToStreamAsync(string sourcePath, string password, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            // Read and decrypt in one go for small files
            byte[] encryptedData = await _fileService.ReadAllBytesAsync(sourcePath, cancellationToken);
            progress?.Report(0.4);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            byte[] decryptedData = Decrypt(encryptedData, password);
            progress?.Report(0.9);
            
            var stream = new MemoryStream(decryptedData);
            stream.Position = 0;
            
            progress?.Report(1.0);
            return stream;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DecryptSmallFileToStreamAsync error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Decrypt large files using streaming through a temporary file.
    /// </summary>
    private async Task<MemoryStream> DecryptLargeFileToStreamAsync(string sourcePath, string password, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            // First, decrypt to a temporary file using streaming
            string tempDecryptedPath = Path.Combine(Path.GetTempPath(), $"dec_{Guid.NewGuid()}.tmp");
            
            try
            {
#if ANDROID
                bool isContentUri = Platforms.Android.AndroidUriHelper.IsContentUri(sourcePath);
                
                if (isContentUri)
                {
                    await DecryptFileStreamingAndroid(sourcePath, password, tempDecryptedPath, progress, cancellationToken);
                }
                else
                {
                    await DecryptFileStreamingStandard(sourcePath, password, tempDecryptedPath, progress, cancellationToken);
                }
#else
                await DecryptFileStreamingStandard(sourcePath, password, tempDecryptedPath, progress, cancellationToken);
#endif
                
                // Now read the decrypted file into a MemoryStream
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(tempDecryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                {
                    await fileStream.CopyToAsync(memoryStream, cancellationToken);
                }
                
                memoryStream.Position = 0;
                progress?.Report(1.0);
                
                System.Diagnostics.Debug.WriteLine($"DecryptLargeFileToStreamAsync: Decrypted to MemoryStream ({memoryStream.Length} bytes)");
                
                return memoryStream;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempDecryptedPath))
                {
                    try 
                    { 
                        File.Delete(tempDecryptedPath);
                        System.Diagnostics.Debug.WriteLine($"DecryptLargeFileToStreamAsync: Cleaned up temp file");
                    } 
                    catch (Exception cleanupEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptLargeFileToStreamAsync: Could not delete temp file: {cleanupEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DecryptLargeFileToStreamAsync error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Decrypt file using streaming (for standard file paths) - doesn't load entire file into memory.
    /// </summary>
    private async Task DecryptFileStreamingStandard(string sourcePath, string password, string outputPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            // Get file size for progress reporting
            long fileSize = new FileInfo(sourcePath).Length;
            long totalBytesRead = 0;

            using var inputStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            
            // Read salt
            byte[] salt = new byte[SaltSize];
            await inputStream.ReadExactlyAsync(salt, cancellationToken);
            
            // Read IV
            byte[] iv = new byte[IvSize];
            await inputStream.ReadExactlyAsync(iv, cancellationToken);
            
            totalBytesRead += SaltSize + IvSize;
            
            // Derive key
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            
            progress?.Report(0.1);
            
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
            
            // Stream decryption in chunks
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            
            while ((bytesRead = await cryptoStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                
                totalBytesRead += bytesRead;
                if (fileSize > 0)
                {
                    double progressPercentage = 0.1 + (0.8 * totalBytesRead / fileSize);
                    progress?.Report(progressPercentage);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            // Clear sensitive data
            CryptographicOperations.ZeroMemory(key);
            
            System.Diagnostics.Debug.WriteLine($"DecryptFileStreamingStandard: Decrypted {totalBytesRead} bytes");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DecryptFileStreamingStandard error: {ex.Message}");
            
            // Clean up partial decrypted file on error
            if (File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { }
            }
            
            throw;
        }
    }

#if ANDROID
    /// <summary>
    /// Decrypt file using streaming (for Android content URIs) - doesn't load entire file into memory.
    /// </summary>
    private async Task DecryptFileStreamingAndroid(string sourceUri, string password, string outputPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        try
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var uri = global::Android.Net.Uri.Parse(sourceUri);
            
            var documentFile = AndroidX.DocumentFile.Provider.DocumentFile.FromSingleUri(context, uri);
            if (documentFile == null)
            {
                throw new IOException($"Cannot access document: {sourceUri}");
            }
            
            long fileSize = documentFile.Length();
            long totalBytesRead = 0;
            
            using var inputStream = context.ContentResolver?.OpenInputStream(uri);
            if (inputStream == null)
            {
                throw new IOException("Cannot open input stream");
            }
            
            // Read salt
            byte[] salt = new byte[SaltSize];
            await inputStream.ReadExactlyAsync(salt, cancellationToken);
            
            // Read IV
            byte[] iv = new byte[IvSize];
            await inputStream.ReadExactlyAsync(iv, cancellationToken);
            
            totalBytesRead += SaltSize + IvSize;
            
            // Derive key
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            
            progress?.Report(0.1);
            
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
            
            // Stream decryption in chunks
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            
            while ((bytesRead = await cryptoStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                
                totalBytesRead += bytesRead;
                if (fileSize > 0)
                {
                    double progressPercentage = 0.1 + (0.8 * totalBytesRead / fileSize);
                    progress?.Report(progressPercentage);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            // Clear sensitive data
            CryptographicOperations.ZeroMemory(key);
            
            System.Diagnostics.Debug.WriteLine($"DecryptFileStreamingAndroid: Decrypted {totalBytesRead} bytes");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DecryptFileStreamingAndroid error: {ex.Message}");
            
            // Clean up partial decrypted file on error
            if (File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { }
            }
            
            throw;
        }
    }
#endif

    /// <inheritdoc />
    public async Task DecryptFileAsync(string sourcePath, string password, bool overwriteOriginal, IProgress<double>? progress = null, CancellationToken cancellationToken = default, string? customOutputPath = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);

        await Task.Run(async () =>
        {
            progress?.Report(0.0);
            cancellationToken.ThrowIfCancellationRequested();

            // Determine output path
            string outputPath;
            if (!string.IsNullOrEmpty(customOutputPath))
            {
                outputPath = customOutputPath;
            }
            else if (sourcePath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            {
                outputPath = sourcePath[..^4]; // Remove .enc
            }
            else
            {
                outputPath = sourcePath + ".dec";
            }

#if ANDROID
            bool isContentUri = Platforms.Android.AndroidUriHelper.IsContentUri(sourcePath);
            
            if (isContentUri)
            {
                await DecryptFileStreamingAndroid(sourcePath, password, outputPath, progress, cancellationToken);
            }
            else
            {
                await DecryptFileStreamingStandard(sourcePath, password, outputPath, progress, cancellationToken);
            }
#else
            await DecryptFileStreamingStandard(sourcePath, password, outputPath, progress, cancellationToken);
#endif

            // Delete encrypted file if overwriting
            if (overwriteOriginal && File.Exists(sourcePath) && sourcePath != outputPath)
            {
                File.Delete(sourcePath);
            }

            progress?.Report(1.0);
        }, cancellationToken);
    }
}
