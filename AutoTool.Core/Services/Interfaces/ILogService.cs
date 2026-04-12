using System;

namespace AutoTool.Services.Interfaces
{
    public interface ILogService
    {
        void Write(params string[] messages);
        void Write(Exception exception);
    }
}
