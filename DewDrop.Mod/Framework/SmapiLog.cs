using DewDrop.Core.Services;
using StardewModdingAPI;

namespace DewDrop.Framework
{
    internal class SmapiLog : ILog
    {
        private readonly IMonitor _monitor;

        public SmapiLog(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void Trace(string message) => _monitor.Log(message, LogLevel.Trace);
        public void Info(string message) => _monitor.Log(message, LogLevel.Info);
        public void Warn(string message) => _monitor.Log(message, LogLevel.Warn);
        public void Error(string message) => _monitor.Log(message, LogLevel.Error);
    }
}
