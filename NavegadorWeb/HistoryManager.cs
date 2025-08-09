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
            // El constructor estático carga el historial cuando la clase es accedida por primera vez.
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
        
        /// <summary>
        /// Devuelve la lista actual del historial de navegación.
        /// </summary>
        public static List<HistoryEntry> GetHistory()
        {
            return _history;
        }

        /// <summary>
        /// Añade una nueva entrada al historial y guarda los cambios.
        /// </summary>
        public static void AddEntry(HistoryEntry entry)
        {
            // Evita duplicados recientes
            var existingEntry = _history.FirstOrDefault(e => e.Url == entry.Url);
            if (existingEntry != null)
            {
                _history.Remove(existingEntry);
            }

            _history.Add(entry);
            SaveHistory();
        }

        /// <summary>
        /// Guarda el historial actual en el archivo JSON.
        /// </summary>
        public static void SaveHistory()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_history, options);
            File.WriteAllText(_historyFilePath, json);
        }

        /// <summary>
        /// Borra todas las entradas del historial y guarda los cambios.
        /// </summary>
        public static void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }
    }
}
