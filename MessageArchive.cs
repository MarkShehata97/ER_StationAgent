using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace ER_StationAgent
{
    internal class MessageArchive
    {
        // New Stack Event - Triggers when we have 10 new messages
        public event Action<string>? ArchRefresh;

        private int newCount_en = 0;
        private int newCount_ar = 0;

        // Maximum number of messages stored per language
        private const int MaxSize = 10;

        // In-memory English message list (bound to UI)
        private readonly BindingList<Message> _en = new();

        // In-memory Arabic message list (bound to UI)
        private readonly BindingList<Message> _ar = new();

        // Public accessors for UI binding
        public BindingList<Message> English => _en;
        public BindingList<Message> Arabic => _ar;

        // Adds a new message to the appropriate language stack
        public void Push(string name, string message, string station, string language, DateTime timestamp)
        {
            // Create message object
            var item = new Message(name, message, station, language, timestamp);

            var isAra = IsArabic(language);

            // Select target list based on language
            var target = isAra ? _ar : _en;

            // Add to list
            target.Add(item);

            // Ensure size limit is enforced
            Trim(target);

            if (isAra) 
            {
                newCount_ar++;

                if (newCount_ar >= MaxSize - 1) 
                {
                    newCount_ar = 0;
                    ArchRefresh?.Invoke("ar");
                }
            }
            else 
            {
                newCount_en++;

                if (newCount_en >= MaxSize - 1)
                {
                    newCount_en = 0;
                    ArchRefresh?.Invoke("en");
                }
            }
        }

        // Removes oldest items when list exceeds MaxSize
        private static void Trim(BindingList<Message> list)
        {
            if (list.Count > MaxSize)
            {
                list.RemoveAt(0);
            }
        }

        // Checks if language is Arabic
        private static bool IsArabic(string? lang)
        {
            return lang?.ToLowerInvariant() == "ar";
        }

        // Clears both language stacks
        public void Clear()
        {
            _en.Clear();
            _ar.Clear();
        }

        // Exports messages as JSON (filtered by station if needed)
        public string ExportJson(string language)
        {
            // Get data based on language 
            var data = GetData(language);

            // Serialize to JSON 
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }

        // Returns correct list based on language
        private IEnumerable<Message> GetData(string language) { return language?.ToLowerInvariant() == "ar" ? _ar : _en; }

        // Loads message history from JSON files
        public void LoadFromFiles(string enPath, string arPath)
        {
            // Load English messages if file exists 
            if (File.Exists(enPath))
            {
                var json = File.ReadAllText(enPath); var items = JsonSerializer.Deserialize<List<Message>>(json) ?? new();
                // Keep only last 10 messages 
                foreach (var msg in items.TakeLast(10)) { _en.Add(msg); }
            }
            // Load Arabic messages if file exists 
            if (File.Exists(arPath))
            {
                var json = File.ReadAllText(arPath); var items = JsonSerializer.Deserialize<List<Message>>(json) ?? new();
                // Keep only last 10 messages 
                foreach (var msg in items.TakeLast(10)) { _ar.Add(msg); }
            }
        }

        // Saves current stack to file per station + language
        public void Save(string language)
        {
            File.WriteAllText(
                $"Messages\\{language}Messages.txt",
                ExportJson(language));
        }

        public bool Remove(string name, string message)
        {
            // Search English stack
            var msgMatch = _en.FirstOrDefault(x =>
                x.Name == name &&
                x.Text == message);

            if (msgMatch != null)
            {
                _en.Remove(msgMatch);
                return true;
            }

            // Search Arabic stack
            var arMatch = _ar.FirstOrDefault(x =>
                x.Name == name &&
                x.Text == message);

            if (arMatch != null)
            {
                _ar.Remove(arMatch);
                return true;
            }

            return false;
        }
    }
}