using Implem.Libraries.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Implem.Pleasanter.Libraries.Services
{
    /// <summary>
    /// Local file storage service implementation using the existing Files utility class
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        public string Read(string path)
        {
            return Files.Read(path);
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            return File.ReadAllText(path, encoding);
        }

        public byte[] Bytes(string path)
        {
            return Files.Bytes(path);
        }

        public void Write(string content, string filePath, string encoding = "utf-8", int reTryCount = 100)
        {
            content.Write(filePath, encoding, reTryCount);
        }

        public void WriteAllText(string path, string content, Encoding encoding)
        {
            File.WriteAllText(path, content, encoding);
        }

        public void WriteBytes(byte[] bytes, string filePath, int reTryCount = 100)
        {
            bytes.Write(filePath, reTryCount);
        }

        public bool Exists(string path)
        {
            return path.Exists();
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public long GetFileSize(string path)
        {
            var info = new FileInfo(path);
            return info.Length;
        }

        public string[] GetFiles(string path)
        {
            return Directory.GetFiles(path);
        }

        public string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            Directory.Move(sourcePath, destinationPath);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public void DeleteDirectory(string path, string searchPattern = "", bool recursive = true)
        {
            Files.DeleteDirectory(path, searchPattern, recursive);
        }

        public void DeleteFile(string path)
        {
            Files.DeleteFile(path);
        }

        public void CopyDirectory(
            string sourcePath,
            string destinationPath,
            IEnumerable<string> excludePathCollection,
            FileAttributes excludeFileAttributes = FileAttributes.Hidden,
            FileAttributes excludeDirectoryAttributes = FileAttributes.Hidden,
            bool overwrite = true)
        {
            Files.CopyDirectory(
                sourcePath,
                destinationPath,
                excludePathCollection,
                excludeFileAttributes,
                excludeDirectoryAttributes,
                overwrite);
        }

        public string CopyToTemp(string sourcePath, string tempPath)
        {
            return Files.CopyToTemp(sourcePath, tempPath);
        }

        public FileInfo WriteToTemp(string content, string path, string encoding = "utf-8")
        {
            return content.WriteToTemp(path, encoding);
        }

        public void DeleteTemporaryFiles(string path, int timeElapsed)
        {
            Files.DeleteTemporaryFiles(path, timeElapsed);
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}
