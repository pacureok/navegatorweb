using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace NavegadorWeb.Windows.Classes
{
    /// <summary>
    /// Gestiona la carga y guardado del historial de navegación en un archivo JSON.
    /// </summary>
    public static class HistoryManager
    {
        private static readonly string HistoryFilePath;
        private static List<HistoryEntry> _history = new();

        static HistoryManager()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string browserFolder = Path.Combine(appDataFolder, "MiNavegadorWeb");
            Directory.CreateDirectory(browserFolder);
            HistoryFilePath = Path.Combine(browserFolder, "history.json");
            LoadHistory();
        }

        private static void LoadHistory()
        {
            if (!File.Exists(HistoryFilePath))
            {
                _history = new List<HistoryEntry>();
                return;
            }

            try
            {
                string json = File.ReadAllText(HistoryFilePath);
                _history = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el historial: {ex.Message}", "Error", MessageBoxButton.OK, Image.Error);
                _history = new List<HistoryEntry>();
            }
        }

        private static void SaveHistory()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_history, options);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el historial: {ex.Message}", "Error", MessageBoxButton.OK, Image.Error);
            }
        }

        public static List<HistoryEntry> GetHistory()
        {
            return new List<HistoryEntry>(_history);
        }

        public static void AddHistoryEntry(string url, string title)
        {
            _history.Insert(0, new HistoryEntry(url, title));
            
            if (_history.Count > 1000)
            {
                _history = _history.Take(1000).ToList();
            }

            SaveHistory();
        }

        public static void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }
    }
}

