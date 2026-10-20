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

        public void Trace(string message) => Write(message, LogLevel.Trace);
        public void Info(string message) => Write(message, LogLevel.Info);
        public void Warn(string message) => Write(message, LogLevel.Warn);
        public void Error(string message) => Write(message, LogLevel.Error);

        // Also used while the game closes, when SMAPI may already have shut its logging down
        private void Write(string message, LogLevel level)
        {
            try
            {
                _monitor.Log(message, level);
            }
            catch
            {
                // Nowhere left to report it
            }
        }
    }
}
