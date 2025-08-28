using System;

namespace LogHelper
{
    public class GlobalLogger
    {
        private static readonly Lazy<Log> _instance = new Lazy<Log>(() => new Log());

        public static Log Instance => _instance.Value;
    }
}