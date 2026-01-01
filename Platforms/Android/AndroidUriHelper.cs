using Android.Content;
using AndroidX.DocumentFile.Provider;
using Uri = Android.Net.Uri;

namespace Encryptor.Platforms.Android
{
    /// <summary>
    /// Helper for working with Android Storage Access Framework URIs.
    /// </summary>
    public static class AndroidUriHelper
    {
        /// <summary>
        /// Try to get the real file path from a content URI.
        /// This works when the app has MANAGE_EXTERNAL_STORAGE permission.
        /// </summary>
        public static string? GetRealPathFromUri(Context context, Uri uri)
        {
            try
            {
                string uriString = uri.ToString();
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetRealPathFromUri: Processing {uriString}");

                // If we have MANAGE_EXTERNAL_STORAGE permission, try to get real path
                if (!StoragePermissionHelper.HasAllFilesAccess())
                {
                    System.Diagnostics.Debug.WriteLine("AndroidUriHelper.GetRealPathFromUri: No all files access");
                    return null;
                }

                // Get the document file
                var documentFile = DocumentFile.FromSingleUri(context, uri);
                if (documentFile == null)
                {
                    System.Diagnostics.Debug.WriteLine("AndroidUriHelper.GetRealPathFromUri: Cannot create DocumentFile");
                    return null;
                }

                // Try to extract path from document ID
                var docId = global::Android.Provider.DocumentsContract.GetDocumentId(uri);
                if (string.IsNullOrEmpty(docId))
                {
                    System.Diagnostics.Debug.WriteLine("AndroidUriHelper.GetRealPathFromUri: No document ID");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetRealPathFromUri: Document ID: {docId}");

                // Decode the document ID
                var decodedDocId = Uri.Decode(docId);
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetRealPathFromUri: Decoded: {decodedDocId}");

                // Extract real path from document ID
                // Common formats:
                // primary:Download/file.txt -> /storage/emulated/0/Download/file.txt
                // raw:/storage/emulated/0/Download/file.txt -> /storage/emulated/0/Download/file.txt

                string? realPath = null;

                if (decodedDocId.StartsWith("raw:"))
                {
                    realPath = decodedDocId.Substring(4);
                }
                else if (decodedDocId.StartsWith("primary:"))
                {
                    var pathPart = decodedDocId.Substring(8);
                    realPath = $"/storage/emulated/0/{pathPart}";
                }
                else if (decodedDocId.Contains(":"))
                {
                    // Try to parse other formats
                    var colonIndex = decodedDocId.IndexOf(':');
                    var pathPart = decodedDocId.Substring(colonIndex + 1);
                    
                    // Try common storage paths
                    var testPath = $"/storage/emulated/0/{pathPart}";
                    if (System.IO.File.Exists(testPath))
                    {
                        realPath = testPath;
                    }
                }

                if (!string.IsNullOrEmpty(realPath) && System.IO.File.Exists(realPath))
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetRealPathFromUri: Real path: {realPath}");
                    return realPath;
                }

                System.Diagnostics.Debug.WriteLine("AndroidUriHelper.GetRealPathFromUri: Could not determine real path");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetRealPathFromUri error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get the parent directory DocumentFile from a document URI.
        /// </summary>
        public static DocumentFile? GetParentDirectory(Context context, Uri documentUri)
        {
            try
            {
                string uriString = documentUri.ToString();
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Getting parent for URI: {uriString}");

                // Check if this is a tree-based document URI
                if (!uriString.Contains("/tree/") || !uriString.Contains("/document/"))
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Not a tree-based document URI");
                    return null;
                }

                // Extract tree ID from the URI
                var treeStart = uriString.IndexOf("/tree/") + 6;
                var documentStart = uriString.IndexOf("/document/");

                if (treeStart <= 6 || documentStart <= treeStart)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Invalid URI structure");
                    return null;
                }

                var treeId = uriString.Substring(treeStart, documentStart - treeStart);

                // Extract document ID (the file's document ID)
                var docIdStart = documentStart + 10;
                var fullDocId = uriString.Substring(docIdStart);

                // Get parent document ID (remove the last part after the last "/")
                var lastSlash = fullDocId.LastIndexOf('/');
                string parentDocId;

                if (lastSlash > 0)
                {
                    parentDocId = fullDocId.Substring(0, lastSlash);
                }
                else
                {
                    // File is in root of tree, use tree ID as parent
                    parentDocId = treeId;
                }

                // Build tree URI for parent
                var authority = documentUri.Authority;
                var parentTreeUriString = $"content://{authority}/tree/{treeId}/document/{parentDocId}";
                var parentTreeUri = Uri.Parse(parentTreeUriString);

                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Parent tree URI: {parentTreeUriString}");

                var parentDir = DocumentFile.FromTreeUri(context, parentTreeUri);
                if (parentDir != null && parentDir.IsDirectory)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Successfully created parent DocumentFile");
                    return parentDir;
                }

                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Parent is not a valid directory");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Try to extract parent tree URI from a single file document URI.
        /// </summary>
        public static Uri? GetParentTreeUri(Uri fileUri)
        {
            try
            {
                string uriString = fileUri.ToString();
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Processing {uriString}");
                
                if (!uriString.Contains("/document/"))
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Not a document URI");
                    return null;
                }
                
                var authority = fileUri.Authority;
                var docStart = uriString.IndexOf("/document/") + 10;
                var docId = uriString.Substring(docStart);
                
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Authority: {authority}, DocId: {docId}");
                
                // Decode the document ID to find the path
                var decodedDocId = Uri.Decode(docId);
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Decoded DocId: {decodedDocId}");
                
                // Try to extract parent path
                string? parentDocId = null;
                
                // Check for path separators
                if (decodedDocId.Contains("/"))
                {
                    var lastSlash = decodedDocId.LastIndexOf('/');
                    if (lastSlash > 0)
                    {
                        parentDocId = decodedDocId.Substring(0, lastSlash);
                    }
                }
                else if (decodedDocId.Contains(":"))
                {
                    var colonIndex = decodedDocId.IndexOf(':');
                    var pathPart = decodedDocId.Substring(colonIndex + 1);
                    
                    if (pathPart.Contains("/"))
                    {
                        var lastSlash = pathPart.LastIndexOf('/');
                        if (lastSlash > 0)
                        {
                            var parentPath = pathPart.Substring(0, lastSlash);
                            parentDocId = decodedDocId.Substring(0, colonIndex + 1) + parentPath;
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(parentDocId))
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Could not extract parent path");
                    return null;
                }
                
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Parent DocId: {parentDocId}");
                
                var encodedParentDocId = Uri.Encode(parentDocId);
                var parentTreeUriString = $"content://{authority}/tree/{encodedParentDocId}/document/{encodedParentDocId}";
                
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri: Built parent tree URI: {parentTreeUriString}");
                
                return Uri.Parse(parentTreeUriString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.GetParentTreeUri error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a new file in the same directory as the source document.
        /// </summary>
        public static DocumentFile? CreateSiblingFile(Context context, Uri sourceDocumentUri, string newFileName, string mimeType = "application/octet-stream")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.CreateSiblingFile: URI: {sourceDocumentUri}");
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.CreateSiblingFile: New filename: {newFileName}");
                
                string uriString = sourceDocumentUri.ToString();
                
                // Check if this is a tree-based URI
                if (IsTreeBasedUri(uriString))
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.CreateSiblingFile: Tree-based URI detected");
                    
                    var parentDir = GetParentDirectory(context, sourceDocumentUri);
                    if (parentDir == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Cannot get parent directory from tree URI");
                        return null;
                    }

                    // Check if file already exists and delete it
                    var existingFile = parentDir.FindFile(newFileName);
                    if (existingFile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Deleting existing file: {newFileName}");
                        existingFile.Delete();
                    }

                    // Create new file
                    var newFile = parentDir.CreateFile(mimeType, newFileName);
                    if (newFile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Created new file: {newFileName} with URI: {newFile.Uri}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"AndroidUriHelper: Failed to create file: {newFileName}");
                    }

                    return newFile;
                }
                else
                {
                    // Single file URI - try DocumentsContract.CreateDocument
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.CreateSiblingFile: Single file URI - using DocumentsContract");
                    return TryCreateSiblingUsingProvider(context, sourceDocumentUri, newFileName, mimeType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper CreateSiblingFile error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Try to create a sibling file using the document provider API directly.
        /// </summary>
        private static DocumentFile? TryCreateSiblingUsingProvider(Context context, Uri sourceUri, string newFileName, string mimeType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.TryCreateSiblingUsingProvider: Attempting for {newFileName}");
                
                // Get the parent document ID from the source URI
                var parentTreeUri = GetParentTreeUri(sourceUri);
                if (parentTreeUri == null)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.TryCreateSiblingUsingProvider: No parent tree URI");
                    return null;
                }
                
                // Use DocumentsContract to create the document
                var authority = sourceUri.Authority;
                var parentDocId = Uri.Decode(parentTreeUri.LastPathSegment ?? "");
                
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.TryCreateSiblingUsingProvider: Authority: {authority}, Parent DocId: {parentDocId}");
                
                // Build the parent document URI
                var encodedParentDocId = Uri.Encode(parentDocId);
                var parentDocUri = Uri.Parse($"content://{authority}/document/{encodedParentDocId}");
                
                // Try to create the document using DocumentsContract
                var newDocUri = global::Android.Provider.DocumentsContract.CreateDocument(
                    context.ContentResolver,
                    parentDocUri,
                    mimeType,
                    newFileName);
                
                if (newDocUri != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.TryCreateSiblingUsingProvider: Created document at {newDocUri}");
                    return DocumentFile.FromSingleUri(context, newDocUri);
                }
                
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.TryCreateSiblingUsingProvider: CreateDocument returned null");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidUriHelper.TryCreateSiblingUsingProvider error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if a URI is a tree-based document URI (from folder picker).
        /// </summary>
        public static bool IsTreeBasedUri(string uriString)
        {
            return uriString.Contains("/tree/") && uriString.Contains("/document/");
        }
        
        /// <summary>
        /// Check if a path is a content URI.
        /// </summary>
        public static bool IsContentUri(string path)
        {
            return path.StartsWith("content://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
