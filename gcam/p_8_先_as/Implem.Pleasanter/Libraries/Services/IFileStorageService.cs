using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Implem.Pleasanter.Libraries.Services
{
    /// <summary>
    /// Interface for file storage operations supporting both local file system and Azure Files
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Reads the entire content of a file as a string
        /// </summary>
        string Read(string path);

        /// <summary>
        /// Reads the entire content of a file as a string with specified encoding
        /// </summary>
        string ReadAllText(string path, Encoding encoding);

        /// <summary>
        /// Reads the entire content of a file as a byte array
        /// </summary>
        byte[] Bytes(string path);

        /// <summary>
        /// Writes a string to a file
        /// </summary>
        void Write(string content, string filePath, string encoding = "utf-8", int reTryCount = 100);

        /// <summary>
        /// Writes a string to a file with specified encoding
        /// </summary>
        void WriteAllText(string path, string content, Encoding encoding);

        /// <summary>
        /// Writes a byte array to a file (can handle images and other binary data)
        /// </summary>
        void WriteBytes(byte[] bytes, string filePath, int reTryCount = 100);

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Gets the size of a file in bytes
        /// </summary>
        long GetFileSize(string path);

        /// <summary>
        /// Gets all file paths in a directory
        /// </summary>
        string[] GetFiles(string path);

        /// <summary>
        /// Gets all subdirectory paths in a directory
        /// </summary>
        string[] GetDirectories(string path);

        /// <summary>
        /// Creates a directory
        /// </summary>
        void CreateDirectory(string path);

        /// <summary>
        /// Moves a file from source to destination
        /// </summary>
        void Move(string sourcePath, string destinationPath);

        /// <summary>
        /// Moves a directory from source to destination
        /// </summary>
        void MoveDirectory(string sourcePath, string destinationPath);

        /// <summary>
        /// Deletes a file
        /// </summary>
        void Delete(string path);

        /// <summary>
        /// Deletes a directory and optionally its subdirectories
        /// </summary>
        void DeleteDirectory(string path, string searchPattern = "", bool recursive = true);

        /// <summary>
        /// Deletes a file
        /// </summary>
        void DeleteFile(string path);

        /// <summary>
        /// Copies a directory and its contents
        /// </summary>
        void CopyDirectory(
            string sourcePath,
            string destinationPath,
            IEnumerable<string> excludePathCollection,
            System.IO.FileAttributes excludeFileAttributes = System.IO.FileAttributes.Hidden,
            System.IO.FileAttributes excludeDirectoryAttributes = System.IO.FileAttributes.Hidden,
            bool overwrite = true);

        /// <summary>
        /// Copies a file to a temporary location
        /// </summary>
        string CopyToTemp(string sourcePath, string tempPath);

        /// <summary>
        /// Writes a string to a temporary file
        /// </summary>
        System.IO.FileInfo WriteToTemp(string content, string path, string encoding = "utf-8");

        /// <summary>
        /// Deletes temporary files older than specified time
        /// </summary>
        void DeleteTemporaryFiles(string path, int timeElapsed);

        /// <summary>
        /// Creates a directory if it doesn't exist
        /// </summary>
        void EnsureDirectoryExists(string directoryPath);
    }
}
