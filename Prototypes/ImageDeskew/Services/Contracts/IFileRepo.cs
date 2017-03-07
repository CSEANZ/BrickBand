using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImageDeskew.Contracts
{
    public interface IFileRepo
    {

        Task<bool> Write(string file, byte[] data);
        Task<bool> Write(string file, string text);
        Task<byte[]> ReadBytes(string file);
        Task<string> ReadText(string file);
        Task<string> GetOffsetFile(string basePath, string filePath);
        Task<bool> FileExists(string filePath);
        string GetPathSeparator();
        Task<string> GetOffsetFile(params string[] filePath);
        Task<string> GetOffsetFolder(params string[] filePath);
        Task<int> CopyFolder(string source, string target, List<string> allowedExtensions);
        Task<bool> DirectoryExists(string directory);
        Task<string> GetParentFolder(string path);
        Task<bool> HasFiles(string path);
        Task DeleteFiles(string path);
        Task<List<string>> GetFiles(string path, List<string> extensions = null);
        Task<string> GetLocalStoragePath(string fileName);
        Task<bool> CopyFile(string source, string target);
        Task<Stream> ReadStream(string file);
        Task<string> GetHash(string fileName);
        string GetFileNameComponent(string fullPath);
        Task DeleteDirectory(string path);
        Task DeleteFile(string path);
        Task<bool> FolderExists(string folderPath);
        Task<bool> MoveFile(string source, string target);
        string GetHash(byte[] bytes);
    }
}