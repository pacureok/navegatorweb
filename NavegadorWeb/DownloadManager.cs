using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // Necesario para la serialización JSON
using System.Windows; // Para MessageBox

namespace NavegadorWeb
{
    /// <summary>
    /// Gestiona la carga y guardado de las descargas en un archivo JSON.
    /// </summary>
    public static class DownloadManager
    {
        private static readonly string DownloadsFilePath; // Ruta donde se guardarán las descargas
        private static List<DownloadEntry> _downloads; // Lista en memoria de las descargas

        public static event EventHandler DownloadsUpdated; // Evento para notificar cambios en la lista

        static DownloadManager()
        {
            // Define la ruta del archivo de descargas dentro de la carpeta de datos de la aplicación.
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string browserFolder = Path.Combine(appDataFolder, "MiNavegadorWeb");
            Directory.CreateDirectory(browserFolder); // Asegura que la carpeta exista
            DownloadsFilePath = Path.Combine(browserFolder, "downloads.json");

            _downloads = LoadDownloads(); // Carga las descargas al inicio
        }

        /// <summary>
        /// Carga la lista de descargas desde el archivo JSON.
        /// </summary>
        /// <returns>Una lista de DownloadEntry, ordenada por fecha de inicio descendente.</returns>
        private static List<DownloadEntry> LoadDownloads()
        {
            if (!File.Exists(DownloadsFilePath))
            {
                return new List<DownloadEntry>(); // Retorna una lista vacía si el archivo no existe
            }

            try
            {
                string jsonString = File.ReadAllText(DownloadsFilePath);
                // Deserializa la lista de DownloadEntry desde el JSON.
                var downloads = JsonSerializer.Deserialize<List<DownloadEntry>>(jsonString);
                // Asegura que la lista no sea nula y la ordena por fecha descendente.
                return downloads?.OrderByDescending(e => e.StartTime).ToList() ?? new List<DownloadEntry>();
            }
            catch (Exception ex)
            {
                // Manejo de errores si el archivo está corrupto o hay problemas de lectura.
                MessageBox.Show($"Error al cargar las descargas: {ex.Message}", "Error de Descargas", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<DownloadEntry>();
            }
        }

        /// <summary>
        /// Guarda la lista de descargas en el archivo JSON.
        /// </summary>
        private static void SaveDownloads()
        {
            try
            {
                // Serializa la lista de DownloadEntry a formato JSON con opciones de formato.
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(_downloads, options);
                File.WriteAllText(DownloadsFilePath, jsonString);
                DownloadsUpdated?.Invoke(null, EventArgs.Empty); // Notifica a los suscriptores
            }
            catch (Exception ex)
            {
                // Manejo de errores si hay problemas de escritura.
                MessageBox.Show($"Error al guardar las descargas: {ex.Message}", "Error de Descargas", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Obtiene la lista actual de descargas.
        /// </summary>
        public static List<DownloadEntry> GetDownloads()
        {
            return _downloads;
        }

        /// <summary>
        /// Añade una nueva entrada de descarga a la lista y la guarda.
        /// </summary>
        /// <param name="entry">La DownloadEntry a añadir.</param>
        public static void AddOrUpdateDownload(DownloadEntry entry)
        {
            var existingEntry = _downloads.FirstOrDefault(d => d.Id == entry.Id);
            if (existingEntry != null)
            {
                // Actualizar la entrada existente
                existingEntry.FileName = entry.FileName;
                existingEntry.Url = entry.Url;
                existingEntry.TargetPath = entry.TargetPath;
                existingEntry.TotalBytes = entry.TotalBytes;
                existingEntry.ReceivedBytes = entry.ReceivedBytes;
                existingEntry.Progress = entry.Progress;
                existingEntry.State = entry.State;
                existingEntry.EndTime = entry.EndTime;
                existingEntry.IsActive = entry.IsActive;
            }
            else
            {
                // Añadir nueva entrada (asegurarse de que las descargas más recientes estén al principio)
                _downloads.Insert(0, entry);
            }
            SaveDownloads(); // Guardar la lista actualizada
        }

        /// <summary>
        /// Elimina una descarga específica de la lista y la guarda.
        /// </summary>
        /// <param name="downloadToRemove">La DownloadEntry a eliminar.</param>
        public static void RemoveDownload(DownloadEntry downloadToRemove)
        {
            _downloads.RemoveAll(d => d.Id == downloadToRemove.Id);
            SaveDownloads();
        }

        /// <summary>
        /// Borra todas las descargas completadas o interrumpidas.
        /// </summary>
        public static void ClearCompletedDownloads()
        {
            _downloads.RemoveAll(d => d.State == CoreWebView2DownloadState.Completed || d.State == CoreWebView2DownloadState.Interrupted);
            SaveDownloads();
            MessageBox.Show("Descargas completadas/interrumpidas borradas con éxito.", "Limpieza de Descargas", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
