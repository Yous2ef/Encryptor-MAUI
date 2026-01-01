using Android.Content;
using AndroidX.DocumentFile.Provider;
using Encryptor.Models;
using System.Collections.Generic;
using System.Linq;

namespace Encryptor.Platforms.Android
{
    /// <summary>
    /// Android-specific helper for accessing files through Storage Access Framework (SAF).
    /// Required for Android 11+ scoped storage.
    /// </summary>
    public static class AndroidFileHelper
    {
        /// <summary>
        /// Enumerate all files in a folder using Android's DocumentFile API.
        /// This works with SAF URIs on Android 11+.
        /// </summary>
        public static IEnumerable<FileModel> GetFilesFromDocumentTree(Context context, global::Android.Net.Uri treeUri)
        {
            var files = new List<FileModel>();
            var documentFile = DocumentFile.FromTreeUri(context, treeUri);
            
            if (documentFile == null || !documentFile.IsDirectory)
            {
                System.Diagnostics.Debug.WriteLine("AndroidFileHelper: Invalid document tree");
                return files;
            }

            System.Diagnostics.Debug.WriteLine($"AndroidFileHelper: Starting enumeration from URI: {treeUri}");
            EnumerateFiles(context, documentFile, files);
            System.Diagnostics.Debug.WriteLine($"AndroidFileHelper: Found {files.Count} total files");
            
            return files;
        }

        private static void EnumerateFiles(Context context, DocumentFile folder, List<FileModel> files)
        {
            try
            {
                var children = folder.ListFiles();
                if (children == null || children.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidFileHelper: No children in {folder.Name}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"AndroidFileHelper: Found {children.Length} items in {folder.Name}");

                foreach (var child in children)
                {
                    if (child == null) continue;

                    if (child.IsDirectory)
                    {
                        // Recursively enumerate subdirectories
                        EnumerateFiles(context, child, files);
                    }
                    else if (child.IsFile)
                    {
                        // Create FileModel from DocumentFile
                        var fileModel = CreateFileModelFromDocument(context, child);
                        if (fileModel != null)
                        {
                            files.Add(fileModel);
                            System.Diagnostics.Debug.WriteLine($"AndroidFileHelper: Added file: {fileModel.FileName}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidFileHelper error: {ex.Message}");
            }
        }

        private static FileModel? CreateFileModelFromDocument(Context context, DocumentFile document)
        {
            try
            {
                var uri = document.Uri;
                if (uri == null) return null;

                string fileName = document.Name ?? "unknown";
                long fileSize = document.Length();
                string extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();

                // Store the content URI as the file path - we'll handle it specially when reading
                return new FileModel
                {
                    FilePath = uri.ToString(), // Store content:// URI
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
