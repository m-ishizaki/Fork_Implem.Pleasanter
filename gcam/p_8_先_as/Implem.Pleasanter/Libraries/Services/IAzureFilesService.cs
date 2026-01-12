using System.IO;
using System.Threading.Tasks;

namespace Implem.Pleasanter.Libraries.Services
{
    /// <summary>
    /// Interface for Azure Files storage operations
    /// </summary>
    public interface IAzureFilesService
    {
        /// <summary>
        /// Check if a file exists in Azure Files
        /// </summary>
        Task<bool> ExistsAsync(string shareName, string filePath);

        /// <summary>
        /// Read all text from a file
        /// </summary>
        Task<string> ReadAsync(string shareName, string filePath);

        /// <summary>
        /// Read file as byte array
        /// </summary>
        Task<byte[]> ReadBytesAsync(string shareName, string filePath);

        /// <summary>
        /// Write text to a file
        /// </summary>
        Task WriteAsync(string shareName, string filePath, string content, string encoding = "utf-8");

        /// <summary>
        /// Write byte array to a file
        /// </summary>
        Task WriteBytesAsync(string shareName, string filePath, byte[] data);

        /// <summary>
        /// Write stream to a file
        /// </summary>
        Task WriteStreamAsync(string shareName, string filePath, Stream stream);

        /// <summary>
        /// Delete a file
        /// </summary>
        Task DeleteFileAsync(string shareName, string filePath);

        /// <summary>
        /// Delete a directory and its contents
        /// </summary>
        Task DeleteDirectoryAsync(string shareName, string directoryPath, bool recursive = true);

        /// <summary>
        /// Create a directory
        /// </summary>
        Task CreateDirectoryAsync(string shareName, string directoryPath);

        /// <summary>
        /// Check if directory exists
        /// </summary>
        Task<bool> DirectoryExistsAsync(string shareName, string directoryPath);

        /// <summary>
        /// Copy a file
        /// </summary>
        Task CopyFileAsync(string shareName, string sourceFilePath, string destinationFilePath);

        /// <summary>
        /// Get file info (last modified time, size, etc.)
        /// </summary>
        Task<AzureFileInfo> GetFileInfoAsync(string shareName, string filePath);

        /// <summary>
        /// Get all file paths in a directory
        /// </summary>
        Task<string[]> GetFilesAsync(string shareName, string directoryPath);

        /// <summary>
        /// Get all subdirectory paths in a directory
        /// </summary>
        Task<string[]> GetDirectoriesAsync(string shareName, string directoryPath);
    }

    /// <summary>
    /// File information from Azure Files
    /// </summary>
    public class AzureFileInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public System.DateTimeOffset? LastModified { get; set; }
    }
}
