using Implem.Libraries.Utilities;
using Implem.DefinitionAccessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Implem.Pleasanter.Libraries.Services
{
    /// <summary>
    /// Azure Files storage service implementation
    /// </summary>
    public class AzureFilesStorageService : IFileStorageService
    {
        private readonly IAzureFilesService _azureFilesService;
        private readonly string _shareName;

        public AzureFilesStorageService(IAzureFilesService azureFilesService)
        {
            _azureFilesService = azureFilesService ?? throw new ArgumentNullException(nameof(azureFilesService));
            _shareName = Parameters.AzureFilesSettings?.FileShareName ?? "pleasanter-files";
        }

        public string Read(string path)
        {
            var bytes = _azureFilesService.ReadBytesAsync(_shareName, path).GetAwaiter().GetResult();
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            var bytes = _azureFilesService.ReadBytesAsync(_shareName, path).GetAwaiter().GetResult();
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            return encoding.GetString(bytes);
        }

        public byte[] Bytes(string path)
        {
            return _azureFilesService.ReadBytesAsync(_shareName, path).GetAwaiter().GetResult();
        }

        public void Write(string content, string filePath, string encoding = "utf-8", int reTryCount = 100)
        {
            var bytes = Encoding.GetEncoding(encoding).GetBytes(content);
            WriteBytes(bytes, filePath, reTryCount);
        }

        public void WriteAllText(string path, string content, Encoding encoding)
        {
            var bytes = encoding.GetBytes(content);
            WriteBytes(bytes, path);
        }

        public void WriteBytes(byte[] bytes, string filePath, int reTryCount = 100)
        {
            var successful = false;
            var errorCount = 0;
            
            EnsureDirectoryExists(Path.GetDirectoryName(filePath));

            while (!successful)
            {
                try
                {
                    _azureFilesService.WriteBytesAsync(_shareName, filePath, bytes).GetAwaiter().GetResult();
                    successful = true;
                }
                catch
                {
                    errorCount++;
                    System.Threading.Thread.Sleep(1);
                    if (errorCount > reTryCount)
                    {
                        throw;
                    }
                }
            }
        }

        public bool Exists(string path)
        {
            return _azureFilesService.ExistsAsync(_shareName, path).GetAwaiter().GetResult();
        }

        public bool DirectoryExists(string path)
        {
            return _azureFilesService.DirectoryExistsAsync(_shareName, path).GetAwaiter().GetResult();
        }

        public long GetFileSize(string path)
        {
            // Get file size from Azure Files
            var bytes = Bytes(path);
            return bytes?.Length ?? 0;
        }

        public string[] GetFiles(string path)
        {
            return _azureFilesService.GetFilesAsync(_shareName, path).GetAwaiter().GetResult();
        }

        public string[] GetDirectories(string path)
        {
            return _azureFilesService.GetDirectoriesAsync(_shareName, path).GetAwaiter().GetResult();
        }

        public void CreateDirectory(string path)
        {
            _azureFilesService.CreateDirectoryAsync(_shareName, path).GetAwaiter().GetResult();
        }

        public void Move(string sourcePath, string destinationPath)
        {
            // Azure Files doesn't have native move, so we copy and delete
            var bytes = Bytes(sourcePath);
            WriteBytes(bytes, destinationPath);
            DeleteFile(sourcePath);
        }

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            // Azure Files doesn't have native directory move
            // This would require recursive copy and delete
            // For now, throw NotImplementedException as this is a complex operation
            throw new NotImplementedException("MoveDirectory is not yet implemented for Azure Files. Please use copy and delete operations instead.");
        }

        public void Delete(string path)
        {
            DeleteFile(path);
        }

        public void DeleteDirectory(string path, string searchPattern = "", bool recursive = true)
        {
            _azureFilesService.DeleteDirectoryAsync(_shareName, path, recursive).GetAwaiter().GetResult();
        }

        public void DeleteFile(string path)
        {
            if (Exists(path))
            {
                _azureFilesService.DeleteFileAsync(_shareName, path).GetAwaiter().GetResult();
            }
        }

        public void CopyDirectory(
            string sourcePath,
            string destinationPath,
            IEnumerable<string> excludePathCollection,
            System.IO.FileAttributes excludeFileAttributes = System.IO.FileAttributes.Hidden,
            System.IO.FileAttributes excludeDirectoryAttributes = System.IO.FileAttributes.Hidden,
            bool overwrite = true)
        {
            if (excludePathCollection.Any(o => destinationPath.ToLower().EndsWith(o.ToLower())))
            {
                return;
            }

            EnsureDirectoryExists(destinationPath);

            // Note: Azure Files doesn't directly support listing files and directories
            // This is a simplified implementation
            // In a production environment, you would need to implement proper file/directory enumeration
        }

        public string CopyToTemp(string sourcePath, string tempPath)
        {
            var extension = Path.GetExtension(sourcePath);
            var path = Path.Combine(tempPath, Strings.NewGuid() + extension);
            EnsureDirectoryExists(tempPath);
            var bytes = Bytes(sourcePath);
            WriteBytes(bytes, path);
            return path;
        }

        public System.IO.FileInfo WriteToTemp(string content, string path, string encoding = "utf-8")
        {
            var tempPath = Path.Combine(path, Strings.NewGuid() + ".txt");
            Write(content, tempPath, encoding);
            // Return a FileInfo object with the virtual path for Azure Files
            return new System.IO.FileInfo(tempPath);
        }

        public void DeleteTemporaryFiles(string path, int timeElapsed)
        {
            EnsureDirectoryExists(path);
            // Note: This would require implementing file enumeration in Azure Files
            // For now, this is a placeholder
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath) && !_azureFilesService.DirectoryExistsAsync(_shareName, directoryPath).GetAwaiter().GetResult())
            {
                _azureFilesService.CreateDirectoryAsync(_shareName, directoryPath).GetAwaiter().GetResult();
            }
        }
    }
}
