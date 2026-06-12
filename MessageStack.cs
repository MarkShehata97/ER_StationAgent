using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace ER_StationAgent
{
    internal class MessageStack
    {
        // In-memory English message list (bound to UI)
        private readonly BindingList<Message> _queue = new();

        // Public accessors for UI binding
        public BindingList<Message> MessageQueue => _queue;

        // Adds a new message to the appropriate language stack
        public void Push(string name, string message, string station, string language, DateTime timestamp)
        {
            // Create message object
            var item = new Message(name, message, station, language, timestamp);

            // Add to list
            _queue.Add(item);
        }

        // Removes item at index 0
        private static void Trim(BindingList<Message> list)
        {
            if (list.Count > 0) 
            {
                list.RemoveAt(0);
            }
        }

        // Removes oldest items when list exceeds MaxSize
        public void Trim()
        {
            Trim(_queue);
        }

        // Clears both language stacks
        public void Clear()
        {
            _queue.Clear();
        }

        // Exports messages as JSON (filtered by station if needed)
        public string ExportJson(string station)
        {
            // Get data based on language
            var data = GetData();

            // Filter by station unless "all"
            var filtered = string.IsNullOrWhiteSpace(station) || station == "all"
                ? data
                : data.Where(x => x.Station == station);

            // Serialize to JSON
            return JsonSerializer.Serialize(filtered.First(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // Exports messages as JSON (filtered by station if needed)
        public string ExportFullJson(string station)
        {
            // Get data based on language
            var data = GetData();

            // Filter by station unless "all"
            var filtered = string.IsNullOrWhiteSpace(station) || station == "all"
                ? data
                : data.Where(x => x.Station == station);

            // Serialize to JSON
            return JsonSerializer.Serialize(filtered, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // Returns correct list based on language
        private IEnumerable<Message> GetData()
        {
            return _queue;
        }

        // Loads message history from JSON files
        public void LoadFromFiles(string filePath)
        {
            // Load English messages if file exists
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);

                if (json == string.Empty) { return; }

                var items = JsonSerializer.Deserialize<List<Message>>(json) ?? new();

                // Keep only last 10 messages
                foreach (var msg in items)
                {
                    _queue.Add(msg);
                }
            }
        }

        // Saves current stack to file per station + language
        public void Save(string station)
        {
            File.WriteAllText(
                $"Messages\\{station}_Messages.txt",
                ExportFullJson(station));
        }

        public bool Remove(string name, string message)
        {
            // Search English stack
            var msgMatch = _queue.FirstOrDefault(x =>
                x.Name == name &&
                x.Text == message);

            if (msgMatch != null)
            {
                _queue.Remove(msgMatch);
                return true;
            }

            return false;
        }
    }

    // Immutable message record stored in memory and disk
    internal record Message(
        string Name,
        string Text,
        string Station,
        string Language,
        DateTime Timestamp
    );
}