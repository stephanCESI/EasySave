using EasySave.Core.Models;

namespace EasySave.Logging
{
    public interface ILogWriter
    {
        void WriteLog(LogEntry logEntry);
    }
}
