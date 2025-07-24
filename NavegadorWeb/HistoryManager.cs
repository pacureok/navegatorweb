using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // Necesario para la serialización JSON

namespace NavegadorWeb
{
    /// <summary>
    /// Gestiona la carga y guardado del historial de navegación en un archivo JSON.
    /// </summary>
    public static class HistoryManager
    {
        private static readonly string HistoryFilePath; // Ruta donde se guardará el historial

        static HistoryManager()
        {
            // Define la ruta del archivo de historial dentro de la carpeta de datos de la aplicación.
            // Esto asegura que el archivo se guarde en un lugar apropiado y persistente para el usuario.
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string browserFolder = Path.Combine(appDataFolder, "MiNavegadorWeb");
            Directory.CreateDirectory(browserFolder); // Asegura que la carpeta exista
            HistoryFilePath = Path.Combine(browserFolder, "history.json");
        }

        /// <summary>
        /// Carga el historial de navegación desde el archivo JSON.
        /// </summary>
        /// <returns>Una lista de HistoryEntry, ordenada por fecha descendente.</returns>
        public static List<HistoryEntry> LoadHistory()
        {
            if (!File.Exists(HistoryFilePath))
            {
                return new List<HistoryEntry>(); // Retorna una lista vacía si el archivo no existe
            }

            try
            {
                string jsonString = File.ReadAllText(HistoryFilePath);
                // Deserializa la lista de HistoryEntry desde el JSON.
                var history = JsonSerializer.Deserialize<List<HistoryEntry>>(jsonString);
                // Asegura que la lista no sea nula y la ordena por fecha descendente.
                return history?.OrderByDescending(e => e.Timestamp).ToList() ?? new List<HistoryEntry>();
            }
            catch (Exception ex)
            {
                // Manejo de errores si el archivo está corrupto o hay problemas de lectura.
                MessageBox.Show($"Error al cargar el historial: {ex.Message}", "Error de Historial", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<HistoryEntry>();
            }
        }

        /// <summary>
        /// Guarda el historial de navegación en el archivo JSON.
        /// </summary>
        /// <param name="history">La lista de HistoryEntry a guardar.</param>
        public static void SaveHistory(List<HistoryEntry> history)
        {
            try
            {
                // Serializa la lista de HistoryEntry a formato JSON con opciones de formato.
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(history, options);
                File.WriteAllText(HistoryFilePath, jsonString);
            }
            catch (Exception ex)
            {
                // Manejo de errores si hay problemas de escritura.
                MessageBox.Show($"Error al guardar el historial: {ex.Message}", "Error de Historial", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Añade una nueva entrada al historial y lo guarda.
        /// </summary>
        /// <param name="url">La URL de la página visitada.</param>
        /// <param name="title">El título de la página visitada.</param>
        public static void AddHistoryEntry(string url, string title)
        {
            var history = LoadHistory(); // Carga el historial actual
            // Opcional: Evitar duplicados recientes o limitar el tamaño del historial
            // Por simplicidad, solo añadimos al principio y limitamos el tamaño.
            history.Insert(0, new HistoryEntry(url, title)); // Añade la nueva entrada al principio

            // Limitar el historial a un número razonable de entradas (ej. 1000)
            if (history.Count > 1000)
            {
                history = history.Take(1000).ToList();
            }

            SaveHistory(history); // Guarda el historial actualizado
        }

        /// <summary>
        /// Borra todo el historial de navegación.
        /// </summary>
        public static void ClearHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    File.Delete(HistoryFilePath);
                }
                MessageBox.Show("Historial de navegación borrado con éxito.", "Historial Borrado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al borrar el historial: {ex.Message}", "Error de Historial", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
