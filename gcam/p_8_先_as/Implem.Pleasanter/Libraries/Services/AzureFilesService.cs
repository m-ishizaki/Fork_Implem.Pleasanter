using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Implem.Pleasanter.Libraries.Services
{
    /// <summary>
    /// Implementation of Azure Files storage operations
    /// </summary>
    public class AzureFilesService : IAzureFilesService
    {
        private readonly string _connectionString;
        private readonly int _maxRetries;

        public AzureFilesService(string connectionString, int maxRetries = 3)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _maxRetries = maxRetries;
        }

        private ShareClient GetShareClient(string shareName)
        {
            return new ShareClient(_connectionString, shareName);
        }

        private async Task<ShareFileClient> GetFileClientAsync(string shareName, string filePath, bool createDirectory = false)
        {
            var shareClient = GetShareClient(shareName);
            
            // Ensure share exists
            await shareClient.CreateIfNotExistsAsync();

            var parts = filePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            ShareDirectoryClient directoryClient = shareClient.GetRootDirectoryClient();

            // Navigate through directory structure
            for (int i = 0; i < parts.Length - 1; i++)
            {
                directoryClient = directoryClient.GetSubdirectoryClient(parts[i]);
                if (createDirectory)
                {
                    await directoryClient.CreateIfNotExistsAsync();
                }
            }

            return directoryClient.GetFileClient(parts[parts.Length - 1]);
        }

        public async Task<bool> ExistsAsync(string shareName, string filePath)
        {
            try
            {
                var fileClient = await GetFileClientAsync(shareName, filePath);
                return await fileClient.ExistsAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> ReadAsync(string shareName, string filePath)
        {
            var fileClient = await GetFileClientAsync(shareName, filePath);
            var download = await fileClient.DownloadAsync();
            
            using (var streamReader = new StreamReader(download.Value.Content))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public async Task<byte[]> ReadBytesAsync(string shareName, string filePath)
        {
            var fileClient = await GetFileClientAsync(shareName, filePath);
            var download = await fileClient.DownloadAsync();
            
            using (var memoryStream = new MemoryStream())
            {
                await download.Value.Content.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public async Task WriteAsync(string shareName, string filePath, string content, string encoding = "utf-8")
        {
            var bytes = Encoding.GetEncoding(encoding).GetBytes(content);
            await WriteBytesAsync(shareName, filePath, bytes);
        }

        public async Task WriteBytesAsync(string shareName, string filePath, byte[] data)
        {
            await RetryOperationAsync(async () =>
            {
                var fileClient = await GetFileClientAsync(shareName, filePath, createDirectory: true);
                
                using (var stream = new MemoryStream(data))
                {
                    await fileClient.CreateAsync(data.Length);
                    await fileClient.UploadAsync(stream);
                }
            });
        }

        public async Task WriteStreamAsync(string shareName, string filePath, Stream stream)
        {
            await RetryOperationAsync(async () =>
            {
                var fileClient = await GetFileClientAsync(shareName, filePath, createDirectory: true);
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadAsync(stream);
            });
        }

        public async Task DeleteFileAsync(string shareName, string filePath)
        {
            try
            {
                var fileClient = await GetFileClientAsync(shareName, filePath);
                await fileClient.DeleteIfExistsAsync();
            }
            catch (RequestFailedException)
            {
                // File doesn't exist or already deleted
            }
        }

        public async Task DeleteDirectoryAsync(string shareName, string directoryPath, bool recursive = true)
        {
            try
            {
                var shareClient = GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);
                
                if (recursive && await directoryClient.ExistsAsync())
                {
                    await DeleteDirectoryRecursiveAsync(directoryClient);
                }
                
                await directoryClient.DeleteIfExistsAsync();
            }
            catch (RequestFailedException)
            {
                // Directory doesn't exist or already deleted
            }
        }

        private async Task DeleteDirectoryRecursiveAsync(ShareDirectoryClient directoryClient)
        {
            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory)
                {
                    var subDir = directoryClient.GetSubdirectoryClient(item.Name);
                    await DeleteDirectoryRecursiveAsync(subDir);
                    await subDir.DeleteAsync();
                }
                else
                {
                    var fileClient = directoryClient.GetFileClient(item.Name);
                    await fileClient.DeleteAsync();
                }
            }
        }

        public async Task CreateDirectoryAsync(string shareName, string directoryPath)
        {
            var shareClient = GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();
            
            var directoryClient = shareClient.GetDirectoryClient(directoryPath);
            await directoryClient.CreateIfNotExistsAsync();
        }

        public async Task<bool> DirectoryExistsAsync(string shareName, string directoryPath)
        {
            try
            {
                var shareClient = GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryPath);
                return await directoryClient.ExistsAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task CopyFileAsync(string shareName, string sourceFilePath, string destinationFilePath)
        {
            var sourceClient = await GetFileClientAsync(shareName, sourceFilePath);
            var destinationClient = await GetFileClientAsync(shareName, destinationFilePath, createDirectory: true);
            
            // Download source and upload to destination
            var download = await sourceClient.DownloadAsync();
            await destinationClient.CreateAsync(download.Value.ContentLength);
            await destinationClient.UploadAsync(download.Value.Content);
        }

        public async Task<AzureFileInfo> GetFileInfoAsync(string shareName, string filePath)
        {
            var fileClient = await GetFileClientAsync(shareName, filePath);
            var properties = await fileClient.GetPropertiesAsync();
            
            return new AzureFileInfo
            {
                Name = System.IO.Path.GetFileName(filePath),
                Size = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified
            };
        }

        public async Task<string[]> GetFilesAsync(string shareName, string directoryPath)
        {
            var shareClient = GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(directoryPath);
            
            if (!await directoryClient.ExistsAsync())
            {
                return Array.Empty<string>();
            }

            var files = new System.Collections.Generic.List<string>();
            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    files.Add(Path.Combine(directoryPath, item.Name));
                }
            }
            
            return files.ToArray();
        }

        public async Task<string[]> GetDirectoriesAsync(string shareName, string directoryPath)
        {
            var shareClient = GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(directoryPath);
            
            if (!await directoryClient.ExistsAsync())
            {
                return Array.Empty<string>();
            }

            var directories = new System.Collections.Generic.List<string>();
            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory)
                {
                    directories.Add(Path.Combine(directoryPath, item.Name));
                }
            }
            
            return directories.ToArray();
        }

        private async Task RetryOperationAsync(Func<Task> operation)
        {
            var retryCount = 0;
            while (retryCount <= _maxRetries)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (Exception) when (retryCount < _maxRetries)
                {
                    retryCount++;
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 100));
                }
            }
        }
    }
}
