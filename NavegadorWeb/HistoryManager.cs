using NavegadorWeb.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NavegadorWeb.Services
{
    public static class HistoryManager
    {
        private static readonly string _historyFilePath = "history.json";
        private static List<HistoryEntry> _history = new();

        static HistoryManager()
        {
            LoadHistory();
        }

        public static void LoadHistory()
        {
            if (File.Exists(_historyFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_historyFilePath);
                    _history = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
                }
                catch
                {
                    _history = new List<HistoryEntry>();
                }
            }
        }

        public static void SaveHistory()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_history, options);
            File.WriteAllText(_historyFilePath, json);
        }

        public static List<HistoryEntry> GetHistory()
        {
            return _history;
        }

        public static void AddEntry(HistoryEntry entry)
        {
            // Evita duplicados recientes
            if (_history.Any(e => e.Url == entry.Url))
            {
                var existingEntry = _history.FirstOrDefault(e => e.Url == entry.Url);
                if (existingEntry != null)
                {
                    _history.Remove(existingEntry);
                }
            }

            _history.Add(entry);
            SaveHistory();
        }
    }
}
