using EasySave.Maui.Models;

namespace EasySave.Maui.Interfaces
{
    public interface ILogWriter
    {
        void WriteLog(LogEntry logEntry);
    }
}
