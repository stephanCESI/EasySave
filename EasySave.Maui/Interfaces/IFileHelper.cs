namespace EasySave.Maui.Interfaces
{
    public interface IFileHelper
    {
        void CopyFile(string sourceFile, string destinationFile);
        bool FileExists(string path);
        long GetFileSize(string path);
        DateTime GetLastModified(string path);
    }
}
