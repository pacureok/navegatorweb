using System;

// Por favor, verifica que este namespace coincida con la ubicación de tu archivo.
// Por ejemplo, si el archivo está en la carpeta 'Classes', usa 'NavegadorWeb.Classes'.
namespace NavegadorWeb
{
    /// <summary>
    /// Representa una entrada individual en el historial de navegación.
    /// </summary>
    public class HistoryEntry
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public DateTime Timestamp { get; set; } // Cuándo se visitó la página

        public HistoryEntry()
        {
            // Constructor vacío para serialización/deserialización
        }

        public HistoryEntry(string url, string title)
        {
            Url = url;
            Title = title;
            Timestamp = DateTime.Now; // Establece la marca de tiempo al momento de la creación
        }
    }
}
