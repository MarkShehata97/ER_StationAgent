using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ER_StationAgent
{
    public class Logger
    {
        private static readonly Lazy<Logger> _instance =
            new(() => new Logger());

        public static Logger Instance => _instance.Value;

        private readonly BlockingCollection<string> _queue = new();
        private readonly string _filePath = "app_log.txt";

        public event Action<string>? OnLog;

        private Logger()
        {
            Task.Run(ProcessQueue);
        }

        private async Task ProcessQueue()
        {
            using var writer = new StreamWriter(_filePath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };

            foreach (var msg in _queue.GetConsumingEnumerable())
            {
                await writer.WriteLineAsync(msg);
            }
        }

        public void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";

            _queue.Add(line);

            OnLog?.Invoke(line);
        }
    }
}