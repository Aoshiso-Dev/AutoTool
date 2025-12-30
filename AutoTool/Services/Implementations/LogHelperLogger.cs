using System;
using AutoTool.Services.Interfaces;
using LogHelper;

namespace AutoTool.Services.Implementations
{
    public class LogHelperLogger : ILogService
    {
        public void Write(params string[] messages)
        {
            Log.Instance.Write(messages);
        }

        public void Write(Exception exception)
        {
            Log.Instance.Write(exception);
        }
    }
}
